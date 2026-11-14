#requires -Version 7.0

param(
    [ValidateSet('all', 'build', 'test')]
    [string] $Check = 'all'
)

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'SilentlyContinue'
Set-Location $PSScriptRoot

function Write-Log($message)
{
    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    Write-Host "[$stamp] $message"
}

function Get-Configuration
{
    $repositoryRoot     = (Get-Item '..').FullName
    $solutionFolder     = "$repositoryRoot\src"
    $solutionFilters    = @('*.slnx', '*.sln')
    $expectedSolutions  = 1
    $dotNetCommand      = 'dotnet'
    $buildConfiguration = 'Release'
    $testLogger         = 'console;verbosity=normal'

    return [pscustomobject]@{
        RepositoryRoot     = $repositoryRoot
        SolutionFolder     = $solutionFolder
        SolutionFilters    = $solutionFilters
        ExpectedSolutions  = $expectedSolutions
        DotNetCommand      = $dotNetCommand
        BuildConfiguration = $buildConfiguration
        TestLogger         = $testLogger
    }
}

function Get-SolutionFile($configuration)
{
    $solutionFolder    = $configuration.SolutionFolder
    $solutionFilters   = $configuration.SolutionFilters
    $expectedSolutions = $configuration.ExpectedSolutions

    $solutionFiles = $solutionFilters |% {
        $solutionFilter = $_
        Get-ChildItem -Path $solutionFolder -Filter $solutionFilter -File
    }

    $solutionCount = @($solutionFiles).Count
    if ($solutionCount -ne $expectedSolutions)
    {
        throw "ci: expected $expectedSolutions solution file in $solutionFolder, found $solutionCount"
    }

    $solutionFile = $solutionFiles | Select-Object -First 1
    return $solutionFile
}

function Add-RuntimeConfiguration($configuration)
{
    $solutionFile = Get-SolutionFile $configuration
    $solutionPath = $solutionFile.FullName
    $configuration | Add-Member -NotePropertyName 'SolutionPath' -NotePropertyValue $solutionPath -Force
    return $configuration
}

function Invoke-Build($configuration)
{
    $solutionPath       = $configuration.SolutionPath
    $dotNetCommand      = $configuration.DotNetCommand
    $buildConfiguration = $configuration.BuildConfiguration
    Write-Log "build $buildConfiguration $solutionPath"

    & $dotNetCommand build $solutionPath --configuration $buildConfiguration --nologo
    if ($LASTEXITCODE -ne 0)
    {
        throw "ci: $dotNetCommand build failed with exit code $LASTEXITCODE"
    }
}

function Invoke-Test($configuration)
{
    $solutionPath       = $configuration.SolutionPath
    $dotNetCommand      = $configuration.DotNetCommand
    $buildConfiguration = $configuration.BuildConfiguration
    $testLogger         = $configuration.TestLogger
    Write-Log "test $solutionPath"

    & $dotNetCommand test $solutionPath --configuration $buildConfiguration --no-build --nologo --logger $testLogger
    if ($LASTEXITCODE -ne 0)
    {
        throw "ci: $dotNetCommand test failed with exit code $LASTEXITCODE"
    }
}

$configuration = Get-Configuration
$configuration = Add-RuntimeConfiguration $configuration

if ($Check -in @('all', 'build'))
{
    Invoke-Build $configuration
}

if ($Check -in @('all', 'test'))
{
    Invoke-Test $configuration
}

Write-Log "ci passed ($Check)"
