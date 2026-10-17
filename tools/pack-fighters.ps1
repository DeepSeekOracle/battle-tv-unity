# Copies painted plates into StreamingAssets/fighters/<id>.
# The folder is the character the match loads. Art/Duel stays the paint workshop and the cards.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$art = Join-Path $root "Assets\Art\Duel"
$out = Join-Path $root "Assets\StreamingAssets\fighters"
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Frames([string]$folder, [int]$count) {
    $list = @()
    for ($i = 0; $i -lt $count; $i++) {
        $list += "{0}/f{1:00}.png" -f $folder, $i
    }
    return $list
}

function Pack($id, $flip, $clipPivot, $walk, $idle, $clips, $idlePivot = 0.5) {
    $dir = Join-Path $out $id
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $used = @{}
    function Copy-Plate([string]$srcRel) {
        if ($used.ContainsKey($srcRel)) { return $used[$srcRel] }
        $src = Join-Path $art ($srcRel -replace "/", "\")
        if (-not (Test-Path -LiteralPath $src)) { throw "Missing plate $srcRel" }
        $name = ($srcRel -replace "[\\/]", "-")
        Copy-Item -LiteralPath $src -Destination (Join-Path $dir $name)
        $used[$srcRel] = $name
        return $name
    }
    $idleName = Copy-Plate $idle
    $outClips = @()
    foreach ($clip in $clips) {
        $frames = @()
        foreach ($rel in $clip.frames) {
            if ($rel -eq "IDLE") { $frames += $idleName }
            else { $frames += (Copy-Plate $rel) }
        }
        $item = [ordered]@{
            name = $clip.name
            seconds = $clip.seconds
            pivot = $clip.pivot
            frames = $frames
        }
        if ($clip.pace -gt 0) { $item.pace = $clip.pace }
        if ($clip.pivots) { $item.pivots = $clip.pivots }
        $outClips += $item
    }
    $entity = [ordered]@{
        id = $id
        flip = $flip
        clipPivot = [bool]$clipPivot
        walk = $walk
        idle = $idleName
    }
    if ([math]::Abs([double]$idlePivot - 0.5) -gt 0.001) { $entity.idlePivot = [double]$idlePivot }
    $entity.clips = $outClips
    $json = $entity | ConvertTo-Json -Depth 6
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Join-Path $dir "entity.json"), $json, $utf8)
    "packed $id $($used.Count) plates"
}

$brutePivot = Get-Content -Raw (Join-Path $art "brute-pivots.json") | ConvertFrom-Json
function PivotOf($key) { return [double]$brutePivot.$key }

Pack "vale" "against" $true 0.62 "vale.png" @(
    @{ name = "Shield Bash"; seconds = 0.40; pivot = 0.39; frames = (Frames "vale-bash" 6) }
    @{ name = "Shield Hook"; seconds = 0.40; pivot = 0.39; frames = (Frames "vale-bash" 6) }
    @{ name = "Cleave"; seconds = 0.34; pivot = 0.37; frames = (Frames "vale-punch" 6) }
    @{ name = "Punch"; seconds = 0.34; pivot = 0.37; frames = (Frames "vale-punch" 6) }
    @{ name = "Cover"; seconds = 0.50; pivot = 0.30; frames = (Frames "vale-toss" 8) }
    @{ name = "Bulwark"; seconds = 0.50; pivot = 0.54; frames = (Frames "vale-line" 5) }
    @{ name = "Shield Wall"; seconds = 0.40; pivot = 0.56; frames = (Frames "vale-plant" 4) }
    @{ name = "Close"; seconds = 0.40; pivot = 0.39; frames = (Frames "vale-bash" 6) }
    @{ name = "walk"; seconds = 0.62; pivot = 0.50; frames = (Frames "vale-walk" 8) }
    @{ name = "death"; seconds = 1.60; pivot = 0.42; frames = (Frames "vale-death" 4) }
    @{ name = "win"; seconds = 1.15; pivot = 0.40; frames = (Frames "vale-win" 2) }
)

Pack "brute" "with" $false 0.84 "brute-drag.png" @(
    @{ name = "Heavy Sweep"; seconds = 1.42; pivot = 0; pace = 0.72; frames = (Frames "brute-sweep" 8); pivots = @(
        (PivotOf "brute-sweep/f00.png"), (PivotOf "brute-sweep/f01.png"), (PivotOf "brute-sweep/f02.png"), (PivotOf "brute-sweep/f03.png"),
        (PivotOf "brute-sweep/f04.png"), (PivotOf "brute-sweep/f05.png"), (PivotOf "brute-sweep/f06.png"), (PivotOf "brute-sweep/f07.png")
    ) }
    @{ name = "Ground Pound"; seconds = 0.70; pivot = 0; frames = (Frames "brute-pound" 2); pivots = @((PivotOf "brute-pound/f00.png"), (PivotOf "brute-pound/f01.png")) }
    @{ name = "Ward Field"; seconds = 0.55; pivot = 0; frames = (Frames "brute-ward" 2); pivots = @((PivotOf "brute-ward/f00.png"), (PivotOf "brute-ward/f01.png")) }
    @{ name = "Close"; seconds = 0.35; pivot = 0; frames = (Frames "brute-close" 1); pivots = @((PivotOf "brute-close/f00.png")) }
    @{ name = "Slip"; seconds = 0.35; pivot = 0; frames = (Frames "brute-close" 1); pivots = @((PivotOf "brute-close/f00.png")) }
    @{ name = "Iron Grip"; seconds = 0.40; pivot = 0; frames = @("brute-grip.png") }
    @{ name = "walk"; seconds = 0.84; pivot = 0.50; frames = (Frames "brute-walk" 8) }
    @{ name = "death"; seconds = 1.55; pivot = 0; frames = (Frames "brute-death" 3); pivots = @((PivotOf "brute-death/f00.png"), (PivotOf "brute-death/f01.png"), (PivotOf "brute-death/f02.png")) }
    @{ name = "win"; seconds = 1.15; pivot = 0; frames = (Frames "brute-win" 2); pivots = @((PivotOf "brute-win/f00.png"), (PivotOf "brute-win/f01.png")) }
) 0.6164

Pack "lightfather" "against" $true 0.72 "lightfather.png" @(
    @{ name = "Seal Bolt"; seconds = 0.42; pivot = 0.50; frames = (Frames "lf-bolt" 4) }
    @{ name = ([string][char]0x0394) + "9 Seal"; seconds = 0.70; pivot = 0.50; frames = (Frames "lf-seal" 3) }
    @{ name = "Provenance"; seconds = 0.80; pivot = 0.50; frames = (Frames "lf-provenance" 3) }
    @{ name = "Cover"; seconds = 0.50; pivot = 0.50; frames = (Frames "lf-cover" 4) }
    @{ name = "Standard Raised"; seconds = 0.55; pivot = 0.50; frames = (Frames "lf-standard" 4) }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = (Frames "lf-slip" 2) }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = (Frames "lf-slip" 2) }
    @{ name = "walk"; seconds = 0.72; pivot = 0.50; frames = (Frames "lf-walk" 8) }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "lf-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "lf-win" 1) }
)

Pack "kael" "against" $true 0.76 "kael.png" @(
    @{ name = "Shard Volley"; seconds = 0.72; pivot = 0.50; frames = (Frames "kael-shard" 6) }
    @{ name = "Rend"; seconds = 0.42; pivot = 0.50; frames = (Frames "kael-rend" 3) }
    @{ name = "Shield Wall"; seconds = 0.50; pivot = 0.50; frames = (Frames "kael-wall" 2) }
    @{ name = "Squire's Charge"; seconds = 0.40; pivot = 0.50; frames = (Frames "kael-charge" 2) }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = (Frames "kael-close" 2) }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = (Frames "kael-close" 2) }
    @{ name = "walk"; seconds = 0.76; pivot = 0.50; frames = (Frames "kael-walk" 8) }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "kael-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "kael-win" 1) }
)

Pack "orin" "against" $true 0.80 "orin.png" @(
    @{ name = "Sigil Shard"; seconds = 0.36; pivot = 0.50; frames = (Frames "orin-shard" 3) }
    @{ name = "Unwrite"; seconds = 0.45; pivot = 0.50; frames = (Frames "orin-unwrite" 2) }
    @{ name = "Resonance Flask"; seconds = 0.45; pivot = 0.50; frames = (Frames "orin-flask" 2) }
    @{ name = "Chorus Flask"; seconds = 0.50; pivot = 0.50; frames = (Frames "orin-drink" 2) }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = (Frames "orin-slip" 2) }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = (Frames "orin-slip" 2) }
    @{ name = "walk"; seconds = 0.80; pivot = 0.50; frames = (Frames "orin-walk" 8) }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "orin-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "orin-win" 1) }
)

Pack "nia" "against" $true 0.58 "nia.png" @(
    @{ name = "Path Arrow"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "nia-arrow/f01.png", "IDLE") }
    @{ name = "Pinning Volley"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "nia-pin/f01.png") }
    @{ name = "Dash"; seconds = 0.42; pivot = 0.50; frames = @("nia-dash/f00.png", "IDLE") }
    @{ name = "Needle Rain"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "nia-rain/f01.png") }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = @("nia-slip/f00.png", "IDLE") }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = @("nia-slip/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.58; pivot = 0.50; frames = (Frames "nia-walk" 8) }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "nia-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "nia-win" 1) }
)

Pack "lyra" "against" $true 0.72 "lyra.png" @(
    @{ name = "Spiral Note"; seconds = 0.48; pivot = 0.50; frames = @("IDLE", "lyra-note/f01.png", "IDLE") }
    @{ name = "Hymn"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "lyra-hymn/f01.png") }
    @{ name = "Spiral Chorus"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "lyra-chorus/f01.png") }
    @{ name = "Weave Bloom"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "lyra-bloom/f01.png") }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = @("lyra-slip/f00.png", "IDLE") }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = @("lyra-slip/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.72; pivot = 0.50; frames = (Frames "lyra-walk" 8) }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "lyra-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "lyra-win" 1) }
)

Pack "arkos" "against" $true 0.74 "arkos.png" @(
    @{ name = "Lance Thrust"; seconds = 0.48; pivot = 0.50; frames = @("IDLE", "arkos-thrust/f01.png", "IDLE") }
    @{ name = "Geodesic"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "arkos-geodesic/f01.png") }
    @{ name = "True Line"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "arkos-line/f01.png") }
    @{ name = "Dash"; seconds = 0.42; pivot = 0.50; frames = @("arkos-dash/f00.png", "IDLE") }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = @("arkos-close/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.74; pivot = 0.50; frames = (Frames "arkos-walk" 4) }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "arkos-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "arkos-win" 1) }
)

Pack "d9ra" "against" $true 0.48 "d9ra.png" @(
    @{ name = "Wolf Fist"; seconds = 0.42; pivot = 0.50; frames = @("IDLE", "d9ra-fist/f01.png", "IDLE") }
    @{ name = "Shockwave"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "d9ra-shock/f01.png") }
    @{ name = "Wolf Fury"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "d9ra-fury/f01.png") }
    @{ name = "Squire's Charge"; seconds = 0.40; pivot = 0.50; frames = @("IDLE", "d9ra-charge/f01.png") }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = @("d9ra-close/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.48; pivot = 0.50; frames = @("IDLE", "d9ra-walk/f00.png") }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "d9ra-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "d9ra-win" 1) }
)

Pack "srath" "against" $true 0.48 "srath.png" @(
    @{ name = "Needle"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "srath-needle/f01.png", "IDLE") }
    @{ name = "Doublespeak"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "srath-speak/f01.png") }
    @{ name = "Needle Rain"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "srath-rain/f01.png") }
    @{ name = "Dash"; seconds = 0.42; pivot = 0.50; frames = @("srath-dash/f00.png", "IDLE") }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = @("srath-slip/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.48; pivot = 0.50; frames = @("IDLE", "srath-walk/f00.png") }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "srath-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "srath-win" 1) }
)

Pack "kairos" "against" $true 0.72 "kairos.png" @(
    @{ name = "Hour Bolt"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "kairos-bolt/f01.png", "IDLE") }
    @{ name = "Right-Time"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "kairos-time/f01.png") }
    @{ name = "Borrowed Second"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "kairos-borrow/f01.png") }
    @{ name = ([string]([char]0x0394) + "9 Seal"); seconds = 0.70; pivot = 0.50; frames = @("IDLE", "kairos-seal/f01.png") }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = @("kairos-slip/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.72; pivot = 0.50; frames = @("IDLE", "kairos-walk/f00.png") }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "kairos-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "kairos-win" 1) }
)

Pack "justicae" "against" $true 0.72 "justicae.png" @(
    @{ name = "Accord Blade"; seconds = 0.48; pivot = 0.50; frames = @("IDLE", "justicae-blade/f01.png", "IDLE") }
    @{ name = "Fair Plate"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "justicae-plate/f01.png") }
    @{ name = "Verdict"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "justicae-verdict/f01.png") }
    @{ name = "Bulwark"; seconds = 0.55; pivot = 0.50; frames = @("IDLE", "justicae-bulwark/f01.png") }
    @{ name = "Writ of Pursuit"; seconds = 0.40; pivot = 0.50; frames = @("justicae-writ/f00.png", "IDLE") }
    @{ name = "Close"; seconds = 0.35; pivot = 0.50; frames = @("justicae-close/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.72; pivot = 0.50; frames = @("IDLE", "justicae-walk/f00.png") }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "justicae-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "justicae-win" 1) }
)

Pack "seidon" "against" $true 0.72 "seidon.png" @(
    @{ name = "Tide Bolt"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "seidon-bolt/f01.png", "IDLE") }
    @{ name = "Undertow"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "seidon-undertow/f01.png") }
    @{ name = "Current"; seconds = 0.50; pivot = 0.50; frames = @("IDLE", "seidon-current/f01.png") }
    @{ name = "Geodesic"; seconds = 0.52; pivot = 0.50; frames = @("IDLE", "seidon-geo/f01.png") }
    @{ name = "Slip"; seconds = 0.35; pivot = 0.50; frames = @("seidon-slip/f00.png", "IDLE") }
    @{ name = "walk"; seconds = 0.72; pivot = 0.50; frames = @("IDLE", "seidon-walk/f00.png") }
    @{ name = "death"; seconds = 1.55; pivot = 0.50; frames = (Frames "seidon-death" 1) }
    @{ name = "win"; seconds = 1.15; pivot = 0.50; frames = (Frames "seidon-win" 1) }
)
