#requires -Version 7.0

function Get-PackageIdentity($packageFileName)
{
    $packagePattern = '^(?<id>.+?)\.(?<version>[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?(-[0-9A-Za-z.-]+)?)\.nupkg$'

    switch -Regex ($packageFileName)
    {
        $packagePattern {
            $versionText = $Matches['version'] -replace '-.*$', ''
            return [pscustomobject]@{
                Id = $Matches['id']
                Version = [version]$versionText
            }
        }
        default { throw "Package file name holds no id and version: $packageFileName" }
    }
}
