using System.Globalization;
using System.Xml.Linq;
using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.UserData;
using AwsEmulatorSharp.Emulator.Wire;
using Microsoft.AspNetCore.Http;

namespace AwsEmulatorSharp.Emulator.Ec2.Lifecycle;

public class RunInstancesOperation : IQueryOperation
{
    private const string RunInstancesActionName = "RunInstances";
    private const string ImageIdParameterName = "ImageId";
    private const string InstanceTypeParameterName = "InstanceType";
    private const string UserDataParameterName = "UserData";
    private const string AmiLabelKey = "ami-id";
    private const string InvalidImageIdNotFoundCode = "InvalidAMIID.NotFound";
    private const string ImageNotFoundMessageFormat = "The image id '[{0}]' does not exist";
    private const string ResponseRootElementName = "RunInstancesResponse";
    private const string RequestIdElementName = "requestId";
    private const string ReservationIdElementName = "reservationId";
    private const string InstancesSetElementName = "instancesSet";
    private const int SuccessfulExitCode = 0;

    private readonly Ec2Configuration _configuration;
    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;
    private readonly QueryResponseWriter _writer;

    public RunInstancesOperation(Ec2Configuration configuration, InstanceStore store, ContainersSurface containers, QueryResponseWriter writer)
    {
        _configuration = configuration;
        _store = store;
        _containers = containers;
        _writer = writer;
    }

    public string ActionName => RunInstancesActionName;

    public async Task HandleAsync(HttpContext context, IFormCollection form)
    {
        var imageId = form[ImageIdParameterName].ToString();
        if (!_configuration.AmiToImage.ContainsKey(imageId))
        {
            var message = string.Format(CultureInfo.InvariantCulture, ImageNotFoundMessageFormat, imageId);
            await _writer.WriteErrorAsync(context.Response, InvalidImageIdNotFoundCode, message);
            return;
        }

        var imageReference = _configuration.AmiToImage[imageId];
        var instanceType = form[InstanceTypeParameterName].ToString();
        var labels = new Dictionary<string, string> { [AmiLabelKey] = imageId };
        var container = _containers.Start(imageReference, labels);
        var instance = _store.Launch(imageId, instanceType, container.ContainerId, container.Address);

        var encodedUserData = form[UserDataParameterName].ToString();
        if (encodedUserData.Length > 0)
        {
            var script = encodedUserData.DecodeUserData();
            instance = RunUserDataScript(instance, script);
        }

        await _writer.WriteAsync(context.Response, BuildDocument(instance));
    }

    private Ec2Instance RunUserDataScript(Ec2Instance instance, string script)
    {
        var commandResult = _containers.RunPowerShellScript(instance.ContainerId, script);
        var recordedInstance = _store.RecordConsoleOutput(instance, commandResult.StandardOutput);
        if (commandResult.ExitCode == SuccessfulExitCode)
        {
            return recordedInstance;
        }

        return ApplyUserDataFailurePolicy(recordedInstance);
    }

    private Ec2Instance ApplyUserDataFailurePolicy(Ec2Instance instance)
    {
        var policyTerminatesInstance = _configuration.UserDataFailurePolicy == Ec2UserDataFailurePolicy.Terminate;
        if (policyTerminatesInstance)
        {
            _containers.Remove(instance.ContainerId);
            return _store.Terminate(instance);
        }

        return _store.RecordUserDataFailure(instance);
    }

    private XDocument BuildDocument(Ec2Instance instance)
    {
        var xmlNamespace = _writer.XmlNamespace;

        return new XDocument(
            new XElement(xmlNamespace + ResponseRootElementName,
                new XElement(xmlNamespace + RequestIdElementName, _writer.NextRequestId()),
                new XElement(xmlNamespace + ReservationIdElementName, instance.ReservationId),
                new XElement(xmlNamespace + InstancesSetElementName,
                    instance.ToInstanceItem(xmlNamespace))));
    }
}
