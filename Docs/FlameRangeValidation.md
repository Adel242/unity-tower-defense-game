# Alcance de fuego: corrección y validación

La detección adquiría enemigos midiendo a sus pies en 3D y comprobaba después
su punto de apuntado. En el borde podía perder/reobtener el mismo objetivo y
reiniciar firstShotDelay. Ahora adquisición y seguimiento usan la raíz del
enemigo y distancia horizontal desde la base, en todas las torres.

Fuego usa TowerData.range (6 actualmente) también para el daño del cono de 55°.
Aplica cada pulso al disparar, sin esperar un proyectil invisible. Conserva daño
y cadencia. El antiguo coneRange se conserva para compatibilidad con llamadas
ApplyImpact, pero la torre ya no usa esa ruta.

El efecto sale de la boquilla con longitud limitada por su separación horizontal
respecto de la base. Es una representación con partículas, no una frontera exacta:
su dispersión, tamaño y vida hacen que no todos los puntos del cono se iluminen igual.

Validación: compilación dotnet correcta. Pendiente Play Mode:
- Objetivos a 5.9 y 6.1 unidades horizontales: adquirir solo el primero.
- Repetir a distintas alturas y a izquierda/derecha; igual detección.
- Mantener enemigo en 5.9: disparos continuos, sin reinicio perpetuo del primer tiro.
- Varios enemigos dentro de 6 y ±27.5°: daño por pulso a cada uno.
- Enemigos fuera del ángulo/radio: ningún daño de fuego.
- Comprobar boquilla, giro y partículas desde ambas orientaciones del camino.
- Verificar que rayos, cañón y arcana mantienen sus proyectiles y cadencia.
