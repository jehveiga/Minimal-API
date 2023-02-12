using DemoMinimalAPI.Data;
using DemoMinimalAPI.Models;
using Microsoft.EntityFrameworkCore;
using MiniValidation;

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
    .Produces<Provider>(StatusCodes.Status200OK) //Add especification the documentation to API (success)
    .Produces(StatusCodes.Status404NotFound)     //Add especification the documentation to API (error)
    .WithName("GetProviderById")
    .WithTags("Provider");

// Mapping the Post Verb to add a provider to DbContext
app.MapPost("/provider", async (
    MinimalContextDb context, 
    Provider provider) =>
{
    if(!MiniValidator.TryValidate(provider, out var errors))
        return Results.ValidationProblem(errors);

    context.Providers.Add(provider);
    var result = await context.SaveChangesAsync();

    return result > 0
        //? Results.Created($"/provider/{provider.Id}", provider) another way to do the endpoint below
        ? Results.CreatedAtRoute("GetProviderById", new { id = provider.Id}, provider)
        : Results.BadRequest("There was a problem, to save the register");

}).ProducesValidationProblem()
.Produces<Provider>(StatusCodes.Status201Created) //Add especification the documentation to API (success)
.Produces(StatusCodes.Status400BadRequest)        //Add especification the documentation to API (error)
.WithName("PostProvider")
.WithTags("Provider");

app.MapPut("/provider/{id}", async(
    Guid id,
    MinimalContextDb context,
    Provider provider) => 
{
    var providerBase = await context.Providers.FindAsync(id);
    if(providerBase is null) return Results.NotFound();

    if(!MiniValidator.TryValidate(provider, out var errors))
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

app.Run();
