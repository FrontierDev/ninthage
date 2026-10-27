# Ninth Age — Phase 1.0 Performance Evidence Catalogue

**Active provisional Editor reference campaign:** `campaign-20261015T205113129Z-d1d3d540`  
**Source workload SHA:** `d22a939232c842a1415bd627c2a6a0be3663ce95`, branch `dev`, clean  
**Evidence captured on GitHub in commit:** `c2840577b7d098e7c9118d7f56161721038e7663` (distinct from workload SHA)  
**Paths:** `Builds/Benchmarks/Results/Campaigns/campaign-20261015T205113129Z-d1d3d540/{campaign.json,index.json}` and `Builds/Benchmarks/Results/run-*/`  
**Updated:** 2026-10-15  
**Documentation:** [baseline](Phase-1-Performance-Baseline.md), [budgets](Phase-1-Performance-Budgets.md)

> This catalogue describes historical Editor/test-harness evidence only. The harness is retired and is not an accepted evidence source going forward. Current Stage 1.5+ evidence is collected from an ordinary dedicated server launched with `--profiling`, real connected clients and ordinary runtime population/gameplay, producing `performance-report-*.json` beside the active Unity server log. Synthetic fixture counts and archived `samples.ndjson` files do not substitute for live-server measurements.

> The earlier `campaign-20261015T095750417Z-878db4d7` campaign is **superseded**, not merged. The owner plans to delete that obsolete campaign after the updated harness/evidence is accepted. This catalogue refers to the newer 20:51–21:04 UTC campaign only.

## 1. Retrieval and integrity state

- The current GitHub tree contains **25 run directories**, the campaign manifest/index and, for each run, `fixture.json`, `run.json`, `summary.json`, `report.md`, `artifacts.json` and bounded `samples.ndjson`. All **required** artifacts named in the index are tracked, and their **repository blob byte sizes match** the index. This is a structural/byte-size cross-check, **not an independent SHA-256 re-hash**.
- All 25 index entries report `status=COMPLETE-MEASURED` and `artifact_index_status=verified` **at generation time**. That stored status does not prove that a fresh clone contains every declared optional artifact.
- **Archive status: INCOMPLETE in a fresh GitHub clone.** All 25 manifests declare `logs/runner.log` as `present` / `optional=true` / `required=false` with hashes and byte sizes, but none of these files is tracked because the repository ignores `*.log`. This is an archive-completeness issue **rather than a measured-metric failure** under the revised reader.
- **No required artifact is missing**, and no byte-count mismatch was found for tracked required/optional artifacts. The raw files were not independently re-hashed in this review. Do not invent the missing runner logs, silently alter hashes, or claim a complete portable archive.
- The old campaign must **not** be repaired or rerun as a prerequisite to this new baseline. If the *new* campaign's original optional runner logs still exist locally, they may be included without rerunning tests: `git add -f Builds/Benchmarks/Results/run-*/logs/runner.log` (verify these match the new campaign IDs before committing). If absent, keep `INCOMPLETE` explicitly.

**Immutable campaign manifest:** [committed campaign manifest](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/Campaigns/campaign-20261015T205113129Z-d1d3d540/campaign.json)  
**Immutable index:** [committed campaign index](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/Campaigns/campaign-20261015T205113129Z-d1d3d540/index.json)

## 2. All 25 recorded attempts

Each link opens that run's original `artifacts.json` containing per-file paths, cryptographic digests, lengths and collection status. **The hash in a run's manifest is a recorded source value and must not be replaced to conceal absent files.**

| Job / repeat | Archived run artifact manifest | Recorded execution | Index generated-state | GitHub integrity note |
| --- | --- | --- | --- | --- |
| ECS Minimal / 1 | [`run-20261015T205158215Z-6ec0a0a1`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205158215Z-6ec0a0a1/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Minimal / 2 | [`run-20261015T205220496Z-442cfc8d`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205220496Z-442cfc8d/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Minimal / 3 | [`run-20261015T205250832Z-b59359e0`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205250832Z-b59359e0/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Minimal / 4 | [`run-20261015T205320467Z-d4bd44e8`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205320467Z-d4bd44e8/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Minimal / 5 | [`run-20261015T205350925Z-606932d6`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205350925Z-606932d6/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Activity / 1 | [`run-20261015T205421753Z-4c2ff9d4`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205421753Z-4c2ff9d4/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Activity / 2 | [`run-20261015T205452021Z-85be7c27`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205452021Z-85be7c27/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Activity / 3 | [`run-20261015T205522723Z-74800cc2`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205522723Z-74800cc2/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Activity / 4 | [`run-20261015T205553060Z-a8f424dd`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205553060Z-a8f424dd/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| ECS Activity / 5 | [`run-20261015T205623808Z-e603a07d`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205623808Z-e603a07d/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Outdoor / 1 | [`run-20261015T205654257Z-7ddb53fe`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205654257Z-7ddb53fe/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Outdoor / 2 | [`run-20261015T205724864Z-05e4723c`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205724864Z-05e4723c/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Outdoor / 3 | [`run-20261015T205756061Z-43e7464b`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205756061Z-43e7464b/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Outdoor / 4 | [`run-20261015T205828168Z-551c0fab`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205828168Z-551c0fab/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Outdoor / 5 | [`run-20261015T205900042Z-b4469cdb`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205900042Z-b4469cdb/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Forest / 1 | [`run-20261015T205931604Z-6c330bed`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T205931604Z-6c330bed/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Forest / 2 | [`run-20261015T210003555Z-cb0e5ace`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210003555Z-cb0e5ace/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Forest / 3 | [`run-20261015T210035468Z-8e201942`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210035468Z-8e201942/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Forest / 4 | [`run-20261015T210106923Z-c4348e77`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210106923Z-c4348e77/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Forest / 5 | [`run-20261015T210138457Z-be889ab1`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210138457Z-be889ab1/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Settlement / 1 | [`run-20261015T210209875Z-aa0ec874`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210209875Z-aa0ec874/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Settlement / 2 | [`run-20261015T210241860Z-ebcd4324`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210241860Z-ebcd4324/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Settlement / 3 | [`run-20261015T210313501Z-36a2bde8`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210313501Z-36a2bde8/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Settlement / 4 | [`run-20261015T210344812Z-6bb132e5`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210344812Z-6bb132e5/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |
| Graphical Settlement / 5 | [`run-20261015T210416552Z-2c74488b`](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/run-20261015T210416552Z-2c74488b/artifacts.json) | `COMPLETE-MEASURED` | verified | missing optional `logs/runner.log` |

All five job groups have exactly five independent completed run IDs. The manifest includes no failed/retry attempts for this archive. See the [baseline](Phase-1-Performance-Baseline.md) for the correctness/metric qualification distinction and run-level outlier observations.

## 3. Independent offline verification — PowerShell

Run from the repository root **after checking out the commit that holds the intended evidence**. This validates each recorded present file with SHA-256 and byte count, reports optional declared-present files separately, and does **not** alter original evidence.

```powershell
$results = (Resolve-Path 'Builds/Benchmarks/Results').Path
$campaignId = 'campaign-20261015T205113129Z-d1d3d540'
$manifest = Join-Path $results "Campaigns/$campaignId/campaign.json"
$index = Join-Path $results "Campaigns/$campaignId/index.json"
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf) -or
    -not (Test-Path -LiteralPath $index -PathType Leaf)) {
    throw "Required campaign manifest/index missing: $campaignId"
}

$indexData = Get-Content -LiteralPath $index -Raw | ConvertFrom-Json
$missingRequired = @()
$missingOptionalPresent = @()
$corrupt = @()
foreach ($attempt in @($indexData.attempts)) {
    $runRoot = Join-Path $results ([string]$attempt.run_id)
    foreach ($file in @($attempt.artifacts)) {
        $relative = [string]$file.path
        if ([string]::IsNullOrWhiteSpace($relative)) { throw "Empty artifact path: $($attempt.run_id)" }
        if ([string]$file.status -eq 'absent') {
            if ([bool]$file.required) { $missingRequired += "$($attempt.run_id)/$relative (declared absent)" }
            continue
        }
        $path = Join-Path $runRoot ($relative -replace '/', '\')
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            if ([bool]$file.required) { $missingRequired += "$($attempt.run_id)/$relative" }
            else { $missingOptionalPresent += "$($attempt.run_id)/$relative" }
            continue
        }
        $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        $actualBytes = (Get-Item -LiteralPath $path).Length
        if ($actualHash -ne ([string]$file.sha256).ToLowerInvariant() -or
            $actualBytes -ne [long]$file.bytes) { $corrupt += "$($attempt.run_id)/$relative" }
    }
}

"Runs in index: $(@($indexData.attempts).Count)"
"Missing required artifacts: $($missingRequired.Count)"
"Missing declared-present optional artifacts: $($missingOptionalPresent.Count)"
"Corrupt present artifacts: $($corrupt.Count)"
$missingRequired
$missingOptionalPresent
$corrupt
if ($missingRequired.Count -gt 0 -or $corrupt.Count -gt 0) {
    throw 'Evidence integrity validation failed.'
}
if ($missingOptionalPresent.Count -gt 0) {
    Write-Warning 'Archive INCOMPLETE: declared-present optional artifacts are missing.'
}
```

Run the verification against a copy/clean clone if assessing **portability**, rather than relying only on locally ignored files. The index's stored `verified` string is historical metadata, not an independent current-filesystem assertion. If publishing or copying the archive, preserve failures, blocked/incomplete attempts and unchanged manifests; do not silently remove anomalous runs.

## 4. Qualification and reproducibility notes

- All 25 summaries report `COMPLETE-MEASURED`, correctness `pass` and cleanup complete; the three graphical scenarios also report five of five `rendering=verified`. All selected numeric-budget metrics have 5/5 p99-capable qualified measurements.
- 2 s ramp / 2 s warmup / 5 s measured / 1 s recovery; Unity 6000.3.10f1; Ryzen 7 5700X; RTX 4060 Ti; Direct3D12 / URP Forward+; seed 424242. Exact other configuration and provenance values are in `campaign.json`.
- Per-run frame percentiles represent measured work. **Queue wall-clock time includes Editor startup/reload/restoration and is diagnostic only**, not work time; differences in tab focus or operating-system load cannot be deduced without direct evidence.
- Some graphics metrics are absent for unsupported profiles, but no required metric was filled with fabricated zero. Route interval metrics with ~11 samples do not qualify for p99 (<100).
- Editor baseline approval by the project owner remains pending. No standalone/headless/networked capacity conclusion is supported by this dataset.
