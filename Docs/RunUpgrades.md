# Mejoras de partida — primera versión

Tres tarjetas aleatorias, sin duplicados en la misma oferta. Elegir exactamente una antes de iniciar la oleada 1 y al completar 5, 10, 15, 20 y cada múltiplo de 5 futuro. No se ofrecen antes de la 5: aparecen al terminarla. El temporizador de la próxima oleada empieza después de elegir. La oferta de la 20 se mantiene aunque actualmente sea la última.

## Catálogo (25 opciones)

- Globales: daño +6%, cadencia +6%, alcance +5%, oro por baja +10%, descuento 5%.
- Cada familia (básica, cañón, rayos, fuego, arcana): daño +10%, cadencia +8%, descuento 8% (15 opciones).
- Cañón: radio de explosión +10%.
- Fuego: alcance +8% o apertura del cono +10%.
- Rayos: +1 rebote o distancia entre rebotes +10%.

Cadencia y velocidad de ataque son el mismo atributo, no dos bonificaciones distintas. Límite de 3 elecciones por mejora; rebotes, 2. Las mejoras pueden reaparecer en ofertas posteriores hasta su límite. Acumulación aditiva sobre valores base: daño global +6% y específico +10% = +16%. Descuento máximo defensivo 50%, redondeo de coste hacia arriba. Apertura máxima 150°. El oro conserva restos fraccionarios entre bajas para no perder pequeñas bonificaciones. No aumenta oro inicial ni otras fuentes.

Afectan torres ya construidas y nuevas. No se escriben los ScriptableObjects ni se guarda progresión entre partidas. Reiniciar/cerrar la escena de juego restablece las mejoras. La economía base no fue retocada; habrá que probar la dificultad con estos beneficios.

## Implementación y edición

- `TowerData.cs`: propiedades efectivas Damage/Range/FireRate/Cost y clase de reglas RunUpgradeState (catálogo y acumulación).
- `WaveManager.cs`: ciclo de ofertas, referencias a tarjetas, cierre y bloqueo de siguiente oleada. Posee el reinicio del estado de partida.
- `Game.unity`: objeto raíz **Upgrade Selection Canvas**, con tres botones y sus textos TMP ya enlazados al WaveManager. Está inactivo fuera de las ofertas; puede activarse temporalmente en edición para modificar el diseño. No se generan tarjetas durante Play.
- Combatientes, panel de stats, precios y compra leen los valores efectivos. Área/rebotes consultan el mismo estado. Daño mejorado de cañón incluye explosión secundaria.
- La pantalla bloquea construcción, selección y movimiento de cámara; protege también el frame del clic de cierre. No altera Time.timeScale. Al pausar se oculta para dejar usar el menú de pausa y reaparece al reanudar.
- No requiere instalar paquetes ni asignar referencias manualmente en la escena Game. Utiliza el EventSystem de la escena UI cargada aditivamente por el juego.

## Validación

Compilación C# de Unity mediante dotnet y comprobación estática de IDs/referencias del Canvas. Pruebas de reglas:

```powershell
dotnet run --project Tools/UpgradeTests/UpgradeTests.csproj
```

El arnés compila el TowerData.cs real con sustitutos mínimos de Unity; requiere SDK .NET 10. No sustituye pruebas del motor. Comprueba 25 opciones, 500 ofertas sin duplicados, límites, daño, cadencia, familia de área, rebotes, precio, doble selección, bloqueo del clic y reinicio. La advertencia de attackData sin asignar en este arnés es esperable: allí no existe serialización de Unity.

Pendiente Play Mode:
1. Iniciar Game: tres tarjetas y ningún enemigo hasta elegir e iniciar oleada.
2. Elegir: aplicar una sola mejora, sin construir ni seleccionar detrás.
3. Completar 5/10/15/20: oferta única; el contador no corre mientras se elige.
4. Seleccionar descuento: coinciden coste en botón, panel, disponibilidad y descuento real de oro.
5. Probar mejoras en torres nuevas y existentes; verificar visualmente área de fuego/cañón y cadenas de rayos.
6. Pausar/reanudar durante oferta; probar ratón y navegación con teclado.
7. Reiniciar escena: sin bonos heredados ni ofertas pendientes.
8. Revisar tarjetas en distintas resoluciones. Diseño provisional, sin ilustraciones ni animaciones especiales.
