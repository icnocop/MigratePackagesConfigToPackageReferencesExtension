// <copyright file="FileSystemService.cs" company="Rami Abughazaleh">
//   Copyright (c) Rami Abughazaleh. All rights reserved.
// </copyright>

namespace MigratePackagesConfigToPackageReferencesExtension.Services
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides support for operations on the file system.
    /// </summary>
    public class FileSystemService(LoggingService loggingService)
    {
        private readonly LoggingService loggingService = loggingService;

        /// <summary>
        /// Removes the read-only attribute on the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task RemoveReadOnlyAttributeAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            FileAttributes attributes = File.GetAttributes(filePath);
            if ((attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
            {
                return;
            }

            // make the file read/write
            await this.loggingService.LogDebugAsync($"Removing read-only flag on file \"{filePath}\"...");

            attributes &= ~FileAttributes.ReadOnly;
            File.SetAttributes(filePath, attributes);
        }

        /// <summary>
        /// Backs up the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task BackupFileAsync(string filePath)
        {
            string backupFilePath = $"{filePath}.bak";

            await this.RemoveReadOnlyAttributeAsync(backupFilePath);

            await this.loggingService.LogDebugAsync($"Copying \"{filePath}\" to \"{backupFilePath}\"...");

            File.Copy(filePath, backupFilePath, true);
        }
    }
}
