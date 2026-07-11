[CmdletBinding()]
param(
    [switch] $StopDocker,
    [switch] $DryRun
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$fixedRuntimePorts = @(5280, 5281, 5180, 18598)

$projectNames = @(
    "BlazorShop.ControlPlane.API",
    "BlazorShop.ControlPlane.Web",
    "BlazorShop.CommerceNode.API",
    "BlazorShop.Storefront.V2"
)

$projectPaths = @(
    "BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj",
    "BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj",
    "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj",
    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"
) | ForEach-Object { (Join-Path $repoRoot $_).Replace("/", "\") }

function Stop-ProcessId {
    param(
        [int] $ProcessId,
        [string] $Reason
    )

    if ($ProcessId -le 0 -or $script:stoppedProcessIds.Contains($ProcessId)) {
        return
    }

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        return
    }

    $script:stoppedProcessIds.Add($ProcessId) | Out-Null
    Write-Host "Stopping $($process.ProcessName) pid=$ProcessId ($Reason)"

    if (-not $DryRun) {
        Stop-Process -Id $ProcessId -Force
    }
}

function Test-ContainsIgnoreCase {
    param(
        [string] $Text,
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Text) -or [string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Text.IndexOf($Value, [StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Stop-PortProcess {
    param([int] $Port)

    $processIds = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($processId in $processIds) {
        Stop-ProcessId -ProcessId $processId -Reason "listening on port $Port"
    }
}

function Stop-NamedV2Processes {
    foreach ($process in Get-Process -Name $projectNames -ErrorAction SilentlyContinue) {
        if ([string]::IsNullOrWhiteSpace($process.Path)) {
            continue
        }

        if ($process.Path.StartsWith($repoRoot.ProviderPath, [StringComparison]::OrdinalIgnoreCase)) {
            Stop-ProcessId -ProcessId $process.Id -Reason "BlazorShop V2 apphost"
        }
    }
}

function Stop-DotNetRunProcesses {
    $repoPath = $repoRoot.ProviderPath
    $processes = Get-CimInstance Win32_Process |
        Where-Object {
            ($_.Name -in @("dotnet.exe", "powershell.exe", "pwsh.exe")) -and
            -not [string]::IsNullOrWhiteSpace($_.CommandLine) -and
            ((Test-ContainsIgnoreCase -Text $_.CommandLine -Value $repoPath) -or
                (Test-ContainsIgnoreCase -Text $_.CommandLine -Value "run-dotnet-service.ps1"))
        }

    foreach ($process in $processes) {
        $commandLine = $process.CommandLine
        $matchesProject = $false
        foreach ($projectPath in $projectPaths) {
            if (Test-ContainsIgnoreCase -Text $commandLine -Value $projectPath) {
                $matchesProject = $true
                break
            }
        }

        if ($matchesProject -or (Test-ContainsIgnoreCase -Text $commandLine -Value "run-dotnet-service.ps1")) {
            Stop-ProcessId -ProcessId $process.ProcessId -Reason "V2 dotnet run helper"
        }
    }
}

$script:stoppedProcessIds = [System.Collections.Generic.HashSet[int]]::new()

Write-Host "Stopping BlazorShop V2 local runtime"
Write-Host "  fixed ports: $($fixedRuntimePorts -join ', ')"

foreach ($port in $fixedRuntimePorts) {
    Stop-PortProcess -Port $port
}

Stop-NamedV2Processes
Stop-DotNetRunProcesses

if ($StopDocker) {
    Write-Host "Stopping Docker compose services"
    if (-not $DryRun) {
        docker compose -f (Join-Path $repoRoot "compose.controlplane.yml") stop
        docker compose -f (Join-Path $repoRoot "compose.commercenode.yml") stop
    }
}

if ($script:stoppedProcessIds.Count -eq 0) {
    Write-Host "No BlazorShop V2 runtime processes were found."
}
else {
    Write-Host "Stopped $($script:stoppedProcessIds.Count) process(es)."
}
