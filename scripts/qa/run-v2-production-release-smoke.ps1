param(
    [string] $ControlPlaneApiUrl = $env:CONTROLPLANE_QA_API_URL,
    [string] $ControlPlaneWebUrl = $env:CONTROLPLANE_QA_WEB_URL,
    [string] $CommerceNodeApiUrl = $env:COMMERCENODE_QA_API_URL,
    [string] $CommerceNodeNginxUrl = $env:COMMERCENODE_QA_NGINX_URL,
    [string] $StorefrontBaseUrl = $env:STOREFRONT_QA_BASE_URL,
    [switch] $SkipSwagger,
    [switch] $SkipNginx,
    [switch] $Describe
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ControlPlaneApiUrl)) {
    $ControlPlaneApiUrl = "http://localhost:5280"
}

if ([string]::IsNullOrWhiteSpace($ControlPlaneWebUrl)) {
    $ControlPlaneWebUrl = "http://localhost:5281"
}

if ([string]::IsNullOrWhiteSpace($CommerceNodeApiUrl)) {
    $CommerceNodeApiUrl = "http://localhost:5180"
}

if ([string]::IsNullOrWhiteSpace($CommerceNodeNginxUrl)) {
    $CommerceNodeNginxUrl = "http://localhost:8088"
}

if ([string]::IsNullOrWhiteSpace($StorefrontBaseUrl)) {
    $StorefrontBaseUrl = "http://localhost:18598"
}

function Join-Url {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BaseUrl,

        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    return "$($BaseUrl.TrimEnd('/'))/$($Path.TrimStart('/'))"
}

function Invoke-ReleaseSmokeRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $Url,

        [int] $ExpectedStatus = 200,

        [hashtable] $Headers = @{}
    )

    $statusCode = $null
    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $Headers -UseBasicParsing -TimeoutSec 20
        $statusCode = [int] $response.StatusCode
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int] $_.Exception.Response.StatusCode
        }
        else {
            throw "[$Name] request failed before an HTTP response was available: $($_.Exception.Message)"
        }
    }

    if ($statusCode -ne $ExpectedStatus) {
        throw "[$Name] expected HTTP $ExpectedStatus but got HTTP $statusCode for $Url"
    }

    Write-Host "PASS [$Name] HTTP $statusCode $Url"
}

$checks = @(
    @{ Name = "ControlPlane API health"; Url = Join-Url $ControlPlaneApiUrl "/health"; ExpectedStatus = 200 },
    @{ Name = "ControlPlane Web root"; Url = Join-Url $ControlPlaneWebUrl "/"; ExpectedStatus = 200 },
    @{ Name = "CommerceNode API health"; Url = Join-Url $CommerceNodeApiUrl "/health"; ExpectedStatus = 200 },
    @{ Name = "Storefront V2 health"; Url = Join-Url $StorefrontBaseUrl "/health"; ExpectedStatus = 200 }
)

if (-not $SkipSwagger) {
    $checks += @(
        @{ Name = "CommerceNode Storefront Swagger"; Url = Join-Url $CommerceNodeApiUrl "/swagger/storefront/swagger.json"; ExpectedStatus = 200 },
        @{ Name = "CommerceNode CommerceAdmin Swagger"; Url = Join-Url $CommerceNodeApiUrl "/swagger/commerce-admin/swagger.json"; ExpectedStatus = 200 }
    )
}

if (-not $SkipNginx) {
    $checks += @(
        @{ Name = "CommerceNode Nginx unknown host deny"; Url = Join-Url $CommerceNodeNginxUrl "/"; ExpectedStatus = 403; Headers = @{ Host = "unknown.invalid" } }
    )
}

if ($Describe) {
    $checks | ForEach-Object {
        Write-Host "$($_.Name): $($_.Url) -> HTTP $($_.ExpectedStatus)"
    }
    exit 0
}

foreach ($check in $checks) {
    $headers = @{}
    if ($check.ContainsKey("Headers")) {
        $headers = $check.Headers
    }

    Invoke-ReleaseSmokeRequest `
        -Name $check.Name `
        -Url $check.Url `
        -ExpectedStatus $check.ExpectedStatus `
        -Headers $headers
}
