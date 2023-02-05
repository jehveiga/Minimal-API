using DemoMinimalAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

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

// Mapping the Get Verb to fetch a list of providers 
app.MapGet("/provider", async (
    MinimalContextDb context) => 
    await context.Providers.ToListAsync())
    .WithName("GetProvider")
    .WithTags("Provider");

app.Run();
