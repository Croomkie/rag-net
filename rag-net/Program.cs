using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rag_net;
using rag_net.Db;
using rag_net.services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<DbContextRag>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPdfParseUtils, PdfParseUtils>();
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContextRag>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.MapGet("/query", (string message) =>
{
    //Create a simple RAG response
    return "RAG Response for: " + message;
});

app.MapPost("/populate",
        (IFormFileCollection files, IPdfParseUtils parser) =>
        {
            var sentences = parser.ExtractChunksFromPdf(files);
            
            
            return Results.Ok();
        })
    .DisableAntiforgery();

app.Run();