using API.Common.Response.Model.Responses;
using CrossQueue.Hub.Services.Interfaces;
using CSharpTypes.Extensions.Date;
using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.String;
using EFCore.CrudKit.Library.Data.Interfaces;
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

            if (!user.EmailConfirmed || !user.IsActive)
            {
                return new ForbiddenResponse("You can't login at the moment. You have either not confirmed you email yet or your account is inactive");
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
            await _pubSub.PublishAsync(new EmailNotification
            {
                EmailAddress = user.Email!,
                ReceipientName = user.FirstName,
                Type = EmailType.AccountActivation,
                Subject = "Activate Your Account",
                Otp = new OtpData
                {
                    Value = otp,
                    Span = (int)Math.Ceiling(otpEntry.ExpiresAt.Subtract(DateTime.UtcNow).TotalMinutes)
                }
            }, routingKey: "account.activation");
            // Return to the user
            return new OkResponse<RegistrationDto>(new RegistrationDto
            {
                Email = user.Email,
                Message = "Registration successful. Please check your mail for your activation code"
            });
        }

        public async Task<ApiBaseResponse> VerifyAccountAsync(AccountVerificationRequest request)
        {
            if(request == null || request.Email.IsNullOrEmpty() || request.Otp.IsNullOrEmpty())
            {
                return new BadRequestResponse($"Invalid request");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if(user == null)
            {
                return new NotFoundResponse($"No user found with the specified email address");
            }

            var otpEntry = await _crudKit
                .AsQueryable<OtpEntry>(o => o.UserId.Equals(user.Id), false)
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
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            await _crudKit.DeleteAsync(otpEntry);
            return new OkResponse<string>("Account successfully verified. Please proceed to login");
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
        #endregion
    }
}
