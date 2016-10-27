using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GogsActiveDirectorySync
{
    public class GroupSyncService
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task backgroundTask;

        public void Start()
        {
            if (cancellationTokenSource != null) throw new InvalidOperationException("Already started");

            cancellationTokenSource = new CancellationTokenSource();
            backgroundTask = RunAsync(cancellationTokenSource.Token);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Starting service...");

            var logProgress = new LogProgress(logger);

            var appConfiguration = new AppConfiguration();
            appConfiguration.LoadConfigFile();

            if (appConfiguration.GogsAccessToken == null && appConfiguration.EnableGogsAccessTokenGeneration)
            {
                var gogsAccessTokenGenerator = new GogsAccessTokenGenerator(appConfiguration);
                var gogsAccessTokens = await gogsAccessTokenGenerator.CreateOrGetAccessTokensAsync();
                if (gogsAccessTokens.Any())
                {
                    appConfiguration.GogsAccessToken = gogsAccessTokens.First().Sha1;
                }
            }

            // Force a minimum wait interval (we probably don't want this thing running constantly)
            var syncInterval = appConfiguration.SyncInterval;
            if (syncInterval.TotalMinutes < 1)
            {
                syncInterval = TimeSpan.FromMinutes(1);
            }

            var synchronizer = new GroupSynchronizer(appConfiguration);
            while (!cancellationToken.IsCancellationRequested)
            {
                // If we have a min/max time, then we'll wait for a valid time period.
                var waitTime = TimeUtils.CalculateWaitTime(DateTime.Now.TimeOfDay, appConfiguration.MinimumTimeOfDay, appConfiguration.MaximumTimeOfDay);
                if (waitTime.HasValue)
                {
                    await Task.Delay(waitTime.Value, cancellationToken);
                }

                try
                {
                    logger.Info("Starting sync...");
                    await synchronizer.SynchronizeAsync(progress: logProgress, cancellationToken: cancellationToken);
                    logger.Info("Sync completed successfully.");
                    await Task.Delay(syncInterval, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    logger.Info("Sync canceled.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Sync failed.");
                    await Task.Delay(syncInterval, cancellationToken: cancellationToken);
                }
            }
        }

        private class LogProgress : IProgress<string>
        {
            private readonly ILogger logger;

            public LogProgress(ILogger logger)
            {
                if (logger == null) throw new ArgumentNullException(nameof(logger));
                this.logger = logger;
            }

            public void Report(string value)
            {
                logger.Info(value);
            }
        }

        public void Stop()
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Stopping service...");

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;
            }

            if (backgroundTask != null)
            {
                try
                {
                    backgroundTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException ex)
                {
                    logger.Debug(ex, "Canceled while stopping the service");
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, ex.Message);
                }
                backgroundTask = null;
            }

            logger.Info("Service stopped");
        }
    }
}
