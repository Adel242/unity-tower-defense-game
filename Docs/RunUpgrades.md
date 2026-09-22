# Mejoras de partida

## Tarjetas y renovación

Diseño oscuro con marcos dobles, ornamentos, seis ilustraciones góticas con cel shading suave (`Assets/Game/UI/UpgradeArt`) y textos TMP separados. Arte por familia; oro/descuentos usan Fortune. Todos los elementos visuales y el botón RENOVAR están serializados en `Game/Upgrade Selection Canvas`, editables fuera de Play. Los colores distinguen familias/economía, no rarezas. Prompts y procedencia: `Docs/UpgradeArtPrompts.md`.

Renovar cuesta 30 de oro, luego 45, 60… (+15 por uso durante toda la partida). Reiniciar restablece el precio. El botón muestra coste y oro disponible y se desactiva sin fondos, durante animaciones o si no quedan alternativas. Se cobran fondos mediante PlayerGold.SpendGold; no se aplican mejoras al renovar. Se evitan las tres opciones anteriores cuando hay suficientes alternativas; cerca del límite se garantiza al menos una nueva sin duplicados. Sin alternativas no se cobra.

La renovación reúne las tarjetas en el centro con giro, escala FEEL y desvanecimiento (0.48 s), seguido de entrada escalonada y pulso dorado breve (0.42 s). El botón tiene squash/rebote FEEL. No mueve la cámara ni altera el tiempo del juego; usa tiempo escalado para respetar pausa. Bloquea interacción hasta completar la secuencia. Hover con borde iluminado y elevación; confirmación con rebote/destello y salida animada de las alternativas. Pendiente comprobar visualmente en Play Mode, incluyendo doble clic, pausa durante renovación, coste persistente en la siguiente oferta y distintas resoluciones.

Inicio: mensaje breve «Prepara tus defensas / Protege la base» durante 1.8 segundos, preparación libre para construir, pulsar Iniciar oleada, elegir una de tres mejoras y comienzo automático de la oleada 1. No aparecen tarjetas al entrar en la escena. Las ofertas posteriores siguen al completar cada múltiplo de 5. El temporizador de la próxima oleada empieza después de elegir.

## Catálogo (40 opciones)

- Globales: daño, cadencia, alcance, oro, descuento, probabilidad crítica y daño crítico.
- Cada familia (básica, cañón, rayos, fuego, arcana): daño, cadencia, alcance, crítico y descuento (25 opciones).
- Cañón: radio y daño de explosión.
- Fuego: alcance, apertura del cono, daño de quemadura y duración de quemadura.
- Rayos: rebotes adicionales y distancia entre rebotes.

Cadencia y velocidad de ataque son el mismo atributo. Las mejoras numéricas admiten entre 8 y 20 acumulaciones; descuentos, 5–6; rebotes, 6. El crítico base inflige 175% de daño y puede mejorar. La quemadura pulsa cada 0,5 s y al reaplicarse renueva el efecto más fuerte, sin acumular coroutines ilimitadas. Acumulación aditiva sobre valores base. Descuento máximo defensivo 50%, redondeo de coste hacia arriba. Apertura máxima 150°. El oro conserva restos fraccionarios entre bajas para no perder pequeñas bonificaciones.

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

El arnés compila el TowerData.cs real con sustitutos mínimos de Unity; requiere SDK .NET 10. No sustituye pruebas del motor. Comprueba 40 opciones, 500 ofertas sin duplicados, límites extendidos, crítico determinista, quemadura exclusiva de fuego, daño, cadencia, familia de área, rebotes, precio, doble selección, bloqueo del clic y reinicio. La advertencia de attackData sin asignar en este arnés es esperable: allí no existe serialización de Unity.

Pendiente Play Mode:
1. Iniciar Game: mensaje de misión, luego construir sin tarjetas; pulsar Iniciar oleada abre la primera elección. Confirmarla inicia enemigos sin un segundo clic.
2. Elegir: aplicar una sola mejora, sin construir ni seleccionar detrás.
3. Completar 5/10/15/20: oferta única; el contador no corre mientras se elige.
4. Seleccionar descuento: coinciden coste en botón, panel, disponibilidad y descuento real de oro.
5. Probar mejoras en torres nuevas y existentes; verificar visualmente área de fuego/cañón y cadenas de rayos.
6. Pausar/reanudar durante oferta; probar ratón y navegación con teclado.
7. Reiniciar escena: sin bonos heredados ni ofertas pendientes.
8. Revisar tarjetas en distintas resoluciones. Diseño con categoría, icono de torre, bonificación destacada, descripción y nivel; colores por familia/economía, entrada escalonada, hover/foco y pulso al confirmar. Tarjetas editables en Game.

FEEL (MMF_Player y MMF_Scale) controla entrada escalonada, foco, salida del foco, confirmación y aparición del título de misión. Curvas absolutas evitan acumular escala; cada cambio detiene feedbacks previos sobre la misma tarjeta. Cada reproductor ocupa su propio objeto hijo de WaveManager porque MMF_Player usa DisallowMultipleComponent; los objetos de las tarjetas siguen guardados en escena.

La entrada bloquea interacción durante 0.4 segundos, sin foco automático. Tab o flechas activan navegación; mover el ratón elimina el foco persistente. Outline y marcos iluminan el borde con el color de su mejora. La confirmación retira las alternativas con giro, desplazamiento y desvanecimiento; anima la elegida durante 0.6 segundos con FEEL antes de aplicar la elección. Outline y CanvasGroup se añaden una sola vez a las tarjetas existentes. Animaciones y confirmación respetan pausa. La misión reutiliza el título del Canvas y restaura tamaño/posición al terminar.
