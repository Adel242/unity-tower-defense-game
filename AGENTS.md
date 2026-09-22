# Guía breve de trabajo

- Responder en español, con resultados concisos. No repetir planes ni contexto ya confirmado.
- Consultar `Docs/ProjectContext.md` como índice inicial; comprobar el código actual antes de editar. La documentación no reemplaza la evidencia.
- Leer solo los scripts, assets y referencias necesarios para la petición. Usar búsquedas dirigidas con `rg`; evitar volcar escenas, prefabs grandes o paquetes completos.
- Respetar las instrucciones superiores y las skills aplicables: esta guía no permite omitir sus lecturas obligatorias.
- Revisar el estado de Git y preservar cambios previos. No revertir, limpiar ni hacer commits sin petición.
- Preferir cambios pequeños en los sistemas existentes. No añadir paquetes, gestores ni refactorizaciones ajenas a la tarea.
- El usuario prefiere HUD y terreno editables en escena, no regenerados al entrar en Play. Conservar esta preferencia al ampliar funciones.
- Preservar GUID, fileID y referencias de Unity. No editar Library, Temp ni proyectos C# generados.
- Validar proporcionalmente: compilación para cambios C#, analizador para balance y Play Mode cuando esté disponible para interacción/visuales. Compilar no demuestra que un bug visual esté resuelto.
- Actualizar el contexto solo si cambian decisiones, arquitectura o pendientes relevantes; mantenerlo corto, no convertirlo en un historial de conversación.

## Comandos desde la raíz

```powershell
dotnet build Assembly-CSharp.csproj --no-restore -v:quiet
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnalyzeBalance.ps1
git diff --check
```

La compilación requiere el proyecto C# generado por Unity. El analizador es una aproximación, no una simulación de partida. No repetir compilaciones si solo cambió documentación.
