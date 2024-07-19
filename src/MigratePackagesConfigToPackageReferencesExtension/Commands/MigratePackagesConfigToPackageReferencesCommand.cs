// <copyright file="MigratePackagesConfigToPackageReferencesCommand.cs" company="Rami Abughazaleh">
//   Copyright (c) Rami Abughazaleh. All rights reserved.
// </copyright>

namespace MigratePackagesConfigToPackageReferencesExtension.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Community.VisualStudio.Toolkit;
    using Community.VisualStudio.Toolkit.DependencyInjection;
    using Community.VisualStudio.Toolkit.DependencyInjection.Core;
    using Microsoft.VisualStudio.Shell;
    using MigratePackagesConfigToPackageReferencesExtension.Services;
    using Parallel = MigratePackagesConfigToPackageReferencesExtension.Threading.Parallel;

    /// <summary>
    /// Migrate packages.config to PackageReferences command.
    /// </summary>
    /// <param name="package">The dependency injection aware extension toolkit package.</param>
    /// <param name="loggingService">The logging service.</param>
    /// <param name="projectService">The project service.</param>
    /// <param name="fileSystemService">The file system service.</param>
    [Command(
        PackageGuids.guidMigratePackagesConfigToPackageReferencesExtensionCmdSetString,
        PackageIds.MigratePackagesConfigToPackageReferencesCommand)]
    internal sealed class MigratePackagesConfigToPackageReferencesCommand(
        DIToolkitPackage package,
        LoggingService loggingService,
        ProjectService projectService,
        FileSystemService fileSystemService)
        : BaseDICommand(package)
    {
        private readonly LoggingService loggingService = loggingService;
        private readonly ProjectService projectService = projectService;
        private readonly FileSystemService fileSystemService = fileSystemService;

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var selectedFiles = await this.GetSelectedPackagesConfigFilesAsync();
                if (!selectedFiles.Any())
                {
                    await this.loggingService.LogErrorAsync("No \"packages.config\" files selected.");
                    return;
                }

                await VS.StatusBar.StartAnimationAsync(StatusAnimation.General);

                await this.MigratePackagesConfigAsync(selectedFiles);

                await this.loggingService.LogInfoAsync($"Migration completed successfully.");
                await VS.StatusBar.ShowMessageAsync("Packages.config migration finished successfully.");
            }
            catch (Exception ex)
            {
                await this.loggingService.LogErrorAsync(ex);
                await VS.StatusBar.ShowMessageAsync("Packages.config migration failed. Please see the Output Window for details.");
            }
            finally
            {
                await VS.StatusBar.EndAnimationAsync(StatusAnimation.General);
            }
        }

        private async Task<IEnumerable<SolutionItem>> GetSelectedPackagesConfigFilesAsync()
        {
            return (await VS.Solutions.GetActiveItemsAsync())
                .Where(x => Path.GetFileName(x.FullPath) == "packages.config");
        }

        private async Task MigratePackagesConfigAsync(IEnumerable<SolutionItem> packagesConfigFiles)
        {
            int count = packagesConfigFiles.Count();

            await VS.StatusBar.ShowMessageAsync($"Migrating packages.config to PackageReferences...");

            await Parallel.ForEachAsync(
                Enumerable.Range(0, count).ToList(),
                maxDegreeOfParallelism: Environment.ProcessorCount,
                new Func<int, Task>(async i =>
                {
                    var packagesConfigItem = packagesConfigFiles.ElementAt(i);

                    await this.MigratePackagesConfigAsync(packagesConfigItem);
                }));
        }

        private async Task MigratePackagesConfigAsync(SolutionItem packagesConfigItem)
        {
            XNamespace defaultNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
            var packageReferences = new XElement(defaultNamespace + "ItemGroup");
            var packagesConfigPath = packagesConfigItem.FullPath;
            var projectPath = packagesConfigItem.Parent.FullPath;
            Project project = await VS.Solutions.GetActiveProjectAsync();

            await this.loggingService.LogInfoAsync($"Migrating \"{packagesConfigPath}\"...");

            // backup packages.config
            await this.fileSystemService.BackupFileAsync(packagesConfigPath);

            // backup project file
            await this.fileSystemService.BackupFileAsync(projectPath);

            // check out files from source control
            await this.projectService.CheckOutFileFromSourceControlAsync(projectPath);
            await this.projectService.CheckOutFileFromSourceControlAsync(packagesConfigPath);

            // load project file
            await this.loggingService.LogDebugAsync($"Loading \"{projectPath}\" ...");
            var projectXmlDocument = XDocument.Load(projectPath);

            // load packages.config
            await this.loggingService.LogDebugAsync($"Loading \"{packagesConfigPath}\" ...");
            var packagesConfigXmlDocument = XDocument.Load(packagesConfigPath);

            // get references to the elements in the project file to remove
            await this.loggingService.LogDebugAsync("Getting `<Reference />` elemeents ...");
            var oldReferences = projectXmlDocument.Root.Descendants().Where(c => c.Name.LocalName == "Reference");

            await this.loggingService.LogDebugAsync("Getting `<Error />` elemeents ...");
            var errors = projectXmlDocument.Root.Descendants().Where(c => c.Name.LocalName == "Error");

            await this.loggingService.LogDebugAsync("Getting `<Import />` elemeents ...");
            var targets = projectXmlDocument.Root.Descendants().Where(c => c.Name.LocalName == "Import");

            foreach (var row in packagesConfigXmlDocument.Root.Elements().ToList())
            {
                // create the new PackageReference
                packageReferences.Add(new XElement(
                    defaultNamespace + "PackageReference",
                    new XAttribute("Include", row.Attribute("id").Value),
                    new XAttribute("Version", row.Attribute("version").Value)));

                // remove the old standard reference
                await this.loggingService.LogDebugAsync("Removing `<Reference Include />` elemeents ...");
                oldReferences.Where(c => c.Attribute("Include").Value.Split([','])[0].ToLower() == row.Attribute("id").Value.ToLower()).ToList()
                    .ForEach(c => c.Remove());

                // remove any remaining standard references where the PackageId is in the inner text
                await this.loggingService.LogDebugAsync("Removing `<Reference />` elemeents ...");
                oldReferences.Where(c => c.Descendants().Any(d => d.Value.Contains(row.Attribute("id").Value))).ToList()
                    .ForEach(c => c.Remove());

                // remove any Error conditions for missing package targets
                await this.loggingService.LogDebugAsync("Removing `<Error />` elemeents ...");
                errors.Where(c => c.Attribute("Condition").Value.Contains(row.Attribute("id").Value)).ToList()
                    .ForEach(c => c.Remove());

                // remove any package targets
                await this.loggingService.LogDebugAsync("Removing `<Import Project />` elemeents ...");
                targets.Where(c => c.Attribute("Project").Value.Contains(row.Attribute("id").Value)).ToList()
                    .ForEach(c => c.Remove());
            }

            // add new PackageReferences
            await this.loggingService.LogDebugAsync("Adding `<PackageReference />` ...");
            projectXmlDocument.Root.Elements().Last().AddAfterSelf(packageReferences);

            // remove packages.config
            var packageConfigReference = projectXmlDocument.Root.Descendants().FirstOrDefault(c => c.Name.LocalName == "None" && c.Attribute("Include").Value == "packages.config");
            if (packageConfigReference != null)
            {
                await this.loggingService.LogDebugAsync("Removing `<None Include=\"packages.config\" />` ...");
                packageConfigReference.Remove();
            }

            // remove empty targets
            var nugetBuildImports = projectXmlDocument.Root.Descendants().FirstOrDefault(c => c.Name.LocalName == "Target" && c.Attribute("Name").Value == "EnsureNuGetPackageBuildImports");
            if (nugetBuildImports != null && nugetBuildImports.Descendants().Count(c => c.Name.LocalName == "Error") == 0)
            {
                await this.loggingService.LogDebugAsync("Removing `<Target Name=\"EnsureNuGetPackageBuildImports\" />` ...");
                nugetBuildImports.Remove();
            }

            // save the project
            await this.loggingService.LogDebugAsync($"Saving \"{projectPath}\" ...");
            projectXmlDocument.Save(projectPath, SaveOptions.None);

            // delete packages.config
            await this.projectService.DeleteFileFromProjectAsync(packagesConfigPath);

            // unload project
            await this.loggingService.LogDebugAsync($"Unloading \"{projectPath}\" ...");
            await project.UnloadAsync();

            // reload project
            await this.loggingService.LogDebugAsync($"Loading \"{projectPath}\" ...");
            await project.LoadAsync();
        }
    }
}
