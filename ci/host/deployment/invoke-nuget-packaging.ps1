#requires -Version 7.0
<#
.SYNOPSIS
    Packs every project build.xml declares, at the version ci.ps1 generated, and pushes it to the
    shared feed.

.DESCRIPTION
    The version is not composed here. ci.ps1 wrote it into the generated config once, and this
    build step is the first to consume it.

    nuget.exe packs rather than dotnet: a nuspec beside a project is the whole declaration of what
    the package holds. The tool is downloaded once into cached_assets and reused by later runs.
#>

param()

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'SilentlyContinue'
Set-Location $PSScriptRoot

Get-ChildItem -Path '..\includes\*.ps1' |% { . $_.FullName }

function Get-Configuration
{
    $hostDir          = (Get-Item '..').FullName
    $repoRoot         = (Get-Item '..\..\..').FullName
    $configPath       = "$hostDir\configs\AwsEmulatorSharp.json"
    $cachedAssetsRoot = "$hostDir\cached_assets"

    if (!(Test-Path $configPath))
    {
        throw "Config not found: $configPath. Run ci\ci.ps1 once to generate it."
    }

    $configuration = Get-Content -Path $configPath -Raw | ConvertFrom-Json

    $configuration | Add-Member -NotePropertyName HostDir -NotePropertyValue $hostDir
    $configuration | Add-Member -NotePropertyName RepoRoot -NotePropertyValue $repoRoot
    $configuration | Add-Member -NotePropertyName ConfigPath -NotePropertyValue $configPath
    $configuration | Add-Member -NotePropertyName BuildXmlPath -NotePropertyValue "$repoRoot\$($configuration.buildXmlFile)"
    $configuration | Add-Member -NotePropertyName NuGetExePath -NotePropertyValue "$cachedAssetsRoot\nuget.exe"
    $configuration | Add-Member -NotePropertyName NuGetExeUrl -NotePropertyValue 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe'
    $configuration | Add-Member -NotePropertyName PackRoot -NotePropertyValue "$repoRoot\temporary_assets\packages"
    $configuration | Add-Member -NotePropertyName PublishFolder -NotePropertyValue "$repoRoot\temporary_assets\publish"
    $configuration | Add-Member -NotePropertyName FeedPath -NotePropertyValue "$($configuration.sharedFeedPath)"

    return $configuration
}

function Get-BuildItems($configuration, $itemName)
{
    $buildXmlPath = $configuration.BuildXmlPath
    $repoRoot = $configuration.RepoRoot
    $itemValues = @()

    Push-Location $repoRoot
    $itemQueryText = & dotnet msbuild $buildXmlPath "-getItem:$itemName" '-nologo'
    Pop-Location

    if ($LASTEXITCODE -ne 0)
    {
        throw "reading $itemName from $buildXmlPath failed with exit code $LASTEXITCODE"
    }

    $itemQuery = $itemQueryText -join "`n" | ConvertFrom-Json
    $itemProperty = $itemQuery.Items.PSObject.Properties[$itemName]
    if ($null -ne $itemProperty)
    {
        $itemValues = @($itemProperty.Value)
    }

    return $itemValues
}

function Get-PackDetails($configuration, $sourceItem)
{
    $projectFile = Get-Item -Path $sourceItem.FullPath
    $packageId = $projectFile.BaseName
    $projectFolder = $projectFile.DirectoryName
    $projectPath = $projectFile.FullName
    $nuspecPath = "$projectFolder\$packageId.nuspec"
    $deploymentIds = $configuration.DeploymentIds

    $basePath = "$projectFolder\bin\$($configuration.configuration)\net10.0"
    if ($deploymentIds -contains $packageId)
    {
        $basePath = $configuration.PublishFolder
    }

    return [pscustomobject]@{
        PackageId   = $packageId
        ProjectPath = $projectPath
        NuspecPath  = $nuspecPath
        BasePath    = $basePath
    }
}

function Add-RuntimeConfiguration($configuration)
{
    $sourceItems = Get-BuildItems $configuration 'ProjectsForNuGetPackaging'
    $deploymentItems = Get-BuildItems $configuration 'ProjectForDeployment'
    $deploymentIds = @($deploymentItems |% { $_.Identity })

    $configuration | Add-Member -NotePropertyName DeploymentIds -NotePropertyValue $deploymentIds
    $configuration | Add-Member -NotePropertyName PackageVersion -NotePropertyValue $configuration.buildVersion

    $packDetails = @($sourceItems |% { Get-PackDetails $configuration $_ })
    $configuration | Add-Member -NotePropertyName PackDetails -NotePropertyValue $packDetails

    Write-Log "pack $($packDetails.Count) project(s) at $($configuration.PackageVersion)"
    Write-Log "published projects: $($deploymentIds -join ', ')"

    return $configuration
}

function Invoke-PreflightCheck($configuration)
{
    $buildXmlPath = $configuration.BuildXmlPath
    $feedPath = $configuration.FeedPath
    $packDetails = $configuration.PackDetails
    Write-Log "build file $buildXmlPath"
    Write-Log "feed $feedPath"

    if (!(Test-Path $buildXmlPath))
    {
        throw "build file not found: $buildXmlPath"
    }

    if ($packDetails.Count -eq 0)
    {
        throw "ProjectsForNuGetPackaging in $buildXmlPath is empty, so this step would publish nothing"
    }

    if (!(Test-Path $feedPath))
    {
        throw "Shared feed not found: $feedPath. The feed lives outside the repository, so the machine running this build step mounts it."
    }

    $packDetails |% {
        $packDetail = $_
        if (!(Test-Path $packDetail.NuspecPath))
        {
            throw "No nuspec beside $($packDetail.ProjectPath): a packed project holds a nuspec named after it, in its own folder."
        }
    }
}

function Invoke-EnsureNuGetExe($configuration)
{
    $nugetExePath = $configuration.NuGetExePath
    $nugetExeUrl = $configuration.NuGetExeUrl

    if (Test-Path $nugetExePath)
    {
        Write-Log "nuget.exe present at $nugetExePath"
        return
    }

    Write-Log "download $nugetExeUrl to $nugetExePath"
    $folder = $nugetExePath.Substring(0, $nugetExePath.Length - 'nuget.exe'.Length - 1)
    $null = New-Item -ItemType Directory -Path $folder -Force
    Invoke-WebRequest -Uri $nugetExeUrl -OutFile $nugetExePath
}

function Invoke-PackProject($configuration, $packDetail)
{
    $packRoot = $configuration.PackRoot
    $packageVersion = $configuration.PackageVersion
    $nugetExePath = $configuration.NuGetExePath
    $repoRoot = $configuration.RepoRoot
    Write-Log "pack $($packDetail.PackageId) $packageVersion from $($packDetail.BasePath)"

    $code = Invoke-LoggedCommand $nugetExePath @(
        'pack', $packDetail.NuspecPath,
        '-Version', $packageVersion,
        '-BasePath', $packDetail.BasePath,
        '-OutputDirectory', $packRoot,
        '-NonInteractive',
        '-ForceEnglishOutput') $repoRoot
    if ($code -ne 0)
    {
        throw "nuget pack failed for $($packDetail.PackageId) with exit code $code"
    }
}

function Invoke-Pack($configuration)
{
    $packDetails = $configuration.PackDetails
    $packRoot = $configuration.PackRoot

    if (Test-Path $packRoot)
    {
        Remove-Item -Path $packRoot -Recurse -Force
    }

    $null = New-Item -ItemType Directory -Path $packRoot -Force

    $packDetails |% {
        $packDetail = $_
        Invoke-PackProject $configuration $packDetail
    }

    $packages = @(Get-ChildItem -Path $packRoot -Filter '*.nupkg' -File)
    if ($packages.Count -ne $packDetails.Count)
    {
        throw "$packRoot holds $($packages.Count) package(s), the item group declares $($packDetails.Count)"
    }

    $packages |% {
        $package = $_
        Write-Log "packed $($package.Name)" 'pass'
    }
}

function Invoke-Push($configuration)
{
    $packRoot = $configuration.PackRoot
    $feedPath = $configuration.FeedPath
    $nugetExePath = $configuration.NuGetExePath
    $repoRoot = $configuration.RepoRoot

    Get-ChildItem -Path $packRoot -Filter '*.nupkg' -File |% {
        $package = $_
        Write-Log "push $($package.Name) into $feedPath"

        $code = Invoke-LoggedCommand $nugetExePath @(
            'push', $package.FullName,
            '-Source', $feedPath,
            '-NonInteractive',
            '-ForceEnglishOutput') $repoRoot
        if ($code -ne 0)
        {
            throw "nuget push failed for $($package.Name) with exit code $code"
        }
    }
}

$configuration = Get-Configuration
$configuration = Add-RuntimeConfiguration $configuration
Invoke-PreflightCheck $configuration
Invoke-EnsureNuGetExe $configuration
Invoke-Pack $configuration
Invoke-Push $configuration

Write-Log "packed and pushed $($configuration.PackDetails.Count) package(s) at $($configuration.PackageVersion)" 'pass'
exit 0
