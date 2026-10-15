# Publishes the menu-only WebGL player into docs/.
# Threads stay off. GitHub Pages does not send COOP/COEP, so SharedArrayBuffer is not assumed.
# Decompression fallback stays on. Pages does not send Content-Encoding: br.
# Paths in index.html stay relative to /battle-tv-unity/. The approved jpg and mp4 are not recompressed.
param(
    [string]$Editor = "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe",
    [string]$Project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$plate = Join-Path $Project "Assets\Art\Menu\Vale-vs-Brute.jpg"
$clip = Join-Path $Project "Assets\StreamingAssets\menu\Vale-vs-Brute-push.mp4"
if ((Get-Item -LiteralPath $plate).Length -ne 282368) { throw "Plate is not 282368 bytes." }
if ((Get-Item -LiteralPath $clip).Length -ne 2145773) { throw "Clip is not 2145773 bytes." }

$log = Join-Path $env:TEMP "battle-tv-webgl.log"
if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }
Start-Process -FilePath $Editor -ArgumentList @(
    "-batchmode", "-quit", "-projectPath", $Project,
    "-executeMethod", "BattleTv.Menu.EditorTools.WebGlPublish.Publish",
    "-logFile", $log
) | Out-Null
$deadline = (Get-Date).AddMinutes(45)
$done = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    if (Test-Path -LiteralPath $log) {
        $text = Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue
        if ($text -match "\[WebGlPublish\] docs bytes") { $done = $true; break }
        if ($text -match "Scripts have compiler errors|Exiting without the bug reporter") { break }
    }
    $busy = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
        Where-Object { $_.CommandLine -like "*battle-tv-unity*" -and $_.CommandLine -like "*WebGlPublish.Publish*" }
    $quiet = (Test-Path -LiteralPath $log) -and (((Get-Date) - (Get-Item -LiteralPath $log).LastWriteTime).TotalSeconds -gt 20)
    if (-not $busy -and $quiet) { break }
}
if (-not $done) { throw "Unity WebGL publish failed. See $log" }

$index = Get-Content -LiteralPath (Join-Path $Project "docs\index.html") -Raw
if ($index -match '"/Build' -or $index -match "'/Build" -or $index -match '"/StreamingAssets' -or $index -match "'/StreamingAssets") {
    throw "index.html uses a root-absolute /Build path."
}
$publishedClip = Join-Path $Project "docs\StreamingAssets\menu\Vale-vs-Brute-push.mp4"
if ((Get-Item -LiteralPath $publishedClip).Length -ne 2145773) { throw "Published clip bytes changed." }
Write-Host "WebGL menu is in docs/. Manifest:"
Get-Content -LiteralPath (Join-Path $Project "docs\build-manifest.json")
