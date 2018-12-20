using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Taxi.Helpers.Creational;
using Taxi.RedisEntities;

namespace Taxi.Services
{
    public class ChatDataRepository
    {
        private string subscriptionPrefix = "subscription";
        private string connectionPrefix = "connection";
        private string channelUsersPrefix = "channelUsers";
        private string unreadPrefix = "unread";
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

        public void RemoveSubscriptonForUser(string uid, string channelId)
        {
            _database.SetRemove(subscriptionPrefix + uid, channelId);
            if (_database.SetContains(subscriptionPrefix + uid, channelId)) //todo: just for testing purposes remove;
            {
                throw new ArgumentException();
            }
        }
        public void WriteMessagesForChannel(string channelId, UserMessage userMessage)
        {
            var json = JsonConvert.SerializeObject(userMessage);

            _database.ListLeftPush(channelId, json);
        }

        public List<UserMessage> GetMessagesForChannel(string channelId, int l, int r)
        {
            var jsonString = _database.ListRange(channelId, l, r).Select(d => d.ToString());
            
            var messages = new List<UserMessage>();

            foreach (var d in jsonString)
            {
                messages.Add( JsonConvert.DeserializeObject<UserMessage>(d));
            }

            return messages;
        }

        public void AddUserForChannel(string channelId,string uid)
        {
            _database.SetAdd(channelUsersPrefix + channelId, uid);
        }

        public List<string> GetUsersForChannel(string channelId)
        {
            var members = _database.SetMembers(channelUsersPrefix + channelId);

            var uids = new List<string>();

            foreach (var m in members)
            {
                uids.Add(m.ToString());
            }

            return uids;
        }

        public void AddtoUnread(string uid, string channel)
        {
            string unreadMessage;

            if (_database.HashExists(unreadPrefix + uid, channel))
            {
                unreadMessage = _database.HashGet(unreadPrefix + uid, channel);

                _database.HashDelete(unreadPrefix + uid, channel);

                var unread = JsonConvert.DeserializeObject<UnreadMessages>(unreadMessage);

                unread.LastUpDateTime = DateTime.UtcNow;

                ++unread.NumberOfUnread;

                _database.HashSet(unreadPrefix + uid, channel, JsonConvert.SerializeObject(unread));
            }
            else
            {
                var newUnread = new UnreadMessages()
                {
                    ChannelId = channel, 
                    LastUpDateTime = DateTime.UtcNow, 
                    NumberOfUnread = 1
                };
                _database.HashSet(unreadPrefix + uid, channel, JsonConvert.SerializeObject(newUnread));
            }
        }

        public void RemoveFromUnread(string uid, string channel)
        {
            if (_database.HashExists(unreadPrefix + uid, channel))
                _database.HashDelete(unreadPrefix + uid, channel);
        }

        public List<UnreadMessages> GetUnreadForUser(string uid)
        {
            var values = _database.HashValues(unreadPrefix + uid);

            var unread = new List<UnreadMessages>();

            foreach (var u in values)
            {
                unread.Add( JsonConvert.DeserializeObject<UnreadMessages>(u.ToString()));
            }

            return unread;
        }
    }
}
