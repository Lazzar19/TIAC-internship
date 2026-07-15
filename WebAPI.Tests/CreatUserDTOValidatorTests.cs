using Microsoft.AspNetCore.Http;
using WebAPI.Application.Validators;

namespace WebAPI.Tests;

using WebAPI.Domain;
using FluentAssertions;
using Xunit;
using WebAPI.Application;

public class CreatUserDTOValidatorTests
{

    private readonly CreateUserDTOValidator validator = new();

    [Fact]
    public void Should_Fail_When_Username_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "", Email = "test@gmail.com", Password = "testtest" };
        var result = validator.Validate(dto);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");

    }

    [Fact]
    public void Should_Fail_When_Username_Exceeds_MaxLength()
    {
        var dto = new CreateUserDTO{ Username = new string('a', 21), Email = "test@gmail.com", Password = "testtest" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]

    public void Should_Fail_When_Email_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "", Password = "testtest" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    
    public void Should_Fail_When_Email_Format_Is_Invalid()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test", Password = "testtest" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]

    public void Should_Fail_When_Password_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = "" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    
    public void Should_Fail_When_Password_Is_Less_Than_6_Characters()
    {
        var dto = new CreateUserDTO{ Username = "test", Email = "test@gmail.com", Password = new string('a', 5) };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Should_Fail_When_Password_Is_More_Than_20_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 21) };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
    

    [Fact]
    public void Should_Pass_When_Password_Is_Exactly_20_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 20) };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_Password_Is_Exactly_MinLength()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 6) };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }


    [Fact]
    public void Should_Pass_With_Valid_Data()
    {
        var dto = new CreateUserDTO { Username = "validUser", Email = "test@gmail.com", Password = "validPassword123" };
        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }


}