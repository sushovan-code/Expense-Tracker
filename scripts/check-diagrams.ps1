<#
  CI check for pull requests. Fails when a diagram source changed but its
  rendered image was not committed with it. Warns when architecture files
  changed but no diagram did. Needs: checkout with fetch-depth 0, BASE_REF set.
#>
$ErrorActionPreference = 'Stop'
$changed = @(git diff --name-only "origin/$env:BASE_REF...HEAD")
$failed = $false

foreach ($f in $changed | Where-Object { $_ -match '^docs/diagrams/.+\.(mmd|py)$' }) {
    $image = if ($f -like '*.mmd') { $f -replace '\.mmd$', '.svg' } else { $f -replace '\.py$', '.png' }
    if ($changed -notcontains $image -and (Test-Path $f)) {
        Write-Host "::error file=$f::$f changed but $image was not re-rendered. Run: pwsh -File scripts/render-diagrams.ps1"
        $failed = $true
    }
}

$archChanged = $changed | Where-Object { $_ -match '(AppHost/|^infra/|Contracts/)' }
$diagramsChanged = $changed | Where-Object { $_ -like 'docs/diagrams/*' }
if ($archChanged -and -not $diagramsChanged) {
    Write-Host "::warning::AppHost, infra/ or message contracts changed but no diagram did. Run /update-diagrams if a service, store, queue or flow changed."
}

if ($failed) { exit 1 }
Write-Host 'Diagrams are in step with their sources.'
