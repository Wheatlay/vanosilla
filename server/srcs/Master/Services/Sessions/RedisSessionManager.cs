using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using WingsAPI.Communication.Sessions.Model;

namespace WingsEmu.Master.Sessions
{
    public class RedisSessionManager : ISessionManager
    {
        private const string SessionPrefix = "session";
        private const string SessionMappingPrefix = "session:mapping";
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(4);

        private readonly IDatabase _db;

        public RedisSessionManager(IConnectionMultiplexer multiplexer) => _db = multiplexer.GetDatabase(0);

        public async Task<bool> Create(Session session)
        {
            bool exists = await _db.KeyExistsAsync(CreateSessionKey(session.Id));
            if (exists)
            {
                return false;
            }

            await Set(session);

            return true;
        }

        public async Task<bool> Update(Session session)
        {
            bool exists = await _db.KeyExistsAsync(CreateSessionKey(session.Id));
            if (!exists)
            {
                return false;
            }

            await Set(session);

            return true;
        }

        public async Task<Session> GetSessionByAccountName(string accountName)
        {
            string sessionKey = await _db.StringGetAsync(CreateAccountNameMappingKey(accountName));
            if (sessionKey is null)
            {
                return default;
            }

            string serializedSession = await _db.StringGetAsync(sessionKey);
            if (serializedSession is null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<Session>(serializedSession);
        }

        public async Task<Session> GetSessionByAccountId(long accountId)
        {
            string sessionKey = await _db.StringGetAsync(CreateAccountIdMappingKey(accountId));
            if (sessionKey is null)
            {
                return default;
            }

            string serializedSession = await _db.StringGetAsync(sessionKey);
            if (serializedSession is null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<Session>(serializedSession);
        }

        public async Task<bool> Pulse(Session session)
        {
            bool exists = await _db.KeyExistsAsync(CreateSessionKey(session.Id));
            if (!exists)
            {
                return false;
            }

            await _db.KeyExpireAsync(CreateSessionKey(session.Id), Ttl);
            await _db.KeyExpireAsync(CreateAccountIdMappingKey(session.AccountId), Ttl);
            await _db.KeyExpireAsync(CreateAccountNameMappingKey(session.AccountName), Ttl);

            return true;
        }

        private async Task Set(Session session)
        {
            await _db.StringSetAsync(CreateSessionKey(session.Id), JsonConvert.SerializeObject(session), Ttl);
            await _db.StringSetAsync(CreateAccountIdMappingKey(session.AccountId), CreateSessionKey(session.Id), Ttl);
            await _db.StringSetAsync(CreateAccountNameMappingKey(session.AccountName), CreateSessionKey(session.Id), Ttl);
        }

        private static string CreateSessionKey(string sessionId) => $"{SessionPrefix}:{sessionId}";
        private static string CreateAccountIdMappingKey(long accountId) => $"{SessionMappingPrefix}:account-id:{accountId}";
        private static string CreateAccountNameMappingKey(string accountName) => $"{SessionMappingPrefix}:account-name:{accountName}";
    }
}