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
            command.CommandText = @"SELECT Id, Sender, Topic, Date FROM Email";
            using var read = await command.ExecuteReaderAsync();

            while (await read.ReadAsync())
            {
                emails.Add(new EmailData(read.GetInt32(0),
                read.GetString(1),
                read.GetString(2),
                DateTime.Parse(read.GetString(3))
                ));
            }

            // Cache the Emails
            _cache.Set(cacheKey, emails, TimeSpan.FromMinutes(5));

            return emails;
        }

        // Add Emails

        public async Task AddEmails(EmailData email)
        {
            using var connection = new SqliteConnection("Data Source = EmailData.db");
            await connection.OpenAsync();

            using var command = connection.CreateCommand(); 

            command.CommandText = @"INSERT INTO Email(Sender,Topic, Date) VALUES (@sender, @topic, @date)";
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
    }
}
