using FluentValidation;
using WebAPI.Application;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;

public class ChangePasswordDTOValidator : AbstractValidator<ChangePasswordDTO>
{

    public ChangePasswordDTOValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required");


        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("New password is required")
            .MinimumLength(PasswordRules.MinimumLength).WithMessage("New password must contain at least 12 characters")
            .MaximumLength(PasswordRules.MaximumLength).WithMessage("New password cannot exceed 128 characters.")
            .NotEqual(x => x.CurrentPassword).
            WithMessage("New password must be different from current password");

    }
    
}