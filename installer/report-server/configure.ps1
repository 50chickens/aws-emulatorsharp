$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'SilentlyContinue'
Set-Location $PSScriptRoot

function Write-Log($message)
{
    Write-Host "[$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))] $message"
}

function Get-Configuration
{
    $scriptFolder                = (Get-Item ".").FullName
    $settingsPath                = "$scriptFolder\settings.json"
    $settings                    = Get-Content -Path $settingsPath -Raw | ConvertFrom-Json
    $databaseServerName          = $settings.databaseServerName
    $databaseName                = $settings.databaseName
    $databaseCredentialsType     = $settings.databaseCredentialsType
    $baseUrl                     = $settings.baseUrl
    $applications                = $settings.applications
    $connectionString            = "Data Source=$databaseServerName;Integrated Security=True"
    $databaseConnectionArguments = @{ ConnectionString = $connectionString; CredentialsType = $databaseCredentialsType }
    $createDatabaseArguments     = @{ ConnectionString = $connectionString; ApplicationName = $databaseName }
    $virtualDirectoryCalls = @($applications | ForEach-Object {
        [pscustomobject]@{
            MethodName = 'SetVirtualDirectory'
            Arguments  = @{ Application = $_.name; VirtualDirectory = $_.virtualDirectory }
        }
    })
    $reservationCalls = @($applications | ForEach-Object {
        [pscustomobject]@{
            MethodName = 'ReserveUrl'
            Arguments  = @{ Application = $_.name; Url = $baseUrl }
        }
    })
    $methodCalls = @(
        [pscustomobject]@{ MethodName = 'SetDatabaseConnection'; Arguments = $databaseConnectionArguments },
        [pscustomobject]@{ MethodName = 'CreateDatabase'; Arguments = $createDatabaseArguments }
    ) + $virtualDirectoryCalls + $reservationCalls

    $configuration = $settings
    $configuration | Add-Member -NotePropertyName 'ConnectionString' -NotePropertyValue $connectionString -Force
    $configuration | Add-Member -NotePropertyName 'MethodCalls'      -NotePropertyValue $methodCalls      -Force
    return $configuration
}

function Add-RuntimeConfiguration($configuration)
{
    $wmiNamespace = $configuration.wmiNamespace
    $instanceName = $configuration.instanceName
    Write-Log "Reading the configuration setting of instance $instanceName from $wmiNamespace"
    $configurationSetting = Get-CimInstance -Namespace $wmiNamespace -ClassName 'MSReportServer_ConfigurationSetting' | Select-Object -First 1
    $configuration | Add-Member -NotePropertyName 'ConfigurationSetting' -NotePropertyValue $configurationSetting -Force
    return $configuration
}

function Invoke-ConfigurationSettingMethod($configurationSetting, $methodCall)
{
    $methodName  = $methodCall.MethodName
    $arguments   = $methodCall.Arguments
    $result      = Invoke-CimMethod -InputObject $configurationSetting -MethodName $methodName -Arguments $arguments
    $returnValue = $result.ReturnValue
    Write-Log "$methodName returned HRESULT $returnValue"
    if ($returnValue -ne 0)
    {
        throw "$methodName failed with HRESULT $returnValue"
    }
}

function Invoke-ReportServerConfiguration($configuration)
{
    $configurationSetting = $configuration.ConfigurationSetting
    $methodCalls          = $configuration.MethodCalls
    $instanceName         = $configuration.instanceName
    Write-Log "Configuring report server instance $instanceName"
    $methodCalls |% {
        $methodCall = $_
        Invoke-ConfigurationSettingMethod $configurationSetting $methodCall
    }
}

$configuration = Get-Configuration
$configuration = Add-RuntimeConfiguration $configuration
Invoke-ReportServerConfiguration $configuration
