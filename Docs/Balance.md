# Balance infinito de hordas

Objetivo: muchas unidades visibles, aparición rápida, economía controlada y dificultad que finalmente supere al jugador. Es una aproximación calculada; requiere partida real y perfilado desde la oleada 30.

## Torres

| Torre | Coste | Daño | Disparos/s | DPS individual | DPS con 4 objetivos |
| --- | ---: | ---: | ---: | ---: | ---: |
| Básica | 50 | 5 | 2,2 | 11 | 11 |
| Cañón | 100 | 14 + 6 de área | 0,9 | 12,6 | 28,8 |
| Rayos | 120 | 6 | 1,8 | 10,8 | 43,2 |
| Fuego | 75 | 2,5 | 3,2 | 8 | 32 |
| Arcana | 130 | 38 | 0,65 | 24,7 | 24,7 |

Básica y arcana sostienen daño individual; cañón, rayos y fuego aprovechan los grupos. Los precios conservan varias opciones con los 500 de oro iniciales.

## Generación infinita

- La tendencia base parte en 32 y agrega 4 por oleada, pero no es la cantidad final: una oscilación determinista de ±14% y el tipo de oleada cambian cada resultado.
- Aparecen en grupos de 2. Las hordas suman uno y el grupo base crece gradualmente hasta 5.
- Cada tercera oleada es horda (x1,45 enemigos), cada cuarta no especial es rápida (x1,15) y cada quinta es élite (x0,82). Por eso una oleada posterior puede tener menos unidades que la anterior sin perder progresión.
- El intervalo entre grupos comienza en 0,55 s, decae 4% por oleada y tiene mínimo de 0,08 s.
- La vida comienza en 40 y crece 8,5% por oleada; las élites reciben 40% adicional. Las hordas conservan sus números pero tienen 85% de la vida normal; las rápidas tienen 90%. Esto suaviza picos entre hitos sin quitar la sensación de masa.
- Velocidad máxima: x1,75. Cantidad máxima: 400; después continúa escalando la vida.
- Recompensa inicial: x0,5 del oro base; +0,025 cada cinco oleadas y x1,1 para élites. El cañón tiene 6 de daño en área con radio 2,75.

| Oleada | Tipo | Enemigos | Grupo | Vida | Intervalo | Tiempo generando | Oro total |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Normal | 32 | 2 | 40 | 0,550 s | 8,2 s | 128 |
| 2 | Normal | 31 | 2 | 43 | 0,528 s | 7,9 s | 124 |
| 3 | Horda | 55 | 3 | 40 | 0,406 s | 7,3 s | 220 |
| 4 | Rápida | 56 | 2 | 46 | 0,487 s | 13,1 s | 224 |
| 5 | Élite | 43 | 2 | 78 | 0,514 s | 10,8 s | 172 |
| 6 | Horda | 70 | 3 | 51 | 0,359 s | 8,3 s | 280 |
| 7 | Normal | 49 | 2 | 65 | 0,431 s | 10,3 s | 196 |
| 8 | Rápida | 71 | 2 | 64 | 0,413 s | 14,5 s | 284 |
| 9 | Horda | 106 | 3 | 65 | 0,317 s | 11,1 s | 424 |
| 10 | Élite | 58 | 2 | 117 | 0,419 s | 11,7 s | 290 |
| 15 | Élite | 69 | 3 | 175 | 0,342 s | 7,5 s | 345 |
| 20 | Élite | 79 | 3 | 264 | 0,279 s | 7,2 s | 395 |
| 25 | Élite | 90 | 4 | 397 | 0,227 s | 5 s | 450 |
| 30 | Élite | 106 | 4 | 597 | 0,185 s | 4,8 s | 636 |

Las oleadas se generan mucho antes, pero terminan solo cuando muere o llega a la base el último enemigo.

## Capacidad de defensa aproximada

`AnalyzeBalance.ps1` suma oro inicial y recompensas de cada oleada. Supone que se invierte cerca del 90% al principio y que esa fracción desciende gradualmente al 60%; el resto queda de reserva. Divide el gasto entre básica/cañón/rayos/fuego/arcana en proporción 10/30/25/25/10. Calcula cantidad comprable según coste, con máximo hipotético de 100 torres útiles. El DPS considera daño y cadencia reales, daño de splash, rebotes y blancos dentro del cono. Estima el número de blancos a partir de grupos, intervalo de aparición, velocidad, radio y ángulo. Aplica 65% de cobertura efectiva (torres disparando), +2,5% de daño medio cada cinco oleadas por mejoras, y un recorrido supuesto de 60 unidades.

| Oleada | Oro acumulado antes | Reserva supuesta | Torres útiles | Vida del grupo | DPS efectivo | Tiempo para eliminar | Ventana disponible | Presión |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 500 | 50 | 5 | 1.280 | 54 | 23,8 s | 38,2 s | 0,62 |
| 10 | 2.552 | 772 | 20 | 6.768 | 237 | 28,5 s | 41,2 s | 0,69 |
| 15 | 4.222 | 1.469 | 31 | 12.107 | 477 | 25,4 s | 35,4 s | 0,72 |
| 20 | 6.877 | 2.559 | 48 | 20.844 | 830 | 25,1 s | 33,8 s | 0,74 |
| 25 | 10.182 | 3.921 | 70 | 35.706 | 1.497 | 23,9 s | 30,3 s | 0,79 |
| 30 | 13.907 | 5.452 | 95 | 63.235 | 2.214 | 28,6 s | 29,0 s | 0,98 |
| 35 | 18.243 | 7.219 | 100 | 114.818 | 2.559 | 44,9 s | 27,8 s | 1,61 |

Presión = tiempo estimado para eliminar / ventana disponible. Más de 1 sugiere fugas en esta composición, pero **no equivale a probabilidad de victoria**. La reserva cercana a 4.000 en la 25 es una hipótesis basada en la partida reportada, no un límite impuesto al jugador. La cantidad máxima de torres, cobertura, longitud del camino, mejoras y composición son supuestos: falta medirlos en Play Mode. Tampoco se simulan selección de objetivos, sobre-daño, zonas sin cobertura, coste de rerolls, oro perdido por fugas o mejoras concretas. Con solo 60 torres útiles, la presión estimada cambia a 0,92 en la 25 y 1,55 en la 30. Los tipos de oleada producen variaciones locales: la horda 24 ronda 0,97, la 25 ronda 0,79 y la horda 27 ronda 1,07 en el escenario base. La curva se evalúa por tendencia, no por monotonía estricta.

## Rendimiento

`EnemyMovement` mantiene el registro activo usado por targeting y ataques de área. El spawner usa `Physics.CheckSphere` sin crear arrays. Los enemigos todavía usan Instantiate/Destroy; agregar pooling requiere primero medir CPU y memoria en una partida representativa.

## Análisis reproducible

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnalyzeBalance.ps1
```

Lee escena y assets, reproduce las reglas infinitas y muestra oleadas 1–10 y cada múltiplo de 5. Usa `-ShowAllWaves` para ver cada oleada. Parámetros opcionales: `-MaxWave`, `-EarlySpendFraction`, `-LateSpendFraction`, `-MaxTowers`, `-Coverage` y `-PathLength`.

## Prueba pendiente

Jugar hasta la oleada 30 con composiciones distintas. Registrar oro sobrante, enemigos simultáneos, duración real de cada oleada, vida perdida y tiempos de frame en 10/20/30.
