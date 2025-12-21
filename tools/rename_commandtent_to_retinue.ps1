param(
    [string]$Root = "src"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Root)) {
    throw "Root path not found: $Root"
}

$files = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter *.cs
$updatedCount = 0

foreach ($f in $files) {
    $path = $f.FullName
    $old = Get-Content -LiteralPath $path -Raw
    $new = $old.Replace("Enlisted.Features.CommandTent", "Enlisted.Features.Retinue").Replace("Features.CommandTent", "Features.Retinue")

    if ($new -ne $old) {
        # Use UTF8 to avoid PowerShell's default UTF-16 output.
        Set-Content -LiteralPath $path -Value $new -Encoding UTF8 -NoNewline
        $updatedCount++
    }
}

Write-Host "Updated $updatedCount file(s)."


