<#
  Renders every diagram in docs/diagrams.
    *.mmd -> *.svg  (Mermaid CLI via npx, pinned version)
    *.py  -> *.png  (Python "diagrams" library + Graphviz)
  Run from anywhere:  pwsh -File scripts/render-diagrams.ps1
  Used by CI too, so local and CI render the same way.
#>
$ErrorActionPreference = 'Stop'
$dir = Resolve-Path "$PSScriptRoot/../docs/diagrams"
$mmdc = '@mermaid-js/mermaid-cli@11.4.2'

Get-ChildItem $dir -Filter *.mmd | ForEach-Object {
    $out = [IO.Path]::ChangeExtension($_.FullName, '.svg')
    Write-Host "Mermaid  $($_.Name) -> $([IO.Path]::GetFileName($out))"
    npx -y $mmdc -i $_.FullName -o $out -b white `
        -c "$dir/mermaid-config.json" -p "$dir/puppeteer-config.json" -q
    if ($LASTEXITCODE) { throw "Mermaid failed on $($_.Name)" }
}

Push-Location $dir
try {
    Get-ChildItem -Filter *.py | ForEach-Object {
        Write-Host "diagrams $($_.Name) -> $($_.BaseName).png"
        python $_.Name
        if ($LASTEXITCODE) { throw "diagrams failed on $($_.Name)" }
    }
} finally { Pop-Location }

Write-Host 'Done. Open the .svg/.png files to check them, then commit sources and images together.'
