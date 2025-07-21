using System.Text;
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
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<IOpenAiChunkService, OpenAiChunkService>();

builder.Services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", config =>
    {
        config.WithOrigins("http://localhost:3000", "https://rag-ui-neon.vercel.app")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors("CorsPolicy");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContextRag>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.MapGet("/query",
    async (string message, string productName, IEmbeddingService embeddingService) =>
        await embeddingService.SearchByEmbeddingAsync(message, productName));

app.MapGet("/chat",
    async (HttpContext context, string message, string productName, IEmbeddingService embeddingService) =>
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        var writer = new StreamWriter(context.Response.Body, Encoding.UTF8);

        await foreach (var token in embeddingService.ChatResponseAsync(message, productName))
        {
            await writer.WriteAsync(token);
            await writer.FlushAsync();
        }
    });

app.MapPost("/populate", async
        (IFormFileCollection files, string productName, IPdfParseUtils parser, IEmbeddingService embeddingService) =>
    {
        var chunks = await parser.ExtractChunksFromPdf(files, 300, productName);

        await embeddingService.SaveAllEmbeddingsAsync(chunks);

        return Results.Ok("Le PDF a bien été enregistré");
    })
    .DisableAntiforgery();

app.Run();