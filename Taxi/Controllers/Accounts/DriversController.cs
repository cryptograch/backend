using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taxi.Entities;
using Taxi.Helpers;
using Taxi.Models;
using Taxi.Models.Drivers;
using Taxi.Services;

namespace Taxi.Controllers.Accounts
{
    [Route("api/accounts/drivers")]
    public class DriversController : Controller
    {
        private IMapper _mapper;
        private UserManager<AppUser> _userManager;
        private IUsersRepository _usersRepository;
        private IEmailSender _emailSender;
        private IUploadService _uploadService;
        private IResourceUriHelper _resourceUriHelper;


        public DriversController(UserManager<AppUser> userManager, IMapper mapper, IUsersRepository usersRepository, IEmailSender emailSender, IUploadService uploadService, IResourceUriHelper resourceUriHelper)
        {
            _mapper = mapper;
            _userManager = userManager;
            _usersRepository = usersRepository;
            _emailSender = emailSender;
            _uploadService = uploadService;
            _resourceUriHelper = resourceUriHelper;
        }

        [ProducesResponseType(201)]
        [Produces(contentType: "application/json")]
        [HttpPost]
        public async Task<IActionResult> RegisterDriver([FromBody] DriverRegistrationDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userIdentity = _mapper.Map<AppUser>(model);

            var result = await _userManager.CreateAsync(userIdentity, model.Password);

            if (!result.Succeeded)
            {
                if (result.Errors.FirstOrDefault(o => o.Code == "DuplicateUserName") != null)
                    ModelState.AddModelError(nameof(CustomerRegistrationDto), "User name already taken");
                return BadRequest(ModelState);
            }
            var driver = _mapper.Map<Driver>(model);
            driver.IdentityId = userIdentity.Id;

            var addDbres = await _usersRepository.AddDriver(driver);

            var customerFromDriver = _mapper.Map<Customer>(driver);

            var addres = await _usersRepository.AddCustomer(customerFromDriver);

            if (!addres || !addDbres)
                return Conflict();

            var driverDto = _mapper.Map<DriverDto>(model);

            driverDto.Id = driver.Id;

            if (!userIdentity.EmailConfirmed)
            {
                var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(userIdentity);
                var emailConfirmUrl = Url.RouteUrl("ConfirmEmail", new { uid = userIdentity.Id, token = confirmToken }, this.Request.Scheme);
                try
                {
                    await _emailSender.SendEmailAsync(userIdentity.Email, "Confirm your account",
                        $"Please confirm your account by this ref <a href=\"{emailConfirmUrl}\">link</a>");
                }
                catch
                {
                    ModelState.AddModelError("email", "Failed to send confirmation letter");
                    return BadRequest(ModelState);
                }
            }

            return CreatedAtRoute("GetDriver", new { id = driver.Id }, driverDto);
        }
        [Produces(contentType: "application/json")]
        [HttpGet("{id}",Name = "GetDriver")]
        public async Task<IActionResult> GetDriver(Guid id)
        {
            var driver = _usersRepository.GetDriverById(id);

            if (driver == null)
            {
                return NotFound();
            }

            var driverIdentity = await _userManager.Users.Include(o => o.ProfilePicture).FirstOrDefaultAsync(p => p.Id == driver.IdentityId);

            if (driverIdentity == null)
            {
                return NotFound();
            }
            var driverDto =_mapper.Map<DriverDto>(driverIdentity);

            _mapper.Map(driver, driverDto);

            driverDto.IdentityId = driverIdentity.Id;

            driverDto.VehicleId = driver.Vehicle?.Id;

            driverDto.ProfilePictureId = driverIdentity?.ProfilePicture?.Id;

            driverDto.Rating = _usersRepository.GetRatingForDriver(driverDto.Id);

            return Ok(driverDto);
        }
        
        [HttpPut("{id}")]
        [Authorize(Policy = "Driver")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> UpdateDriver([FromBody]DriverUpdateDto driverDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var id = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

            var driver = _usersRepository.GetDriverById(Guid.Parse(id));

            if (driver == null)
            {
                return NotFound();
            }

            Mapper.Map(driverDto, driver.Identity);

            Mapper.Map(driverDto, driver);


            if (driverDto.CurrentPassword != null && driverDto.NewPassword != null)
            {
                var result = await _userManager.ChangePasswordAsync(driver.Identity, driverDto.CurrentPassword, driverDto.NewPassword);
                if (!result.Succeeded)
                    return Conflict();
            }

            var res = await _usersRepository.UpdateDriver(driver);

            if (!res)
                return Conflict();

            return NoContent();
        }
        [HttpGet("{driverId}/comments", Name = "GetCommentsForDriver")]
        public async Task<IActionResult> GetCommentsForDriver(Guid driverId, [FromQuery] PaginationParameters paginationParameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var driver = _usersRepository.GetDriverById(driverId);

            if (driver == null)
            {
                return NotFound();
            }

            var comments = await _usersRepository.GetDriverComments(driverId, paginationParameters);

            var prevLink = comments.HasPrevious
                ? _resourceUriHelper.CreateResourceUri(paginationParameters, ResourceUriType.PrevoiusPage, nameof(GetCommentsForDriver)) : null;

            var nextLink = comments.HasNext
                ? _resourceUriHelper.CreateResourceUri(paginationParameters, ResourceUriType.NextPage, nameof(GetCommentsForDriver)) : null;

            Response.Headers.Add("X-Pagination", Helpers.PaginationMetadata.GeneratePaginationMetadata(comments, paginationParameters, prevLink, nextLink));
            
            var commentsDto = new List<DriverCommentDto>();
            
            foreach (var c in comments)
            {
                var comment = Mapper.Map<DriverCommentDto>(c);

                var user = _usersRepository.GetCustomerById(c.CustomerId);

                comment.PictureId = user.Identity.ProfilePicture?.Id;

                comment.FirstName = user.Identity.FirstName;

                comment.LastName = user.Identity.LastName;

                commentsDto.Add(comment);
            }

            return Ok(commentsDto);
        }
    }
}
