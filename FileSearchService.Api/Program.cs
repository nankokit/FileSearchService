using FileSearchService.Application.Interfaces;
using FileSearchService.Application.Services;
using FileSearchService.Infrastructure;
using FileSearchService.Infrastructure.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSingleton(Log.Logger);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IQdrantClient, QdrantClient>();
builder.Services.AddSingleton<IFileReader, FileReader>();
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IFileIndexingService, FileIndexingService>();
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<IFileUploadService, FileUploadService>();
builder.Services.AddHostedService<FileIndexingBackgroundService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

app.Run();