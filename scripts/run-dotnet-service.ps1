[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Project,

    [Parameter(Mandatory = $true)]
    [string] $Url,

    [Parameter(Mandatory = $true)]
    [string] $EnvFile,

    [Parameter(Mandatory = $true)]
    [string] $Prefix
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$envFilePath = Resolve-Path $EnvFile

function Read-EnvFile {
    param([string] $Path)

    $values = [ordered]@{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) {
            continue
        }

        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) {
            continue
        }

        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $values[$key] = $value
    }

    return $values
}

function Apply-ServiceEnvironment {
    param(
        [hashtable] $Values,
        [string] $ServicePrefix
    )

    foreach ($entry in $Values.GetEnumerator()) {
        if ($entry.Key.StartsWith("COMMON__", [StringComparison]::OrdinalIgnoreCase)) {
            $name = $entry.Key.Substring("COMMON__".Length)
            [Environment]::SetEnvironmentVariable($name, $entry.Value, "Process")
        }

        if ($entry.Key.StartsWith($ServicePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            $name = $entry.Key.Substring($ServicePrefix.Length)
            [Environment]::SetEnvironmentVariable($name, $entry.Value, "Process")
        }
    }
}

$values = Read-EnvFile -Path $envFilePath
Apply-ServiceEnvironment -Values $values -ServicePrefix $Prefix

Set-Location $repoRoot
Write-Host "Starting $Project on $Url"
dotnet run --project $Project --urls $Url
exit $LASTEXITCODE
