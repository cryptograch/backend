﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Taxi.Entities;
using Taxi.Helpers;
using Taxi.Models;
using Taxi.Models.Drivers;
using Taxi.Services;

namespace Taxi.Controllers
{
    [Route("api/documents")]
    public class DocumentsController: Controller
    {
        private UserManager<AppUser> _userManager;
        private IUploadService _uploadService;
        private IUsersRepository _usersRepository;
        private IHostingEnvironment _hostingEnvironment;

        public DocumentsController(UserManager<AppUser> userManager,
            IUploadService uploadService,
            IUsersRepository usersRepository,
            IHostingEnvironment env)
        {
            _userManager = userManager;
            _uploadService = uploadService;
            _usersRepository = usersRepository;
            _hostingEnvironment = env;
        }

        //[Authorize(Policy = "DriverReg")]
        //[HttpGet("driverlicense/image")]
        //public async Task<IActionResult> GetLicensePicture()
        //{
        //    var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

        //    var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

        //    if (driver?.DriverLicense == null)
        //        return NotFound();

        //    FileDto res = await _uploadService.GetObjectAsync(driver.DriverLicense.ImageId);

        //    if (res == null)
        //        return NotFound();

        //    res.Stream.Seek(0, SeekOrigin.Begin);
        //    return File(res.Stream, res.ContentType);
        //}

        //[Authorize(Policy = "DriverReg")]
        //[HttpPut("driverlicense")]
        //public async Task<IActionResult> CreateLicence([FromBody]LicenseCreationDto licenseCreation)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);
        //    var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

        //    var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

        //    if (driver == null)
        //        return NotFound();

        //    if (driver?.DriverLicense != null)
        //    {
        //        var res = await _usersRepository.RemoveDriverLicense(driver.DriverLicense);
        //        if (!res)
        //            return Conflict();
        //    }

        //    DateTime licensedTo;
        //    DateTime licensedFrom;

        //    try
        //    {
        //        licensedFrom = new DateTime(licenseCreation.YearFrom, licenseCreation.MonthFrom, licenseCreation.DayFrom);
        //        licensedTo = new DateTime(licenseCreation.YearTo, licenseCreation.MonthTo, licenseCreation.DayTo);
        //    }
        //    catch
        //    {
        //        return BadRequest();
        //    }

        //    if (licensedFrom > licensedTo)
        //        return BadRequest();
        //    var license = new DriverLicense()
        //    {
        //        DriverId = Guid.Parse(driverId),
        //        LicensedTo = licensedTo,
        //        LicensedFrom = licensedFrom
        //    };
        //    var addres = await _usersRepository.AddDriverLicense(license);

        //    if (!addres)
        //        return Conflict();

        //    return NoContent();
        //}

        [Authorize(Policy = "DriverReg")]
        [HttpPut("driverlicense/imagefront")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SetFrontLicensePicture(List<IFormFile> files)
        {
            var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;
            
            var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

            long size = files.Sum(f => f.Length);

            if (files.Count < 1)
            {
                return NotFound();
            }

            var formFile = files[0];

            if (!formFile.IsImage())
            {
                return BadRequest();
            }

            if (driver.DriverLicense == null)
            {
                driver.DriverLicense = new DriverLicense();
                //remove picture from data context
            }

            if (driver.DriverLicense.FrontId != null)
            {
                await _uploadService.DeleteObjectAsync(driver.DriverLicense.FrontId);
            }
            if (formFile.Length > 0)
            {
                var filename = ContentDispositionHeaderValue
                        .Parse(formFile.ContentDisposition)
                        .FileName
                        .TrimStart().ToString();
                filename = _hostingEnvironment.WebRootPath + $@"\uploads" + $@"\{formFile.FileName}";
                size += formFile.Length;
                using (var fs = System.IO.File.Create(filename))
                {
                    await formFile.CopyToAsync(fs);
                    fs.Flush();
                }//these code snippets saves the uploaded files to the project directory
                var imageId = Guid.NewGuid().ToString() + Path.GetExtension(filename);
                await _uploadService.PutObjectToStorage(imageId.ToString(), filename);//this is the method to upload saved file to S3
                driver.DriverLicense.UpdateTime = DateTime.UtcNow;
                driver.DriverLicense.FrontId = imageId;
                driver.DriverLicense.IsApproved = false;
                var res = await _usersRepository.UpdateDriverLicense(driver.DriverLicense);
                if (!res)
                    return Conflict();
                System.IO.File.Delete(filename);
                return Ok();
            }
            return BadRequest();
        }
        [HttpPut("driverlicense/imageback")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SetBackLicensePicture(List<IFormFile> files)
        {
            var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

            var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

            long size = files.Sum(f => f.Length);

            if (files.Count < 1)
            {
                return NotFound();
            }

            var formFile = files[0];

            if (!formFile.IsImage())
            {
                return BadRequest();
            }

            if (driver.DriverLicense == null)
            {
                driver.DriverLicense = new DriverLicense();
                //remove picture from data context
            }

            if (driver.DriverLicense.BackId != null)
            {
                await _uploadService.DeleteObjectAsync(driver.DriverLicense.BackId);
            }
            if (formFile.Length > 0)
            {
                var filename = ContentDispositionHeaderValue
                        .Parse(formFile.ContentDisposition)
                        .FileName
                        .TrimStart().ToString();
                filename = _hostingEnvironment.WebRootPath + $@"\uploads" + $@"\{formFile.FileName}";
                size += formFile.Length;
                using (var fs = System.IO.File.Create(filename))
                {
                    await formFile.CopyToAsync(fs);
                    fs.Flush();
                }//these code snippets saves the uploaded files to the project directory
                var imageId = Guid.NewGuid().ToString() + Path.GetExtension(filename);
                await _uploadService.PutObjectToStorage(imageId.ToString(), filename);//this is the method to upload saved file to S3
                driver.DriverLicense.UpdateTime = DateTime.UtcNow;
                driver.DriverLicense.BackId = imageId;
                driver.DriverLicense.IsApproved = false;
                var res = await _usersRepository.UpdateDriverLicense(driver.DriverLicense);
                if (!res)
                    return Conflict();
                System.IO.File.Delete(filename);
                return Ok();
            }
            return BadRequest();
        }


        [Authorize(Policy = "DriverReg")]
        [HttpPut("driverlicense")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SetLicensePictures(List<IFormFile> files)
        {
            var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

            var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

            long size = files.Sum(f => f.Length);

            if (files.Count != 2)
            {
                ModelState.AddModelError(nameof(driver.DriverLicense), "Upload front and back images");
                return BadRequest(ModelState);
            }

            for (int i = 0; i < files.Count; ++i)
            {
                if (!files[i].IsImage())
                {
                    ModelState.AddModelError(nameof(driver.DriverLicense), "File is not an image");
                    return BadRequest(ModelState);
                }
            }

            if (driver?.DriverLicense != null)
            {
                if (driver.DriverLicense.FrontId != null)
                    await _uploadService.DeleteObjectAsync(driver.DriverLicense.FrontId);
                if (driver.DriverLicense.BackId != null)
                    await _uploadService.DeleteObjectAsync(driver.DriverLicense.BackId);
            }

            if (driver.DriverLicense == null)
            {
                driver.DriverLicense = new DriverLicense() { };
            }

            for (int i = 0; i < files.Count; ++i)
            {
                var formFile = files[i];

                
                
                if (formFile.Length > 0)
                {
                    var filename = ContentDispositionHeaderValue
                        .Parse(formFile.ContentDisposition)
                        .FileName
                        .TrimStart().ToString();
                    filename = _hostingEnvironment.WebRootPath + $@"\uploads" + $@"\{formFile.FileName}";
                    size += formFile.Length;
                    using (var fs = System.IO.File.Create(filename))
                    {
                        await formFile.CopyToAsync(fs);
                        fs.Flush();
                    }//these code snippets saves the uploaded files to the project directory
                    var imageId = Guid.NewGuid().ToString() + Path.GetExtension(filename);
                    await _uploadService.PutObjectToStorage(imageId.ToString(), filename);//this is the method to upload saved file to S3
                    driver.DriverLicense.UpdateTime = DateTime.UtcNow;
                    driver.DriverLicense.IsApproved = false;
                    if (i == 0) //front image
                        driver.DriverLicense.FrontId = imageId;
                    if (i == 1) //back image
                        driver.DriverLicense.BackId = imageId;
                    System.IO.File.Delete(filename);
                }
                else
                {
                    return BadRequest();
                }

            }
            var res = await _usersRepository.UpdateDriverLicense(driver.DriverLicense);
            if (!res)
                return Conflict();
            return Ok();
        }
        


        [Authorize(Policy = "DriverReg")]
        [HttpGet("driverlicense")]
        public  IActionResult GetDriverLicense()
        {
            var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

            var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

            if (driver?.DriverLicense == null)
                return NotFound();

            var licenseToReturn = Mapper.Map<DriverLicenseDto>(driver.DriverLicense);

            return Ok(licenseToReturn);
        }

        [Authorize(Policy = "Driver")]
        [HttpDelete("driverlicense")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RemoveLicense()
        {
            var driverId = User.Claims.FirstOrDefault(c => c.Type == Helpers.Constants.Strings.JwtClaimIdentifiers.DriverId)?.Value;

            var driver = _usersRepository.GetDriverById(Guid.Parse(driverId));

            if (driver.DriverLicense != null)
            {
                bool res = await _usersRepository.RemoveDriverLicense(driver.DriverLicense);
                //remove picture from data context
                if (!res)
                    return BadRequest();
            }
            else return NotFound();

            return NoContent();
        }


    }
}
