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
using System.Management;

namespace Email_Client
{
    public class EmailService
    {
        private readonly UserCredential _credentials;
        private readonly EmailRepository _repository;
        private readonly IMemoryCache _cache;
        private string _emailUser;
        private readonly Dictionary<uint, MimeMessage> _contentCache = new Dictionary<uint, MimeMessage>();
        private ImapClient _imapclient;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private readonly SemaphoreSlim _fetchLock = new(5, 5);
        private readonly string cacheKey = "Cached_Emails";

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
                _cache.Set(cacheKey, emailsDB, TimeSpan.FromMinutes(2));
                return emailsDB;
            }
            */

            List<EmailData>? emails;
            if(_cache.TryGetValue(cacheKey, out List<EmailData>? cacheEmails))
            {
                emails = cacheEmails;
            }
            else
            {
                emails = new List<EmailData>();
            }


            // Load from Server
            var emailsServer = await RetrieveEmailsGmail();

            foreach(var email in emailsServer)
            {
                await _repository.AddEmails(email);
            }

            _cache.Set(cacheKey, emailsServer, TimeSpan.FromMinutes(2));
            emails = emailsServer.OrderByDescending(e => e.Date).ToList();
            return emails;

            //return await _repository.GetEmails();

        }

        async public Task<List<EmailData>> RetrieveEmailsGmail()
        {

            await EnsureUser();

            var client = await CreateConnection();
            
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            /*
            var latestDate = await _repository.GetLatestEmailsDate();
            var searchQuery = latestDate.HasValue
                ? SearchQuery.DeliveredAfter(latestDate.Value)
                : SearchQuery.All;
            */

            /*
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
            */

            int total = inbox.Count;
            int fetchLatest = Math.Max(0, total - 100);

            //var fetchCount = Math.Min(uids.Count, 10);

            //var recentUids = uids.Skip(Math.Max(0, uids.Count - 10)).ToList();

            var messages = await inbox.FetchAsync(fetchLatest, -1, MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate | MessageSummaryItems.UniqueId
                );

            var sortMessages = messages
                .OrderByDescending(m => m.Date)
                .Take(100)
                .ToList();

            var emails = new List<EmailData>();

            foreach (var message in sortMessages)
            {
                emails.Add(new EmailData(
                    message.UniqueId.Id, 
                    message.Envelope.From.Mailboxes.FirstOrDefault()?.Name??("No Sender"),
                    message.Envelope.Subject ?? "(No Subject)",
                    message.Date.DateTime
                    ));
            }

            //await client.DisconnectAsync(true);

            return emails;
        }

        async public Task<MimeMessage> getContent(uint uid)
        {
            if (_contentCache.Count > 100)
            {
                _contentCache.Clear();
            }

            await _fetchLock.WaitAsync();

            try
            {

                if (_contentCache.TryGetValue(uid, out var cached))
                {
                    return cached;
                }

                var client = await CreateConnection();
                var inbox = client.Inbox;

                if(!inbox.IsOpen)
                {
                    await inbox.OpenAsync(FolderAccess.ReadOnly);
                }
        
                var inboxMsg = await inbox.GetMessageAsync(new UniqueId(uid));
                _contentCache[uid] = inboxMsg;
                //await client.DisconnectAsync(true);
                return inboxMsg;
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        public async Task<ImapClient> CreateConnection()
        {

            await _connectionLock.WaitAsync();

            try
            {

                if(_imapclient != null && _imapclient.IsConnected)
                {
                    return _imapclient;
                }

                if (_credentials.Token.IsStale)
                {
                    await _credentials.RefreshTokenAsync(CancellationToken.None);
                }

                var authorizationOAuth = new SaslMechanismOAuthBearer(_emailUser, _credentials.Token.AccessToken);
                _imapclient = new ImapClient();
                _imapclient.Timeout = 10000;
                await _imapclient.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);

                await _imapclient.AuthenticateAsync(authorizationOAuth);

                return _imapclient;
            }

            finally
            {
                _connectionLock.Release();
            }
            
        }

        private async Task EnsureUser()
        {
            if(!string.IsNullOrEmpty(_emailUser))
            {
                return;
            }

            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credentials,
                ApplicationName = "Email Client"
            });

            var user = await service.Users.GetProfile("me").ExecuteAsync();
            _emailUser = user.EmailAddress;
        }

    }
}
