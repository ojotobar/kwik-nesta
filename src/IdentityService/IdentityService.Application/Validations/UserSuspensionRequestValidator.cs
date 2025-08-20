using FluentValidation;
using IdentityService.Contracts.Requests;

namespace IdentityService.Application.Validations
{
    internal class UserSuspensionRequestValidator : AbstractValidator<UserSuspensionRequest>
    {
        public UserSuspensionRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("{PropertyName} field is required.");
            RuleFor(x => x.Reason)
                .IsInEnum().WithMessage("Invalid suspension reason");
        }
    }
}
