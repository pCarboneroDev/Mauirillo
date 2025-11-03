using Microsoft.AspNetCore.ResponseCompression;
using Servidor.Hubs;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.MaximumReceiveMessageSize = 5242880; // 5 Mb
});
builder.Services.AddAntiforgery();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5178")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/", () => "Hello World!");

app.MapHub<Services>("/hub");

app.Run();
