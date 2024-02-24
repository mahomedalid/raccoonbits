using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace RaccoonBitsCore
{
    public class FavoritesAnalyzer
    {
        private readonly Db db;

        private readonly WeightsProfile weightsProfile;

        public ILogger? Logger { get; set; }

        public FavoritesAnalyzer(Db db, WeightsProfile weightsProfile)
        {
            this.db = db;
            this.weightsProfile = weightsProfile;
        }

        public void Execute()
        {
            Dictionary<string, int> wordCountDictionary = new Dictionary<string, int>();
            Dictionary<string, int> accounts = new Dictionary<string, int>();
            Dictionary<string, int> instances = new Dictionary<string, int>();

            int numberOfLikes = 0;

            try
            {
                using (SQLiteConnection connection = db.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT * FROM likes";
                    using (SQLiteCommand command = new(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            numberOfLikes++;

                            ulong id = ulong.Parse(reader["id"]?.ToString()!);
                            string jsonObject = reader["jsonObject"]?.ToString()!;

                            dynamic item = JsonConvert.DeserializeObject(jsonObject!)!;

                            var itemContent = item?.content?.ToString();
                            var accountNote = (item?.account?.note ?? string.Empty).ToString();
                            var acct = (item?.account?.acct ?? string.Empty).ToString();
                            var mstdAccount = acct;

                            Uri uri = new(item?.url?.ToString());
                            var mstdInstance = uri.Host;

                            //var acctParts = acct.Split('@');

                            //string mstdAccount = acctParts.Length > 0 ? acctParts[0] : string.Empty;
                            //string mstdInstance = acctParts.Length > 1 ? acctParts[1] : string.Empty;

                            if (!string.IsNullOrWhiteSpace(mstdAccount))
                            {
                                accounts.TryGetValue(mstdAccount, out int accountsCount);
                                accounts[mstdAccount] = accountsCount + 1;
                            }
                            else
                            {
                                Logger?.LogError($"Error parsing: {acct}");
                            }

                            if (!string.IsNullOrWhiteSpace(mstdInstance))
                            {
                                instances.TryGetValue(mstdInstance, out int instancesCount);
                                instances[mstdInstance] = instancesCount + 1;
                            }

                            var content = $"{itemContent} {accountNote}";
                            string plainText = StringUtils.RemoveStopwords(StringUtils.StripHtmlTags(content).ToLowerInvariant());

                            foreach (string word in plainText.Split(' '))
                            {
                                string cleanWord = StringUtils.CleanWord(word);

                                if (!string.IsNullOrWhiteSpace(cleanWord))
                                {
                                    wordCountDictionary.TryGetValue(cleanWord, out int count);
                                    wordCountDictionary[cleanWord] = count + 1;
                                }
                            }
                        }
                    }

                    // Console.WriteLine("==== Frequent words");

                    var frequentWords = wordCountDictionary.Where(kv => kv.Value > weightsProfile.MinimumFavoritesWordsCount)
                                                    .OrderByDescending(kv => kv.Value);

                    string[] commonWords = {
                        "the", "and", "you", "that", "for", "are", "with", "have", "this", "from",
                        "not", "but", "can", "what", "when", "one", "more", "use", "word", "they",
                        "about", "how", "which", "time", "will", "make", "like", "know", "get", "go",
                        "see", "as", "way", "day", "could", "through", "our", "come", "back", "also",
                        "many", "even", "only", "new", "these", "first", "other", "two", "may", "should",
                        "over", "most", "people", "into", "year", "just", "some", "now", "than", "then",
                        "its", "down", "out", "when", "up", "time", "very", "after", "would", "them", "some",
                        "because"
                    };

                    foreach (var kvp in frequentWords)
                    {
                        var wordScore = MathUtils.Normalize(kvp.Value, 1, numberOfLikes, 1, 100);

                        if (commonWords.Contains(kvp.Key))
                        {
                            wordScore /= 4;
                        }

                        db.InsertOrReplaceWordScore(kvp.Key, (int)wordScore);
                    }

                    //  Console.WriteLine("==== Accounts");

                    // foreach (var kvp in accounts.Where(kv => kv.Value > 1).OrderByDescending(kv => kv.Value))
                    // {
                    //     Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    // }

                    //  Console.WriteLine("==== Instances");

                    foreach (var kvp in instances)
                    {
                        db.InsertOrReplaceMstdInstance(kvp.Key, kvp.Value);
                        //  Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"An error occurred: {ex.Message}");
            }
        }
    }
}