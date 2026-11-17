#requires -Version 7.0
<#
.SYNOPSIS
    Keeps the newest releases of every package in the shared feed and removes the rest.

.DESCRIPTION
    The feed is a folder of published packages, one file per version. Nothing here compares or
    verifies a package: this build step only bounds how much the feed grows, so a version that is
    no longer installable is also no longer on disk.
#>

param()

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'SilentlyContinue'
Set-Location $PSScriptRoot

Get-ChildItem -Path '..\includes\*.ps1' |% { . $_.FullName }

function Get-Configuration
{
    $hostDir    = (Get-Item '..').FullName
    $repoRoot   = (Get-Item '..\..\..').FullName
    $configPath = "$hostDir\configs\AwsEmulatorSharp.json"

    if (!(Test-Path $configPath))
    {
        throw "Config not found: $configPath. Run ci\ci.ps1 once to generate it."
    }

    $configuration = Get-Content -Path $configPath -Raw | ConvertFrom-Json

    $configuration | Add-Member -NotePropertyName HostDir -NotePropertyValue $hostDir
    $configuration | Add-Member -NotePropertyName RepoRoot -NotePropertyValue $repoRoot
    $configuration | Add-Member -NotePropertyName ConfigPath -NotePropertyValue $configPath
    $configuration | Add-Member -NotePropertyName FeedPath -NotePropertyValue "$($configuration.sharedFeedPath)"

    return $configuration
}

function Invoke-PreflightCheck($configuration)
{
    $feedPath = $configuration.FeedPath
    $retentionCount = $configuration.feedRetentionCount
    Write-Log "feed $feedPath"
    Write-Log "keep the newest $retentionCount version(s) of every id"

    if (!(Test-Path $feedPath))
    {
        throw "Shared feed not found: $feedPath. The feed lives outside the repository, so the machine running this build step mounts it."
    }

    if ($retentionCount -lt 1)
    {
        throw "feedRetentionCount must be at least 1 in $($configuration.ConfigPath)"
    }
}

function Invoke-PruneFeed($configuration)
{
    $feedPath = $configuration.FeedPath
    $retentionCount = $configuration.feedRetentionCount

    $packages = Get-ChildItem -Path $feedPath -Filter '*.nupkg' -File
    $packageGroups = $packages | Group-Object {
        $packageName = $_.Name
        $identity = Get-PackageIdentity $packageName
        $identity.Id
    }

    $packageGroups |% {
        $packageGroup = $_
        $ordered = @($packageGroup.Group | Sort-Object { (Get-PackageIdentity $_.Name).Version } -Descending)
        $expired = @($ordered | Select-Object -Skip $retentionCount)

        $expired |% {
            $package = $_
            Remove-Item -Path $package.FullName -Force
            Write-Log "removed $($package.Name)"
        }

        Write-Log "kept $(($ordered.Count - $expired.Count)) version(s) of $($packageGroup.Name)"
    }
}

$configuration = Get-Configuration
Invoke-PreflightCheck $configuration
Invoke-PruneFeed $configuration

Write-Log 'feed pruned' 'pass'
exit 0
