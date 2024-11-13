using EntityArchitect.CRUD;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.Entities;
using EntityArchitect.Example.Services.Logger;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddEntityArchitect(typeof(Program).Assembly, builder.Configuration);
builder.UseActions(typeof(Program).Assembly);
builder.Services.AddScoped<ILogger, Logger>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapEntityArchitectCrud(typeof(Program).Assembly);

app.Run();