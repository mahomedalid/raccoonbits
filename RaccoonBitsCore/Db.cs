using System.Data.SQLite;
using System.Xml.Linq;

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
    }
}