# Migrate packages.config to PackageReferences

This Visual Studio extension migrates packages.config to PackageReferences.

It works with C# csproj and C++ vcxproj Visual Studio project files.

[Download it from the Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=RamiAbughazaleh.MigratePackagesConfigToPackageReferencesExtension)

## Getting started

1. Install the extension
2. Right-click on `packages.config` and select `Migrate packages.config to PackageReferences...`.

![Preview](Preview.png)

3. Wait until the process finishes.  
  Check the status bar or the `Migrate packages.config to PackageReferences Extension` pane in the `Output Window` for details.


## Technical Details

This extension will first create a backup of the project and `packages.config` files.  
For example, `MyProject.vcxproj` and `packages.config` will be copied to `MyProject.vcxproj.bak` and `packages.config.bak` respectively.  

If the project and `packages.config` files are source controlled, they will be checked out for modification.  

The NuGet packages referenced in `packages.config` will be migrated to the project file in the new `PackageReference` format.  
For example, the following elements in the `vcxproj` project file:
```
  <Import Project="..\packages\MyPackage.1.0.0\build\MyPackage.props" Condition="Exists('..\packages\MyPackage.1.0.0\build\MyPackage.props')" />

  <ItemGroup>
    <None Include="packages.config" />
  </ItemGroup>

  <Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild">
    <PropertyGroup>
      <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.</ErrorText>
    </PropertyGroup>
    <Error Condition="!Exists('..\packages\MyPackage.1.0.0\build\MyPackage.props')" Text="$([System.String]::Format('$(ErrorText)', '..\packages\MyPackage.1.0.0\build\MyPackage.props'))" />
  </Target>
```

will be converted to this:
```
  <ItemGroup>
    <PackageReference Include="MyPackage" Version="1.0.0" />
  </ItemGroup>
```


After the project file has been updated, `packages.config` will be deleted, and the project reloaded.


## Troubleshooting

Check the `Migrate packages.config to PackageReferences Extension` pane in the `Output Window` for detailed logs.


## Rate and Review

Has this extension helped you at all?

If so, please rate and share it.

Thank you! :)