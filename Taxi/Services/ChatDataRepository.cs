using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Taxi.Helpers.Creational;

namespace Taxi.Services
{
    public class ChatDataRepository
    {
        private string subscriptionPrefix = "subscription";
        string connectionPrefix = "connection";
        private static IDatabase _database;
        private static ConnectionMultiplexer _redis;
        public ChatDataRepository()
        {
            _redis = RedisConnectionFactory.GetConnection();

            _database = _redis.GetDatabase();
        }
        
        public void SetUserIdForConnection(string uid, string connectionId)
        {    
            _database.StringSet(connectionPrefix + uid, connectionId);
        }

        public void RemoveUserIdForConnection(string uid)
        {
            _database.KeyDelete(connectionPrefix + uid);
        }

        public string GetConnectionForUid(string uid)
        {
            return _database.StringGet(connectionPrefix + uid);
        }

        public void AddSubscriptionForUser(string uid, string channelId)
        {
            _database.SetAdd(subscriptionPrefix + uid, channelId);
        }

        public List<string> GetSubscriptionsForUser(string uid)
        {
            var redisValues  = _database.SetMembers(subscriptionPrefix + uid);

            var channels = new List<string>();

            foreach (var red in redisValues)
            {
                channels.Add(red.ToString());
            }
            return channels;
        }
    }
}
