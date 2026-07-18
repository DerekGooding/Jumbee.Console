<#
.SYNOPSIS
    Generates the Markdown API reference for the Jumbee.Console core libraries.

.DESCRIPTION
    Runs `docfx metadata` (per docfx.json) to emit one Markdown page per public type into docs/api,
    then builds docs/api/README.md — a namespace-grouped index (with each type's summary) that GitHub
    renders automatically when the folder is opened. docfx does not produce such an index itself; it
    only emits toc.yml (for its HTML site) and the per-namespace pages.

.NOTES
    Run from the repo root:  powershell -File build-api-docs.ps1
    (Skip the docfx step with -NoMetadata if the pages are already current.)
#>
[CmdletBinding()]
param(
    [switch] $NoMetadata
)

Write-Host 'Building Jumbee.Console API documentation...' -ForegroundColor Blue

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$apiDir = Join-Path $repoRoot 'docs/api'

if (-not $NoMetadata) {
    Write-Host 'Running docfx metadata...' -ForegroundColor Cyan
    dotnet docfx metadata (Join-Path $repoRoot 'docfx.json')
    if ($LASTEXITCODE -ne 0) { throw "docfx metadata failed (exit $LASTEXITCODE)." }
}

Write-Host 'Building docs/api/README.md index...' -ForegroundColor Cyan

# Category display order within each namespace.
$kindOrder = @{ 'Class' = 0; 'Struct' = 1; 'Interface' = 2; 'Enum' = 3; 'Delegate' = 4 }
$kindHeading = @{ 'Class' = 'Classes'; 'Struct' = 'Structs'; 'Interface' = 'Interfaces'; 'Enum' = 'Enums'; 'Delegate' = 'Delegates' }
$dash = [string][char]0x2014   # em dash (kept out of the script literal to avoid encoding issues)

$types = New-Object System.Collections.Generic.List[object]

Get-ChildItem -Path $apiDir -Filter '*.md' | Where-Object { $_.Name -ne 'README.md' } | ForEach-Object {
    $lines = Get-Content -LiteralPath $_.FullName -Encoding UTF8
    if ($lines.Count -eq 0) { return }

    # H1, e.g. '# <a id="Jumbee_Console_Globe"></a> Class Globe'
    if ($lines[0] -notmatch '^#\s+(?:<a id="[^"]*"></a>\s*)?(Namespace|Class|Struct|Interface|Enum|Delegate)\s+(.+?)\s*$') { return }
    $kind = $Matches[1]
    if ($kind -eq 'Namespace') { return }   # namespace pages are section anchors, not entries

    $ns = ''
    foreach ($l in $lines) { if ($l -match '^Namespace:\s+\[([^\]]+)\]') { $ns = $Matches[1]; break } }
    if (-not $ns) { return }

    # Display name = file basename minus the "<namespace>." prefix (keeps nested types qualified, e.g. BarChart.HorizontalBar).
    $base = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    $display = $base
    if ($base.StartsWith("$ns.")) { $display = $base.Substring($ns.Length + 1) }

    # Summary = the text between the "Assembly:" line and the first ```csharp fence.
    $summary = ''
    $ai = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^Assembly:') { $ai = $i; break } }
    if ($ai -ge 0) {
        $buf = @()
        for ($i = $ai + 1; $i -lt $lines.Count; $i++) {
            $t = $lines[$i]
            if ($t -match '^\s*```') { break }
            if ($t.Trim() -ne '') { $buf += $t.Trim() }
        }
        $summary = ($buf -join ' ') -replace '\s+', ' '
    }

    # docfx emits inline <xref>/<code>/<em>/<b> in summaries; convert to plain Markdown so it reads on GitHub.
    $bt = [string][char]96   # backtick
    $summary = [regex]::Replace($summary, '<xref href="([^"]+)"[^>]*>\s*</xref>', {
        param($m) $uid = $m.Groups[1].Value -replace '\(.*$', ''; ('{0}{1}{0}' -f [string][char]96, ($uid -split '\.')[-1])
    })
    $summary = $summary -replace '</?code>', $bt
    $summary = $summary -replace '</?(em|i)>', '*'
    $summary = $summary -replace '</?(b|strong)>', '**'
    $summary = $summary -replace '<[^>]+>', ''          # drop any remaining tags
    $summary = $summary -replace '&lt;', '<' -replace '&gt;', '>' -replace '&quot;', '"' -replace '&#39;', "'" -replace '&amp;', '&'
    $summary = ($summary -replace '\s+', ' ').Trim()

    $types.Add([pscustomobject]@{
        Namespace = $ns
        Display   = $display
        File      = $_.Name
        Kind      = $kind
        Summary   = $summary
    })
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# Jumbee.Console API Reference')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('Auto-generated from the core libraries'' XML-doc comments with [docfx](https://dotnet.github.io/docfx/).')
[void]$sb.AppendLine('**Do not edit by hand** - regenerate with `powershell -File build-api-docs.ps1` from the repo root.')
[void]$sb.AppendLine('')

foreach ($ns in ($types | Select-Object -ExpandProperty Namespace -Unique | Sort-Object)) {
    [void]$sb.AppendLine("## $ns")
    [void]$sb.AppendLine('')
    $inNs = $types | Where-Object { $_.Namespace -eq $ns }
    $groups = $inNs | Group-Object Kind | Sort-Object { $kindOrder[$_.Name] }
    foreach ($g in $groups) {
        [void]$sb.AppendLine("### $($kindHeading[$g.Name])")
        [void]$sb.AppendLine('')
        foreach ($t in ($g.Group | Sort-Object Display)) {
            $line = "- [$($t.Display)]($($t.File))"
            if ($t.Summary) { $line += " $dash $($t.Summary)" }
            [void]$sb.AppendLine($line)
        }
        [void]$sb.AppendLine('')
    }
}

$readmePath = Join-Path $apiDir 'README.md'
$enc = New-Object System.Text.UTF8Encoding($false)   # UTF-8, no BOM
[System.IO.File]::WriteAllText($readmePath, $sb.ToString(), $enc)

Write-Host ("Wrote {0} ({1} types across {2} namespaces)." -f $readmePath, $types.Count, (($types | Select-Object -ExpandProperty Namespace -Unique).Count)) -ForegroundColor Green
