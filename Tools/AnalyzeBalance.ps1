param(
    [double]$EffectiveDpsPerGold = 0.12,
    [double]$ExposureSeconds = 12
)
# Read-only first-order model, not a NavMesh/combat simulation.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$culture = [Globalization.CultureInfo]::InvariantCulture
function Read-Number([string]$text, [string]$field) {
    $match = [regex]::Match($text, '(?m)^  ' + [regex]::Escape($field) + ': ([0-9.]+)\s*$')
    if (-not $match.Success) { throw "Missing numeric field: $field" }
    return [double]::Parse($match.Groups[1].Value, $culture)
}
function Read-Asset([string]$relative) {
    return [IO.File]::ReadAllText((Join-Path $projectRoot $relative))
}
if ($EffectiveDpsPerGold -le 0 -or $ExposureSeconds -le 0) {
    throw 'Model assumptions must be positive.'
}
$cannon = Read-Asset 'Assets/Data/Towers/Attacks/CannonAttackData.asset'
$lightning = Read-Asset 'Assets/Data/Towers/Attacks/LightningAttackData.asset'
$towerRows = foreach ($name in 'Basic','Cannon','Lightning','Flame','Arcane','Arrow') {
    $data = Read-Asset ("Assets/Data/Towers/" + $name + "TowerData.asset")
    $damage = Read-Number $data 'damage'
    $rate = Read-Number $data 'fireRate'
    $cost = Read-Number $data 'cost'
    if ($cost -le 0 -or $rate -le 0) { throw "Invalid tower: $name" }
    $single = $damage * $rate
    $three = $single
    $four = $single
    if ($name -eq 'Cannon') {
        $splash = Read-Number $cannon 'splashDamage'
        $three = ($damage + 2 * $splash) * $rate
        $four = ($damage + 3 * $splash) * $rate
    } elseif ($name -eq 'Lightning') {
        $targets = 1 + (Read-Number $lightning 'maxBounces')
        $three = $single * [Math]::Min(3, $targets)
        $four = $single * [Math]::Min(4, $targets)
    } elseif ($name -eq 'Flame') {
        $three = $single * 3
        $four = $single * 4
    }
    [pscustomobject]@{
        Tower=$name; Cost=$cost; Range=(Read-Number $data 'range')
        DPS1=[Math]::Round($single,2); DPS3=[Math]::Round($three,2)
        DPS4=[Math]::Round($four,2); DPS3per100G=[Math]::Round($three * 100 / $cost,2)
    }
}
$scene = Read-Asset 'Assets/Game/Scenes/Game.unity'
$budget = Read-Number $scene 'startingGold'
$enemy = Read-Asset 'Assets/Data/Enemies/ZombieData.asset'
$baseHP = Read-Number $enemy 'maxHealth'
$baseReward = Read-Number $enemy 'goldReward'
$files = @(Get-ChildItem (Join-Path $projectRoot 'Assets/Data/Waves') -Filter 'Wave_*.asset' | Sort-Object Name)
if ($files.Count -ne 20) { throw "Expected 20 waves, found $($files.Count)" }
$waveBlock = [regex]::Match($scene, '(?s)  waves:\r?\n(.*?)  enemySpawner:').Groups[1].Value
$sceneGuids = @([regex]::Matches($waveBlock, 'guid: ([a-f0-9]{32})') | ForEach-Object { $_.Groups[1].Value })
if ($sceneGuids.Count -ne 20) { throw 'Game scene must reference 20 waves.' }
$knownGuids = @{}
$waveRows = for ($i = 0; $i -lt $files.Count; $i++) {
    $file = $files[$i]
    if ($file.BaseName -ne ('Wave_{0:00}' -f ($i+1))) { throw 'Wave sequence has gaps.' }
    $meta = [IO.File]::ReadAllText($file.FullName + '.meta')
    $guid = [regex]::Match($meta, '(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
    if ($guid -eq '' -or $knownGuids.ContainsKey($guid)) { throw "Invalid GUID: $file" }
    $knownGuids[$guid] = $true
    if ($sceneGuids[$i] -ne $guid) { throw "Wrong wave order in scene: $file" }
    $data = [IO.File]::ReadAllText($file.FullName)
    $count = Read-Number $data 'enemyCount'
    $hp = $baseHP * (Read-Number $data 'healthMultiplier')
    $speed = Read-Number $data 'speedMultiplier'
    $interval = Read-Number $data 'timeBetweenEnemies'
    $reward = [Math]::Max(1, [Math]::Round($baseReward * (Read-Number $data 'goldRewardMultiplier'), [MidpointRounding]::ToEven))
    if ($count -le 0 -or $hp -le 0 -or $speed -le 0 -or $interval -le 0) { throw "Invalid wave: $file" }
    # SpawnWave waits after EVERY spawn, including the last one.
    $spawnSeconds = $count * $interval
    $requiredDps = $hp * $count / ($spawnSeconds + $ExposureSeconds / $speed)
    $row = [pscustomobject]@{
        Wave=$i+1; Enemies=$count; HP=[Math]::Round($hp,1); Speed=$speed
        Interval=$interval; GoldEach=$reward; WaveGold=$count*$reward
        BudgetBefore=$budget; Pressure=[Math]::Round($requiredDps / ($budget*$EffectiveDpsPerGold),3)
    }
    $budget += $count * $reward
    $row
}
$towerRows | Format-Table -AutoSize
$waveRows | Format-Table -AutoSize
Write-Output "Lifetime gold if every enemy dies: $budget (includes starting gold; not unspent balance)."
Write-Output "Model: $EffectiveDpsPerGold effective DPS/gold; $ExposureSeconds seconds exposure at speed x1."
Write-Output 'Pressure is a tuning heuristic, not a measured difficulty or win probability.'
