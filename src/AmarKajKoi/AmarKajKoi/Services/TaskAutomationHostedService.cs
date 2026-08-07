using AmarKajKoi.Database;
using AmarKajKoi.Entities;
using AmarKajKoi.ServicesInterface;

namespace AmarKajKoi.Services
{
    /// <summary>
    /// Background worker that periodically runs SRS-required automatic actions:
    ///   FR-29: mark tasks Overdue when the due date passes
    ///   FR-30: notify management about tasks stuck Overdue for more than 2 days
    ///   FR-17: escalate Voice Review items pending for more than 24 hours to Top Management
    ///   FR-39: purge voice files once they pass the retention window (1 year by default)
    ///
    /// Runs every 10 minutes; heavier checks (escalations) are throttled by de-duplication
    /// against the notifications table to avoid spamming.
    /// </summary>
    public class TaskAutomationHostedService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

        /// <summary>Voice retention is a slow-moving job; running it once a day is enough.</summary>
        private static readonly TimeSpan RetentionSweepEvery = TimeSpan.FromHours(24);

        /// <summary>Cap per sweep so one run cannot lock the table for long.</summary>
        private const int RetentionBatchSize = 200;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaskAutomationHostedService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly int _voiceRetentionDays;

        private DateTime _lastRetentionSweepUtc = DateTime.MinValue;

        public TaskAutomationHostedService(IServiceScopeFactory scopeFactory,
                                           ILogger<TaskAutomationHostedService> logger,
                                           IWebHostEnvironment env,
                                           IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _env = env;
            _voiceRetentionDays = config.GetValue<int?>("VoiceRetention:Days") ?? 365;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Small startup delay so the API is ready to serve first.
            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); } catch { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TaskAutomationHostedService iteration failed");
                }

                try { await Task.Delay(Interval, stoppingToken); } catch { break; }
            }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // No HTTP request here, so RealtimeNotificationFilter never runs: this loop
            // flushes the badge updates itself after its own commits.
            var realtime = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

            // ---- FR-29: mark tasks Overdue ----
            uow.Begin();
            try
            {
                var overdueCount = await uow.Tasks.MarkOverdueDueTasksAsync();
                uow.Commit();
                if (overdueCount > 0)
                    _logger.LogInformation("Auto-marked {Count} tasks as Overdue", overdueCount);
            }
            catch { uow.Rollback(); throw; }

            // ---- FR-30: notify management about tasks Overdue > 2 days ----
            var escalations = await uow.Tasks.GetOverdueForEscalationAsync(overdueDays: 2);
            if (escalations.Count > 0)
            {
                var management = await uow.Users.GetByRoleAsync("TopManagement");
                if (management.Count > 0)
                {
                    var toInsert = new List<Notification>();
                    foreach (var t in escalations)
                    {
                        // De-dup: only escalate if we haven't sent one in the last 24 hours for this task.
                        if (await AlreadyNotifiedRecentlyAsync(uow, t.TaskId, "Overdue > 2 days", TimeSpan.FromHours(24)))
                            continue;
                        toInsert.AddRange(management.Select(u => new Notification
                        {
                            UserId = u.UserId,
                            TaskId = t.TaskId,
                            Title = "Overdue > 2 days",
                            Body = $"'{t.TaskName}' assigned to {t.AssignedToName ?? "-"} is overdue for more than 2 days."
                        }));
                    }
                    if (toInsert.Count > 0)
                    {
                        uow.Begin();
                        try { await uow.Notifications.CreateManyAsync(toInsert); uow.Commit(); }
                        catch { uow.Rollback(); throw; }

                        realtime.QueueUnreadRefresh(toInsert.Select(n => n.UserId));
                        await realtime.FlushAsync();
                    }
                }
            }

            // ---- FR-17: escalate PendingVoiceReview > 24 hrs to Top Management ----
            var stalePendingReview = await uow.Tasks.GetPendingVoiceReviewOlderThanAsync(hours: 24);
            if (stalePendingReview.Count > 0)
            {
                var management = await uow.Users.GetByRoleAsync("TopManagement");
                if (management.Count > 0)
                {
                    var toInsert = new List<Notification>();
                    foreach (var t in stalePendingReview)
                    {
                        if (await AlreadyNotifiedRecentlyAsync(uow, t.TaskId, "Voice review pending > 24h", TimeSpan.FromHours(24)))
                            continue;
                        toInsert.AddRange(management.Select(u => new Notification
                        {
                            UserId = u.UserId,
                            TaskId = t.TaskId,
                            Title = "Voice review pending > 24h",
                            Body = $"Voice target '{t.TaskName}' has been awaiting review for over 24 hours."
                        }));
                    }
                    if (toInsert.Count > 0)
                    {
                        uow.Begin();
                        try { await uow.Notifications.CreateManyAsync(toInsert); uow.Commit(); }
                        catch { uow.Rollback(); throw; }

                        realtime.QueueUnreadRefresh(toInsert.Select(n => n.UserId));
                        await realtime.FlushAsync();
                    }
                }
            }

            // ---- FR-39: purge voice files past the retention window ----
            if (DateTime.UtcNow - _lastRetentionSweepUtc >= RetentionSweepEvery)
            {
                await PurgeExpiredVoiceFilesAsync(uow);
                _lastRetentionSweepUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// FR-39: voice files are kept for <c>VoiceRetention:Days</c> (365 by default). Anything
        /// older loses both its database row and the file on disk. A retention of 0 or less
        /// disables the sweep so an operator can opt out via configuration.
        /// </summary>
        private async Task PurgeExpiredVoiceFilesAsync(IUnitOfWork uow)
        {
            if (_voiceRetentionDays <= 0) return;

            var cutoff = DateTime.UtcNow.AddDays(-_voiceRetentionDays);
            var expired = await uow.VoiceFiles.GetOlderThanAsync(cutoff, RetentionBatchSize);
            if (expired.Count == 0) return;

            var storageRoot = Path.Combine(_env.ContentRootPath, "UploadedVoiceFiles");
            var deleted = 0;

            foreach (var v in expired)
            {
                uow.Begin();
                try
                {
                    await uow.VoiceFiles.DeleteAsync(v.VoiceFileId);
                    uow.Commit();
                }
                catch (Exception ex)
                {
                    uow.Rollback();
                    _logger.LogWarning(ex, "Could not purge voice file {VoiceFileId}", v.VoiceFileId);
                    continue;
                }

                // Only touch the disk once the row is gone, so a failed delete never
                // leaves a database row pointing at a missing file.
                try
                {
                    var path = Path.Combine(storageRoot, v.StoragePath);
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Purged row for voice file {VoiceFileId} but could not delete {Path}",
                                       v.VoiceFileId, v.StoragePath);
                }

                deleted++;
            }

            _logger.LogInformation("Voice retention sweep removed {Count} file(s) older than {Days} days",
                                   deleted, _voiceRetentionDays);
        }

        private static async Task<bool> AlreadyNotifiedRecentlyAsync(IUnitOfWork uow, Guid taskId, string title, TimeSpan within)
        {
            const string sql = @"
                SELECT COUNT(1) FROM dbo.Notifications
                WHERE TaskId = @taskId AND Title = @title
                  AND CreatedAt >= DATEADD(SECOND, -@sec, SYSUTCDATETIME());";
            var count = await Dapper.SqlMapper.ExecuteScalarAsync<int>(uow.Connection, sql,
                new { taskId, title, sec = (int)within.TotalSeconds }, uow.Transaction);
            return count > 0;
        }
    }
}
