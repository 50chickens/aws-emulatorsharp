#requires -Version 7.0

<#
    One log line shape for the whole pipeline, timestamps included so the duration of a stage is
    visible without a separate tool.
#>

function Write-Log
{
    param(
        [Parameter(Mandatory)][string]$Message,
        [ValidateSet('info', 'pass', 'warn', 'fail')][string]$Level = 'info'
    )

    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    $colour = switch ($Level)
    {
        'pass' { 'Green' }
        'warn' { 'Yellow' }
        'fail' { 'Red' }
        default { 'Gray' }
    }

    Write-Host "[$stamp] $Message" -ForegroundColor $colour
}

<#
    Runs a native command, logs the command line before it and its exit code after, and returns
    the exit code. Callers check the return value; no exception is thrown for a non-zero exit,
    because a stage may want to report it.
#>
function Invoke-LoggedCommand
{
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [string]$WorkingDirectory
    )

    Write-Log "$FilePath $($ArgumentList -join ' ')"

    $previous = $null
    if ($WorkingDirectory)
    {
        $previous = Get-Location
        Set-Location $WorkingDirectory
    }

    try
    {
        & $FilePath @ArgumentList | Out-Host
        $code = $LASTEXITCODE
        if ($null -eq $code)
        {
            $code = 0
        }

        Write-Log "exit $code" $(if ($code -eq 0) { 'info' } else { 'fail' })
        return $code
    }
    finally
    {
        if ($previous)
        {
            Set-Location $previous
        }
    }
}
