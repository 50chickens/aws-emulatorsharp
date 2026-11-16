using AwsEmulatorSharp.Emulator.Certificates;
using AwsEmulatorSharp.Emulator.S3;
using AwsEmulatorSharp.Emulator.Wire;
using AwsEmulatorSharp.Host.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEmulatorModules();

var app = builder.Build();
app.UseCertificatesModule();
app.MapWireModule();
app.MapS3Module();
app.Run();
