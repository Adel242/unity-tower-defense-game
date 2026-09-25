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
- Oleadas infinitas variables: tendencia 32 + 4 por oleada con oscilación determinista ±14%; hordas x1,45 enemigos y 85% de vida, rápidas x1,15 y 90% de vida, élites x0,82 con más vida. Grupos base de 2 crecen hasta 5 y las hordas agregan uno. Intervalo 0,55–0,08 s; vida inicial x0,4 y +8,5% por oleada, velocidad máxima x1,75, cantidad máxima 400. Recompensa x0,5, +0,025 cada cinco y élites x1,1. `EnemyMovement.ActiveEnemies` evita búsquedas por tag.
- Oro inicial 500. Precios: básica 50, cañón 100, rayos 120, fuego 75, arcana 130. Daño/cadencia actuales: 5/2,2; 14/0,9; 6/1,8; 2,5/3,2; 38/0,65. El cañón hace 6 de splash en radio 2,75. Modelo aproximado en `Docs/Balance.md` y `Tools/AnalyzeBalance.ps1`: oro invertido, mezcla de torres, AoE/rebotes/cono, DPS, vida grupal y tiempo disponible; los supuestos requieren comprobarse en partida.
- Construcción: cuadrícula de 2 unidades, una torre por casilla. Ocupación por coordenadas, no por colliders de selección. Refresh al mostrar y construir; una futura venta/movimiento debe refrescarla. Se conserva validación de soporte del terreno.
- El clic de colocación queda consumido para impedir seleccionar involuntariamente la torre recién construida. Se cancela construcción al quedar sin dinero suficiente.
- El panel de datos de torre se usa solo como tooltip de los botones de construcción. Seleccionar una torre muestra un botón de venta en el HUD; devuelve 70% del coste efectivo, destruye su contenedor de feedback y libera la casilla.
- Venta: `TowerSaleEffect` en `TowerSelectionManager.cs` encoge y baja la torre durante 0,3 s, con anillo expansivo, 18 motas del color de su familia y texto dorado del reembolso. Detiene feedbacks y Animator antes de animar; desactiva targeting y colliders y libera la casilla de inmediato. Pendiente comprobación visual en Play.
- Zombis: siempre usan `EnemyDeathVfx`, ráfaga rojiza de 32 partículas. Instant Destruction, los prefabs/generador de fragmentación y su respaldo local fueron eliminados. Unity debe regenerar los proyectos C# al actualizar los assets retirados.
- Inicio: `WaveManager.GameplayReady` habilita juntos HUD y movimiento al terminar el mensaje de misión. La cámara permite WASD/flechas, desplazamiento por bordes y zoom con rueda hasta la altura inicial. El giro horizontal con clic derecho comienza al arrastrar al menos 6 px horizontalmente, sin feedback de FOV; WASD/flechas siguen activos durante el giro y el desplazamiento por bordes se pausa. Doble clic derecho devuelve el ángulo inicial. El clic derecho cancela construcción y los controles ignoran el HUD. Usa cursores góticos editables en `UI/Cursors`.
- La animación FEEL de construcción mueve/escala un contenedor externo; nunca el root importado que controla el Animator. Seleccionar repetidamente la misma torre no reconstruye su indicador ni reinicia el feedback del panel. Disparos, proyectiles y audio rechazan posiciones no finitas para evitar cascadas de `Invalid worldAABB`/`IsFinite(gain)`.
- Selección: MeshRenderer usa límites locales; SkinnedMeshRenderer se evalúa por triángulos de la pose actual con BakeMesh al hacer clic, para incluir armas y huesos desplazados. Ignora partículas y círculo. Falta verificar en Play Mode el clic sobre la boquilla de fuego y torres contiguas.
- Alcance: adquisición y seguimiento usan distancia horizontal desde la base a la raíz del enemigo; el punto de apuntado queda para dirigir el disparo.
- La cadencia pertenece a la torre y continúa contando aunque pierda el objetivo; adquirir otro enemigo puede aplicar `firstShotDelay`, pero nunca acorta el cooldown pendiente de un disparo anterior.
- Fuego: pulso directo en cono de 55°, rango tomado de TowerData (6); no espera un proyectil invisible. Daño 2 a 3 disparos/s por enemigo. El antiguo coneRange permanece para compatibilidad de ApplyImpact, no gobierna FireCone.
- Otros ataques conservan proyectiles: cañón balístico y área; rayos con rebotes; arcana de alto daño individual y cadencia lenta.
- El impacto arcano instancia `F_StylizedImpactAndExplosions/VFX_2.prefab` a escala 0,7 y lo limpia tras 2,5 s.
- EnemyHealth descarta daño cuando ya murió para evitar oro duplicado durante el frame de destrucción.

## Validación y pendientes

Sistema de mejoras: `Docs/RunUpgrades.md`. 40 opciones con límites largos, crítico y quemadura; elegir 1 de 3 antes de oleada 1 y después de cada múltiplo de 5. Tarjetas serializadas en Game/Upgrade Selection Canvas. WaveManager controla ofertas y reinicio; RunUpgradeState en TowerData.cs almacena bonos solo de partida. Consumidores deben usar Damage/Range/FireRate/Cost (propiedades efectivas), no campos base. Pruebas de reglas en Tools/UpgradeTests; falta validación visual en Unity.

Flujo inicial: misión → construir libremente → Iniciar oleada → elegir mejora → oleada 1 automática. FEEL anima misión, entrada, foco y confirmación de tarjetas, con escala absoluta y respeto de pausa.

Tarjetas oscuras con marcos y seis ilustraciones góticas en UI/UpgradeArt, editables en el Canvas de Game. Reroll con recogida/giro, escala FEEL y pulso dorado; respeta pausa y no sacude cámara. Renovar cuesta 30 de oro y aumenta 15 por uso durante la partida; RunUpgradeState gestiona precio/alternativas y WaveManager cobra mediante PlayerGold. Pruebas de reglas incluyen renovación, falta de fondos, agotamiento y reinicio. Pendiente revisión visual en Play Mode.

Últimas compilaciones con dotnet correctas. La progresión infinita se comprobó estáticamente en oleadas 1, 5, 10, 20, 30, 50 y 100. No confundir estas comprobaciones con una prueba en Unity: siguen pendientes partida completa y verificaciones visuales en Play Mode.

Prioridades de prueba: ritmo/economía durante al menos 20 oleadas y rendimiento desde la 30; colocar junto a básica sin bloquear vecinos; selección de torres contiguas; fuego en ambos lados del camino y en el límite de alcance. Procedimiento específico: `Docs/FlameRangeValidation.md`.

Consultar `Docs/Balance.md` para economía y su modelo aproximado. Consultar documentación adicional en `Docs/AI` o `Docs/ConfiguracionDeTorres.md` solo si resulta pertinente; puede describir estados anteriores.
