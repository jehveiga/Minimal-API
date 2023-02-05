using DemoMinimalAPI.Data;
using DemoMinimalAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Start configuration request - down here
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Definition first the rote -> mode Async -> parameters -> action
// Mapping the Get Verb to fetch a list of providers 
app.MapGet("/provider", async (
    MinimalContextDb context) => 
    await context.Providers.ToListAsync())
    .WithName("GetProvider")
    .WithTags("Provider");

// Mapping the Get Verb to fetch by id a provider
app.MapGet("/provider/{id}", async (
    MinimalContextDb context, 
    Guid id) =>
    await context.Providers.FindAsync(id)
        is Provider provider 
            ? Results.Ok(provider)
            : Results.NotFound())
    .Produces<Provider>(StatusCodes.Status200OK) //Add especification the documentation to API
    .Produces(StatusCodes.Status404NotFound)     //Add especification the documentation to API
    .WithName("GetProviderById")
    .WithTags("Provider");

app.Run();
