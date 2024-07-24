// <copyright file="MigratePackagesConfigToPackageReferencesExtensionPackage.cs" company="Rami Abughazaleh">
//   Copyright (c) Rami Abughazaleh. All rights reserved.
// </copyright>

namespace MigratePackagesConfigToPackageReferencesExtension
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Community.VisualStudio.Toolkit;
    using Community.VisualStudio.Toolkit.DependencyInjection.Microsoft;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.Shell;
    using MigratePackagesConfigToPackageReferencesExtension.Services;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Migrate packages.config to PackageReferences extension package.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidMigratePackagesConfigToPackageReferencesExtensionString)]
    [ProvideUIContextRule(
        contextGuid: PackageGuids.guidMigratePackagesConfigToPackageReferencesExtensionUIRuleString,
        name: "packages.config files",
        expression: "packages.config",
        termNames: ["packages.config"],
        termValues: ["HierSingleSelectionName:packages.config$"])]
    public sealed class MigratePackagesConfigToPackageReferencesExtensionPackage
        : MicrosoftDIToolkitPackage<MigratePackagesConfigToPackageReferencesExtensionPackage>
    {
        /// <inheritdoc/>
        protected override void InitializeServices(IServiceCollection services)
        {
            base.InitializeServices(services);

            // register services
            services.AddSingleton<FileSystemService>();

            services.AddSingleton((serviceProvider)
                => new LoggingService("Migrate packages.config to PackageReferences Extension"));

            services.AddSingleton((serviceProvider)
                => new ProjectService(
                    VS.GetRequiredService<DTE, DTE2>(),
                    serviceProvider.GetRequiredService<LoggingService>(),
                    serviceProvider.GetRequiredService<FileSystemService>()));

            // register commands
            services.RegisterCommands(ServiceLifetime.Singleton);
        }

        /// <inheritdoc/>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
        }
    }
}