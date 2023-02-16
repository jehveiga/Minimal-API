using DemoMinimalAPI.Data;
using DemoMinimalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

#region Configure Services

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("DemoMinimalAPI")));

builder.Services.AddIdentityConfiguration();

builder.Services.AddJwtConfiguration(builder.Configuration, "AppJwtSettings");

// Add service of authorization to the container, include the claims.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DeleteProvider",
        policy => policy.RequireClaim("DeleteProvider"));
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Developed by Jefferson Veiga - Owner @ VeigaSolutions",
        Contact = new OpenApiContact { Name = "Jefferson Veiga", Email = "contato@veiga.net.br" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insert the token JWT this way: Bearer {you token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

#endregion

#region Configure Pipeline

// Start configuration request - down here
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();

app.UseHttpsRedirection();

MapActions(app);

app.Run();

#endregion

#region Actions

void MapActions(WebApplication app)
{

    // Always to leave the parameter "registerUser" for last
    app.MapPost("/registerUser", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser is null) return Results.BadRequest("User not informat");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded) return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(user.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);
    })
        .ProducesValidationProblem()
        .Produces<Provider>(StatusCodes.Status200OK) //Add especification the documentation to API (success)
        .Produces(StatusCodes.Status400BadRequest)        //Add especification the documentation to API (error)
        .WithName("RegisterUser")
        .WithTags("User");

    app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        LoginUser loginUser) =>
    {
        if (loginUser is null) return Results.BadRequest("User not informat");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut) return Results.BadRequest("User Blocked");

        if (!result.Succeeded) return Results.BadRequest("User or Password unvalid");

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);
    })
        .ProducesValidationProblem()
        .Produces<Provider>(StatusCodes.Status200OK) //Add especification the documentation to API (success)
        .Produces(StatusCodes.Status400BadRequest)        //Add especification the documentation to API (error)
        .WithName("LoginUser")
        .WithTags("User");


    // Definition first the rote -> mode Async -> parameters -> action
    // Mapping the Get Verb to fetch a list of providers 
    app.MapGet("/provider", [AllowAnonymous] async (
        MinimalContextDb context) =>
        await context.Providers.ToListAsync())
        .WithName("GetProvider")
        .WithTags("Provider");

    // Definition first the rote -> mode Async -> parameters -> action
    // Mapping the Get Verb to fetch by id a provider
    app.MapGet("/provider/{id}", async (
        MinimalContextDb context,
        Guid id) =>
        await context.Providers.FindAsync(id)
            is Provider provider
                ? Results.Ok(provider)
                : Results.NotFound())
        .Produces<Provider>(StatusCodes.Status200OK) //Add especification the documentation to API (success)
        .Produces(StatusCodes.Status404NotFound)     //Add especification the documentation to API (error)
        .WithName("GetProviderById")
        .WithTags("Provider");

    // Definition first the rote -> mode Async -> parameters -> action
    // Mapping the Post Verb to add a provider to DbContext
    app.MapPost("/provider", [Authorize] async (
        MinimalContextDb context,
        Provider provider) =>
    {
        if (!MiniValidator.TryValidate(provider, out var errors))
            return Results.ValidationProblem(errors);

        context.Providers.Add(provider);
        var result = await context.SaveChangesAsync();

        return result > 0
            //? Results.Created($"/provider/{provider.Id}", provider) another way to do the endpoint below
            ? Results.CreatedAtRoute("GetProviderById", new { id = provider.Id }, provider) // Return object type Provider Created
            : Results.BadRequest("There was a problem, to save the register");

    }).ProducesValidationProblem()
    .Produces<Provider>(StatusCodes.Status201Created) //Add especification the documentation to API (success)
    .Produces(StatusCodes.Status400BadRequest)        //Add especification the documentation to API (error)
    .WithName("PostProvider")
    .WithTags("Provider");

    // Definition first the rote -> mode Async -> parameters -> action
    //Map Verb Put
    app.MapPut("/provider/{id}", [Authorize] async (
        Guid id,
        MinimalContextDb context,
        Provider provider) =>
    {
        var providerBase = await context.Providers.AsNoTracking<Provider>()
                                                    .FirstOrDefaultAsync(p => p.Id == id);

        if (providerBase is null) return Results.NotFound();

        if (!MiniValidator.TryValidate(provider, out var errors))
            return Results.ValidationProblem(errors);

        context.Providers.Update(provider);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent() //Return a code 204
            : Results.BadRequest("There was a problem, to save the register");
    }).ProducesValidationProblem()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("PutFornecedor")
        .WithName("Provider");

    // Definition first the rote -> mode Async -> parameters -> action
    //Map Verb Delete
    app.MapDelete("/provider/{id}", [Authorize] async (
        Guid id,
        MinimalContextDb context) =>
    {
        var provider = await context.Providers.FindAsync(id);
        if (provider is null) return Results.NotFound();

        context.Providers.Remove(provider);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("There was a problem, to save the register");
    }).Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization("DeleteProvider") // Information the type of claim for delete provider reference table "AspNetUserClaims"
        .WithName("DeleteProvider")
        .WithTags("Provider");

    #endregion
}