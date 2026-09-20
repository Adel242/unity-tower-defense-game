# Contexto del proyecto Unity

<!-- unity-onboarding:generated:start -->

## Resumen

- **Raíz:** `C:/Users/user/Desktop/unity-tower-defense-game`
- **Producto:** tower defense 3D para un jugador. El jugador inicia oleadas, construye torres con oro, elimina zombis antes de que alcancen la base y recibe oro por cada baja.
- **Estado actual:** prototipo jugable con dos oleadas, un enemigo, economía, construcción, selección de torres, HUD, pausa/opciones y varios modelos de torre.
- **Último análisis:** 2026-09-20
- **Commit analizado:** `e2973eb` (`background rotation`, 2026-09-18). El árbol de trabajo contiene cambios locales posteriores.

## Entorno confirmado

- **Unity:** 6000.3.21f1, revisión `c02631ffc030`.
- **Render:** Universal Render Pipeline 17.3.0.
- **Entrada:** Input System 1.20.0 exclusivamente (`activeInputHandler: 1`).
- **Navegación:** AI Navigation 2.0.14 y `NavMeshAgent`.
- **UI:** uGUI 2.0.0 y TextMesh Pro.
- **Plataforma preparada:** perfil de Windows Standalone. No se confirmó preparación real de otras plataformas.
- **Red:** no hay sistema multijugador.

## Estructura importante

| Ruta | Responsabilidad |
| --- | --- |
| `Assets/Game/Scripts` | Código propio de gameplay, UI y flujo de escenas. |
| `Assets/Game/Scenes` | `MainMenu`, `Game`, `UI` y `PauseMenu`. |
| `Assets/Game/Prefabs` | Zombie, proyectil, seis prefabs de torre y UI reutilizable. |
| `Assets/Data` | ScriptableObjects de torres, enemigo, ataques y oleadas. |
| `Assets/Settings` | Assets URP para PC/móvil y perfil de build Windows. |
| `Assets/AllSkyFree` | Paquete visual de terceros. |
| `Assets/Stylized Water 3` | Paquete de agua de terceros actualmente sin seguimiento en Git. |

## Escenas y arranque

- Build Settings: `MainMenu` → `Game` → `PauseMenu` → `UI`, todas habilitadas.
- `MainMenu` carga `Game` de forma normal.
- `GameSceneLoader`, dentro de `Game`, carga `UI` y `PauseMenu` de forma aditiva.
- La escena principal contiene economía, oleadas, spawn, destino/base, navegación, construcción, selección, cámara y luz.
- Un script de Editor (`BuildSceneSetup`) vuelve a generar automáticamente la lista de escenas de build desde `Assets/Game/Scenes`.

## Arquitectura y sistemas

- **Patrón principal confirmado:** componentes `MonoBehaviour` conectados por Inspector, búsquedas globales puntuales (`FindFirstObjectByType`) y datos de balance en `ScriptableObject`.
- **Oleadas:** `WaveManager` consume `WaveData`; hay dos assets configurados con 10 y 20 zombis, respectivamente. La primera oleada espera al botón y entre oleadas hay temporizador o inicio manual.
- **Enemigos:** aparecen sobre NavMesh, caminan hacia la base, infligen daño al llegar y entregan oro al morir. El único tipo de datos confirmado es `ZombieData`.
- **Torres:** buscan el enemigo más cercano dentro del rango, giran horizontalmente, respetan tolerancia/retardo/cadencia y disparan proyectiles reutilizados mediante pool.
- **Construcción:** previsualización verde/roja, restricción por capas, separación entre torres y cobro de oro. El manager mantiene un solo prefab de torre configurable.
- **Selección:** clic sobre una torre muestra nombre, daño, rango, cadencia y coste.
- **Base:** tiene vida y registra `GAME OVER`, pero todavía no existe una pantalla o transición de fin de partida.
- **Ataques en desarrollo:** nueva jerarquía `TowerAttackData`; `CannonAttackData` aplica daño directo y splash, y `LightningAttackData` encadena el proyectil entre objetivos.
- **Pools:** proyectiles y textos de daño usan reutilización de objetos.

## Ensamblados y convenciones

- El código propio no tiene `.asmdef`; todo compila en el ensamblado predeterminado `Assembly-CSharp`. Solo Stylized Water 3 define ensamblados separados de runtime/editor.
- No se usan namespaces.
- Predominan campos privados con `[SerializeField]`, aunque los datos de torre/enemigo exponen algunos campos públicos.
- El estilo de llaves y espaciado no es uniforme entre archivos.
- No se observaron patrones async propios; el flujo temporal usa coroutines y `LoadSceneAsync`.

## Pruebas y herramientas

- Unity Test Framework llega como dependencia transitiva, pero no existen tests EditMode/PlayMode propios ni ensamblados de pruebas.
- No se encontró configuración CI.
- No hay proveedor Unity MCP configurado en el proyecto ni herramientas de Editor conectadas disponibles en esta sesión. Consola, estado de compilación y Play Mode quedan sin verificar.

## Trabajo local actual

- Se añadió `TowerAttackData` y sus implementaciones para cañón (splash) y rayo (rebotes).
- `Projectile` ahora conserva los enemigos afectados y permite continuar hacia otro objetivo.
- `TowerData` recibe una referencia opcional a `attackData`; `TowerTargeting` la pasa al proyectil.
- `CannonTowerData` ya referencia `CannonAttackData`.
- `LightningAttackData.asset` está enlazado a `LightningTowerData` y su comportamiento fue confirmado por el usuario en Play Mode.
- `Game.unity` tiene un cambio local grande (114 inserciones/920 eliminaciones) que debe revisarse visualmente antes de confirmar; aparecen eliminaciones de instancias/referencias de prefabs y puntos de disparo.
- Se añadieron Stylized Water 3, datos de iluminación y una reflection probe sin seguimiento en Git, lo que sugiere trabajo visual paralelo en el escenario.

## Riesgos y dudas

- **Alta confianza:** entorno, paquetes, escenas, flujo y arquitectura se verificaron directamente en configuración y código.
- **Sin validar:** compilación actual, errores de consola, comportamiento en Play Mode, NavMesh horneada y referencias rotas en escena/prefabs.
- La búsqueda de objetivos mediante `FindGameObjectsWithTag` ocurre repetidamente por torre y también en cada impacto especial; puede escalar mal con muchas torres/enemigos.
- El ataque de cañón busca por tag y luego filtra capa; requiere que todos los enemigos mantengan tag `Enemy` y layer 7.
- El README aún es un marcador; `Docs/ConfiguracionDeTorres.md` es la documentación funcional existente.
- Cada botón de construcción del Canvas configura explícitamente su propio prefab mediante `TowerBuildButton`; no se generan botones en tiempo de ejecución.

## Fuentes principales inspeccionadas

- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`, `Packages/packages-lock.json`
- `Assets/Game/Scripts/**`, `Assets/Game/Editor/BuildSceneSetup.cs`
- `Assets/Game/Scenes/*.unity`, inventario de prefabs y assets de datos
- `Assets/Data/Towers/**`, `Assets/Data/Waves/**`, `Assets/Data/Enemies/**`
- `README.md`, `Docs/ConfiguracionDeTorres.md`, historial y cambios locales de Git

<!-- unity-onboarding:generated:end -->
