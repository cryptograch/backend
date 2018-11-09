using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Taxi.Helpers.Creational;
using Taxi.Services;

namespace Taxi.Hubs
{
    [Authorize]
    public class ChatHub:Hub
    {
        private ChatDataRepository _chatRepo;
        private static IDatabase _database;
        private static ConnectionMultiplexer _redis;
        private IHubContext<ChatHub> _hubContext;
        private static ConcurrentDictionary<string, ISubscriber> _subscribers = new ConcurrentDictionary<string, ISubscriber>();

        public ChatHub(IHubContext<ChatHub> hubContext)
        {
            _chatRepo = new ChatDataRepository();
            _redis = RedisConnectionFactory.GetConnection();
            _database = _redis.GetDatabase();
            _hubContext = hubContext;
        }
        public override async Task OnConnectedAsync()
        {
            var uid = Context.User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.Id)?.Value; // todo : check if user exists

            _chatRepo.SetUserIdForConnection(uid, Context.ConnectionId);

            _subscribers.TryAdd(uid, _redis.GetSubscriber());
            
            ConnectAllSubscriptionsForUser(uid);

            await base.OnConnectedAsync();
        }
        
        public void Subscribe(string secondUserId)
        {
            var uid = Context.User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.Id)?.Value;

            List<string> ids = new List<string> {uid, secondUserId};

            var chanalName = GetChannelName(ids);
            
            foreach (var id in ids)
            {
                if (!_chatRepo.GetSubscriptionsForUser(id).Contains(chanalName)) // todo: optimaze
                {
                    _chatRepo.AddSubscriptionForUser(id, chanalName);
                    if (_chatRepo.GetConnectionForUid(id) != null)
                    {
                        ConnectSubscriptionForUser(chanalName, id);
                    }
                }
            }        
        }

        public void Publish(string secondUserId, string message)
        {
            var uid = Context.User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.Id)?.Value;

            List<string> ids = new List<string> { uid, secondUserId };

            string chanalName = GetChannelName(ids);

            var subscriber = _redis.GetSubscriber();

            subscriber.Publish(chanalName, message); //todo : store messages
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var uid = Context.User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.Id)?.Value;

            _chatRepo.RemoveUserIdForConnection(uid);

            DisconnectSubscriptionsForUser(uid);

            _subscribers.TryRemove(uid, out _ );

            return base.OnDisconnectedAsync(exception);
        }

        private string GetChannelName(List<string> ids)
        {
            ids.Sort();

            string chanalName = string.Join('_', ids);

            return chanalName;
        }

        private void ConnectAllSubscriptionsForUser(string uid)
        {
            var channelIds = _chatRepo.GetSubscriptionsForUser(uid);

            foreach (var c in channelIds)
            {
                ConnectSubscriptionForUser(c, uid);
            }
        }

        private void ConnectSubscriptionForUser(string chanalName, string uid)
        {
            _subscribers.TryGetValue(uid, out var subscriber);
            subscriber?.Subscribe(chanalName, (channel, message) =>
            {
                try
                {
                    var connId = _chatRepo.GetConnectionForUid(uid);
                    if (connId != null)
                    {

                        _hubContext.Clients.Client(connId).SendAsync("publication", (string)channel + (string)message);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
        }

        private void DisconnectSubscriptionsForUser(string uid)
        {
            _subscribers.TryGetValue(uid, out var subscriber);
            subscriber?.UnsubscribeAll();
        }
    }
}
