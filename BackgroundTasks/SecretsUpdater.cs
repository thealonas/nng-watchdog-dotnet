using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nng_watchdog.API;
using nng_watchdog.Providers;
using nng.DatabaseProviders;
using Sentry;

namespace nng_watchdog.BackgroundTasks;

public class SecretsUpdater : BackgroundService
{
    private readonly GroupsDatabaseProvider _groups;
    private readonly ILogger<SecretsUpdater> _logger;
    private readonly GroupSecretsProvider _secrets;
    private readonly VkProcessor _vkProcessor;

    private Timer? _timer;

    public SecretsUpdater(GroupSecretsProvider secrets, VkProvider provider, ILogger<SecretsUpdater> logger,
        GroupsDatabaseProvider groups)
    {
        _secrets = secrets;
        _vkProcessor = provider.VkProcessor;
        _logger = logger;
        _groups = groups;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(delegate
        {
            try
            {
                Update();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                throw;
            }
        }, null, TimeSpan.Zero, TimeSpan.FromHours(24));

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private void Update()
    {
        var allGroups = _groups.Collection.ToList().Select(x => x.GroupId);
        var allSecrets = _secrets.Collection.ToList();

        var groupsWithSecret = allSecrets.Select(x => x.GroupId);

        var groupsWithoutSecret = allGroups.Where(x => !groupsWithSecret.Any(y => y.Equals(x))).ToList();

        if (!groupsWithoutSecret.Any())
        {
            _logger.LogInformation("Сикреты обновлять не требуется");
            return;
        }

        _logger.LogInformation("Групп на обновление: {Count}", groupsWithoutSecret.Count);

        var secrets = _vkProcessor.GetSecrets(groupsWithoutSecret);

        foreach (var (group, secret) in secrets)
        {
            _logger.LogInformation("Обновляю группу {Group}", group);
            var data = allSecrets[(int) group];
            data.Secret = secret;
            _secrets.Collection.Update(data);
        }

        _logger.LogInformation("Обновление завершено");
    }
}
