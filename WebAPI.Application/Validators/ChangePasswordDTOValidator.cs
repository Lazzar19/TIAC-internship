using FluentValidation;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;

public class ChangePasswordDTOValidator : AbstractValidator<ChangePasswordDTO>
{

    public ChangePasswordDTOValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required");


        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("New password is required")
            .MinimumLength(6).WithMessage("New password must contain at least 6 characters")
            .MaximumLength(20).WithMessage("New password cannot exceed 20 characters.")
            .NotEqual(x => x.CurrentPassword).
            WithMessage("New password must be different from current password");

    }
    
}