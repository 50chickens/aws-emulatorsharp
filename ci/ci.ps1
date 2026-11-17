#requires -Version 7.0
<#
.SYNOPSIS
    Runs every enabled build step in pipeline order.

.DESCRIPTION
    The build step list below is the pipeline. An entry that is absent does not run, and an entry
    whose Enabled is false does not run, so a run never has to be told which steps it meant.

    The selected environment config is copied over the generated working copy before any step
    runs. Every build step reads only the generated copy, so no build step knows an environment
    name and two environments cannot disagree about which file was read.

    The release and image steps of the pipeline skill do not apply here: this host is consumed as a
    package from the shared feed, and nothing installs it as a folder set or hosts it under docker.
#>

param(
    [string]$Environment = '.local'
)

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'SilentlyContinue'
Set-Location $PSScriptRoot

Get-ChildItem -Path '.\host\includes' -Filter '*.ps1' |% { . $_.FullName }

function Get-Configuration($environment)
{
    $ciRoot = (Get-Item '.').FullName
    $configName = 'AwsEmulatorSharp'
    $configPath = "$ciRoot\configs\$configName$environment.json"
    $generatedConfigFolder = "$ciRoot\host\configs"
    $generatedConfigPath = "$generatedConfigFolder\$configName.json"
    $deploymentRoot = "$ciRoot\host\deployment"

    $scriptsToExecute = @(
        [pscustomobject]@{ Enabled = $true; FilePath = "$deploymentRoot\build-test.ps1" }
        [pscustomobject]@{ Enabled = $true; FilePath = "$deploymentRoot\invoke-nuget-packaging.ps1" }
        [pscustomobject]@{ Enabled = $true; FilePath = "$deploymentRoot\invoke-nuget-packages-cleanup.ps1" }
    )

    $configuration = [pscustomobject]@{
        CiRoot                = $ciRoot
        ConfigPath            = $configPath
        GeneratedConfigFolder = $generatedConfigFolder
        GeneratedConfigPath   = $generatedConfigPath
    }

    $configuration | Add-Member -NotePropertyName EnabledScripts -NotePropertyValue @($scriptsToExecute |? { $_.Enabled })
    return $configuration
}

function Add-RuntimeConfiguration($configuration)
{
    $now          = Get-Date
    $dateField    = $now.ToString('yyyyMMdd')
    $timeField    = [int]$now.ToString('HHmmss')
    $buildVersion = "1.0.$dateField.$timeField"

    $configuration | Add-Member -NotePropertyName BuildVersion -NotePropertyValue $buildVersion -Force
    Write-Log "build version $buildVersion"

    return $configuration
}

function Invoke-PreflightCheck($configuration)
{
    $enabledScripts = $configuration.EnabledScripts
    $configPath     = $configuration.ConfigPath

    if (!(Test-Path $configPath))
    {
        throw "environment config not found: $configPath"
    }

    if ($enabledScripts.Count -eq 0)
    {
        throw "no build step is enabled in $($configuration.CiRoot)\ci.ps1; a run with nothing enabled would report complete having done nothing"
    }

    $enabledScripts |% {
        $script = $_
        if (!(Test-Path $script.FilePath))
        {
            throw "build step not found: $($script.FilePath)"
        }

        Write-Log "build step enabled: $($script.FilePath)"
    }
}

function Invoke-PrepareConfiguration($configuration)
{
    $configPath = $configuration.ConfigPath
    $generatedConfigPath = $configuration.GeneratedConfigPath
    $generatedConfigFolder = $configuration.GeneratedConfigFolder
    $buildVersion = $configuration.BuildVersion

    $null = New-Item -ItemType Directory -Path $generatedConfigFolder -Force
    Copy-Item -Path $configPath -Destination $generatedConfigPath -Force

    $settings = Get-Content -Path $generatedConfigPath -Raw | ConvertFrom-Json
    $settings | Add-Member -NotePropertyName buildVersion -NotePropertyValue $buildVersion -Force
    $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $generatedConfigPath -Encoding utf8
    Write-Log "config $configPath -> $generatedConfigPath, buildVersion $buildVersion"
}

function Invoke-BuildStep($configuration, $script)
{
    $scriptPath = $script.FilePath
    Write-Log "=== $scriptPath ==="

    & $scriptPath
    if ($LASTEXITCODE -ne 0)
    {
        throw "$scriptPath failed with exit code $LASTEXITCODE"
    }

    Write-Log "$scriptPath complete" 'pass'
}

function Invoke-BuildSteps($configuration)
{
    $enabledScripts = $configuration.EnabledScripts

    $enabledScripts |% {
        $script = $_
        Invoke-BuildStep $configuration $script
    }

    Write-Log '=== CI complete ===' 'pass'
}

$configuration = Get-Configuration $Environment
$configuration = Add-RuntimeConfiguration $configuration
Invoke-PreflightCheck $configuration
Invoke-PrepareConfiguration $configuration
Invoke-BuildSteps $configuration
