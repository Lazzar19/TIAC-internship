using Microsoft.AspNetCore.Http;
using WebAPI.Application.Validators;
using WebAPI.Domain;
using WebAPI.Application;

namespace WebAPI.Tests;

using FluentAssertions;
using WebAPI.Application;

using Xunit;

public class CreateProductDTOValidatorTests
{

    private readonly CreateProductDTOValidator validator = new();

    [Fact]

    public void Should_Fail_When_Name_Is_Empty_String()
    {
        var dto = new CreatedProductDTO { Name = "", Price = 10, Stock = 10, Description = "test description" };
        var result = validator.Validate(dto);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");

    }
    
    [Fact]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        var dto = new CreatedProductDTO { Name = new string('a', 31), Price = 10, Stock = 10, Description = "test description" };
        var result = validator.Validate(dto);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
    
    [Fact]

    public void Should_Pass_When_Name_Is_Exactly_MaxLength()
    {
        var dto = new CreatedProductDTO { Name = new string('a', 30), Price = 10, Stock = 10, Description = "test description" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Price_Is_Zero_Or_Negative()
    {
        var dto = new CreatedProductDTO { Name = "Test", Price = 0, Stock = 5, Description = "test description" };
        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }
    
    [Fact]
    public void Should_Pass_When_Price_Is_Smallest_Valid_Value()
    {
        var dto = new CreatedProductDTO { Name = "Test", Price = 0.01m, Stock = 5, Description = "test description" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }
    
    

    [Fact]

    public void Should_Fail_When_Description_Is_Empty_String()
    {
        var dto = new CreatedProductDTO { Name = "Test", Description = String.Empty, Price = 10, Stock = 10 };
        var result = validator.Validate(dto);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Should_Fail_When_Stock_Is_Negative()
    {
        var dto = new CreatedProductDTO { Name = "Test", Price = 10, Stock = -5, Description = "test description" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Stock");
    }
    
    

    [Fact]
    public void Should_Pass_With_Valid_Data()
    {
        var dto = new CreatedProductDTO()
        {
            Name = "Test",
            Description = "Test desc",
            Price = 10,
            Stock = 10
        };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

}