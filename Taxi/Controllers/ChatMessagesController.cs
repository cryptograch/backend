using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Org.BouncyCastle.Asn1;
using StackExchange.Redis;
using Taxi.Entities;
using Taxi.Helpers;
using Taxi.Helpers.Creational;
using Taxi.Models.Admins;
using Taxi.Models.Chat;
using Taxi.Services;

namespace Taxi.Controllers
{
    [Route("api/chat")]
    public class ChatMessagesController : Controller
    {
        private ChatDataRepository _chatRepo;
        private static IDatabase _database;
        private static ConnectionMultiplexer _redis;
        private UserManager<AppUser> _userManager;
        private IUsersRepository _usersRepository;

        public ChatMessagesController(UserManager<AppUser> userManager, IUsersRepository usersRepository)
        {
            _chatRepo = new ChatDataRepository();
            _redis = RedisConnectionFactory.GetConnection();
            _database = _redis.GetDatabase();
            _usersRepository = usersRepository;
            _userManager = userManager;
        }

        [HttpGet("getuserinfo/{id}")]
        public async Task<IActionResult> GetUserInfo(string id)
        {
            var user = _usersRepository.GetUser(id);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = Mapper.Map<UserDto>(user);

            var claims = await _userManager.GetClaimsAsync(user);

            foreach (var c in claims)
            {
                if (c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.Rol)
                    userDto.Roles.Add(c.Value);
                if (c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.CustomerId ||
                    c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId ||
                    c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.AdminId)
                {
                    userDto.Ids[c.Type] = c.Value;
                }
            }

            userDto.ProfilePictureId = user.ProfilePicture?.Id;
            return Ok(userDto);
        }


        [HttpGet("getmessages")]
        [Authorize]
        public IActionResult GetMessagesForChat(string channelId, int from, int to)
        {
            var uid = User.Claims.Single(c => c.Type == Constants.Strings.JwtClaimIdentifiers.Id).Value;
            
            if (string.IsNullOrWhiteSpace(channelId) || !channelId.Contains(uid))
            {
                return NotFound();
            }
            
            var messages = _chatRepo.GetMessagesForChannel(channelId, from, to);

            return Ok(messages);
        }

        [HttpGet("getchannels")]
        [Authorize]
        public async Task<IActionResult> GetChannelsForUser()
        {
            var uid = User.Claims.Single(c => c.Type == Constants.Strings.JwtClaimIdentifiers.Id).Value;

            var channelDtos = getChannelsForUser(uid);  

            return Ok(channelDtos);
        }

        [Authorize]
        [HttpDelete("chat/{channelId}")]
        public async Task<IActionResult> RemoveChannelForUser(string channelId)
        {
            var uid = User.Claims.Single(c => c.Type == Constants.Strings.JwtClaimIdentifiers.Id).Value;

            var channel = _chatRepo.GetSubscriptionsForUser(uid)?.FirstOrDefault(c => c == channelId);
            
            if (channel == null)
            {
                return NotFound();
            }

            _chatRepo.RemoveSubscriptonForUser(uid, channel);

            return Ok(getChannelsForUser(uid));
        }

        [Authorize]
        [HttpPost("chat/read/{channelId}")]
        public async Task<IActionResult> ReadMessagesForChannel(string channelId)
        {
            var uid = User.Claims.Single(c => c.Type == Constants.Strings.JwtClaimIdentifiers.Id).Value;

            
            if (channelId == null)
            {
                return NotFound();
            }
            
            _chatRepo.RemoveFromUnread(uid, channelId);

            return NoContent();
        }

        private List<ChannelDto> getChannelsForUser(string uid)
        {
            var channels = _chatRepo.GetSubscriptionsForUser(uid);

            var channelDtos = new List<ChannelDto>();

            var unread = _chatRepo.GetUnreadForUser(uid);
            
            foreach (var c in channels)
            {
                var uids = _chatRepo.GetUsersForChannel(c);

                var thisunread = unread.FirstOrDefault(u => u.ChannelId == c);
                
                var dto = new ChannelDto()
                {
                    Id = c,
                    Members = new List<ChatUserDto>()
                };

                if (thisunread != null)
                {
                    dto.LastUpdate = thisunread.LastUpDateTime;
                    dto.NumUnread = thisunread.NumberOfUnread;
                }
                //if no unread get last message
                if (dto.LastUpdate == default(DateTime))
                {
                    var lastMessage = _chatRepo.GetMessagesForChannel(c, 0, 1);
                    if (lastMessage.Count > 0)
                        dto.LastUpdate = lastMessage[0].PublicationTime;
                }

                foreach (var id in uids)
                {
                    var identity = _usersRepository.GetUser(id);
                    if (identity != null)
                    {
                        dto.Members.Add(new ChatUserDto()
                        {
                            IdentityId = identity.Id,
                            FirstName = identity.FirstName,
                            LastName = identity.LastName,
                            ProfilePictureId = identity.ProfilePicture?.Id
                        });
                    }
                }
                channelDtos.Add(dto);
            }

            channelDtos.Sort((rhs, other) =>
            {
                if (rhs.LastUpdate > other.LastUpdate)
                {
                    return -1;
                }
                return 1;
            });

            return channelDtos;
        } 
    }
}
