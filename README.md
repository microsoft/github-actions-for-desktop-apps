# DevOps for Windows Desktop Apps Using GitHub Actions

### Create CI/CD workflows for WPF and Windows Forms Applications built on .Net Core 3.x

This repo contains a sample application to demonstrate how to create CI/CD pipelines using [GitHub Actions] (https://github.com/features/actions "GitHub Actions page"). 

With GitHub Actions, you can quickly and easily automate your software workflows with CI/CD.
* Integrate code changes directly into GitHub to speed up development cycles
* Trigger builds to quickly identify breaking changes and create testable debug builds
* Continuously run tests to identify and eliminate bugs, improving code quality 
* Automatically build, sign, package and deploy branches that pass CI 

Build, test, and deploy your code entirely within GitHub.  

![Wpf Continuous Integration](https://github.com/microsoft/github-actions-for-desktop-apps/workflows/Wpf%20Continuous%20Integration/badge.svg)

![Wpf Continuous Delivery](https://github.com/microsoft/github-actions-for-desktop-apps/workflows/Wpf%20Continuous%20Delivery/badge.svg)

## Workflows

To take advantage of GitHub Actions, workflows are defined in YAML files that are in a .github/workflows folder. 
In our project, we define two workflows:
* .github/workflows/ci.yml
* .github/workflows/cd.yml

The ci.yml file defines our continuous integration workflow which we will use to build, test, and create a package every time a developer pushes code to the repo.  Because we want to take advantage of testing every time we push a code change in order to ensure better code quality, we execute tests and create a testable build every time we ```git push```.

Our CI workflow takes advantage of GitHub’s workflow syntax for setting up a build matrix in order to build, test and package multiple build configurations.  [Learn how to configure a build matrix.](https://help.github.com/en/actions/configuring-and-managing-workflows/configuring-a-workflow#configuring-a-build-matrix, "Configuring a build matrix page")

Because ci.yml is triggered on every push, we keep the workflow relatively lightweight by only testing those configurations that are necessary to ensure good quality.  This minimizes the amount of time necessary to build, test and get results.

The cd.yml file defines our continuous delivery workflow.  With this workflow, we build, sign, package and archive our release assets for every configuration we plan to release.  

We also define a build matrix that produces artifacts for three channels: development (Dev), production sideload (Prod_Sideload) and also production for the [Microsoft Store](https://www.microsoft.com/en-us/store/apps/windows "Microsoft Store home page") (Prod_Store). In this example, each channel is built for two configurations: x86 and x64.  However, arm or arm64 are valid configurations as well.

A build matrix can be created to execute jobs across multiple operating systems, build configurations or different supported versions of a programming language. With GitHub Actions, you can define incredibly complex build matrices that can generate up to 256 builds per run!   For more information, see GitHub's [Workflow Syntax for GitHub Actions](https://help.github.com/en/actions/reference/workflow-syntax-for-github-actions#jobsjob_idstrategy "Workflow Syntax for GitHub Actions page").


### ci.yml: Build, test, package, and save package artifacts.

The CI workflow defines the Package.Identity.Name in the Windows Application Packaging project’s Package.appxmanifest to identify the application as "MyWPFApp.DevOpsDemo.Local." By suffixing the application name with ".Local," developers are able to install it side by side with other channels of the app.
```yaml
  <Identity
    Name="MyWPFApp.DevOpsDemo.Local"
    Publisher="CN=GitHubActionsDemo"
    Version="0.0.1.0" />
```

On every push to the repo, we take advantage of the [setup-dotnet](https://github.com/actions/setup-dotnet "Setup dotnet GitHub Action") GitHub Action and install the [dotnet core cli](https://github.com/dotnet/cli "DotNet Core CLI page") environment. Then we add [MSBuild](https://github.com/microsoft/setup-msbuild "MSBuild GitHub Action page") to the PATH and execute unit tests using the [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test "DotNet test page") runner console application.
```yaml
    - name: Install .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 3.1.100

    # Add  MsBuild to the PATH: https://github.com/microsoft/setup-msbuild
    - name: Setup MSBuild.exe
      uses: microsoft/setup-msbuild@v1.0.0
      
    # Test
    - name: Execute Unit Tests
      run: dotnet test $env:Test_Project_Path
```

As mentioned above, we are able to target multiple platforms by authoring the workflow file to define a build matrix, a set of different configurations that are each run in a fresh instance of a virtual environment by the [GitHub-hosted runner](https://help.github.com/en/actions/getting-started-with-github-actions/core-concepts-for-github-actions#github-hosted-runner "GitHub Hosted Runner page").

In the continuous integration workflow, we create a release build for x86 and x64 that runs on the latest windows OS installed on the GitHub-hosted runners.  We also define [environment variables](https://help.github.com/en/actions/configuring-and-managing-workflows/using-environment-variables "Configuring and Managing Workflows Using Environment Variables page") for use by the GitHub Actions workflow run.  In our case, we define variables common to both runs defined in the matrix such as the signing certificate name, the relative path to the solution file and the Windows Application Packaging project name.
 

```yaml
    strategy:
      matrix:
        targetplatform: [x86, x64]

    runs-on: windows-latest

    env:
      SigningCertificate: GitHubActionsDemo.pfx
      Solution_Path: MyWpfApp.sln
      Test_Project_Path: MyWpfApp.Tests\MyWpfApp.Tests.csproj
      Wpf_Project_Path: MyWpfApp\MyWpfApp.csproj
      Wap_Project_Directory: MyWpfApp.Package
      Wap_Project_Name: MyWpfApp.Package.wapproj
```

Next, we execute the unit tests in MYWPFApp.Tests by calling ‘donet test’.
```yaml
    # Test
    - name: Execute Unit Tests
      run: dotnet test $env:Test_Project_Path
```


After executing the tests, we restore the application while passing in the RuntimeIdentifier parameter in order to populate the obj folder with the appropriate platform dependencies for use during build.

```yaml
    # Restore the application
    - name:  Restore the Wpf application to populate the obj folder
      run: msbuild $env:Solution_Path /t:Restore /p:Configuration=$env:Configuration /p:RuntimeIdentifier=$env:RuntimeIdentifier
      env:
        Configuration: Debug
        RuntimeIdentifier: win-${{ matrix.targetplatform }}
```

Once the application has been restored, we are ready to build and create the MSIX.  Rather than build each project separately, we simply build the solution, making sure to pass the target platform, configuration, build mode, whether we want to produce an app bundle, the signing certificate, and certificate password as parameters.
```yaml
    # Build the Windows Application Packaging project
    - name: Build the Windows Application Packaging Project (wapproj) 
      run: msbuild $env:Solution_Path /p:Platform=$env:TargetPlatform /p:Configuration=$env:Configuration /p:UapAppxPackageBuildMode=$env:BuildMode /p:AppxBundle=$env:AppxBundle /p:PackageCertificateKeyFile=$env:SigningCertificate /p:PackageCertificatePassword=${{ secrets.Pfx_Key }}
      env:
        AppxBundle: Never
        BuildMode: SideLoadOnly
        Configuration: Release
        TargetPlatform: ${{ matrix.targetplatform }}
```

Once we have created the app package, we take advantage of the [upload-artifact](https://github.com/marketplace/actions/upload-artifact "upload-artifact GitHub Action page") GitHub Action to save the artifact. You have the option to download the artifact to test the build or upload the artifact to a website or file share to distribute the application. 
```yaml
    # Upload the MSIX package: https://github.com/marketplace/actions/upload-artifact
    - name: Upload build artifacts
      uses: actions/upload-artifact@v1
      with:
        name: MSIX Package
        path: MyWpfApp.Package\AppPackages\
```

To find the artifact, navigate to "Actions," select the workflow, then download the artifact on the right side of the window.
![](doc/images/findArtifact.png)


### cd.yml: Build, package, and create a GitHub release for multiple channels

Build, package and distribute code for multiple channels such as 'Dev' and 'Prod_Sideload' and 'Prod_Store'.   On every `push` to a [tag](https://git-scm.com/book/en/v2/Git-Basics-Tagging) matching the pattern `*`, [create a release](https://developer.github.com/v3/repos/releases/#create-a-release) and [upload a release asset](https://developer.github.com/v3/repos/releases/#upload-a-release-asset).

```yaml
on: 
  push:
    tags:
      - '*'
```

To create a git `tag`, run the following commands on the branch you wish to release:
```cmd
git tag 1.0.0.0
git push origin --tags
```

The above commands will add the tag "1.0.0.0" and then `push` the branch and tag to the repo. [Learn more.](https://git-scm.com/book/en/v2/Git-Basics-Tagging)

In this workflow, the GitHub agent builds the WPF .Net Core application and creates a MSIX package.
Prior to building the code, the application's Identity Name, Publisher, Application DisplayName, and other elements in the Package.appxmanifest are changed according to which channel should be built. 

```yaml
    # Update the appxmanifest before build by setting the per-channel values set in the matrix.
    - name: Update manifest version
      run: |
        [xml]$manifest = get-content ".\$env:Wap_Project_Directory\Package.appxmanifest"
        $manifest.Package.Identity.Version = "$env:NBGV_SimpleVersion.0"
        $manifest.Package.Identity.Name = "${{ matrix.MsixPackageId }}"
        $manifest.Package.Identity.Publisher = "${{ matrix.MsixPublisherId }}"
        $manifest.Package.Properties.DisplayName = "${{ matrix.MsixPackageDisplayName }}"
        $manifest.Package.Applications.Application.VisualElements.DisplayName = "${{ matrix.MsixPackageDisplayName }}"
        $manifest.save(".\$env:Wap_Project_Directory\Package.appxmanifest")
```
This Powershell script effectively overwrites the Package.Identity.Name defined in the Windows Application Packaging project's Package.appxmanifest.  This changes the identity of the application to *MyWPFApp.DevOpsDemo.Dev*, *MyWPFApp.DevOpsDemo.ProdSideload*, or *MyWPFApp.DevOpsDemo.ProdStore* depending on which matrix channel is being built, thus enabling the ability to have multiple channels of an application.

```xml
  <Identity
    Name="MyWPFApp.DevOpsDemo.ProdSideload"
    Publisher="CN=GitHubActionsDemo"
    Version="0.0.1.0" />
```

Channels and variables are defined in the Build Matrix and will build and create app packages for Dev, Prod_Sideload and Prod_Store. [Learn more.](https://help.github.com/en/actions/configuring-and-managing-workflows/configuring-a-workflow#configuring-a-build-matrix)

```yaml
jobs:

  build:

    strategy:
      matrix:
        channel: [Dev, Prod_Sideload, Prod_Store]
        targetPlatform: [x86, x64]
        include:
          
          # includes the following variables for the matrix leg matching Dev
          - channel: Dev
            ChannelName: Dev
            Configuration: Debug
            DistributionUrl: https://microsoft.github.io/github-actions-for-desktop-apps-distribution-dev
            MsixPackageId: MyWPFApp.DevOpsDemo.Dev
            MsixPublisherId: CN=GitHubActionsDemo
            MsixPackageDisplayName: MyWPFApp (Dev)

          # includes the following variables for the matrix leg matching Prod_Sideload
          - channel: Prod_Sideload
            Configuration: Release
            ChannelName: Prod_Sideload
            DistributionUrl: https://microsoft.github.io/github-actions-for-desktop-apps-distribution-prod
            MsixPackageId: MyWPFApp.DevOpsDemo.ProdSideload
            MsixPublisherId: CN=GitHubActionsDemo
            MsixPackageDisplayName: MyWPFApp (ProdSideload)

          # includes the following variables for the matrix leg matching Prod_Store
          - channel: Prod_Store
            Configuration: Release
            ChannelName: Prod_Store
            DistributionUrl: 
            MsixPackageId: MyWPFApp.DevOpsDemo.ProdStore
            MsixPublisherId: CN=GitHubActionsDemo
            MsixPackageDisplayName: MyWPFApp (ProdStore)
```

Like the CI workflow, restore the solution:
```yaml
    # Restore the application
    - name:  Restore the Wpf application to populate the obj folder
      run: msbuild $env:Solution_Path /t:Restore /p:Configuration=$env:Configuration /p:RuntimeIdentifier=$env:RuntimeIdentifier
      env:
        Configuration: ${{ matrix.Configuration }}
        RuntimeIdentifier: win-${{ matrix.targetplatform }}
```
Using GitHub's ```if``` conditional, either build and create an MSIX page for Dev and Prod_Sideload (which require a signing certificate) or for Prod_Store.

```yaml
    # Build the Windows Application Packaging project for Dev and Prod_Sideload
    - name: Build the Windows Application Packaging Project (wapproj) for ${{ matrix.ChannelName }}
      run: msbuild $env:Solution_Path /p:Platform=$env:TargetPlatform /p:Configuration=$env:Configuration /p:UapAppxPackageBuildMode=$env:BuildMode /p:AppxBundle=$env:AppxBundle /p:PackageCertificateKeyFile=$env:SigningCertificate /p:PackageCertificatePassword=${{ secrets.Pfx_Key }}
      if: matrix.ChannelName != 'Prod_Store'
      env:
        AppxBundle: Never
        AppInstallerUri: ${{ matrix.DistributionUrl }}
        BuildMode: SideLoadOnly
        Configuration: ${{ matrix.Configuration }}
        GenerateAppInstallerFile: True
        TargetPlatform: ${{ matrix.targetplatform }}
        
    # Build the Windows Application Packaging project for Prod_Store
    - name: Build the Windows Application Packaging Project (wapproj) for ${{ matrix.ChannelName }}
      run: msbuild $env:Solution_Path /p:Platform=$env:TargetPlatform /p:Configuration=$env:Configuration /p:UapAppxPackageBuildMode=$env:BuildMode /p:AppxBundle=$env:AppxBundle /p:GenerateAppInstallerFile=$env:GenerateAppInstallerFile /p:AppxPackageSigningEnabled=$env:AppxPackageSigningEnabled
      if: matrix.ChannelName == 'Prod_Store'
      env:
        AppxBundle: Never
        AppxPackageSigningEnabled: False
        BuildMode: StoreOnly
        Configuration: ${{ matrix.Configuration }}
        GenerateAppInstallerFile: False
        TargetPlatform: ${{ matrix.targetplatform }}
```

Once the MSIX is created for each channel, the agent archives the AppPackages folder then creates a Release with the specified git release tag.  The archive is uploaded to the release as an asset for storage or distribution. Release names must be unique or an error will be generated.

```yaml
    # Create the release:  https://github.com/actions/create-release
    - name: Create release
      id: create_release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }} # This token is provided by Actions, you do not need to create your own token
      with:
        tag_name: ${{ github.ref}}.${{matrix.ChannelName}}.${{ matrix.targetplatform }}
        release_name:  ${{ github.ref }}.${{ matrix.ChannelName }}.${{ matrix.targetplatform }}
        draft: false
        prerelease: false

    # Upload release asset:   https://github.com/actions/upload-release-asset
    - name: Update release asset
      id: upload-release-asset
      uses: actions/upload-release-asset@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        upload_url: ${{ steps.create_release.outputs.upload_url }}  # This pulls from the CREATE RELEASE step above, referencing it's ID to get its outputs object, which include a `upload_url`. See this blog post for more info: https://jasonet.co/posts/new-features-of-github-actions/#passing-data-to-future-steps 
        asset_path: MyWpfApp.Package\AppPackages\AppPackages.zip
        asset_name: AppPackages.zip
        asset_content_type: application/zip

```

Creating channels for the application is a powerful way to create multiple distributions of an application in the same CD pipeline.

### Signing
Avoid submitting certificates to the repo if at all possible. (Git ignores them by default.) To manage the safe handling of sensitive files like certificates, take advantage of [GitHub secrets](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/creating-and-using-encrypted-secrets), which allow the storage of sensitive information in the repository.

Generate a signing certificate in the Windows Application Packaging Project or add an existing signing certificate to the project and then use PowerShell to encode the .pfx file using Base64 encoding.

```pwsh
$pfx_cert = Get-Content '.\GitHubActionsDemo.pfx' -Encoding Byte
[System.Convert]::ToBase64String($pfx_cert) | Out-File 'SigningCertificate_Encoded.txt'
```

Copy the string from the output file, *SigningCertificate_Encoded.txt*, and add it to the repo as a GitHub secret. [Add a secret to the workflow.](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/virtual-environments-for-github-hosted-runners#creating-and-using-secrets-encrypted-variables)

In the workflow, add a step to decode the secret, save the .pfx to the build agent, and package your application with the Windows Application Packaging project.

```yaml
    # Decode the Base64 encoded Pfx
    - name: Decode the Pfx
      run: |
        $pfx_cert_byte = [System.Convert]::FromBase64String("${{ secrets.Base64_Encoded_Pfx }}")
        $currentDirectory = Get-Location
        $certificatePath = Join-Path -Path $currentDirectory -ChildPath $env:Wap_Project_Directory -AdditionalChildPath $env:SigningCertificate
        [IO.File]::WriteAllBytes("$certificatePath", $pfx_cert_byte)
```

Once the certificate is decoded and saved to the Windows Application Packaging Project, sign the package during packaging and pass the signing certificate's password to MSBuild.

```yaml
    # Build the Windows Application Packaging project for Dev and Prod_Sideload
    - name: Build the Windows Application Packaging Project (wapproj) for ${{ matrix.ChannelName }}
      run: msbuild $env:Wap_Project_Directory/$env:Wap_Project_Name /p:Platform=$env:TargetPlatform /p:Configuration=$env:Configuration /p:UapAppxPackageBuildMode=$env:BuildMode /p:GenerateAppInstallerFile=$env:GenerateAppInstallerFile /p:AppInstallerUri=$env:AppInstallerUri /p:PackageCertificateKeyFile=$env:SigningCertificate /p:PackageCertificatePassword=${{ secrets.Pfx_Key }}
      if: ${{ matrix.ChannelName }} != Prod_Store
      env:
        AppInstallerUri: ${{ matrix.DistributionUrl }}
        BuildMode: SideLoadOnly
        Configuration: ${{ matrix.Configuration }}
        GenerateAppInstallerFile: True
        TargetPlatform: ${{ matrix.TargetPlatform }}
```

Finally, delete the .pfx.

```yaml
    # Remove the .pfx
    - name: Remove the .pfx
      run: Remove-Item -path $env:Wap_Project_Directory/$env:SigningCertificate
      if: ${{ matrix.ChannelName }} != Prod_Store
```

### Versioning

In both workflows, one of the first things we do is create a version.  Having a different version for every push is especially important when we create a release as each release must have a unique release_name.

The [Nerdbank.GitVersioning GitHub Action](https://github.com/AArnott/nbgv) sets the build version based on a combination of the included version.json file, and the git height of the version which is the number of commits in the longest path from HEAD to the commit that set the major.minor version number to the values found in the HEAD. Once the action runs, a number of environment variables are available for use, such as:

```yaml
    # Use Nerdbank.GitVersioning to set version variables: https://github.com/AArnott/nbgv
    - name: Use Nerdbank.GitVersioning to set version variables
      uses: aarnott/nbgv@v0.3
      with:
        setAllVars: true
 ```

* NBGV_Version (e.g. 1.1.159.47562)
* NBGV_SimpleVersion (e.g. 1.1.159)
* NBGV_NuGetPackageVersion (e.g. 1.1.159-gcab9873dd7)
* NBGV_ChocolateyPackageVersion 
* NBGV_NpmPackageVersion

![Environment variables set by NBGV.](doc/images/versionEnvironmentVariables.png)

See the [Nerdbank.GitVersioning](https://github.com/aarnott/nerdbank.gitversioning) package for more information.

### Publisher Profiles
Publisher Profiles allow developers to reference publishing information about their application in the Windows Application Packaging Project.

To add a Publisher Profile, right click MyWpfApp and select Publish.  In the Publish dialog, select 'New.'  In the "Pick a publish target" dialog, choose the folder or file share to publish the app to and "Create Profile."

![](doc/images/pickAPublishTarget.png)


In the Publish dialog, click "Edit" to customize the profile settings.

![](doc/images/editToCustomizeSettings.png)


Select the configuration, framework and runtime to target.  Select whether the deployment mode should be "Framework Dependent" or "Self-contained."

![](doc/images/profileSettings.png)


Edit the profile name to reflect the settings by clicking "Rename" in the Publish dialog.

![](doc/images/renameProfile.png)

In the packaging project, add a reference to the Publish Profile.  In the Solution Explorer, open MyWPFApp.Package and navigate to Applications.  Click on MyWFPApp.  In the properties window, select Publishing Profile.  The dropdown should be populated with the recently-created profile.

![](doc/images/myWpfApp.Package.Properties.png)

To ensure the settings were added correctly to MyWPFApp.Package, double click on the project to open the .wapproj file.  Scroll to the bottom to find the PublishProfile elements.
![](doc/images/publishProfileComplete.png)

# Contributions
This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.

## License
The scripts and documentation in this project are released under the [MIT License](LICENSE)
