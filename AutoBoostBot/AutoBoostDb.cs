using System.Data.SQLite;

namespace AutoBoostBot
{
    /// <summary>
    /// SQLite database for tracking auto-boost accounts and deduplicating boosted posts.
    /// </summary>
    public class AutoBoostDb
    {
        private readonly string connectionString;

        private readonly string dbPath;

        public AutoBoostDb(string dbPath)
        {
            this.dbPath = dbPath;
            this.connectionString = $"Data Source={dbPath};Version=3;";

            EnsureCreated();
        }

        private SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection(connectionString);
            connection.Open();

            return connection;
        }

        private void EnsureCreated()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using var connection = OpenConnection();

            using (var command = new SQLiteCommand(
                @"CREATE TABLE IF NOT EXISTS accounts (
                    acct TEXT PRIMARY KEY,
                    added_at TEXT NOT NULL
                )", connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand(
                @"CREATE TABLE IF NOT EXISTS boost_log (
                    status_uri TEXT PRIMARY KEY,
                    acct TEXT NOT NULL,
                    boosted_at TEXT NOT NULL
                )", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        public void AddAccount(string acct)
        {
            using var connection = OpenConnection();

            using var command = new SQLiteCommand(
                "INSERT OR IGNORE INTO accounts (acct, added_at) VALUES (@acct, @addedAt)",
                connection);

            command.Parameters.AddWithValue("@acct", acct);
            command.Parameters.AddWithValue("@addedAt", DateTime.UtcNow.ToString("o"));
            command.ExecuteNonQuery();
        }

        public void RemoveAccount(string acct)
        {
            using var connection = OpenConnection();

            using var command = new SQLiteCommand(
                "DELETE FROM accounts WHERE acct = @acct",
                connection);

            command.Parameters.AddWithValue("@acct", acct);
            command.ExecuteNonQuery();
        }

        public IList<string> GetAccounts()
        {
            var accounts = new List<string>();

            using var connection = OpenConnection();

            using var command = new SQLiteCommand("SELECT acct FROM accounts ORDER BY acct", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                accounts.Add(reader["acct"]?.ToString() ?? string.Empty);
            }

            return accounts;
        }

        public bool IsAlreadyBoosted(string statusUri)
        {
            using var connection = OpenConnection();

            using var command = new SQLiteCommand(
                "SELECT COUNT(*) FROM boost_log WHERE status_uri = @statusUri",
                connection);

            command.Parameters.AddWithValue("@statusUri", statusUri);

            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        public void MarkBoosted(string statusUri, string acct)
        {
            using var connection = OpenConnection();

            using var command = new SQLiteCommand(
                "INSERT OR IGNORE INTO boost_log (status_uri, acct, boosted_at) VALUES (@statusUri, @acct, @boostedAt)",
                connection);

            command.Parameters.AddWithValue("@statusUri", statusUri);
            command.Parameters.AddWithValue("@acct", acct);
            command.Parameters.AddWithValue("@boostedAt", DateTime.UtcNow.ToString("o"));
            command.ExecuteNonQuery();
        }

        public int GetBoostedCount()
        {
            using var connection = OpenConnection();

            using var command = new SQLiteCommand("SELECT COUNT(*) FROM boost_log", connection);

            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
