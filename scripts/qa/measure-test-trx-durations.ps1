param(
    [Parameter(Mandatory = $true)]
    [string] $Path,

    [int] $Top = 15,

    [double] $SlowThresholdSeconds = 10,

    [string] $ClassFilter = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Path)) {
    throw "TRX file was not found: $Path"
}

[xml] $trx = Get-Content -LiteralPath $Path
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$namespaceManager.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$testsById = @{}
$trx.SelectNodes("//t:UnitTest", $namespaceManager) | ForEach-Object {
    $testMethod = $_.SelectSingleNode("t:TestMethod", $namespaceManager)
    $testsById[$_.id] = [pscustomobject] @{
        Name = $_.name
        Class = $testMethod.className
    }
}

$results = $trx.SelectNodes("//t:UnitTestResult", $namespaceManager) | ForEach-Object {
    $duration = [TimeSpan]::Parse($_.duration)
    $test = $testsById[$_.testId]
    [pscustomobject] @{
        Name = $_.testName
        Class = $test.Class
        Outcome = $_.outcome
        Seconds = [math]::Round($duration.TotalSeconds, 3)
    }
}

if (-not [string]::IsNullOrWhiteSpace($ClassFilter)) {
    $results = $results | Where-Object { $_.Class -like "*$ClassFilter*" }
}

$times = $trx.SelectSingleNode("//t:Times", $namespaceManager)
$counters = $trx.SelectSingleNode("//t:Counters", $namespaceManager)
$slowResults = @($results | Where-Object { $_.Seconds -ge $SlowThresholdSeconds })
$skippedCount = [int] $counters.total - [int] $counters.executed

Write-Host "TRX: $Path"
Write-Host "Start: $($times.start)"
Write-Host "Finish: $($times.finish)"
Write-Host "Total: $($counters.total), Executed: $($counters.executed), Passed: $($counters.passed), Failed: $($counters.failed), Skipped: $skippedCount"
Write-Host "Slow threshold: ${SlowThresholdSeconds}s"
Write-Host "Slow tests: $($slowResults.Count), Sum: $([math]::Round(($slowResults | Measure-Object Seconds -Sum).Sum, 1))s"
Write-Host ""

Write-Host "Duration buckets"
$results |
    ForEach-Object {
        $bucket = if ($_.Seconds -ge 30) {
            ">=30s"
        } elseif ($_.Seconds -ge $SlowThresholdSeconds) {
            "$SlowThresholdSeconds-30s"
        } elseif ($_.Seconds -ge 1) {
            "1-${SlowThresholdSeconds}s"
        } else {
            "<1s"
        }

        $_ | Add-Member -NotePropertyName Bucket -NotePropertyValue $bucket -Force
        $_
    } |
    Group-Object Bucket |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject] @{
            Bucket = $_.Name
            Count = $_.Count
            SumSeconds = [math]::Round(($_.Group | Measure-Object Seconds -Sum).Sum, 1)
        }
    } |
    Format-Table -AutoSize

Write-Host "Class totals"
$results |
    Group-Object Class |
    ForEach-Object {
        [pscustomobject] @{
            Class = $_.Name
            Count = $_.Count
            SumSeconds = [math]::Round(($_.Group | Measure-Object Seconds -Sum).Sum, 1)
            MaxSeconds = [math]::Round(($_.Group | Measure-Object Seconds -Maximum).Maximum, 1)
        }
    } |
    Sort-Object SumSeconds -Descending |
    Select-Object -First $Top |
    Format-Table -AutoSize

Write-Host "Top tests"
$results |
    Sort-Object Seconds -Descending |
    Select-Object -First $Top Name, Class, Seconds, Outcome |
    Format-Table -AutoSize
