param(
    [int]$MaxWave = 50,
    [double]$EarlySpendFraction = 0.90,
    [double]$LateSpendFraction = 0.60,
    [int]$MaxTowers = 100,
    [double]$Coverage = 0.65,
    [double]$PathLength = 60,
    [switch]$ShowAllWaves
)

# Read-only first-order model for the infinite-wave rules in WaveManager.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$culture = [Globalization.CultureInfo]::InvariantCulture

function Read-Number([string]$text, [string]$field) {
    $match = [regex]::Match($text,
        '(?m)^  ' + [regex]::Escape($field) + ': ([0-9.]+)\s*$')
    if (-not $match.Success) { throw "Missing numeric field: $field" }
    return [double]::Parse($match.Groups[1].Value, $culture)
}

function Read-Asset([string]$relative) {
    return [IO.File]::ReadAllText((Join-Path $projectRoot $relative))
}

if ($MaxWave -lt 1 -or $EarlySpendFraction -le 0 -or $EarlySpendFraction -gt 1 -or
    $LateSpendFraction -le 0 -or $LateSpendFraction -gt $EarlySpendFraction -or
    $MaxTowers -lt 1 -or $Coverage -le 0 -or $Coverage -gt 1 -or
    $PathLength -le 0) {
    throw 'Model assumptions must be positive.'
}

$cannon = Read-Asset 'Assets/Data/Towers/Attacks/CannonAttackData.asset'
$cannonTower = Read-Asset 'Assets/Data/Towers/CannonTowerData.asset'
$lightning = Read-Asset 'Assets/Data/Towers/Attacks/LightningAttackData.asset'
$cannonRadius = Read-Number $cannon 'splashRadius'
$lightningRange = Read-Number $lightning 'bounceRange'
$lightningBounces = Read-Number $lightning 'maxBounces'
$flame = Read-Asset 'Assets/Data/Towers/Attacks/FlameAttackData.asset'
$flameAngle = Read-Number $flame 'coneAngle'
$towerRows = foreach ($name in 'Basic','Cannon','Lightning','Flame','Arcane') {
    $data = Read-Asset ("Assets/Data/Towers/" + $name + "TowerData.asset")
    $damage = Read-Number $data 'damage'
    $rate = Read-Number $data 'fireRate'
    $cost = Read-Number $data 'cost'
    $single = $damage * $rate
    $four = $single
    if ($name -eq 'Cannon') {
        $splash = Read-Number $cannon 'splashDamage'
        $four = ($damage + 3 * $splash) * $rate
    } elseif ($name -eq 'Lightning') {
        $targets = 1 + (Read-Number $lightning 'maxBounces')
        $four = $single * [Math]::Min(4, $targets)
    } elseif ($name -eq 'Flame') {
        $four = $single * 4
    }
    [pscustomobject]@{
        Tower = $name
        Cost = $cost
        Range = Read-Number $data 'range'
        DPS1 = [Math]::Round($single, 2)
        DPS4 = [Math]::Round($four, 2)
        DPS4per100G = [Math]::Round($four * 100 / $cost, 2)
    }
}

$scene = Read-Asset 'Assets/Game/Scenes/Game.unity'
$enemy = Read-Asset 'Assets/Data/Enemies/ZombieData.asset'
$budget = Read-Number $scene 'startingGold'
$baseHealth = Read-Number $enemy 'maxHealth'
$baseReward = Read-Number $enemy 'goldReward'
$baseCount = Read-Number $scene 'baseEnemyCount'
$countGrowth = Read-Number $scene 'enemiesAddedPerWave'
$countVariance = Read-Number $scene 'enemyCountVariance'
$swarmCountMultiplier = Read-Number $scene 'swarmCountMultiplier'
$fastCountMultiplier = Read-Number $scene 'fastCountMultiplier'
$specialCountMultiplier = Read-Number $scene 'specialCountMultiplier'
$baseBatchSize = Read-Number $scene 'baseSpawnBatchSize'
$initialInterval = Read-Number $scene 'initialSpawnInterval'
$minimumInterval = Read-Number $scene 'minimumSpawnInterval'
$intervalDecay = Read-Number $scene 'spawnIntervalDecay'
$initialHealth = Read-Number $scene 'initialHealthMultiplier'
$healthGrowth = Read-Number $scene 'healthGrowthPerWave'
$swarmHealth = Read-Number $scene 'swarmHealthMultiplier'
$fastHealth = Read-Number $scene 'fastHealthMultiplier'
$maximumHealth = Read-Number $scene 'maximumHealthMultiplier'
$speedGrowth = Read-Number $scene 'speedAddedPerWave'
$maximumSpeed = Read-Number $scene 'maximumSpeedMultiplier'
$initialReward = Read-Number $scene 'initialGoldRewardMultiplier'
$rewardGrowth = Read-Number $scene 'rewardGrowthEveryFiveWaves'
$specialReward = Read-Number $scene 'specialRewardMultiplier'

# Fraction of lifetime gold invested in this illustrative mixed defense.
# Each share buys a family of towers; the rest stays liquid for new builds.
$mix = @(
    @{Name='Basic'; Share=0.10},
    @{Name='Cannon'; Share=0.30},
    @{Name='Lightning'; Share=0.25},
    @{Name='Flame'; Share=0.25},
    @{Name='Arcane'; Share=0.10}
)
$costByName = @{}
$dpsByName = @{}
foreach ($row in $towerRows) {
    $costByName[$row.Tower] = $row.Cost
    $dpsByName[$row.Tower] = $row.DPS1
}
$inverseWeightedCost = 0.0
foreach ($family in $mix) {
    $inverseWeightedCost += $family.Share / $costByName[$family.Name]
}

$waveRows = for ($wave = 1; $wave -le $MaxWave; $wave++) {
    $step = $wave - 1
    $special = $wave % 5 -eq 0
    $swarm = -not $special -and $wave % 3 -eq 0
    $fast = -not $special -and $wave % 4 -eq 0

    $countVariation = 1 + [Math]::Sin($wave * 1.37 + 1.77) * $countVariance
    $typeCountMultiplier = if ($special) {
        $specialCountMultiplier
    } elseif ($swarm) {
        $swarmCountMultiplier
    } elseif ($fast) {
        $fastCountMultiplier
    } else {
        1
    }
    $count = [Math]::Round(
        ($baseCount + $step * $countGrowth) *
        $countVariation * $typeCountMultiplier
    )
    $count = [Math]::Min(400, [Math]::Max(1, [int]$count))

    $batchSize = [int]$baseBatchSize + [Math]::Floor($step / 12)
    if ($swarm) { $batchSize++ }
    $batchSize = [Math]::Min(5, [Math]::Max(1, $batchSize))

    $healthMultiplier = $initialHealth * [Math]::Pow($healthGrowth, $step)
    if ($swarm) { $healthMultiplier *= $swarmHealth }
    if ($fast -and -not $swarm) { $healthMultiplier *= $fastHealth }
    if ($special) { $healthMultiplier *= 1.4 }
    $healthMultiplier = [Math]::Min(
        $maximumHealth,
        [Math]::Max(0.1, $healthMultiplier)
    )

    $speed = 1 + $step * $speedGrowth
    if ($fast) { $speed *= 1.12 }
    if ($special) { $speed *= 0.92 }
    $speed = [Math]::Min($maximumSpeed, [Math]::Max(0.75, $speed))

    $interval = $initialInterval * [Math]::Pow($intervalDecay, $step)
    if ($swarm) { $interval *= 0.8 }
    if ($special) { $interval *= 1.1 }
    $interval = [Math]::Max($minimumInterval, $interval)

    $rewardMultiplier = $initialReward +
        [Math]::Floor($step / 5) * $rewardGrowth
    if ($special) { $rewardMultiplier *= $specialReward }
    $rewardMultiplier = [Math]::Min(1.5, $rewardMultiplier)
    $reward = [Math]::Max(1, [Math]::Round(
        $baseReward * $rewardMultiplier))

    $health = $baseHealth * $healthMultiplier
    $spawnBatches = [Math]::Ceiling($count / $batchSize)
    $spawnSeconds = [Math]::Max(0, $spawnBatches - 1) * $interval
    $groupHealth = $health * $count
    $spendFraction = $LateSpendFraction +
        ($EarlySpendFraction - $LateSpendFraction) * [Math]::Exp(-$step / 8.0)
    $invested = $budget * $spendFraction
    $possibleTowers = $invested * $inverseWeightedCost
    $towerScale = [Math]::Min(1.0, [double]$MaxTowers / $possibleTowers)
    # Enemies per metre is only a flow proxy; actual navmesh crowding and
    # target selection are not represented by this static estimate.
    $density = $batchSize / ($interval * (2 * $speed))
    $cannonTargets = 1 + [Math]::Min(4.0, $density * $cannonRadius * 0.15)
    $lightningTargets = 1 + [Math]::Min([double]$lightningBounces,
        $density * $lightningRange * 0.18)
    $flameTargets = 1 + [Math]::Min(5.0,
        $density * ($flameAngle / 55) * 0.45)
    $splashDps = (Read-Number $cannon 'splashDamage') *
        $dpsByName['Cannon'] / (Read-Number $cannonTower 'damage')
    $effectiveDps = 0.0
    $towerCounts = @{}
    foreach ($family in $mix) {
        $towers = $invested * $family.Share / $costByName[$family.Name] * $towerScale
        $towerCounts[$family.Name] = [Math]::Round($towers, 0)
        $familyDps = $dpsByName[$family.Name]
        if ($family.Name -eq 'Cannon') {
            $familyDps += $splashDps * ($cannonTargets - 1)
        } elseif ($family.Name -eq 'Lightning') {
            $familyDps *= $lightningTargets
        } elseif ($family.Name -eq 'Flame') {
            $familyDps *= $flameTargets
        }
        $effectiveDps += $towers * $familyDps
    }
    $effectiveDps *= $Coverage * (1 + 0.025 * [Math]::Floor($step / 5))
    $clearSeconds = $groupHealth / $effectiveDps
    $combatWindow = $spawnSeconds + $PathLength / (2 * $speed)
    $pressure = $clearSeconds / $combatWindow

    $row = [pscustomobject]@{
        Wave = $wave
        Type = if ($special) {'Elite'} elseif ($swarm) {'Horde'} elseif ($fast) {'Fast'} else {'Normal'}
        Enemies = $count
        Batch = $batchSize
        HP = [Math]::Round($health, 0)
        Speed = [Math]::Round($speed, 2)
        Interval = [Math]::Round($interval, 3)
        SpawnSec = [Math]::Round($spawnSeconds, 1)
        Gold = $count * $reward
        BudgetBefore = $budget
        GoldReserve = [Math]::Round($budget - $invested, 0)
        Towers = [Math]::Round($possibleTowers * $towerScale, 0)
        Mix = "$($towerCounts['Basic'])/$($towerCounts['Cannon'])/$($towerCounts['Lightning'])/$($towerCounts['Flame'])/$($towerCounts['Arcane'])"
        GroupHP = [Math]::Round($groupHealth, 0)
        DPS = [Math]::Round($effectiveDps, 0)
        ClearSec = [Math]::Round($clearSeconds, 1)
        WindowSec = [Math]::Round($combatWindow, 1)
        Pressure = [Math]::Round($pressure, 3)
    }
    $budget += $count * $reward
    $row
}

$towerRows | Format-Table -AutoSize
$waveRows | Where-Object {
    $ShowAllWaves -or $_.Wave -le 10 -or $_.Wave % 5 -eq 0
} | Format-Table Wave,Type,Enemies,HP,Gold,BudgetBefore,GoldReserve,Towers,Mix,GroupHP,DPS,ClearSec,WindowSec,Pressure -AutoSize
Write-Output "Lifetime gold through wave ${MaxWave}: $budget (includes starting gold)."
Write-Output "Assumptions: spend from $($EarlySpendFraction*100)% toward $($LateSpendFraction*100)% of lifetime gold, max $MaxTowers towers, $($Coverage*100)% firing coverage, $PathLength-unit path."
Write-Output 'Mix order: basic/cannon/lightning/flame/arcane. Upgrades: +2.5% estimated damage every five waves; no reroll costs.'
Write-Output 'AoE targets depend on group arrival density and attack radius; each tower family spends 10/30/25/25/10% of invested gold.'
Write-Output 'Pressure = estimated clear seconds / spawn-and-travel window; not a combat simulation or win probability.'
