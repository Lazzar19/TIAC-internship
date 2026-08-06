using Microsoft.AspNetCore.Http;
using WebAPI.Application.Validators;

namespace WebAPI.Tests;

using WebAPI.Domain;
using FluentAssertions;
using Moq;
using Xunit;

public class CreatUserDTOValidatorTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly CreateUserDTOValidator validator;

    public CreatUserDTOValidatorTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        validator = new CreateUserDTOValidator(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Should_Fail_When_Username_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "", Email = "test@gmail.com", Password = "testtest" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Should_Fail_When_Username_Exceeds_MaxLength()
    {
        var dto = new CreateUserDTO { Username = new string('a', 21), Email = "test@gmail.com", Password = "testtest" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Should_Fail_When_Email_Is_Empty_String()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "", Password = "testtest" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Should_Fail_When_Email_Format_Is_Invalid()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test", Password = "testtest" };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Should_Fail_When_Email_Already_Exists()
    {
        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync("existing@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateUserDTO { Username = "test", Email = "existing@gmail.com", Password = "testtest" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage == "Email already exists.");
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
    public async Task Should_Fail_When_Password_Is_Less_Than_6_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 5) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Should_Fail_When_Password_Is_More_Than_20_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 21) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Should_Pass_When_Password_Is_Exactly_20_Characters()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 20) };
        var result = await validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_When_Password_Is_Exactly_MinLength()
    {
        var dto = new CreateUserDTO { Username = "test", Email = "test@gmail.com", Password = new string('a', 6) };
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