[CmdletBinding()]
param(
    [string] $EnvFile = "scripts/env/v2-local.env",
    [switch] $SkipDocker,
    [switch] $SkipMigrations,
    [switch] $SkipBootstrap,
    [switch] $NoStorefront,
    [switch] $NoOpenBrowser,
    [switch] $StopExisting,
    [switch] $DryRun
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if ([System.IO.Path]::IsPathRooted($EnvFile)) {
    $envFilePath = Resolve-Path $EnvFile
}
else {
    $envFilePath = Resolve-Path (Join-Path $repoRoot $EnvFile)
}
$logDir = Join-Path $repoRoot ".gstack/run-v2-local"

$projects = @{
    ControlPlaneApi = "BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj"
    ControlPlaneWeb = "BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj"
    CommerceNodeApi = "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj"
    StorefrontV2 = "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"
    Infrastructure = "BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj"
}

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

function Get-EnvValue {
    param(
        [hashtable] $Values,
        [string] $Key,
        [string] $Default = ""
    )

    if ($Values.Contains($Key) -and -not [string]::IsNullOrWhiteSpace($Values[$Key])) {
        return $Values[$Key]
    }

    return $Default
}

function Get-ServiceEnvironment {
    param(
        [hashtable] $Values,
        [string] $Prefix
    )

    $result = @{}
    foreach ($entry in $Values.GetEnumerator()) {
        if ($entry.Key.StartsWith("COMMON__", [StringComparison]::OrdinalIgnoreCase)) {
            $result[$entry.Key.Substring("COMMON__".Length)] = $entry.Value
        }

        if ($entry.Key.StartsWith($Prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $result[$entry.Key.Substring($Prefix.Length)] = $entry.Value
        }
    }

    return $result
}

function Invoke-WithEnvironment {
    param(
        [hashtable] $Environment,
        [scriptblock] $Script
    )

    $previous = @{}
    foreach ($entry in $Environment.GetEnumerator()) {
        $previous[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key, "Process")
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Process")
    }

    try {
        & $Script
    }
    finally {
        foreach ($entry in $previous.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Process")
        }
    }
}

function Get-PortFromUrl {
    param([string] $Url)
    return ([Uri]$Url).Port
}

function Test-PortListening {
    param([int] $Port)
    return [bool](Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Wait-TcpPort {
    param(
        [int] $Port,
        [int] $TimeoutSeconds = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-PortListening -Port $Port) {
            return
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for port $Port."
}

function ConvertTo-ProcessArgument {
    param([string] $Value)

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    return '"' + $Value.Replace('"', '\"') + '"'
}

function Wait-HttpOk {
    param(
        [string] $Url,
        [hashtable] $Headers = @{},
        [int] $TimeoutSeconds = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Headers $Headers -TimeoutSec 3 -UseBasicParsing
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 700
        }
    }

    throw "Timed out waiting for HTTP endpoint $Url."
}

function Stop-PortProcess {
    param([int] $Port)

    $processIds = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($processId in $processIds) {
        if ($processId -gt 0) {
            Write-Host "Stopping process $processId on port $Port"
            Stop-Process -Id $processId -Force
        }
    }
}

function Get-V2RuntimeProcesses {
    $names = @(
        "BlazorShop.ControlPlane.API",
        "BlazorShop.ControlPlane.Web",
        "BlazorShop.CommerceNode.API",
        "BlazorShop.Storefront.V2"
    )

    $repo = $repoRoot.ProviderPath
    return Get-Process -Name $names -ErrorAction SilentlyContinue |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_.Path) -and $_.Path.StartsWith($repo, [StringComparison]::OrdinalIgnoreCase) }
}

function Stop-V2RuntimeProcesses {
    $processes = @(Get-V2RuntimeProcesses)
    foreach ($process in $processes) {
        Write-Host "Stopping $($process.ProcessName) pid=$($process.Id)"
        Stop-Process -Id $process.Id -Force
    }
}

function Assert-NoV2RuntimeProcesses {
    $processes = @(Get-V2RuntimeProcesses)
    if ($processes.Count -eq 0) {
        return
    }

    Write-Host "Existing V2 runtime processes are running and may lock build outputs:"
    foreach ($process in $processes) {
        Write-Host "  $($process.ProcessName) pid=$($process.Id)"
    }

    throw "Stop the V2 processes first or rerun this script with -StopExisting. Use -SkipMigrations only if the databases are already migrated."
}

function Invoke-DotNetEfUpdate {
    param(
        [string] $Context,
        [string] $StartupProject,
        [string] $Prefix,
        [hashtable] $Values
    )

    $serviceEnv = Get-ServiceEnvironment -Values $Values -Prefix $Prefix
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    $logFile = Join-Path $logDir "ef-$Context.log"
    Write-Host "Updating database for $Context"

    Invoke-WithEnvironment -Environment $serviceEnv -Script {
        dotnet ef database update `
            --context $Context `
            --project $projects.Infrastructure `
            --startup-project $StartupProject *> $logFile

        if ($LASTEXITCODE -ne 0) {
            Write-Host "dotnet ef failed. Last log lines:"
            Get-Content -Path $logFile -Tail 80
            throw "dotnet ef database update failed for $Context. See $logFile"
        }
    }
}

function Start-DotNetService {
    param(
        [string] $Name,
        [string] $Project,
        [string] $Url,
        [string] $Prefix
    )

    $port = Get-PortFromUrl -Url $Url
    if (Test-PortListening -Port $port) {
        Write-Host "$Name already listening on $Url; skipping start. Use -StopExisting to restart it."
        return
    }

    $helper = Join-Path $PSScriptRoot "run-dotnet-service.ps1"
    $pwsh = Join-Path $PSHOME "pwsh.exe"
    if (-not (Test-Path $pwsh)) {
        $pwsh = "powershell.exe"
    }

    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    $stdout = Join-Path $logDir "$Name.log"
    $stderr = Join-Path $logDir "$Name.err.log"

    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $helper,
        "-Project", $Project,
        "-Url", $Url,
        "-EnvFile", $envFilePath,
        "-Prefix", $Prefix
    )
    $argumentLine = ($arguments | ForEach-Object { ConvertTo-ProcessArgument -Value $_ }) -join " "

    $process = Start-Process `
        -FilePath $pwsh `
        -ArgumentList $argumentLine `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr `
        -WindowStyle Hidden `
        -PassThru

    Write-Host "Started $Name pid=$($process.Id) url=$Url"
    Write-Host "  log: $stdout"
    Write-Host "  err: $stderr"

    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        if (Test-PortListening -Port $port) {
            return
        }

        $process.Refresh()
        if ($process.HasExited) {
            Write-Host "$Name exited before opening port $port. Last log lines:"
            if (Test-Path $stdout) {
                Get-Content -Path $stdout -Tail 60
            }
            if (Test-Path $stderr) {
                Get-Content -Path $stderr -Tail 60
            }
            throw "$Name failed to start. See $stdout and $stderr"
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for $Name on port $port. See $stdout and $stderr"
}

function Invoke-ControlPlaneApi {
    param(
        [string] $Method,
        [string] $Url,
        [object] $Body = $null,
        [hashtable] $Headers = @{}
    )

    $parameters = @{
        Method = $Method
        Uri = $Url
        Headers = $Headers
        ContentType = "application/json"
    }

    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 8)
    }

    return Invoke-RestMethod @parameters
}

function Ensure-ControlPlaneRegistry {
    param([hashtable] $Values)

    $apiUrl = Get-EnvValue -Values $Values -Key "RUN__CONTROL_PLANE_API_URL"
    $commerceUrl = Get-EnvValue -Values $Values -Key "RUN__COMMERCE_NODE_API_URL"
    $adminEmail = Get-EnvValue -Values $Values -Key "CONTROLPLANE_API__ControlPlane__SeedAdmin__Email" -Default "admin@example.local"
    $adminPassword = Get-EnvValue -Values $Values -Key "CONTROLPLANE_API__ControlPlane__SeedAdmin__Password" -Default "Admin123!"
    $nodeKey = Get-EnvValue -Values $Values -Key "COMMERCENODE_API__CommerceNode__NodeKey" -Default "dev-node"
    $nodeSecret = Get-EnvValue -Values $Values -Key "COMMERCENODE_API__CommerceNode__NodeSecret" -Default "dev-node-secret"
    $nodeName = Get-EnvValue -Values $Values -Key "RUN__CONTROL_PLANE_NODE_NAME" -Default "Local Commerce Node"
    $storeKey = Get-EnvValue -Values $Values -Key "STOREFRONT_V2__Api__StoreKey" -Default "default"
    $storeName = Get-EnvValue -Values $Values -Key "RUN__CONTROL_PLANE_STORE_NAME" -Default "Default QA Store"

    Write-Host "Bootstrapping ControlPlane registry for node '$nodeKey' and store '$storeKey'"

    $login = $null
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        try {
            $login = Invoke-ControlPlaneApi `
                -Method "Post" `
                -Url "$apiUrl/api/control-plane/auth/login" `
                -Body @{ email = $adminEmail; password = $adminPassword }
            break
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if ($null -eq $login -or -not $login.success -or [string]::IsNullOrWhiteSpace($login.data.token)) {
        throw "Could not log in to ControlPlane API with seeded admin account."
    }

    $headers = @{ Authorization = "Bearer $($login.data.token)" }

    $nodes = Invoke-ControlPlaneApi -Method "Get" -Url "$apiUrl/api/control-plane/nodes?limit=100" -Headers $headers
    $node = @($nodes.data.items | Where-Object { $_.nodeKey -eq $nodeKey } | Select-Object -First 1)
    if ($node.Count -eq 0) {
        $created = Invoke-ControlPlaneApi `
            -Method "Post" `
            -Url "$apiUrl/api/control-plane/nodes" `
            -Headers $headers `
            -Body @{
                nodeKey = $nodeKey
                nodeSecret = $nodeSecret
                name = $nodeName
                description = "Local V2 manual QA node"
                controlApiUrl = $commerceUrl
            }
        $node = $created.data
    }
    else {
        $node = $node[0]
        $updated = Invoke-ControlPlaneApi `
            -Method "Put" `
            -Url "$apiUrl/api/control-plane/nodes/$($node.publicId)" `
            -Headers $headers `
            -Body @{
                name = $nodeName
                description = "Local V2 manual QA node"
                controlApiUrl = $commerceUrl
                nodeSecret = $nodeSecret
            }
        $node = $updated.data
    }

    $stores = Invoke-ControlPlaneApi -Method "Get" -Url "$apiUrl/api/control-plane/stores" -Headers $headers
    $store = @($stores.data.items | Where-Object { $_.storeKey -eq $storeKey } | Select-Object -First 1)
    if ($store.Count -eq 0) {
        $createdStore = Invoke-ControlPlaneApi `
            -Method "Post" `
            -Url "$apiUrl/api/control-plane/stores" `
            -Headers $headers `
            -Body @{
                storeKey = $storeKey
                name = $storeName
                nodePublicId = $node.publicId
                metadataJson = '{"source":"scripts/run-v2-local.ps1"}'
            }
        $store = $createdStore.data
    }
    else {
        $store = $store[0]
        $updatedStore = Invoke-ControlPlaneApi `
            -Method "Put" `
            -Url "$apiUrl/api/control-plane/stores/$($store.publicId)" `
            -Headers $headers `
            -Body @{
                name = $storeName
                nodePublicId = $node.publicId
                metadataJson = '{"source":"scripts/run-v2-local.ps1"}'
            }
        $store = $updatedStore.data
    }

    Write-Host "ControlPlane registry ready:"
    Write-Host "  node:  $($node.nodeKey) $($node.publicId)"
    Write-Host "  store: $($store.storeKey) $($store.publicId)"

    try {
        Invoke-ControlPlaneApi `
            -Method "Post" `
            -Url "$apiUrl/api/control-plane/health/nodes/$($node.publicId)/probe" `
            -Headers $headers | Out-Null
        Write-Host "  probe: submitted"
    }
    catch {
        Write-Warning "ControlPlane node probe failed during bootstrap: $($_.Exception.Message)"
    }
}

$values = Read-EnvFile -Path $envFilePath
$controlPlaneApiUrl = Get-EnvValue -Values $values -Key "RUN__CONTROL_PLANE_API_URL" -Default "http://localhost:5280"
$controlPlaneWebUrl = Get-EnvValue -Values $values -Key "RUN__CONTROL_PLANE_WEB_URL" -Default "http://localhost:5281"
$commerceNodeApiUrl = Get-EnvValue -Values $values -Key "RUN__COMMERCE_NODE_API_URL" -Default "http://localhost:5180"
$storefrontUrl = Get-EnvValue -Values $values -Key "RUN__STOREFRONT_URL" -Default "http://localhost:18598"
$openBrowser = (Get-EnvValue -Values $values -Key "RUN__OPEN_BROWSER" -Default "true").Equals("true", [StringComparison]::OrdinalIgnoreCase)
$bootstrapControlPlane = (Get-EnvValue -Values $values -Key "RUN__BOOTSTRAP_CONTROLPLANE" -Default "true").Equals("true", [StringComparison]::OrdinalIgnoreCase)
$nodeKey = Get-EnvValue -Values $values -Key "COMMERCENODE_API__CommerceNode__NodeKey" -Default "dev-node"
$nodeSecret = Get-EnvValue -Values $values -Key "COMMERCENODE_API__CommerceNode__NodeSecret" -Default "dev-node-secret"

Write-Host "BlazorShop V2 local run"
Write-Host "  env:              $envFilePath"
Write-Host "  ControlPlane API: $controlPlaneApiUrl"
Write-Host "  ControlPlane Web: $controlPlaneWebUrl"
Write-Host "  CommerceNode API: $commerceNodeApiUrl"
Write-Host "  Storefront V2:    $storefrontUrl"

if ($DryRun) {
    Write-Host "Dry run only. No processes were started."
    exit 0
}

Set-Location $repoRoot

if (-not $SkipDocker) {
    docker compose -f compose.controlplane.yml up -d
    docker compose -f compose.commercenode.yml up -d
    Wait-TcpPort -Port 5433
    Wait-TcpPort -Port 5434
}

if ($StopExisting) {
    Stop-PortProcess -Port (Get-PortFromUrl -Url $commerceNodeApiUrl)
    Stop-PortProcess -Port (Get-PortFromUrl -Url $controlPlaneApiUrl)
    Stop-PortProcess -Port (Get-PortFromUrl -Url $controlPlaneWebUrl)
    if (-not $NoStorefront) {
        Stop-PortProcess -Port (Get-PortFromUrl -Url $storefrontUrl)
    }

    Stop-V2RuntimeProcesses
}
elseif (-not $SkipMigrations) {
    Assert-NoV2RuntimeProcesses
}

if (-not $SkipMigrations) {
    Invoke-DotNetEfUpdate `
        -Context "ControlPlaneDbContext" `
        -StartupProject $projects.ControlPlaneApi `
        -Prefix "CONTROLPLANE_API__" `
        -Values $values

    Invoke-DotNetEfUpdate `
        -Context "CommerceNodeDbContext" `
        -StartupProject $projects.CommerceNodeApi `
        -Prefix "COMMERCENODE_API__" `
        -Values $values
}

Start-DotNetService `
    -Name "commercenode-api" `
    -Project $projects.CommerceNodeApi `
    -Url $commerceNodeApiUrl `
    -Prefix "COMMERCENODE_API__"

Wait-HttpOk `
    -Url "$commerceNodeApiUrl/api/commerce/healthz" `
    -Headers @{ "X-Node-Key" = $nodeKey; "X-Node-Secret" = $nodeSecret }

Start-DotNetService `
    -Name "controlplane-api" `
    -Project $projects.ControlPlaneApi `
    -Url $controlPlaneApiUrl `
    -Prefix "CONTROLPLANE_API__"

Wait-HttpOk -Url "$controlPlaneApiUrl/swagger/v1/swagger.json"

Start-DotNetService `
    -Name "controlplane-web" `
    -Project $projects.ControlPlaneWeb `
    -Url $controlPlaneWebUrl `
    -Prefix "CONTROLPLANE_WEB__"

Wait-HttpOk -Url $controlPlaneWebUrl

if (-not $NoStorefront) {
    Start-DotNetService `
        -Name "storefront-v2" `
        -Project $projects.StorefrontV2 `
        -Url $storefrontUrl `
        -Prefix "STOREFRONT_V2__"

    Wait-HttpOk -Url $storefrontUrl
}

if ($bootstrapControlPlane -and -not $SkipBootstrap) {
    Ensure-ControlPlaneRegistry -Values $values
}

Write-Host ""
Write-Host "Manual QA URLs"
Write-Host "  Control Plane Web: $controlPlaneWebUrl"
Write-Host "  Control Plane API: $controlPlaneApiUrl/swagger"
Write-Host "  Commerce Node API: $commerceNodeApiUrl/swagger"
if (-not $NoStorefront) {
    Write-Host "  Storefront V2:      $storefrontUrl"
}
Write-Host ""
Write-Host "Control Plane seeded accounts"
Write-Host "  admin@example.local / Admin123!"
Write-Host "  user@example.local  / User123!"
Write-Host ""
Write-Host "Logs: $logDir"

if ($openBrowser -and -not $NoOpenBrowser) {
    Start-Process $controlPlaneWebUrl
}
