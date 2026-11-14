using AwsEmulatorSharp.Host.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEmulatorModules();

var app = builder.Build();
app.Run();
