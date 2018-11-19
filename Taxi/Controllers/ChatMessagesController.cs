using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            if (!channelId.Contains(uid))
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

            var channels = _chatRepo.GetSubscriptionsForUser(uid);

            var channelDtos = new List<ChannelDto>();

            foreach (var c in channels)
            {
                var uids = _chatRepo.GetUsersForChannel(c);
                
                var dto =  new ChannelDto()
                {
                    Id = c,
                    Members = new List<ChatUserDto>()
                };
                
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

            return Ok(channelDtos);
        }
        
    }
}
