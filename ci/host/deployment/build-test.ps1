#requires -Version 7.0
<#
.SYNOPSIS
    Restore, build, test and publish. The only build step that compiles anything.

.DESCRIPTION
    The build and the publish both run against the one restore and the one build this step made,
    so the assembly that passed the tests is the assembly that is packed.

    The publish lands in temporary_assets\publish, which is the base path the packing step reads
    for every project build.xml names in ProjectForDeployment.
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
    $configuration | Add-Member -NotePropertyName SolutionPath -NotePropertyValue "$repoRoot\$($configuration.solutionFile)"
    $configuration | Add-Member -NotePropertyName ProjectPath -NotePropertyValue "$repoRoot\$($configuration.projectFile)"
    $configuration | Add-Member -NotePropertyName TestRoot -NotePropertyValue "$repoRoot\tests"
    $configuration | Add-Member -NotePropertyName PublishFolder -NotePropertyValue "$repoRoot\temporary_assets\publish"
    $configuration | Add-Member -NotePropertyName BuildXmlPath -NotePropertyValue "$repoRoot\$($configuration.buildXmlFile)"

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

function Add-RuntimeConfiguration($configuration)
{
    $packedProjects = Get-BuildItems $configuration 'ProjectsForNuGetPackaging'

    $configuration | Add-Member -NotePropertyName ElapsedSeconds -NotePropertyValue 0 -Force
    $configuration | Add-Member -NotePropertyName PackedProjects -NotePropertyValue $packedProjects -Force

    Write-Log "build step packs $($packedProjects.Count) project(s)"

    return $configuration
}

function Invoke-PreflightCheck($configuration)
{
    $solutionPath = $configuration.SolutionPath
    $projectPath  = $configuration.ProjectPath
    $testRoot     = $configuration.TestRoot
    $testProjects = @(Get-ChildItem -Path $testRoot -Recurse -Filter '*.csproj' -File)

    if (!(Test-Path $solutionPath))
    {
        throw "solution not found: $solutionPath"
    }

    if (!(Test-Path $projectPath))
    {
        throw "project not found: $projectPath"
    }

    if ($testProjects.Count -eq 0)
    {
        throw "no test project under $testRoot; the build step must prove something"
    }

    Write-Log "solution $solutionPath"
    Write-Log "test projects $($testProjects.Count)"
}

function Invoke-RunTest($configuration)
{
    $arguments = @(
        'test', $configuration.SolutionPath,
        '-c', $configuration.configuration,
        '--no-build',
        '--nologo',
        '-v', 'minimal')

    Write-Log 'dotnet test'

    $code = Invoke-LoggedCommand 'dotnet' $arguments $configuration.RepoRoot
    if ($code -ne 0)
    {
        throw "dotnet test failed with exit code $code"
    }
}

function Add-StepDuration($configuration, $stepName, $startedAt)
{
    $elapsed = ((Get-Date) - $startedAt).TotalSeconds
    $total = $configuration.ElapsedSeconds + $elapsed
    $configuration | Add-Member -NotePropertyName ElapsedSeconds -NotePropertyValue $total -Force

    $elapsedText = [math]::Round($elapsed, 1)
    $totalText = [math]::Round($total, 1)
    Write-Log "$stepName $elapsedText s, total $totalText s of $($configuration.durationBudgetSeconds) s"

    if ($total -gt $configuration.durationBudgetSeconds)
    {
        throw "Duration budget exceeded: $totalText s spent against the configured $($configuration.durationBudgetSeconds) s"
    }
}

function Invoke-Clean($configuration)
{
    $startedAt = Get-Date
    $publishFolder = $configuration.PublishFolder
    Write-Log "clean $($configuration.SolutionPath)"

    $code = Invoke-LoggedCommand 'dotnet' @(
        'clean', $configuration.SolutionPath,
        '-c', $configuration.configuration,
        '--nologo',
        '-v', 'minimal') $configuration.RepoRoot
    if ($code -ne 0)
    {
        throw "dotnet clean failed with exit code $code"
    }

    if (Test-Path $publishFolder)
    {
        Remove-Item -Path $publishFolder -Recurse -Force
    }

    Add-StepDuration $configuration 'clean' $startedAt
}

function Invoke-Restore($configuration)
{
    $startedAt = Get-Date
    Write-Log "restore $($configuration.SolutionPath)"

    $code = Invoke-LoggedCommand 'dotnet' @('restore', $configuration.SolutionPath) $configuration.RepoRoot
    if ($code -ne 0)
    {
        throw "dotnet restore failed with exit code $code"
    }

    Add-StepDuration $configuration 'restore' $startedAt
}

function Invoke-Build($configuration)
{
    $startedAt = Get-Date
    Write-Log "build $($configuration.configuration)"

    $code = Invoke-LoggedCommand 'dotnet' @(
        'build', $configuration.SolutionPath,
        '-c', $configuration.configuration,
        '-warnaserror',
        '--nologo',
        '-v', 'minimal') $configuration.RepoRoot
    if ($code -ne 0)
    {
        throw "dotnet build failed with exit code $code"
    }

    Add-StepDuration $configuration 'build' $startedAt
}

function Invoke-Test($configuration)
{
    $startedAt = Get-Date

    Invoke-RunTest $configuration

    Add-StepDuration $configuration 'test' $startedAt
}

function Invoke-Publish($configuration)
{
    if ($configuration.PackedProjects.Count -eq 0)
    {
        Write-Log 'nothing to pack: ProjectsForNuGetPackaging is empty, so nothing is published'
        return
    }

    $startedAt = Get-Date
    $publishFolder = $configuration.PublishFolder
    $publishedExecutable = "$publishFolder\$($configuration.deploymentExecutable)"
    Write-Log "publish to $publishFolder"

    $code = Invoke-LoggedCommand 'dotnet' @(
        'publish', $configuration.ProjectPath,
        '-c', $configuration.configuration,
        '--no-build',
        '--nologo',
        '-v', 'minimal',
        '-o', $publishFolder) $configuration.RepoRoot
    if ($code -ne 0)
    {
        throw "dotnet publish failed with exit code $code"
    }

    if (!(Test-Path $publishedExecutable))
    {
        throw "Publish output has no $($configuration.deploymentExecutable): $publishFolder"
    }

    Add-StepDuration $configuration 'publish' $startedAt
    Write-Log "published build $($configuration.buildVersion) to $publishFolder" 'pass'
}

$configuration = Get-Configuration
$configuration = Add-RuntimeConfiguration $configuration

Invoke-PreflightCheck $configuration
Invoke-Clean $configuration
Invoke-Restore $configuration
Invoke-Build $configuration
Invoke-Test $configuration
Invoke-Publish $configuration

exit 0
