using System.Globalization;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using AwsEmulatorSharp.Emulator.S3;

namespace AwsEmulatorSharp.Emulator.Install;

public class InstallPlanGenerator
{
    private const string ObjectKeySeparator = "/";
    private const string QuietSwitch = "/Q";
    private const string IntegrationServicesFeature = "IS";
    private const string SetupCommandLineFormat = "\"{0}\\{1}\" {2} /ACTION=Install /FEATURES={3} /IACCEPTSQLSERVERLICENSETERMS";

    private readonly IObjectStore _objectStore;
    private readonly MediaUrlMinter _mediaUrlMinter;
    private readonly InstallConfiguration _configuration;

    public InstallPlanGenerator(IObjectStore objectStore, MediaUrlMinter mediaUrlMinter, InstallConfiguration configuration)
    {
        _objectStore = objectStore;
        _mediaUrlMinter = mediaUrlMinter;
        _configuration = configuration;
    }

    public InstallPlan For(Ec2Instance instance)
    {
        var mediaObject = FindMediaObject();
        var mediaUrl = _mediaUrlMinter.For(_configuration.MediaBucketName, mediaObject.Key);
        var setupCommandLine = ToSetupCommandLine(mediaObject.Key);

        return new InstallPlan(instance.InstanceId, _configuration.MediaBucketName, mediaObject.Key, mediaUrl, setupCommandLine);
    }

    private StoredObject FindMediaObject()
    {
        var mediaObjects = _objectStore.FindObjects(_configuration.MediaBucketName, _configuration.MediaKeyPrefix);

        return mediaObjects.Last();
    }

    private string ToSetupCommandLine(string mediaKey)
    {
        var mediaFileName = mediaKey.Split(ObjectKeySeparator).Last();
        var setupCommandLine = string.Format(CultureInfo.InvariantCulture, SetupCommandLineFormat, _configuration.MediaFolderPath, mediaFileName, QuietSwitch, IntegrationServicesFeature);

        return setupCommandLine;
    }
}
