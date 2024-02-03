using Microsoft.Extensions.Logging;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Security.Policy;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace RaccoonBitsCore
{
    public class Db
    {
        private readonly string connectionString;

        private readonly string dbPath;

        public Db(string dbPath)
        {
            this.dbPath = dbPath;
            this.connectionString = $"Data Source={dbPath};Version=3;";

            CreateDb();
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        private void CreateDb()
        {
            if (!File.Exists(dbPath))
            {
                // Create the database file
                SQLiteConnection.CreateFile(dbPath);

                // Open a connection to the database
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(
                        "CREATE TABLE likes (id BIGINT PRIMARY KEY, jsonObject TEXT)",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SQLiteCommand(
                        "CREATE TABLE instances (host string PRIMARY KEY, weight INT)",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SQLiteCommand(
                        "CREATE TABLE posts(uri string PRIMARY KEY, jsonObject TEXT, wordsScore REAL, fameScore REAL, byzzScore REAL, buzzScore REAL, score REAL, boosted INTEGER, hostScore REAL)",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SQLiteCommand(
                        "CREATE TABLE wordsRank(word string PRIMARY KEY, score INT)",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void InsertOrReplaceWordScore(string word, int score)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using var command = new SQLiteCommand(
                    "INSERT OR REPLACE INTO wordsRank (word, score) VALUES (@word, @score)",
                    connection);

                command.Parameters.AddWithValue("@word", word);
                command.Parameters.AddWithValue("@score", score);
                command.ExecuteNonQuery();
            }
        }

        public void InsertOrReplaceLike(ulong id, string content)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "INSERT OR REPLACE INTO likes (id, jsonObject) VALUES (@id, @jsonObject)",
                    connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@jsonObject", content);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void InsertOrReplaceMstdInstance(string host, int weight)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "INSERT OR REPLACE INTO instances (host, weight) VALUES (@host, @weight)",
                    connection))
                {
                    command.Parameters.AddWithValue("@host", host);
                    command.Parameters.AddWithValue("@weight", weight);
                    command.ExecuteNonQuery();
                }
            }
        }

        public IList<string> GetMstdInstances(int weight = 2)
        {
            IList<string> instances = new List<string>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // TODO: add an option to skip the original instance
                string query = $"SELECT * FROM instances WHERE weight > {weight}";
                
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string host = reader["host"]!.ToString()!;
                        instances.Add(host);
                    }
                }
            }

            return instances;
        }

        internal void InsertOrReplacePost(string uri, string content, int wordsScore)
        {
            using(var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "INSERT OR IGNORE INTO posts (uri, jsonObject, wordsScore) VALUES (@uri, @jsonObject, @wordsScore)",
                    connection))
                {
                    command.Parameters.AddWithValue("@uri", uri);
                    command.Parameters.AddWithValue("@jsonObject", content);
                    command.Parameters.AddWithValue("@wordsScore", wordsScore);
                    command.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Post> GetPosts(
            string sql = "SELECT * FROM posts LIMIT 5",
            IDictionary<string, object>? parameters = null,
            IRecordProcessor<Post>? processor = null) {
            IList<Post> posts = new List<Post>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                using (var command = new SQLiteCommand(sql, connection))
                {
                    foreach (var parameter in parameters ?? new Dictionary<string, object>())
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string uri = reader["uri"]?.ToString() ?? string.Empty;
                            string jsonObject = reader["jsonObject"]?.ToString() ?? string.Empty;

                            var post = new Post(uri, jsonObject);

                            if (processor != null)
                            {
                                post = processor.Process(post);
                            }

                            posts.Add(post);
                        }
                    }
                }
            }

            return posts;
        }

        public void UpdateRankedPost(Post post)
        {
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "UPDATE posts SET wordsScore = @wordsScore, hostScore = @hostScore, fameScore = @fameScore, buzzScore = @buzzScore, score = @score WHERE uri = @uri",
                    connection))
                {
                    command.Parameters.AddWithValue("@uri", post.Uri);
                    command.Parameters.AddWithValue("@fameScore", post.FameScore);
                    command.Parameters.AddWithValue("@buzzScore", post.BuzzScore);
                    command.Parameters.AddWithValue("@wordsScore", post.WordsScore);
                    command.Parameters.AddWithValue("@hostScore", post.HostScore);
                    command.Parameters.AddWithValue("@score", post.Score);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Dictionary<string, int> GetHostsRank()
        {
            Dictionary<string, int> hostsScoreDictionary = new Dictionary<string, int>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT host, weight FROM instances";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader?.Read() ?? false)
                    {
                        string host = reader["host"]?.ToString()!;
                        int weight = reader?.GetInt32(1) ?? 0;
                        hostsScoreDictionary[host!] = weight;
                    }
                }
            }

            return hostsScoreDictionary;
        }

        public Dictionary<string, int> GetWordsRank()
        {
            Dictionary<string, int> wordScoreDictionary = new Dictionary<string, int>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT word, score FROM wordsRank WHERE length(word) > 2";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string word = reader["word"]?.ToString()!;
                        int score = reader.GetInt32(1);
                        wordScoreDictionary[word!] = score;
                    }
                }
            }

            return wordScoreDictionary;
        }

        public void MarkPostAsBoosted(Post post)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "UPDATE posts SET boosted = 1 WHERE uri = @uri LIMIT 1",
                    connection))
                {
                    command.Parameters.AddWithValue("@uri", post.Uri!);
                    command.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Word> GetWords(string where)
        {
            IList<Word> rows = new List<Word>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = $"SELECT * FROM wordsRank WHERE {where}";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string word = reader["word"]?.ToString() ?? string.Empty;
                        int score = int.Parse(reader["score"]?.ToString() ?? "0");

                        var item = new Word(word, score);

                        rows.Add(item);
                    }
                }
            }

            return rows;
        }
    }
}