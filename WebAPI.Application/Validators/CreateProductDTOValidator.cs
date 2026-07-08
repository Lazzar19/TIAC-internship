using FluentValidation;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;


internal sealed class CreateProductDTOValidator : AbstractValidator<CreatedProductDTO>
{
   public CreateProductDTOValidator()
   {
      RuleFor(x => x.Name).NotEmpty().WithMessage("Name cannot be empty")
         .MaximumLength(30).WithMessage("Name of product can not be longer than 30 characters");

      RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be higher then zero");

      RuleFor(x => x.Description).NotEmpty().WithMessage("Description of a product is required");
      RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stock cannot be zero or less");
      
   }
   
}