using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Tests.Integration_Tests;

using Xunit;
using FluentAssertions;
using WebAPI.Domain;


public class UserControllerIntegrationTests: IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static StringContent ToJsonContent(object dto)
    {
        return new StringContent(System.Text.Json.JsonSerializer.Serialize(dto),
            System.Text.Encoding.UTF8, "application/json");
    }
    
   // template for creating unique email 
    private string UniqueEmail() => $"user_{Guid.NewGuid().ToString().Substring(0, 8)}@mail.com";
    private string UniqueUsername() => $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";

    [Fact]
    public async Task Get_All_Users_Should_Return_Ok()
    {
        var response =  await _client.GetAsync("/api/User");
        response.StatusCode.Should().Be(HttpStatusCode.OK); // 200 
    }

    [Fact]
    public async Task Get_User_By_Nonexistent_Id_Should_Return_NotFound()
    {
        var response = await _client.GetAsync("/api/User/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Get_User_By_Valid_Id_Should_Return_Ok()
    {
        
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword",
            Email = UniqueEmail()
        };
        
        var createResponse = await _client.PostAsync("/api/User", ToJsonContent(createUserDto));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var userId = jsonDoc.RootElement.GetProperty("id").GetInt32();
        
        // Act
        var response = await _client.GetAsync($"/api/User/{userId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(createUserDto.Username);
    }
    
    
    
    
    [Fact]
    public async Task Create_User_With_Valid_Data_Should_Return_Created()
    {
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        };
        
        var content = ToJsonContent(createUserDto);
        
        var response = await _client.PostAsync("/api/User", content); 
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(createUserDto.Username);
    }
    
    [Fact]
    public async Task Create_User_Should_Fail_With_Invalid_Data()
    {
        var createUserDtO = new CreateUserDTO
        {
            Username = "",
            Email = "",
            Password = ""
        };
        
        var content = ToJsonContent(createUserDtO);
        
        var response = await _client.PostAsync("/api/User", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_User_With_Duplicate_Email_Should_Return_BadRequest()
    {
        var email = UniqueEmail();
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = email
        };
        
        var content = ToJsonContent(createUserDto);
        
        await _client.PostAsync("/api/User", content);
        
        var duplicateDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = email
        };
        
        var second = await _client.PostAsync("/api/User", ToJsonContent(duplicateDto));
        
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_User_With_Invalid_Email_Format_Should_Return_BadRequest()
    {
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = "invalid-email-format"
        };
        
        var content = ToJsonContent(createUserDto);
        var response = await _client.PostAsync("/api/User", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_User_With_Valid_Data_Should_Return_NoContent()
    {
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "testpassword123!",
            Email = UniqueEmail()
        };
        
        var createResponse = await _client.PostAsync("/api/User", ToJsonContent(createUserDto));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var userId = jsonDoc.RootElement.GetProperty("id").GetInt32();
        
        var updateDto = new UpdateUserDTO
        {
            UserName = "updatedname",
            Email = UniqueEmail()
        };
        var response = await _client.PutAsync($"/api/User/{userId}", ToJsonContent(updateDto));
        
     
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_User_With_Invalid_Id_Should_Return_NotFound()
    {
        var updateDto = new UpdateUserDTO
        {
            UserName = "updatedname",
            Email = UniqueEmail()
        };
        
        var response = await _client.PutAsync("/api/User/99999", ToJsonContent(updateDto));
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Change_Password_With_Valid_DATA_Should_Return_NoContent()
    {
       
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "OldPassword123!",
            Email = UniqueEmail()
        };
        
        var createResponse = await _client.PostAsync("/api/User", ToJsonContent(createUserDto));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var userId = jsonDoc.RootElement.GetProperty("id").GetInt32();
        
     
        var changePasswordDto = new ChangePasswordDTO
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };
        var response = await _client.PutAsync($"/api/User/{userId}/password", ToJsonContent(changePasswordDto));
        
   
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Change_Password_With_Wrong_CURRENT_PASSWORD_Should_Return_BadRequest()
    {
        
        var createUserDto = new CreateUserDTO
        {
            Username = UniqueUsername(),
            Password = "CorrectPassword123!",
            Email = UniqueEmail()
        };
        
        var createResponse = await _client.PostAsync("/api/User", ToJsonContent(createUserDto));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(createContent);
        var userId = jsonDoc.RootElement.GetProperty("id").GetInt32();
        
      
        var changePasswordDto = new ChangePasswordDTO
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!"
        };
        var response = await _client.PutAsync($"/api/User/{userId}/password", ToJsonContent(changePasswordDto));
        
    
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_User_With_NonExistent_Id_Should_Return_Forbidden_Without_Admin_Role()
    {
        var response = await _client.DeleteAsync("/api/User/99999");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}