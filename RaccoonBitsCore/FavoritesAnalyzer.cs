using Newtonsoft.Json;
using System.Data.SQLite;

namespace RaccoonBitsCore
{
    public class FavoritesAnalyzer
    {
        private readonly Db db;

        private readonly WeightsProfile weightsProfile;

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
                                Console.WriteLine($"Error parsing: {acct}");
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
                        "its", "down", "out", "when", "up", "time", "very", "after", "would", "them",
                        "some", "him", "into", "even", "look", "way", "year", "good", "our", "give",
                        "our", "under", "name", "very", "through", "just", "form", "much", "great",
                        "think", "say", "help", "low", "line", "before", "turn", "cause", "same", "mean",
                        "differ", "move", "right", "boy", "old", "too", "does", "tell", "sentence",
                        "set", "three", "want", "air", "well", "also", "play", "small", "end", "put",
                        "home", "read", "hand", "port", "large", "spell", "add", "even", "land", "here",
                        "must", "big", "high", "such", "follow", "act", "why", "ask", "men", "change",
                        "went", "light", "kind", "off", "need", "house", "picture", "try", "us", "again",
                        "animal", "point", "mother", "world", "near", "build", "self", "earth", "father",
                        "head", "stand", "own", "page", "should", "country", "found", "answer", "school",
                        "grow", "study", "still", "learn", "plant", "cover", "food", "sun", "four", "between",
                        "state", "keep", "eye", "never", "last", "let", "thought", "city", "tree", "cross",
                        "farm", "hard", "start", "might", "story", "saw", "far", "draw", "left", "late",
                        "run", "while", "press", "close", "night", "real", "life", "few", "stop", "open",
                        "seem", "together", "next", "white", "children", "begin", "got", "walk", "example",
                        "ease", "paper", "often", "always", "music", "those", "both", "mark", "book", "letter",
                        "until", "mile", "river", "car", "feet", "care", "second", "group", "carry", "took",
                        "rain", "eat", "room", "friend", "began", "idea", "fish", "mountain", "north", "once",
                        "base", "hear", "horse", "cut", "sure", "watch", "color", "face", "wood", "main", "enough",
                        "plain", "girl", "usual", "young", "ready", "above", "ever", "red", "list", "though",
                        "feel", "talk", "bird", "soon", "body", "dog", "family", "direct", "pose", "leave", "song",
                        "measure", "door", "product", "black", "short", "numeral", "class", "wind", "question",
                        "happen", "complete", "ship", "area", "half", "rock", "order", "fire", "south", "problem",
                        "piece", "told", "knew", "pass", "since", "top", "whole", "king", "space", "heard", "best",
                        "hour", "better", "true", "during", "hundred", "five", "remember", "step", "early", "hold",
                        "west", "ground", "interest", "reach", "fast", "verb", "sing", "listen", "six", "table",
                        "travel", "less", "morning", "ten", "simple", "several", "vowel", "toward", "war", "lay",
                        "against", "pattern", "slow", "center", "love", "person", "money", "serve", "appear",
                        "road", "map", "rain", "rule", "govern", "pull", "cold", "notice", "voice", "unit", "power",
                        "town", "fine", "certain", "fly", "fall", "lead", "cry", "dark", "machine", "note", "wait",
                        "plan", "figure", "star", "box", "noun", "field", "rest", "correct", "able", "pound", "done",
                        "beauty", "drive", "stood", "contain", "front", "teach", "week", "final", "gave", "green",
                        "oh", "quick", "develop", "ocean", "warm", "free", "minute", "strong", "special", "mind",
                        "behind", "clear", "tail", "produce", "fact", "street", "inch", "multiply", "nothing", "course",
                        "stay", "wheel", "full", "force", "blue", "object", "decide", "surface", "deep", "moon", "island",
                        "foot", "yet", "busy", "test", "record", "boat", "common", "gold", "possible", "plane", "stead",
                        "dry", "wonder", "laugh", "thousand", "ago", "ran", "check", "game", "shape", "equate", "hot",
                        "miss", "brought", "heat", "snow", "tire", "bring", "yes", "distant", "fill", "east", "paint",
                        "language", "among"
                    };

                    foreach (var kvp in frequentWords)
                    {
                        var wordScore = MathUtils.Normalize(kvp.Value, 1, numberOfLikes, 1, 100);

                        if (commonWords.Contains(kvp.Key))
                        {
                            wordScore /= 3;
                        }

                        db.InsertOrReplaceWordScore(kvp.Key, wordScore);
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
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}