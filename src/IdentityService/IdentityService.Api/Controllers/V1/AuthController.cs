using API.Common.Response.Model.ControllerHelpers;
using API.Common.Response.Model.Extensions;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Contracts.DTOs;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
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

        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("refresh")]
        [Authorize]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var baseResult = await _service.Token.RefreshTokenAsync(request);
            if (!baseResult.Success)
            {
                return ProcessError(baseResult);
            }

            var result = baseResult.GetResult<(string AccessToken, string RefreshToken)>();
            return Ok(new LoginTokenDto
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
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
        public async Task<IActionResult> Verify(OtpVerificationRequest request)
        {
            var result = await _service.User.VerifyAccountAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Resends OTP
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(OtpResendRequest request)
        {
            var result = await _service.User.ResendOtpAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Requests password reset
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset(EmailPayload request)
        {
            var result = await _service.User.RequestPasswordResetAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Password reset
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("reset-password")]
        public async Task<IActionResult> PasswordReset(PasswordResetRequest request)
        {
            var result = await _service.User.PasswordResetAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Password change
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> PasswordChange(PasswordChangeRequest request)
        {
            var result = await _service.User.ChangePasswordAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Suspend a user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("suspend")]
        [Authorize(Roles = "SuperAdmin, Admin")]
        public async Task<IActionResult> Suspend(UserSuspensionRequest request)
        {
            var result = await _service.User.SuspendUserAccountAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Reactivate suspended user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("lift-suspension/{userId}")]
        [Authorize(Roles = "SuperAdmin, Admin")]
        public async Task<IActionResult> LiftSuspension([FromRoute] string userId)
        {
            var result = await _service.User.LiftAccountSuspensionAsync(userId);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Deactivates the current user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("deactivate/{userId}")]
        [Authorize]
        public async Task<IActionResult> Deactivate([FromRoute] string userId)
        {
            var result = await _service.User.DeactivateAccountAsync(userId);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Requests account reactivation
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("request-reactivation")]
        public async Task<IActionResult> ReactivationRequest(EmailPayload request)
        {
            var result = await _service.User.RequestAccountReactivationAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }

        /// <summary>
        /// Reactivate an account
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("reactivate")]
        public async Task<IActionResult> Reactivate(OtpVerificationRequest request)
        {
            var result = await _service.User.ReactivateAccountAsync(request);
            if (!result.Success)
            {
                return ProcessError(result);
            }

            return Ok(result.GetResult<SuccessStringDto>());
        }
    }
}
