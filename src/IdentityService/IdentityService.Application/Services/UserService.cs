using API.Common.Response.Model.Responses;
using CrossQueue.Hub.Services.Interfaces;
using CSharpTypes.Extensions.Date;
using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using EFCore.CrudKit.Library.Data.Interfaces;
using FluentValidation;
using IdentityService.Application.Extensions;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Application.Validations;
using IdentityService.Contracts.DTOs;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebRequestMethods;

namespace IdentityService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IHttpContextAccessor _accessor;
        private readonly IEFCoreCrudKit _crudKit;
        private readonly IRabbitMQPubSub _pubSub;

        public UserService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            IHttpContextAccessor accessor, IEFCoreCrudKit crudKit, IRabbitMQPubSub pubSub)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _accessor = accessor;
            _crudKit = crudKit;
            _pubSub = pubSub;
        }

        public async Task<ApiBaseResponse> ValidateUser(LoginRequest request)
        {
            var validation = new LoginValidator().Validate(request);
            if (!validation.IsValid)
            {
                return new BadRequestResponse(validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input");
            }

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                return new NotFoundResponse("User not found");
            }

            if (!user.EmailConfirmed || user.Status != UserStatus.Active)
            {
                return GetStatusResponse(user.Status);
            }

            var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!check.Succeeded)
            {
                return new ForbiddenResponse("Wrong password");
            }

            var roles = (await _userManager.GetRolesAsync(user)).ToArray();
            if (roles == null || roles.Length == 0)
            {
                return new UnauthorizedResponse("User have no assigned role.");
            }

            return new OkResponse<(AppUser User, string[] Roles)>((user, roles));
        }

        public async Task<ApiBaseResponse> RegisterAsync(RegistrationRequest request, bool forAdmin = false)
        {
            var hasPermission = (await GetUserRoles()).Contains(SystemRoles.SuperAdmin);
            if(!hasPermission && (request.Role == SystemRoles.SuperAdmin || request.Role == SystemRoles.Admin))
            {
                return new ForbiddenResponse("You have no permission to add an Admin user");
            }

            var validate = new RegistrationValidator().Validate(request);
            if (!validate.IsValid)
            {
                return new BadRequestResponse(validate.Errors.FirstOrDefault()?.ErrorMessage ?? "Registration failed");
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ForbiddenResponse($"A user already exists with this email: {request.Email}");
            }

            var user = request.Map();
            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return new BadRequestResponse(createResult.Errors?.FirstOrDefault()?.Description ?? "User registration failed. Please try again");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role.GetDescription());
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return new BadRequestResponse($"Registration failed. {roleResult.Errors.FirstOrDefault()?.Description}");
            }

            // OTP
            var otp = GenerateOtp();
            var (hash, salt) = HashOtp(otp);
            var otpEntry = user.Map(hash, salt);
            await _crudKit.InsertAsync(otpEntry);

            // Send activation email to user
            var type = GetNotificationType(OtpType.AccountVerification);
            await _pubSub.PublishAsync(user.Map(otp, otpEntry.ExpiresAt, type),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            // Return to the user
            return new OkResponse<RegistrationDto>(new RegistrationDto
            {
                Email = user.Email,
                Message = "Registration successful. Please check your mail for your activation code"
            });
        }

        public async Task<ApiBaseResponse> UpdateBasicDetails(UpdateUserBasicDetailsRequest request)
        {
            var validate = new UserBasicDetailsRequestValidator().Validate(request);
            if (!validate.IsValid)
            {
                return new BadRequestResponse(validate.Errors.FirstOrDefault()?.ErrorMessage ?? "User details update failed");
            }

            var userId = GetLoggedInUserId();
            var existingUser = await _userManager.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return new NotFoundResponse($"No user found");
            }

            existingUser = existingUser.Map(request);
            await _userManager.UpdateAsync(existingUser);

            return new OkResponse<SuccessStringDto>(new SuccessStringDto("User details successfully updated."));
        }

        public async Task<ApiBaseResponse> ResendOtpAsync(OtpResendRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new NotFoundResponse("No user found with this email");
            }

            if (user.EmailConfirmed && request.Type == OtpType.AccountVerification)
            {
                return new ForbiddenResponse("Account already verified. Please login");
            }

            var existingOtp = await _crudKit
               .AsQueryable<OtpEntry>(o => o.UserId.Equals(user.Id) && o.Type == request.Type, false)
               .OrderByDescending(o => o.ExpiresAt)
               .FirstOrDefaultAsync();

            var otp = GenerateOtp();
            var (Hash, Salt) = HashOtp(otp);
            var otpEntry = user.Map(Hash, Salt, request.Type);
            await _crudKit.InsertAsync(otpEntry);

            var type = GetNotificationType(request.Type);
            await _pubSub.PublishAsync(user.Map(otp, otpEntry.ExpiresAt, type),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            if(existingOtp != null)
            {
                await _crudKit.DeleteAsync(existingOtp);
            }

            return new OkResponse<SuccessStringDto>(new SuccessStringDto($"OTP successfully resent. Please check your email"));
        }

        public async Task<ApiBaseResponse> RequestPasswordResetAsync(EmailPayload request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new NotFoundResponse("No user found with this email");
            }

            var otp = GenerateOtp();
            var (Hash, Salt) = HashOtp(otp);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var otpEntry = user.Map(Hash, Salt, OtpType.ResetPassword, token);

            await _crudKit.InsertAsync(otpEntry);

            var type = GetNotificationType(OtpType.ResetPassword);
            await _pubSub.PublishAsync(user.Map(otp, otpEntry.ExpiresAt, type),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto($"Password reset request successful. Please enter the OTP sent to your email to complete the process"));
        }

        public async Task<ApiBaseResponse> VerifyAccountAsync(OtpVerificationRequest request)
        {
            if(!request.IsValid)
            {
                return new BadRequestResponse($"Invalid request");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if(user == null)
            {
                return new NotFoundResponse($"No user found with the specified email address");
            }

            var otpEntry = await _crudKit
                .AsQueryable<OtpEntry>(o => o.UserId.Equals(user.Id) && o.Type == OtpType.AccountVerification, true)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if(otpEntry == null)
            {
                return new NotFoundResponse($"No valid OTP found for this user");
            }

            bool isValid = VerifyOtp(request.Otp, otpEntry.OtpHash, otpEntry.OtpSalt)
                          && otpEntry.ExpiresAt.IsLaterThan(DateTime.UtcNow);

            if (!isValid)
            {
                return new ForbiddenResponse("OTP has expired. Please request for a new one.");
            }

            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            user.Status = UserStatus.Active;
            await _userManager.UpdateAsync(user);

            await _crudKit.DeleteAsync(otpEntry);
            return new OkResponse<SuccessStringDto>(new SuccessStringDto("Account successfully verified. Please proceed to login"));
        }

        public async Task<ApiBaseResponse> PasswordResetAsync(PasswordResetRequest request)
        {
            if (!request.IsValid)
            {
                return new BadRequestResponse($"Invalid request");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new NotFoundResponse($"No user found with the specified email address");
            }

            var otpEntry = await _crudKit
                .AsQueryable<OtpEntry>(o => o.UserId.Equals(user.Id) && o.Type == OtpType.ResetPassword, true)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null)
            {
                return new NotFoundResponse($"No valid OTP found for this user");
            }

            bool isValid = VerifyOtp(request.Otp, otpEntry.OtpHash, otpEntry.OtpSalt)
                          && otpEntry.ExpiresAt.IsLaterThan(DateTime.UtcNow) && otpEntry.Token!.IsNotNullOrEmpty();

            if (!isValid)
            {
                return new ForbiddenResponse("OTP has expired. Please request for a new one.");
            }

            var result = await _userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(otpEntry.Token!), request.NewPassword);
            if (!result.Succeeded)
            {
                return new BadRequestResponse($"{result.Errors.FirstOrDefault()?.Description}" ?? "Password reset failed.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _crudKit.DeleteAsync(otpEntry);

            // Notify the user.
            await _pubSub.PublishAsync(user.Map(EmailType.PasswordResetNotification),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto("Password successfully reset. Please login with your new password"));
        }

        public async Task<ApiBaseResponse> ChangePasswordAsync(PasswordChangeRequest request)
        {
            if (!request.IsValid)
            {
                return new BadRequestResponse("Invalid request");
            }

            var loggedInUserId = GetLoggedInUserId();
            var user = await _userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new ForbiddenResponse("Access denied");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return new BadRequestResponse($"{result.Errors.FirstOrDefault()?.Description}");
            }

            // Notify the user.
            await _pubSub.PublishAsync(user.Map(EmailType.PasswordResetNotification), 
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto("Password changed successfully. Please login with the new password"));
        }

        public async Task<ApiBaseResponse> SuspendUserAccountAsync(UserSuspensionRequest request)
        {
            var loggedInUserId = GetLoggedInUserId();
            var loggedInUser = await _userManager.FindByIdAsync(loggedInUserId);
            if(loggedInUser == null)
            {
                return new ForbiddenResponse("Access denied!!! You're not authorized to perform this action.");
            }

            var roles = (await _userManager.GetRolesAsync(loggedInUser))?.ToList();
            if(roles == null || (!roles.Contains(SystemRoles.SuperAdmin.GetDescription()) && !roles.Contains(SystemRoles.Admin.GetDescription())))
            {
                return new ForbiddenResponse("Access denied!!! You're not authorized to perform this action.");
            }

            var userToUpdate = await _userManager.FindByIdAsync(request.UserId);
            if(userToUpdate == null)
            {
                return new NotFoundResponse("User not found!");
            }

            userToUpdate.Status = UserStatus.Suspended;
            userToUpdate.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(userToUpdate);

            //Revoke refresh tokens
            var tokens = await _crudKit.AsQueryable<RefreshToken>(rt => rt.UserId.Equals(userToUpdate.Id), true)
                .ToListAsync();
            if (tokens.Count != 0)
            {
                await _crudKit.DeleteRangeAsync(tokens);
            }

            // Notify the user.
            await _pubSub.PublishAsync(userToUpdate.Map(EmailType.AccountSuspension, request.Reason),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto($"Account successfully suspended."));
        }

        public async Task<ApiBaseResponse> LiftAccountSuspensionAsync(string userId)
        {
            var loggedInUserId = GetLoggedInUserId();
            var loggedInUser = await _userManager.FindByIdAsync(loggedInUserId);
            if (loggedInUser == null)
            {
                return new ForbiddenResponse("Access denied!!! You're not authorized to perform this action.");
            }

            var roles = (await _userManager.GetRolesAsync(loggedInUser))?.ToList();
            if (roles == null || (!roles.Contains(SystemRoles.SuperAdmin.GetDescription()) && !roles.Contains(SystemRoles.Admin.GetDescription())))
            {
                return new ForbiddenResponse("Access denied!!! You're not authorized to perform this action.");
            }

            var userToUpdate = await _userManager.FindByIdAsync(userId);
            if (userToUpdate == null)
            {
                return new NotFoundResponse("User not found!");
            }

            userToUpdate.Status = UserStatus.Active;
            userToUpdate.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(userToUpdate);

            // Notify the user.
            await _pubSub.PublishAsync(userToUpdate.Map(EmailType.AdminAccountReactivation),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto($"Account successfully reactivated."));
        }

        public async Task<ApiBaseResponse> DeactivateAccountAsync(string userId)
        {
            var loggedInUserId = GetLoggedInUserId();
            if(string.IsNullOrWhiteSpace(loggedInUserId) || !loggedInUserId.Equals(userId))
            {
                return new ForbiddenResponse("Access denied!!! You're not authorized to perform this action.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new NotFoundResponse("User not found!");
            }

            user.Status = UserStatus.Deactivated;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            //Revoke refresh tokens
            var tokens = await _crudKit.AsQueryable<RefreshToken>(rt => rt.UserId.Equals(user.Id), true)
                .ToListAsync();
            if (tokens.Count != 0)
            {
                await _crudKit.DeleteRangeAsync(tokens);
            }

            // Notify the user.
            await _pubSub.PublishAsync(user.Map(EmailType.AccountDeactivation),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto($"Account successfully deactivated."));
        }

        public async Task<ApiBaseResponse> RequestAccountReactivationAsync(EmailPayload request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new NotFoundResponse("No user found with this email");
            }

            var otp = GenerateOtp();
            var (Hash, Salt) = HashOtp(otp);
            var otpEntry = user.Map(Hash, Salt, OtpType.AccountReactivation);

            await _crudKit.InsertAsync(otpEntry);

            var type = GetNotificationType(OtpType.AccountReactivation);
            await _pubSub.PublishAsync(user.Map(otp, otpEntry.ExpiresAt, type),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());

            return new OkResponse<SuccessStringDto>(new SuccessStringDto($"Account reactivation request successful. Please enter the OTP sent to your email to complete the process"));
        }

        public async Task<ApiBaseResponse> ReactivateAccountAsync(OtpVerificationRequest request)
        {
            if (!request.IsValid)
            {
                return new BadRequestResponse($"Invalid request");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new NotFoundResponse($"No user found with the specified email address");
            }

            var otpEntry = await _crudKit
                .AsQueryable<OtpEntry>(o => o.UserId.Equals(user.Id) && o.Type == OtpType.AccountReactivation, true)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null)
            {
                return new NotFoundResponse($"No valid OTP found for this user");
            }

            bool isValid = VerifyOtp(request.Otp, otpEntry.OtpHash, otpEntry.OtpSalt)
                          && otpEntry.ExpiresAt.IsLaterThan(DateTime.UtcNow);

            if (!isValid)
            {
                return new ForbiddenResponse("OTP has expired. Please request for a new one.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.Status = UserStatus.Active;
            await _userManager.UpdateAsync(user);

            await _crudKit.DeleteAsync(otpEntry);
            await _pubSub.PublishAsync(user.Map(EmailType.AccountReactivationNotification),
                routingKey: RabbitMqRoutingKey.AccountEmail.GetDescription());
            return new OkResponse<SuccessStringDto>(new SuccessStringDto("Account successfully reactivated. Please proceed to login"));
        }

        public async Task<ApiBaseResponse> GetLoggedInUserLeanAsync()
        {
            var userId = GetLoggedInUserId();
            if (userId.IsNullOrEmpty())
            {
                return new UnauthorizedResponse("User is unauthorized");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if(user == null)
            {
                return new NotFoundResponse($"No user information found");
            }

            return new OkResponse<UserLeanDto>(user.Map());
        }

        public async Task<ApiBaseResponse> UpdateUserLastLogin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return new NotFoundResponse($"No user information found");
            }

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new OkResponse<bool>(true);
        }

        public string GetLoggedInUserId()
        {
            return GetUserId();
        }

        public async Task<List<SystemRoles>> GetUserRoles()
        {
            var roles = new List<SystemRoles>();
            var userId = GetUserId();
            if (userId.IsNotNullOrEmpty())
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return roles;
                }

                return (await _userManager.GetRolesAsync(user)).ToList().ParseValues<SystemRoles>();
            }

            return roles;
        }

        #region Private Methods
        private ApiBaseResponse GetStatusResponse(UserStatus status)
        {
            return status switch
            {
                UserStatus.PendingVerification
                    => new ForbiddenResponse("You can't login at the moment. Please confirm your email."),
                UserStatus.Suspended
                    => new ForbiddenResponse("Your account has been suspended. Please contact support."),
                UserStatus.Deactivated
                    => new ForbiddenResponse("Your account has been deactivated. Please contact reactivate or contact support"),
                _ => throw new NotImplementedException()
                    
            };
        }

        private string GetUserId()
        {
            ClaimsPrincipal? userClaim = _accessor.HttpContext?.User;
            return userClaim?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
        
        private static string GenerateOtp(int length = 5)
        {
            if (length <= 0) throw new ArgumentException("OTP length must be positive.");

            // Use a secure random number generator
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int randomNumber = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // ensure non-negative

            // Generate a number with the desired length
            int otpValue = randomNumber % (int)Math.Pow(10, length);

            // Pad with leading zeros if necessary (e.g., "00429")
            return otpValue.ToString(new string('0', length));
        }

        private static (string Hash, string Salt) HashOtp(string otp)
        {
            // Generate random salt
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);

            using var hmac = new HMACSHA256(saltBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(otp));

            return (Convert.ToBase64String(hash), Convert.ToBase64String(saltBytes));
        }

        private static bool VerifyOtp(string otp, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);

            using var hmac = new HMACSHA256(saltBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(otp));

            var computedHash = Convert.ToBase64String(hash);
            return computedHash == storedHash;
        }

        private EmailType GetNotificationType(OtpType otpType)
        {
            return otpType switch
            {
                OtpType.AccountVerification => EmailType.AccountActivation,
                OtpType.ResetPassword => EmailType.PasswordReset,
                OtpType.AccountReactivation => EmailType.AccountReactivation,
                _ => throw new NotImplementedException()
            };
        }
        #endregion
    }
}
