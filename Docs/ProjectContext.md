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
- Inicio: `WaveManager.GameplayReady` habilita juntos HUD y movimiento al terminar el mensaje de misión. La cámara permite desplazamiento por bordes a la misma velocidad que WASD, giro exclusivamente horizontal con arrastre derecho y retorno al ángulo inicial con doble clic derecho; ignora estos controles sobre UI y conserva el clic derecho para cancelar construcción. Usa cursores góticos editables en `UI/Cursors`; durante el giro bloquea traslación por WASD y bordes.
- La animación FEEL de construcción mueve/escala un contenedor externo; nunca el root importado que controla el Animator. Seleccionar repetidamente la misma torre no reconstruye su indicador ni reinicia el feedback del panel. Disparos, proyectiles y audio rechazan posiciones no finitas para evitar cascadas de `Invalid worldAABB`/`IsFinite(gain)`.
- Selección: MeshRenderer usa límites locales; SkinnedMeshRenderer se evalúa por triángulos de la pose actual con BakeMesh al hacer clic, para incluir armas y huesos desplazados. Ignora partículas y círculo. Falta verificar en Play Mode el clic sobre la boquilla de fuego y torres contiguas.
- Alcance: adquisición y seguimiento usan distancia horizontal desde la base a la raíz del enemigo; el punto de apuntado queda para dirigir el disparo.
- Fuego: pulso directo en cono de 55°, rango tomado de TowerData (6); no espera un proyectil invisible. Daño 2 a 3 disparos/s por enemigo. El antiguo coneRange permanece para compatibilidad de ApplyImpact, no gobierna FireCone.
- Otros ataques conservan proyectiles: cañón balístico y área; rayos con rebotes; arcana de alto daño individual y cadencia lenta.
- EnemyHealth descarta daño cuando ya murió para evitar oro duplicado durante el frame de destrucción.

## Validación y pendientes

Sistema de mejoras: `Docs/RunUpgrades.md`. 25 opciones, elegir 1 de 3 antes de oleada 1 y después de cada múltiplo de 5. Tarjetas serializadas en Game/Upgrade Selection Canvas. WaveManager controla ofertas y reinicio; RunUpgradeState en TowerData.cs almacena bonos solo de partida. Consumidores deben usar Damage/Range/FireRate/Cost (propiedades efectivas), no campos base. Pruebas de reglas en Tools/UpgradeTests; falta validación visual en Unity.

Flujo inicial: misión → construir libremente → Iniciar oleada → elegir mejora → oleada 1 automática. FEEL anima misión, entrada, foco y confirmación de tarjetas, con escala absoluta y respeto de pausa.

Tarjetas oscuras con marcos y seis ilustraciones góticas en UI/UpgradeArt, editables en el Canvas de Game. Reroll con recogida/giro, escala FEEL y pulso dorado; respeta pausa y no sacude cámara. Renovar cuesta 30 de oro y aumenta 15 por uso durante la partida; RunUpgradeState gestiona precio/alternativas y WaveManager cobra mediante PlayerGold. Pruebas de reglas incluyen renovación, falta de fondos, agotamiento y reinicio. Pendiente revisión visual en Play Mode.

Últimas compilaciones con dotnet correctas. Analizador de las 20 oleadas ejecutado. No confundir estas comprobaciones con una prueba en Unity: siguen pendientes partida completa y verificaciones visuales en Play Mode.

Prioridades de prueba: ritmo/economía durante 20 oleadas; colocar junto a básica sin bloquear vecinos; selección de torres contiguas; fuego en ambos lados del camino y en el límite de alcance. Procedimiento específico: `Docs/FlameRangeValidation.md`.

Consultar `Docs/Balance.md` para economía y su modelo aproximado. Consultar documentación adicional en `Docs/AI` o `Docs/ConfiguracionDeTorres.md` solo si resulta pertinente; puede describir estados anteriores.
