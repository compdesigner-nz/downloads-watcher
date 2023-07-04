namespace downloads_watcher_service
{
    public class DownloadWorker : BackgroundService
    {
        private readonly ILogger<DownloadWorker> _logger;
        private readonly DownloadService _downloadService;
        private FileSystemWatcher _watcher;

        private NotifyFilters _filter = NotifyFilters.FileName
            | NotifyFilters.DirectoryName
            | NotifyFilters.Attributes
            | NotifyFilters.CreationTime
            | NotifyFilters.Security
            | NotifyFilters.Size;
        public DownloadWorker(DownloadService downloadService, ILogger<DownloadWorker> logger)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            _downloadService = downloadService;
            _downloadService.DownloadsPath = path;
            _downloadService.CreateFolders(path, logger);

            _logger = logger;
            _watcher = new FileSystemWatcher(path);
            _watcher.EnableRaisingEvents = true;
            _watcher.IncludeSubdirectories = false;
            _watcher.NotifyFilter = _filter;

            _watcher.Created += OnFileCreated;
            _watcher.Error += OnError;
        }

        private void LogFileInfo(FileSystemEventArgs e)
        {
            _logger.LogInformation($"Moving {e.Name} from {e.FullPath}");
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            _downloadService.OnError(sender, e, _logger);
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            LogFileInfo(e);
            _downloadService.OnCreated(sender, e, _logger);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _watcher.EndInit();
            _watcher.Dispose();
            base.Dispose();
        }
    }
}