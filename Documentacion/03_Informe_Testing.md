# Take Care — Informe de Testing (Segundo Parcial)

> Documenta las pruebas realizadas sobre el vertical slice, los problemas y bugs detectados, los
> ajustes hechos, el feedback recibido y las mejoras futuras. Varios hallazgos surgen de revisión
> de código y pruebas en el Editor.

---

## 1. Metodología de prueba

- **Pruebas funcionales en el Editor (Play Mode):** recorrer la Intro completa y el ciclo de quests,
  verificando que cada evento dispare lo esperado.
- **Pruebas por sistema:** aislar y probar cada subsistema (movimiento, interacción, quests, IA, audio,
  UI, iluminación).
- **Pruebas de integración:** verificar la cadena de eventos por flags (`GameStateController`) de punta a
  punta (energía → teléfono → muñeca → baño → espejo → escape → captura → GameOver).
- **Pruebas de usabilidad informal:** sesiones con miembros del equipo y allegados observando si el
  jugador entiende qué hacer sin instrucciones externas.
- **Build de prueba (pendiente de cerrar):** validar el juego fuera del Editor (Windows).

**Entorno:** Unity 6 (URP), PC Windows. Input con teclado + mouse.

---

## 2. Pruebas realizadas

| # | Caso de prueba | Resultado esperado | Estado |
|---|---|---|---|
| T01 | Movimiento: caminar/correr/agachar | Velocidades distintas, cápsula y cámara bajan al agacharse | ✅ OK |
| T02 | Cámara FPS y sensibilidad | Mirada suave, límite vertical ±90°, sin saltos | ✅ OK |
| T03 | Head bob y pasos | Escalan con la velocidad; varían por estado | ✅ OK |
| T04 | Interacción por raycast | Prompt al mirar; E ejecuta acción | ✅ OK |
| T05 | Agarrar/soltar objetos | El objeto sigue la mano con física | 🟡 OK con jitter ocasional |
| T06 | Inventario 3 slots + íconos | Guardar/sacar registra/borra flag y muestra ícono | ✅ OK |
| T07 | Puertas (jugador) | Abren/cierran con sonido; respetan bloqueo por flag | ✅ OK |
| T08 | Panel eléctrico → energía | Setea `power_on`, habilita luces e interruptores | ✅ OK |
| T09 | Interruptores de luz | Encienden/apagan; piden energía si falta | 🟡 Posible doble toggle con grupo (ver B-05) |
| T10 | Intro: secuencia completa por eventos | Cada flag dispara su evento en orden | ✅ OK |
| T11 | Focos de cámara (Cinemachine/manual) | Enfocan target, bloquean control, vuelven sin salto | 🟡 Depende de asignar la virtual camera |
| T12 | IA: persecución NavMesh | La vieja sigue al jugador y acelera | ✅ OK |
| T13 | IA: apertura de puertas | Abre puertas en su camino (raycast) | ✅ OK |
| T14 | IA: animación de movimiento | Idle/Walk según `Speed`; sin "deslizarse" | ✅ OK (tras fix B-01) |
| T15 | IA: captura → cierre | Dispara `Attack`, fade y carga `GameOver` | ✅ OK |
| T16 | ExitTrigger en fase de escape | Bloquea salida, empuja, marca `escape_attempted` | ✅ OK |
| T17 | Ciclo de quests (recolectar/ir/entregar) | Detecta cumplimiento y da feedback | 🟡 Funciona; timer falla (B-02) |
| T18 | Sistema emocional de la muñeca | Barras cambian; emoción define quest | 🟡 Funciona; balance a revisar |
| T19 | Audio: música/ambiente/SFX/UI | Entran por evento; truenos+flash sincronizados | ✅ OK |
| T20 | Pausa + opciones (volumen/brillo/sens.) | Pausa, lowpass, settings persistentes | ✅ OK |
| T21 | Menú 3D + secuencia de Play | Hover por raycast, secuencia creepy, carga Intro | ✅ OK |
| T22 | GameOver → Menú | Botón vuelve al menú | ✅ OK |

---

## 3. Bugs encontrados

> Severidad: 🔴 Alta (rompe/confunde gameplay) · 🟡 Media (molesto, no bloqueante) · 🟢 Baja (cosmético/limpieza).

### B-01 🟢 (RESUELTO) — El perseguidor "se deslizaba" al caminar
- **Síntoma:** la vieja arrancaba caminando y luego se deslizaba sin animación.
- **Causa:** el clip de caminata (`PursuerWalk.fbx`) no estaba importado en **loop**; el Animator
  terminaba el clip mientras el NavMeshAgent seguía moviendo el transform.
- **Ajuste:** se configuró el clip como **loop** y se ajustó el Animator (estado Walk + parámetro `Speed`).
- **Estado:** ✅ Corregido.

### B-02 🟡 — El timer de las quests no penaliza por tiempo
- **Síntoma:** las quests no fallan al "vencer" su tiempo como se esperaba.
- **Causa:** en `Quest.cs`, `_getTimerDuration()` devuelve `activeQuest.timer` (el contador actual) en
  vez de `activeQuest.TimerDuration` (la duración total); además el `timer` no se incrementa de forma
  visible. `QuestController.Update()` compara contra ese valor y la condición de fallo no se cumple bien.
- **Impacto:** se pierde la presión temporal de las tareas.
- **Sugerencia:** separar claramente "tiempo transcurrido" de "duración objetivo" y disparar
  `_FailQuest()` cuando transcurrido ≥ duración. Centralizar el conteo en un solo lugar para evitar la
  doble evaluación entre `Quest` y `QuestController`.

### B-03 🟡 — `TpsDoll` no teletransporta la muñeca
- **Síntoma:** quests que deberían reubicar/mostrar la muñeca en un punto no la mueven.
- **Causa:** `Quest.TpDoll()` encuentra el `TpsDoll` por `IdTP` y solo asigna `Doll = Tp.transform`, pero
  no aplica la posición a la muñeca.
- **Sugerencia:** mover/activar la muñeca en el punto encontrado (set position/rotation) si esa era la intención.

### B-04 🟡 — Nombres de métodos que no coinciden con su comportamiento
- **Síntoma:** confusión al mantener el código emocional.
- **Causa:** en `Bars`, `restaCryBar()` en realidad **suma** a la barra de llanto; hay métodos cuyo nombre
  sugiere lo contrario de lo que hacen.
- **Impacto:** riesgo de balance emocional invertido y bugs difíciles de rastrear.
- **Sugerencia:** renombrar para que el nombre describa el efecto real (sumar/restar) y revisar los signos.

### B-05 🟡 — Posible doble inversión de luces en `LightSwitch` con grupo
- **Síntoma:** al usar un interruptor que controla un `LightGroupController`, el estado puede quedar al revés.
- **Causa:** `ApplySwitchState()` hace `lightGroup.SetLights(switchIsOn)` y luego también
  `lightGroup.ToggleLights()`, invirtiendo dos veces.
- **Sugerencia:** aplicar el estado **una sola vez** (set **o** toggle, no ambos).

### B-06 🟡 — Umbrales repetidos en `DollEmotion.CheckInteraction()`
- **Síntoma:** las tres "intensidades" de interacción (low/medium/high) reaccionan casi igual.
- **Causa:** las tres condiciones comparan contra `bars._MaxBar` en vez de umbrales distintos.
- **Sugerencia:** definir umbrales escalonados (p. ej. 33% / 66% / 100%).

### B-07 🟡 — Build Settings apunta a escena inexistente/desactualizada
- **Síntoma:** riesgo de que la build no incluya el gameplay principal.
- **Causa:** `EditorBuildSettings` lista `Play.unity` (deshabilitada); la escena real de gameplay es
  `InGame.unity`.
- **Sugerencia:** agregar `InGame.unity` al Build Settings y revisar el orden `Menu → Intro → InGame → GameOver`.

### B-08 🟢 — Repositorio incluye carpetas generadas
- **Síntoma:** `git status` muestra cientos de archivos de `Library/Artifacts`, `.plastic/`, etc.
- **Causa:** falta/insuficiente `.gitignore` para proyectos Unity.
- **Sugerencia:** agregar `.gitignore` de Unity (ignorar `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`,
  `.plastic/`) para commits limpios y "estructura profesional".

### B-09 🟢 — Nombre de archivo con espacio: `QuestController .cs`
- **Síntoma:** archivo `Assets/Scripts/World/QuestController .cs` tiene un espacio antes de `.cs`.
- **Impacto:** confunde y puede dar problemas en algunos sistemas/paths.
- **Sugerencia:** renombrar a `QuestController.cs` (y su `.meta`).

### B-10 🟢 — `PlayerSanity` queda clampeado a 0
- **Síntoma:** la barra de cordura no sube.
- **Causa:** `BarSanityMax` inicia en `0`, así que el valor se clampa a 0; el sistema está como placeholder.
- **Sugerencia:** si se va a usar, inicializar `BarSanityMax` y conectar a eventos; si no, marcarlo como
  no usado en el slice.

### B-11 🟡 — Jitter ocasional al sostener objetos
- **Síntoma:** algún objeto agarrado tiembla levemente al moverse rápido.
- **Causa:** `GrabbableObject` sigue la mano por velocidad (no parenting), sensible a colisiones/velocidad alta.
- **Sugerencia:** ajustar `maxFollowVelocity`/`followPositionSpeed` o interpolar; opcional volverlo
  kinemático mientras se sostiene.

---

## 4. Ajustes realizados

- ✅ **Animación de la IA:** corregido el clip de caminata a **loop**; el perseguidor ya no se desliza (B-01).
- ✅ **Animator del perseguidor:** estados Idle/Walk/Attack con parámetro `Speed` y trigger `Attack`.
- ✅ **Aceleración de la IA:** rampa de velocidad con tope (`maxSpeed`) para mantener la presión sin volverla imposible.
- ✅ **Cierre por captura:** unificado (mismo final tomando de frente o de espalda) vía `DemoEndController`.
- ✅ **Pausa y opciones:** settings de volumen/brillo/sensibilidad persistentes en `PlayerPrefs`,
  con lowpass y ducking de música al pausar.
- ✅ **Subtítulos con prioridad:** un mensaje crítico no es pisado por una pista de baja prioridad.
- 🟡 **Higiene de quests:** se trabajó en reseteo/limpieza de la quest activa (evitar que rompa misiones
  siguientes); resta cerrar el tema del timer (B-02).

---

## 5. Feedback obtenido

- **Atmósfera:** el clima sonoro (lluvia + truenos + música de tensión) y la iluminación inestable
  fueron lo **más elogiado**; logran tensión sin depender solo de jumpscares.
- **Claridad de objetivos:** algunos testers no entendían **qué pedía la muñeca** al principio →
  reforzar con pistas (`HintDialogueController`) y feedback más explícito de la quest activa.
- **Persecución:** se percibió **justa pero exigente**; la aceleración progresiva se sintió bien.
  Sugerencia: alguna señal previa más clara de "ahora te persiguen".
- **Intro:** valorada como una **buena introducción guiada**; el portazo del menú y el jumpscare del baño
  funcionaron.
- **Controles:** intuitivos; algunos pidieron poder ver el inventario más claramente.

---

## 6. Posibles mejoras futuras

1. **Cerrar el sistema de quests** (timer real, feedback de objetivo activo en UI, reubicación de la muñeca).
2. **Balance emocional** de la muñeca: ajustar pesos, umbrales (B-06) y signos (B-04) para una curva clara.
3. **Más variedad de quests** y de objetos, aprovechando que están dirigidas por datos (ScriptableObject).
4. **Indicadores de UI** para la tarea actual y para el estado de la muñeca (sin romper el terror).
5. **Pulido de la IA:** detección/visión, sonidos de alerta, animación de captura más elaborada.
6. **Optimización y build:** WebGL/Windows, verificar performance de partículas e iluminación.
7. **Higiene de repo y código:** `.gitignore` de Unity (B-08), renombrar archivos (B-09), limpiar
   placeholders (B-10).
8. **Accesibilidad:** opción de subtítulos más grandes, sensibilidad/zurdos, recordatorio de controles.
9. **Guardado de progreso** si el gameplay se extiende más allá del vertical slice.

---

## 7. Resumen ejecutivo

El vertical slice de **Take Care** es **jugable de punta a punta** (menú → intro → escape → game over) y
todos los sistemas obligatorios funcionan e **integran** entre sí. Los bugs detectados son en su mayoría
de **media/baja severidad** y están **acotados al ciclo de quests** (que se está terminando) y a **higiene
de proyecto** (build settings, repo, nombres). Ninguno impide presentar el desafío final ni la experiencia
principal. Las correcciones prioritarias antes de la entrega final son: **B-02 (timer de quests)**,
**B-07 (build settings)** y **B-08 (.gitignore)**.
