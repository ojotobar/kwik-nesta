using API.Common.Response.Model.ControllerHelpers;
using API.Common.Response.Model.Extensions;
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

            var result = validationResult.GetResult<(AppUser User, string[] Roles)>();
            var accessToken = _service.Token.CreateAccessToken(result.User, result.Roles);
            var refreshToken = await _service.Token.CreateAndSaveRefreshTokenAsync(result.User.Id);
            await _service.User.UpdateUserLastLogin(result.User.Id);
            return Ok(new LoginTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            });
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegistrationRequest request)
        {
            var result = await _service.User.RegisterAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<RegistrationDto>());
        }

        /// <summary>
        /// Verifies newly created accounts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("verify")]
        public async Task<IActionResult> Verify(AccountVerificationRequest request)
        {
            var result = await _service.User.VerifyAccountAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<string>());
        }
    }
}
