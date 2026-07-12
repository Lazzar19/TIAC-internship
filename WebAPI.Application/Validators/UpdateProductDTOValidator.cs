using FluentValidation;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;

public sealed class UpdateProductDTOValidator : AbstractValidator<UpdateProductDTO>
{
    public UpdateProductDTOValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required")
            .MaximumLength(30).WithMessage("Name of product can not be longer than 30 characters");
        
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
        RuleFor(x => x.Stock).GreaterThan(0).WithMessage("Stock must be greater than 0");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description of a product is required");
        
    }
    
    
}