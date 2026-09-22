# Balance de torres y 20 oleadas — revisión de ritmo

Objetivo: construir con más frecuencia, con torres individualmente más débiles y más enemigos. Segunda pasada calculada a partir del feedback de juego; pendiente de validar una partida completa.

## Economía y torres

Se conservan 500 de oro iniciales. Ahora una torre de cada tipo cuesta 475 en total (antes 870), seis básicas + dos cañones cuestan 500, o diez básicas cuestan 500. No es necesario esperar varias oleadas para completar una defensa inicial variada.

| Torre | Precio anterior → actual | Daño anterior → actual | Disparos/s | Alcance | DPS individual | DPS total con 3 objetivos |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Básica | 90 → 50 | 6 → 4 | 2 | 8 | 8 | 8 |
| Cañón | 180 → 100 | 18 → 12 | 0.8 | 11 | 9.6 | 20.8 |
| Rayos | 220 → 120 | 7 → 5 | 1.6 | 10 | 8 | 24 |
| Fuego | 140 → 75 | 3 → 2 | 3 | 6 | 6 | 18 |
| Arcana | 240 → 130 | 48 → 32 | 0.6 | 12 | 19.2 | 19.2 |

Daño individual reducido un 29–33%; no se ralentiza la cadencia. Las torres cuestan aproximadamente un 44–46% menos. El DPS por oro mejora aproximadamente un 20–31%, a cambio de necesitar más casillas e inversión en colocación.

DPS teórico = daño × disparos/s. Las cifras múltiples requieren alcance, cono o rebotes efectivos; no incluyen giro, tiempo de proyectil, primer disparo, exceso de daño ni interrupciones.

- Cañón: explosión secundaria de 10 → 7, radio 3.5 sin cambios. DPS sobre N enemigos = (12 + 7 × (N − 1)) × 0.8. El objetivo principal no recibe además daño de explosión.
- Rayos: 5 × 1.6 × min(N, 4), máximo 32 DPS repartido.
- Fuego: 2 × 3 × N; 6 por objetivo. Mantiene su cono y alcance corto.
- Arcana: 32 por impacto, 19.2 DPS individual y alcance 12. Conserva su rol contra resistentes y cadencia lenta.
- Arrow, asset antiguo fuera del menú: precio 75, daño 5, cadencia 2, alcance 12.

## Oleadas

Se mantienen las 20 referencias existentes, sus velocidades y recompensas por baja. Aumenta la cantidad aproximadamente un 25% (redondeada hacia arriba), y el intervalo disminuye un 20%. Así llegan más enemigos sin alargar de manera importante la generación de cada oleada.

La vida adicional aumenta gradualmente: multiplicador sobre la revisión anterior = 1 + 0.25 × (oleada − 1) / 19, redondeando la vida final al entero más cercano. La primera conserva 60 de vida; la última pasa de 660 a 825.

| Oleada | Enemigos | Vida | Velocidad × | Intervalo (s) | Oro por baja | Oro total |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 15 | 60 | 1 | 1.12 | 8 | 120 |
| 2 | 18 | 71 | 1 | 1 | 8 | 144 |
| 3 | 20 | 82 | 1.12 | 0.96 | 8 | 160 |
| 4 | 23 | 94 | 1 | 0.88 | 8 | 184 |
| 5 | 20 | 163 | 0.9 | 1.08 | 12 | 240 |
| 6 | 25 | 128 | 1.03 | 0.84 | 9 | 225 |
| 7 | 28 | 156 | 1.05 | 0.8 | 9 | 252 |
| 8 | 30 | 180 | 1.06 | 0.76 | 9 | 270 |
| 9 | 28 | 166 | 1.32 | 0.72 | 9 | 252 |
| 10 | 30 | 313 | 0.95 | 0.92 | 14 | 420 |
| 11 | 33 | 232 | 1.08 | 0.76 | 10 | 330 |
| 12 | 35 | 263 | 1.1 | 0.72 | 10 | 350 |
| 13 | 38 | 318 | 1.1 | 0.68 | 10 | 380 |
| 14 | 35 | 304 | 1.4 | 0.68 | 10 | 350 |
| 15 | 38 | 527 | 1 | 0.88 | 16 | 608 |
| 16 | 40 | 401 | 1.12 | 0.68 | 11 | 440 |
| 17 | 43 | 460 | 1.15 | 0.68 | 11 | 473 |
| 18 | 45 | 538 | 1.18 | 0.64 | 11 | 495 |
| 19 | 40 | 507 | 1.45 | 0.64 | 11 | 440 |
| 20 | 50 | 825 | 1.1 | 0.8 | 20 | 1000 |

La primera oleada entrega 120: dos básicas, un cañón o una eléctrica. Las oleadas 5/10/15/20 mantienen los picos de resistencia; 9/14/19 son rápidas, con introducción suave en la 3. No se agregan jefes ni grupos mixtos.

Recompensa zombie base: 8. Oro acumulado antes de la última oleada: 6633; total final: 7633, ambos incluyen 500 iniciales y suponen todas las bajas. Son presupuestos de inversión históricos, no saldo sin gastar. Se mantienen vida de base 100 y descanso de 10 segundos.

## Análisis reproducible

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnalyzeBalance.ps1
```

Lee los assets, verifica las 20 referencias ordenadas y calcula DPS, economía y una presión orientativa:

- Generación = cantidad × intervalo; el spawner espera después de cada aparición, incluida la última.
- DPS requerido = vida total / (generación + 12 / velocidad).
- Capacidad = oro acumulado previo × 0.12 DPS efectivo/oro.
- Presión = requerido / capacidad.

El factor efectivo pasa de 0.10 a 0.12 para reflejar conservadoramente la mejora del DPS/oro. Los 12 segundos de exposición son una suposición, no una medición. El modelo no simula combate ni geometría, supone reinvertir todo el dinero y omite sobrantes, fugas, congestión de spawn y cobertura simultánea. No prueba que el juego sea ganable.

Presión orientativa: 0.521 inicial; especiales 0.702 / 0.829 / 0.879 / 1.018. Con eficiencia 0.09, la última sube a 1.357; con 0.15 baja a 0.814. La última queda exigente, pendiente de prueba real.

## Bug de casillas adyacentes

Evidencia estática: TowerTargeting.ConfigureSelectionCollider configura una cápsula de radio 1.5, las casillas miden 2, y la colocación añadía una consulta esférica de radio 1. Los volúmenes de selección podían solaparse con casillas vecinas. La visualización usaba otra esfera, de radio 0.76, por lo que tampoco compartía exactamente la misma validación.

Corrección: ConstructionGrid conserva un conjunto de coordenadas ocupadas por torres activas y lo actualiza al mostrar la cuadrícula y después de construir. Tanto dibujo como colocación consultan el mismo conjunto, sin usar colliders de selección. El preview (targeting desactivado) queda excluido. Se mantienen las comprobaciones de soporte y terreno bloqueado. Se elimina minimumTurretDistance, ya sustituido por la ocupación de casillas. No se cambian colliders de selección.

Límite: las torres de este sistema ocupan una casilla; no se implementan edificios multicasilla. Si se agrega venta/movimiento/desactivación de torres durante construcción en el futuro, esa operación debe refrescar la cuadrícula.

## Verificación

Analizador de las 20 oleadas ejecutado correctamente. Compilación C# mediante dotnet; no se ha reproducido la interacción en Play Mode.

Prueba manual pendiente:
1. Iniciar Game y construir una básica en una casilla interior con vecinos soportados.
2. Construir en cada vecino cardinal y diagonal: deben seguir libres y permitir construir, incluso inmediatamente después de la animación.
3. Intentar construir dos veces en la misma casilla: debe rechazarse y no descontar oro.
4. Cambiar de torre, cancelar y volver a construir: el preview no debe reservar casillas.
5. Verificar bordes del terreno y camino: siguen rechazados cuando falta soporte.
6. Confirmar selección por click, cancelación al quedarse sin oro y ocupación al reiniciar escena.
7. Jugar las 20 oleadas y registrar vida, saldo y compras en 5/10/15/20 con composiciones variadas.

Se conserva la corrección anterior de EnemyHealth que impide recompensar varias veces una muerte por impactos en el mismo frame.
