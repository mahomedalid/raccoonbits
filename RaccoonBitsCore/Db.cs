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

    }
}