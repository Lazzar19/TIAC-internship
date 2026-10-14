using FluentAssertions;
using WebAPI.Application.Validators;
using WebAPI.Domain;

namespace WebAPI.Tests;

public class ChangePasswordDTOValidatorTests
{
    private readonly ChangePasswordDTOValidator validator = new();

    [Fact]
    public async Task Should_Fail_When_New_Password_Is_Shorter_Than_12_Characters()
    {
        var result = await validator.ValidateAsync(new ChangePasswordDTO
        {
            CurrentPassword = "current-password",
            NewPassword = new string('a', 11)
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "NewPassword");
    }

    [Fact]
    public async Task Should_Fail_When_New_Password_Is_Longer_Than_128_Characters()
    {
        var result = await validator.ValidateAsync(new ChangePasswordDTO
        {
            CurrentPassword = "current-password",
            NewPassword = new string('b', 129)
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "NewPassword");
    }

    [Fact]
    public async Task Should_Pass_When_New_Password_Is_Exactly_12_Characters()
    {
        var result = await validator.ValidateAsync(new ChangePasswordDTO
        {
            CurrentPassword = "current-password",
            NewPassword = new string('c', 12)
        });

        result.IsValid.Should().BeTrue();
    }
}
