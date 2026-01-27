using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;


namespace Email_Client
{
    public class EmailRepository
    {
        private readonly IMemoryCache _cache;
        private const string cacheKey = "Cached_Emails";
        public EmailRepository (IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task InitializeDB()
        {
            try
            {
                using var connection = new SqliteConnection("Data Source=EmailData.db");

                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS Email (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Uid INTEGER UNIQUE,
                    Sender TEXT,
                    Topic TEXT,
                    Date TEXT
                    )";

                await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        
        // Retrieve emails
        public async Task<List<EmailData>> GetEmails()
        {
            if (_cache.TryGetValue(cacheKey, out List<EmailData> emailList))
            {
                return emailList;
            }

            using var connection = new SqliteConnection("Data Source = EmailData.db");
            await connection.OpenAsync();

            var emails = new List<EmailData>();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT Uid, Sender, Topic, Date FROM Email ORDER BY Date DESC";
            using var read = await command.ExecuteReaderAsync();

            while (await read.ReadAsync())
            {
                emails.Add(new EmailData(
                read.GetInt32(0),
                read.GetString(1),
                read.GetString(2),
                DateTime.Parse(read.GetString(3))
                ));
            }

            // Cache the Emails
            _cache.Set(cacheKey, emails, TimeSpan.FromMinutes(2));

            return emails;
        }

        // Add Emails

        public async Task AddEmails(EmailData email)
        {
            using var connection = new SqliteConnection("Data Source = EmailData.db");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            command.CommandText = @"INSERT OR IGNORE INTO Email(Uid, Sender,Topic, Date) VALUES (@uid, @sender, @topic, @date)";
            command.Parameters.AddWithValue("@uid", email.Uid);
            command.Parameters.AddWithValue("@sender", email.Sender);
            command.Parameters.AddWithValue("@topic", email.Topic);
            command.Parameters.AddWithValue("@date", email.Date.ToString("o"));

            await command.ExecuteNonQueryAsync();

            InvalidateCache();
        }

        // Delete Emails

        public async Task DeleteEmails(int id)
        {
            using var connection = new SqliteConnection("Data Source = EmailData.db");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Email where Id = @id";
            command.Parameters.AddWithValue(@"id", id);

            await command.ExecuteNonQueryAsync();
            InvalidateCache();
        }

        // When inserting, updating or deleting, this method will be called for ensuring that data stays up to date
        public void InvalidateCache()
        {
            _cache.Remove(cacheKey);
        }

        public async Task<DateTime?> GetLatestEmailsDate()
        {
            using var connection = new SqliteConnection("Data Source = EmailData.db");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT MAX(Date) FROM Email";

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return DateTime.Parse(result.ToString());
        }
    }
}
