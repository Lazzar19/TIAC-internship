using WebAPI.Application;
using WebAPI.Application.Validators;
using WebAPI.Domain;
using FluentAssertions;

namespace WebAPI.Tests;

public class CreateUserDTOValidatorTests
{
    private readonly CreateUserDTOValidator validator = new();

    [Fact]
    public async Task Should_Fail_When_Username_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "", Email = "test@gmail.com", Password = "testtesttest" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Should_Fail_When_Username_Exceeds_MaxLength()
    {
        var dto = new CreateUserDTO { Username = new string('a', 21), Email = "test@gmail.com", Password = "testtesttest" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Should_Fail_When_Email_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "", Password = "testtesttest" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Should_Fail_When_Email_Format_Is_Invalid()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test", Password = "testtesttest" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Should_Fail_When_Password_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = "" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Should_Fail_When_Password_Is_Shorter_Than_12_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 11) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Should_Fail_When_Password_Is_Longer_Than_128_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 129) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Should_Pass_When_Password_Is_Exactly_128_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 128) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_When_Password_Is_Exactly_MinLength()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', PasswordRules.MinimumLength) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_With_Valid_Data()
    {
        var dto = new CreateUserDTO { Username = "validUser", Email = "test@gmail.com", Password = "validPassword123" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }
}
