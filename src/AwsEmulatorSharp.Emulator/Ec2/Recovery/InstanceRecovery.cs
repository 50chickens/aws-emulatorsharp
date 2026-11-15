using AwsEmulatorSharp.Emulator.Containers;
using AwsEmulatorSharp.Emulator.Ec2.Lifecycle;
using Microsoft.Extensions.Hosting;

namespace AwsEmulatorSharp.Emulator.Ec2.Recovery;

public class InstanceRecovery : IHostedService
{
    private readonly Ec2Configuration _configuration;
    private readonly InstanceStore _store;
    private readonly ContainersSurface _containers;

    public InstanceRecovery(Ec2Configuration configuration, InstanceStore store, ContainersSurface containers)
    {
        _configuration = configuration;
        _store = store;
        _containers = containers;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Recover();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public IReadOnlyList<Ec2Instance> Recover()
    {
        var labelledContainers = _containers.FindByLabel(_configuration.AmiLabelKey);
        var adoptableContainers = labelledContainers.Where(IsAdoptable).ToList();
        var recoveredInstances = adoptableContainers.Select(Adopt).ToList();
        var orphanContainerIds = labelledContainers
            .Except(adoptableContainers)
            .Select(container => container.ContainerId)
            .ToList();
        orphanContainerIds.ForEach(_containers.Remove);

        return recoveredInstances;
    }

    private bool IsAdoptable(ContainerInstance container)
    {
        var labels = _containers.Labels(container.ContainerId);
        var isAdoptable = _configuration.AmiToImage.ContainsKey(labels[_configuration.AmiLabelKey]);

        return isAdoptable;
    }

    private Ec2Instance Adopt(ContainerInstance container)
    {
        var labels = _containers.Labels(container.ContainerId);
        var instance = _store.Launch(labels[_configuration.AmiLabelKey], labels[_configuration.InstanceTypeLabelKey], container.ContainerId, container.Address);

        return instance;
    }
}
