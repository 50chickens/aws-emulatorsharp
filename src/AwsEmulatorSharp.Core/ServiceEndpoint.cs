namespace AwsEmulatorSharp.Core;

public class ServiceEndpoint
{
    private readonly string _serviceName;
    private readonly int _port;

    public ServiceEndpoint(string serviceName, int port)
    {
        _serviceName = serviceName;
        _port = port;
    }

    public Uri ToUri()
    {
        return new Uri($"http://localhost:{_port}/{_serviceName}");
    }
}
