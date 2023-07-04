using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace downloads_watcher_service
{
    public sealed class DownloadService
    {
        public string DownloadsPath;

        private const string ImagesFolder = "Images";
        private const string InstallersFolder = "Installers";
        private const string ZipFolder = "Zips";
        private const string DocumentsFolder = "Documents";
        private const string ExecutablesFolder = "Executables";
        private const string DirectoriesFolder = "Folders";

        /// <summary>
        /// This array contains all image extensions handled by the service
        /// </summary>
        private static string[] ImagesExtensions = new string[]
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".tif"
        };

        /// <summary>
        /// This array contains all installer extensions handled by the service
        /// </summary>
        private static string[] InstallerExtensions = new string[]
        {
            ".msi"
        };

        /// <summary>
        /// This array contains all zip extensions handled by the service
        /// </summary>
        private static string[] ZipExtensions = new string[]
        {
            ".zip",
            ".rar",
            ".7z"
        };

        /// <summary>
        /// This array contains all document extensions handled by the service
        /// </summary>
        private static string[] DocumentExtensions = new string[]
        {
            ".docx",
            ".doc",
            ".xlsx",
            ".xls",
            ".pptx",
            ".ppt",
            ".csv",
            ".tsv",
            ".pdf",
            ".txt"
        };

        /// <summary>
        /// This array containsa all executable extensions handled by the service
        /// </summary>
        private static string[] ExecutablesExtensions = new string[] { ".exe" };

        public void CreateFolders(string rootDownload, ILogger logger)
        {
            if (Directory.Exists(rootDownload))
            {
                var imagesPath = Path.Combine(rootDownload, ImagesFolder);
                var installersPath = Path.Combine(rootDownload, InstallersFolder);
                var zipPath = Path.Combine(rootDownload, ZipFolder);
                var documentsPath = Path.Combine(rootDownload, DocumentsFolder);
                var exePath = Path.Combine(rootDownload, ExecutablesFolder);

                CreateDirectory(imagesPath, logger);
                CreateDirectory(installersPath, logger);
                CreateDirectory(zipPath, logger);
                CreateDirectory(documentsPath, logger);
                CreateDirectory(exePath, logger);
            }
            else
            {
                logger.LogError("The downloads folder doesn't exist for the user");
                Environment.Exit(1);
            }
        }

        private void CreateDirectory(string path, ILogger logger)
        {
            if (Directory.Exists(path))
            {
                logger.LogInformation($"The directory at {path} already exists");
            }
            else
            {
                Directory.CreateDirectory(path);
                logger.LogInformation($"Created the {Path.GetDirectoryName(path)} directory at {path}");
            }
        }
        public void OnError(object sender, ErrorEventArgs e, ILogger logger)
        {
            logger.LogError(e.GetException(), e.GetException().Message, e.GetException().StackTrace);
        }

        public void OnRenamed(object sender, RenamedEventArgs e, ILogger logger)
        {
            try
            {
                var res = HandleFileModified(sender, e);
                if (res)
                {
                    logger.LogInformation("File handled");
                }
                else
                {
                    logger.LogInformation("File not handled - skipping");
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, ex.Message, ex.StackTrace);
            }
            
        }

        public void OnCreated(object sender, FileSystemEventArgs e, ILogger logger)
        {
            try
            {
                var res = HandleFileModified(sender, e);
                if (res)
                {
                    logger.LogInformation("File handled");
                }
                else
                {
                    logger.LogInformation("File not handled - skipping");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message, ex.StackTrace);
            }
        }

        private bool HandleFileModified(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                var res = HandleFile(e.FullPath);
                return res;
            }
            else if (Directory.Exists(e.FullPath))
            {
                var res = HandleDirectory(e.FullPath);
                return res;
            }

            return false;
        }

        private bool HandleDirectory(string directoryPath, bool keepTogether = true)
        {
            if (keepTogether)
            {
                var directoryFolder = System.IO.Path.Combine(DownloadsPath, DirectoriesFolder);
                Directory.Move(directoryPath, directoryFolder);
                return true;
            }
            else
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                var results = new List<bool>();
                foreach(string filePath in files)
                {
                    HandleFile(filePath);
                    results.Add(true);
                }
                if (results.Any(x => x == false)) return false;
                return true;
            }
        }

        private bool HandleFile(string? fullPath)
        {
            var extension = System.IO.Path.GetExtension(fullPath);
            if (string.IsNullOrEmpty(extension)) return false;

            if (ImagesExtensions.Contains(extension.ToLower()))
            {
                MoveToImagesFolder(fullPath);
                return true;
            }
            else if (InstallerExtensions.Contains(extension.ToLower()))
            {
                MoveToInstallersFolder(fullPath);
                return true;
            }
            else if (ZipExtensions.Contains(extension.ToLower()))
            {
                MoveToZipsFolder(fullPath);
                return true;
            }
            else if (DocumentExtensions.Contains(extension.ToLower()))
            {
                MoveToDocumentsFolder(fullPath);
                return true;
            }
            else if (ExecutablesExtensions.Contains(extension.ToLower()))
            {
                MoveToExecutablesFolder(fullPath);
                return true;
            }
            return false;
        }

        private void MoveToExecutablesFolder(string? fullPath)
        {
            var executablesFolder = System.IO.Path.Combine(DownloadsPath, ExecutablesFolder);
            MoveFile(fullPath, executablesFolder);
        }

        private void MoveToDocumentsFolder(string? fullPath)
        {
            var documentsFolder = System.IO.Path.Combine(DownloadsPath, DocumentsFolder);
            MoveFile(fullPath, documentsFolder);
        }

        private void MoveToZipsFolder(string? fullPath)
        {
            var zipsFolder = System.IO.Path.Combine(DownloadsPath, ZipFolder);
            HandleZip(fullPath, zipsFolder);
        }

        private void HandleZip(string? fullPath, string zipsFolder)
        {
            var folderName = System.IO.Path.GetDirectoryName(fullPath);
            var newFolderPath = System.IO.Path.Combine(zipsFolder, folderName);
            ZipFile.ExtractToDirectory(fullPath, newFolderPath);
            
        }

        /// <summary>
        /// This method moves a file to the installers folder
        /// </summary>
        /// <param name="fullPath"></param>
        private void MoveToInstallersFolder(string? fullPath)
        {
            var installerFolder = System.IO.Path.Combine(DownloadsPath, InstallersFolder);
            MoveFile(fullPath, installerFolder);
        }

        /// <summary>
        /// This method moves a file to the images folder
        /// </summary>
        /// <param name="fullPath">The filepath of the image</param>
        private void MoveToImagesFolder(string? fullPath)
        {
            var imagesFolder = System.IO.Path.Combine(DownloadsPath, ImagesFolder);
            MoveFile(fullPath, imagesFolder);
        }

        /// <summary>
        /// This method moves a file from its old location to its new directory.
        /// </summary>
        /// <param name="oldFilePath">The current filepath</param>
        /// <param name="newDirectory">The new directory</param>
        private void MoveFile(string? oldFilePath, string? newDirectory)
        {
            var fileName = System.IO.Path.GetFileName(oldFilePath);
            var newFilePath = System.IO.Path.Combine(newDirectory, fileName);

            File.Move(oldFilePath, newFilePath);
        }
    }
}
