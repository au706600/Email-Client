using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using MailKit;
using MailKit.Search;
using MailKit.Net.Imap;
using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using MailKit.Security;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;

namespace Email_Client
{
    public class EmailService
    {
        private readonly UserCredential _credentials;
        private readonly EmailRepository _repository;
        private readonly IMemoryCache _cache;
        //private readonly string cacheKey = "Cached_Emails";

        public EmailService(UserCredential credentials, EmailRepository repository, IMemoryCache cache)
        {
            _credentials = credentials;
            _repository = repository;
            _cache = cache;
        }

        async public Task<List<EmailData>> GetEmails()
        {
            /*
            // Check cache
            if (_cache.TryGetValue(cacheKey, out List<EmailData> cacheEmails))
            {
                return cacheEmails;
            }

            // Check database

            var emailsDB = await _repository.GetEmails();
            if (emailsDB.Any())
            {
                _cache.Set(cacheKey, emailsDB, TimeSpan.FromMinutes(5));
                return emailsDB;
            }
            */

            // Load from Server
            var emailsServer = await RetrieveEmailsGmail();

            foreach(var emails in emailsServer)
            {
                await _repository.AddEmails(emails);
            }

            //_cache.Set(cacheKey, emailsServer, TimeSpan.FromMinutes(5));

            return await _repository.GetEmails();

        }

        async public Task<List<EmailData>> RetrieveEmailsGmail()
        {
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credentials,
                ApplicationName = "Email Client"
            });

            var user = await service.Users.GetProfile("me").ExecuteAsync();
            string emailUser = user.EmailAddress;

            if(_credentials.Token.IsStale)
            {
                await _credentials.RefreshTokenAsync(CancellationToken.None);
            }

            var authorizationOAuth = new SaslMechanismOAuthBearer(emailUser, _credentials.Token.AccessToken);

            // Server
            using var client = new ImapClient();
            client.Timeout=10000;
            await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(authorizationOAuth);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var latestDate = await _repository.GetLatestEmailsDate();

            /*
            var searchQuery = latestDate.HasValue
                ? SearchQuery.DeliveredAfter(latestDate.Value)
                : SearchQuery.All;
            */

            var searchQuery = SearchQuery.All;
            
            var uids = await inbox.SearchAsync(searchQuery);

            if(!uids.Any())
            {
                return new List<EmailData>();
            }

            var latestUids = uids
                .OrderByDescending(u => u.Id)
                .Take(50)
                .ToList();

            //var fetchCount = Math.Min(uids.Count, 10);

            //var recentUids = uids.Skip(Math.Max(0, uids.Count - 10)).ToList();

            var messages = await inbox.FetchAsync(latestUids, MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate | MessageSummaryItems.UniqueId
                );

            var sortMessages = messages
                .OrderByDescending(m => m.Date)
                .Take(50)
                .ToList();
            var emails = new List<EmailData>();

            foreach (var message in sortMessages)
            {
                emails.Add(new EmailData(
                    (int)message.UniqueId.Id,
                    message.Envelope.From.Mailboxes.FirstOrDefault()?.Name??("No Sender"),
                    message.Envelope.Subject ?? "(No Subject)",
                    message.Date.DateTime
                    ));
            }


            /*

            int inboxMessageCount = inbox.Recent;
            int fetchCount = Math.Min(inboxMessageCount, 10);


            for (int i = 0; i < fetchCount; i++)
            {
                var message = await inbox.GetMessageAsync(i);
                emails.Add(new EmailData(
                    i,
                    message.From.Mailboxes.FirstOrDefault()?.Name ?? ("No Sender"),
                    message.Subject??("No Subject"),
                    message.Date.DateTime
                    ));
            }
            */

            client.Disconnect(true);

            return emails;
        }

    }
}
