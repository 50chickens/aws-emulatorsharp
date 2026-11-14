using AwsEmulatorSharp.Emulator.Wire;
using AwsEmulatorSharp.Host.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEmulatorModules();

var app = builder.Build();
app.MapWireModule();
app.Run();
