using EntityArchitect.CRUD;
using EntityArchitect.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddEntityArchitect(typeof(Program).Assembly, builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapEntityArchitectCrud(typeof(Program).Assembly);

app.Run();