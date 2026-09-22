# Contexto breve del proyecto

Actualizado: 2026-09-22. Índice para trabajo enfocado; verificar valores en los assets actuales.

## Entorno y dirección

Tower defense 3D en Unity 6000.3.21f1, URP 17.3.0, Input System 1.20.0 y AI Navigation 2.0.14. FEEL se usa en feedback de torres; Stylized Water 3 forma parte del entorno. Estética deseada: oscura, HUD moderno y legible, animaciones discretas. Evitar sacudidas fuertes y audio saturado.

Código propio: `Assets/Game/Scripts`. Prefabs: `Assets/Game/Prefabs`. Configuración: `Assets/Data`. Escena principal de juego: `Assets/Game/Scenes/Game.unity`.

## Mapa de responsabilidades

Rutas siguientes relativas a `Assets/Game/Scripts`:

| Área | Entradas principales |
| --- | --- |
| Cámara | `CameraMovement.cs` |
| Construcción y casillas | `Towers/TowerPlacementManager.cs`, `Towers/ConstructionGrid.cs` |
| Selección y datos mostrados | `Towers/TowerSelectionManager.cs`, `UI/HUD/TowerInfoPanel.cs` |
| Compra | `UI/HUD/TowerBuildButton.cs`, `Economy/PlayerGold.cs`, `Economy/GoldUI.cs` |
| Combate | `Towers/TowerTargeting.cs`, `Towers/TowerData.cs`, `Towers/*AttackData.cs` |
| Proyectiles | `Projectiles/Projectile.cs`, `Projectiles/ProjectilePool.cs` |
| Enemigos | `Enemies/EnemySpawner.cs`, `EnemyMovement.cs`, `EnemyHealth.cs`, `EnemyData.cs` |
| Oleadas | `Waves/WaveManager.cs`, `WaveData.cs`, `WaveUI.cs`, `WaveProgressBar.cs` |
| Vida de base | `Base/BaseHealth.cs`, `UI/Enemies/BaseHealthUI.cs` |
| Números de daño | `UI/Enemies/DamagePopup.cs`, `DamagePopupPool.cs` |

`Towers/TowerAttackData.cs` también contiene utilidades de VFX y audio. No asumir que están en archivos separados.

## Decisiones y estado actual

- Cinco torres del menú: básica, cañón, rayos, fuego y arcana. Arrow es un asset antiguo, no una sexta opción añadida al HUD.
- Balance: 20 assets `Assets/Data/Waves/Wave_01..20.asset`, referenciados en Game. Cada oleada usa un prefab con multiplicadores, no grupos mixtos. Picos resistentes cada 5; rápidas intercaladas.
- Oro inicial 500. Precios actuales: básica 50, cañón 100, rayos 120, fuego 75, arcana 130. Cálculos completos en `Docs/Balance.md`; no duplicar aquí las 20 filas.
- Construcción: cuadrícula de 2 unidades, una torre por casilla. Ocupación por coordenadas, no por colliders de selección. Refresh al mostrar y construir; una futura venta/movimiento debe refrescarla. Se conserva validación de soporte del terreno.
- El clic de colocación queda consumido para impedir seleccionar involuntariamente la torre recién construida. Se cancela construcción al quedar sin dinero suficiente.
- Selección: prueba límites locales de cada MeshRenderer/SkinnedMeshRenderer y elige el más cercano. Ignora partículas y círculo. No es selección exacta por triángulo/píxel.
- Alcance: adquisición y seguimiento usan distancia horizontal desde la base a la raíz del enemigo; el punto de apuntado queda para dirigir el disparo.
- Fuego: pulso directo en cono de 55°, rango tomado de TowerData (6); no espera un proyectil invisible. Daño 2 a 3 disparos/s por enemigo. El antiguo coneRange permanece para compatibilidad de ApplyImpact, no gobierna FireCone.
- Otros ataques conservan proyectiles: cañón balístico y área; rayos con rebotes; arcana de alto daño individual y cadencia lenta.
- EnemyHealth descarta daño cuando ya murió para evitar oro duplicado durante el frame de destrucción.

## Validación y pendientes

Sistema de mejoras: `Docs/RunUpgrades.md`. 25 opciones, elegir 1 de 3 antes de oleada 1 y después de cada múltiplo de 5. Tarjetas serializadas en Game/Upgrade Selection Canvas. WaveManager controla ofertas y reinicio; RunUpgradeState en TowerData.cs almacena bonos solo de partida. Consumidores deben usar Damage/Range/FireRate/Cost (propiedades efectivas), no campos base. Pruebas de reglas en Tools/UpgradeTests; falta validación visual en Unity.

Últimas compilaciones con dotnet correctas. Analizador de las 20 oleadas ejecutado. No confundir estas comprobaciones con una prueba en Unity: siguen pendientes partida completa y verificaciones visuales en Play Mode.

Prioridades de prueba: ritmo/economía durante 20 oleadas; colocar junto a básica sin bloquear vecinos; selección de torres contiguas; fuego en ambos lados del camino y en el límite de alcance. Procedimiento específico: `Docs/FlameRangeValidation.md`.

Consultar `Docs/Balance.md` para economía y su modelo aproximado. Consultar documentación adicional en `Docs/AI` o `Docs/ConfiguracionDeTorres.md` solo si resulta pertinente; puede describir estados anteriores.
