﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjektRandkowy.Data;
using ProjektRandkowy.Helpers;
using CloudinaryDotNet;
using ProjektRandkowy.Dtos;
using System.Threading.Tasks;
using System.Security.Claims;
using ProjektRandkowy.Models;
using CloudinaryDotNet.Actions;
using System.Linq;


namespace ProjektRandkowy.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _claudinary;
        public PhotosController(IUserRepository repository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account account = new Account
            (
                 _cloudinaryConfig.Value.CloudName,
                 _cloudinaryConfig.Value.ApiKey,
                 _cloudinaryConfig.Value.ApiSecret
            );

            _claudinary = new Cloudinary(account);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepo = await _repository.GetUser(userId);
            var file = photoForCreationDto.File;
            var uploadResult = new ImageUploadResult();
            if (file.Length > 0)
            {
                using ( var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    uploadResult = _claudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if (!userFromRepo.Photos.Any(p => p.IsMain))
                photo.IsMain = true;

            userFromRepo.Photos.Add(photo);

            if (await _repository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                //var id = photo.Id;
                return CreatedAtRoute(nameof(GetPhoto), new { id = photo.Id }, photoToReturn);
               // return CreatedAtRoute("GetPhoto", new { id = photo.Id }, photoToReturn);
            }

            return BadRequest("Nie mozna dodac zdjecia");
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repository.GetPhoto(id);

            var photoForReturn = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photoForReturn);
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();


            var user = await _repository.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await _repository.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("To juz jest główne zdjęcie");

            var currentMainPhoto = await _repository.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;
            photoFromRepo.IsMain = true;

            if (await _repository.SaveAll())
                return NoContent();

            return BadRequest("Nie mozna ustawic zdjecia jako głownego");

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repository.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await _repository.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("Nie mozna usunac zdjecia glownego!");

            if(photoFromRepo.public_id != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.public_id);
                var result = _claudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                    _repository.Delete(photoFromRepo);
            }

            if (photoFromRepo.public_id == null)
                _repository.Delete(photoFromRepo);

                if (await _repository.SaveAll())
                return Ok();

            return BadRequest("Nie udalo sie usunac zdjecia");

        }

    }
}
