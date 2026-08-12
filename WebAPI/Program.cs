using System.Text;
using System.Threading.RateLimiting;
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
using WebAPI.Middleware;
using Serilog;



    var builder = WebApplication.CreateBuilder(args);
    
    

    builder.Host.UseSerilog((context, service, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(service)
        .Enrich.FromLogContext()
        .WriteTo.Console());
    
    

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(); // swagger 

    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
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
    builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();



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

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(3),
                    QueueLimit = 0
                }));
    });



    builder.Services.AddAuthorization();




    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseSerilogRequestLogging(); // SeriLog 

    app.UseMiddleware<ExceptionHandlingMiddleware>(); // middleware exception handling 



    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            dbContext.Database.Migrate();
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.Run();






public partial class Program
{
};


