using API.Common.Response.Model.ControllerHelpers;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Contracts.DTOs;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers.V1
{
    [Route("api/v{version:apiversion}/auth")]
    [ApiVersion("1.0")]
    [ApiController]
    public class AuthController : ApiControllerBase
    {
        private readonly IServiceManager _service;

        public AuthController(IServiceManager service)
        {
            _service = service;
        }

        /// <summary>
        /// Logs in a user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var validationResult = await _service.User.ValidateUser(request);
            if (!validationResult.Success)
            {
                return ProcessError(validationResult);
            }

            var accessToken = _service.Token.CreateAccessToken(new AppUser(), new string[0]);
            var refreshToken = await _service.Token.CreateAndSaveRefreshTokenAsync("");

            return Ok(new LoginTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expires = DateTime.UtcNow.AddMinutes(1)
            });
        }
    }
}
