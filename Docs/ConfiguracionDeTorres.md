# Disparo y giro de las torres

Las cuatro torres tienen su propio prefab en `Assets/Game/Prefabs/Towers`:
`ArcaneTower`, `FlameTower`, `ArrowTower` y `LightningTower`.
Las instancias existentes en `Assets/Game/Scenes/Game.unity` también tienen las referencias asignadas.

## Qué hace cada campo de Tower Targeting

Selecciona el objeto principal de la torre. En el Inspector, busca **Tower Targeting**.

| Campo | Qué debes arrastrar desde Unity |
| --- | --- |
| Tower Data | El archivo de datos de esa torre, desde `Assets/Data/Towers`. Aquí se guardan daño, alcance y velocidades. |
| Turret Head | El Transform que controla el giro horizontal de la parte superior. Arrástralo desde la jerarquía de esa misma torre. |
| Projectile Prefab | `Assets/Game/Prefabs/Projectiles/Projectile.prefab`, el mismo que usa Cannon Tower. |
| Fire Point | El hijo `ProjectileSpawnPoint` de esa misma torre. Su posición determina desde dónde sale el proyectil. |

**No arrastres el eje ni el punto de disparo de Cannon Tower a otra torre:** cada una necesita referencias a sus propios hijos.

## Referencias exactas

Las rutas de esta tabla empiezan debajo del objeto principal de cada torre.

| Torre | Tower Data | Turret Head | Fire Point |
| --- | --- | --- | --- |
| ArcaneTower | ArcaneTowerData | `Arcane_tower/Root/Upper_Yaw` | `Arcane_tower/Root/Upper_Yaw/Gem/ProjectileSpawnPoint` |
| FlameTower | FlameTowerData | `Flame_Tower/Root/Upper_Yaw` | `Flame_Tower/Root/Upper_Yaw/Cannon_Pitch/ProjectileSpawnPoint` |
| ArrowTower | ArrowTowerData | `Arrow_tower/Root/Platform_Yaw` | `Arrow_tower/Root/Platform_Yaw/Harpoon_Pitch/ProjectileSpawnPoint` |
| LightningTower | LightningTowerData | `Rotating_Platform` | `Rotating_Platform/Energy_Sphere/ProjectileSpawnPoint` |

En Arcane, Flame y Arrow, usa el hueso de giro indicado; el objeto de malla llamado `Rotating_Platform` no cumple la misma función que en Cannon.

## Valores iniciales, copiados de Cannon Tower

Selecciona el archivo `...TowerData` en Project para verlos y cambiarlos en el Inspector.

| Campo | Valor | Significado |
| --- | --- | --- |
| Damage | 7 | Daño de cada impacto. |
| Range | 12 | Distancia máxima de detección. |
| Fire Rate | 2 | Disparos por segundo. |
| Rotation Speed | 250 | Grados por segundo al seguir al enemigo. |
| Search Rotation Speed | 100 | Grados por segundo sin enemigo. |
| Aim Tolerance | 20 | Desviación angular máxima para permitir el disparo. |
| First Shot Delay | 0.25 | Espera inicial, descontada mientras la torre apunta dentro de la tolerancia. |
| Cost | 100 | Precio de construcción. |

Cada torre tiene una copia independiente de los datos. Editar `FlameTowerData` no modifica Cannon ni las otras torres.
El giro se aplica después de la animación para que esta no sobrescriba la orientación hacia el enemigo.
Se reutiliza el script `TowerTargeting` que ya controlaba Cannon. Las animaciones existentes se conservan: Arcane mueve sus piedras y Flame mueve sus tanques. El clip de Arcane también escribe una rotación fija en `Upper_Yaw`, por eso el apuntado se aplica después. En los archivos revisados, Arrow no incluye un clip y el controlador de Lightning está vacío.
Este comportamiento incluye giro horizontal sobre Y, como Cannon; no añade apuntado vertical automático.
Las cuatro disparan el proyectil común: sus nombres no añaden por sí solos llamas continuas, rayos encadenados ni efectos mágicos.

## Agregar otra torre paso a paso

1. Arrastra su modelo a la Hierarchy, fuera de Play Mode.
2. En el objeto principal, pulsa **Add Component**, busca **Tower Targeting** y agrégalo.
3. En Project, duplica `CannonTowerData` con **Ctrl+D**, renombra la copia y cambia **Tower Name**. Arrastra la copia a **Tower Data**.
4. Expande la torre en Hierarchy. Arrastra su pivote de giro horizontal a **Turret Head**. Debe mover toda la parte superior y dejar fija la base. Para comprobarlo, cambia temporalmente su rotación Y y luego deshaz el cambio.
5. Si no trae un punto de salida, selecciona el cañón, gema o emisor bajo ese pivote y usa **Create Empty**. Llámalo `ProjectileSpawnPoint`. Colócalo en la boca del arma o delante de la gema, con el eje azul Z apuntando hacia delante.
6. Arrastra ese `ProjectileSpawnPoint` a **Fire Point**.
7. Arrastra `Assets/Game/Prefabs/Projectiles/Projectile.prefab` a **Projectile Prefab**.
8. Arrastra el objeto principal desde Hierarchy a `Assets/Game/Prefabs/Towers` para guardar un prefab reutilizable. Si editaste una instancia de un prefab existente, guarda los cambios con **Overrides > Apply All** cuando corresponda.
9. Entra en Play Mode: sin enemigos debe girar buscando; con un enemigo dentro del alcance debe orientarse y disparar. Los enemigos deben tener tag `Enemy`, componente `EnemyMovement` y su `Target Point` asignado.

Para colocar las cuatro torres ya configuradas, arrastra sus **prefabs** a la escena. Arrastrar otra vez el FBX desde `Models` crea un modelo sin el componente de disparo.
El menú de construcción conserva su configuración actual; crear los prefabs no agrega botones nuevos al HUD.
