using Microsoft.EntityFrameworkCore;
using rag_net;
using rag_net.Db;
using rag_net.Repository;
using rag_net.services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<DbContextRag>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseVector()));

builder.Services.AddScoped<IPdfParseUtils, PdfParseUtils>();
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

builder.Services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.MapOpenApi();
app.MapScalarApiReference();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContextRag>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.MapGet("/query",
    async (string message, string productName, IEmbeddingService embeddingService) =>
        await embeddingService.SearchByEmbeddingAsync(message, productName));

app.MapPost("/populate", async
        (IFormFileCollection files, string productName, IPdfParseUtils parser, IEmbeddingService embeddingService) =>
    {
        var chunks = parser.ExtractChunksFromPdf(files, 300, productName);

        await embeddingService.SaveAllEmbeddingsAsync(chunks);

        return Results.Ok("Le PDF a bien été enregistré");
    })
    .DisableAntiforgery();

app.Run();