using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WebAPI.Application;
using WebAPI.Application.Interfaces;
using WebAPI.Application.Validators;
using WebAPI.Domain;
using WebAPI.Infrastructure;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // swagger 

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
  

});


// database  
builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserProductRepository, UserProductRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();



// fluent validation 
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateProductDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<AssignProductToUserDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordDTOValidator>();


// JWT auth

var jwtSettings_ = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings_["Issuer"],
            ValidAudience = jwtSettings_["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings_["Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseAuthorization();
app.Run();



