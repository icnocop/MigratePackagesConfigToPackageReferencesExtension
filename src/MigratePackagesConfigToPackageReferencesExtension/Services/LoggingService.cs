// <copyright file="LoggingService.cs" company="Rami Abughazaleh">
//   Copyright (c) Rami Abughazaleh. All rights reserved.
// </copyright>

namespace MigratePackagesConfigToPackageReferencesExtension.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Community.VisualStudio.Toolkit;

    /// <summary>
    /// Provides support for logging messages to the Visual Studio output window.
    /// </summary>
    /// <param name="name">The name of the output window pane.</param>
    public class LoggingService(string name)
    {
        private readonly string name = name;
        private OutputWindowPane pane;

        /// <summary>
        /// Logs a message in the debugging category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task LogDebugAsync(string message)
        {
            await this.LogAsync("DEBUG", message);
        }

        /// <summary>
        /// Logs a message in the information category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task LogInfoAsync(string message)
        {
            await this.LogAsync("INFO", message);
        }

        /// <summary>
        /// Logs an exception in the warning category.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task LogWarningAsync(Exception ex)
        {
            await this.LogAsync("WARN", ex.ToString());
        }

        /// <summary>
        /// Logs an exception in the error category.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task LogErrorAsync(Exception ex)
        {
            await this.LogErrorAsync(ex.ToString());
        }

        /// <summary>
        /// Logs a message in the error category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task LogErrorAsync(string message)
        {
            await this.LogAsync("ERROR", message);
        }

        private async Task LogAsync(string category, string message)
        {
            await this.LogAsync($"{category}: {message}");
        }

        private async Task LogAsync(string message)
        {
            try
            {
                this.pane ??= await VS.Windows.CreateOutputWindowPaneAsync(this.name);

                await this.pane?.WriteLineAsync($"{DateTime.Now}: {message}");
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }
    }
}
