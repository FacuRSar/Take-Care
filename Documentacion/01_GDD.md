# Take Care — Game Design Document (GDD)

> Versión: Segundo Parcial (Vertical Slice)
> Motor: Unity 6 (URP) · Género: Survival Horror en primera persona
> Documento actualizado respecto al Primer Parcial.

---

## 1. High Concept

**Take Care** es un juego de **terror psicológico en primera persona** ambientado en una casa
durante una noche de tormenta. El jugador queda al "cuidado" de una **muñeca** que no es lo que
parece: tiene **estados emocionales** que cambian según cómo el jugador la trate y la observe, y
exige ser atendida mediante **tareas (quests)**. Si la muñeca se descontrola emocionalmente —y si
el jugador intenta escapar— la casa responde: las luces fallan, suena el teléfono y una **presencia
(la "vieja")** persigue al jugador.

La experiencia combina **gestión de tensión** (mantener contenta a la muñeca) con momentos de
**survival/escape** (huir del perseguidor), todo envuelto en una ambientación sonora y visual densa.

**Pitch en una línea:** *"Cuidá a la muñeca. No la hagas enojar. Y, pase lo que pase, no intentes irte."*

---

## 2. Core Gameplay

El núcleo es un **bucle de cuidado emocional**:

- La muñeca tiene tres barras emocionales: **Felicidad (Happy)**, **Llanto (Cry)** y **Enojo (Angry)**.
- El comportamiento del jugador (mirarla o ignorarla, acercarse o alejarse, completar o fallar tareas)
  alimenta esas barras.
- La emoción **dominante** determina el **estado** de la muñeca y, con ello, **qué tipo de tarea**
  pide a continuación.
- El jugador explora la casa, **recoge y entrega objetos** y **visita habitaciones** para satisfacer
  esas tareas, evitando que la muñeca caiga en estados negativos.
- La tensión escala hasta el **desafío final**: una secuencia de escape donde la presencia persigue
  al jugador por la casa.

El terror no viene solo de sustos puntuales, sino de la **presión constante** de mantener un equilibrio
emocional inestable mientras la atmósfera se deteriora.

---

## 3. Loop Jugable

```
        ┌─────────────────────────────────────────────────────────┐
        │                                                         │
        ▼                                                         │
  La muñeca está en IDLE                                          │
        │                                                         │
        │  (pasa el tiempo / el jugador la observa o la ignora)   │
        ▼                                                         │
  Suben/bajan las BARRAS emocionales (Happy / Cry / Angry)        │
        │                                                         │
        ▼                                                         │
  La emoción dominante define el ESTADO de la muñeca              │
        │                                                         │
        ▼                                                         │
  La muñeca PIDE UNA QUEST acorde a su emoción                    │
        │                                                         │
        ▼                                                         │
  El jugador EXPLORA / RECOGE / ENTREGA / VA a una habitación     │
        │                                                         │
        ├── Completa a tiempo ──► recompensa emocional ───────────┤
        │                                                         │
        └── Falla / se acaba el tiempo ──► penalización emocional ┘
                                   │
                                   ▼
                 Si la tensión escala lo suficiente / el jugador
                 intenta escapar ──► DESAFÍO FINAL (persecución)
```

**Bucle corto (segundo a segundo):** mirar el entorno, leer pistas/subtítulos, decidir a dónde ir,
interactuar con objetos.

**Bucle medio (por tarea):** recibir quest → resolverla → ver el efecto emocional en la muñeca.

**Bucle largo (sesión):** progresión de la intro narrativa → ciclo de quests → escape final → `GameOver`.

---

## 4. Mecánicas Principales

### 4.1 Movimiento en primera persona
- Caminar / **correr** (Shift) / **agacharse** (Ctrl), con velocidades distintas.
- Físicas con `Rigidbody` + `CapsuleCollider` (no CharacterController).
- **Head bob** dinámico (amplitud/frecuencia según velocidad) y **pasos** con variación de pitch/volumen
  según estado (caminar/correr/agacharse).
- La cámara puede entrar en **modo respiración agitada** en la fase de escape.

### 4.2 Interacción por mirada (raycast)
- Un `Physics.Raycast` desde la cámara detecta `Interactable` y `GrabbableObject`.
- Muestra un **prompt** ("E - …") y, al interactuar, ejecuta la acción.
- **Objetos agarrables**: se levantan con física (siguen la mano por velocidad), no por parenting rígido.

### 4.3 Inventario (3 slots)
- Slots fijos (teclas 1/2/3) con **íconos UI** instanciados.
- Guardar un objeto registra una **flag global** (`GameStateController`) por su `objectID`, lo que conecta
  inventario ↔ quests.

### 4.4 Sistema de Quests (cuidado de la muñeca)
- Definidas como datos en un **ScriptableObject** (`StructureQuest`), con tipo, emoción asociada,
  diálogos de inicio/éxito/fallo, objetos requeridos, habitación destino y recompensa/penalización emocional.
- Tres **tipos** de objetivo (`typeQuest`):
  - **ToCollect** — recoger N objetos (revisa el inventario por `objectID`).
  - **ToGo** — llegar a una habitación (distancia a un `Piece`).
  - **ToDelivery** — llevar objetos a una habitación destino.
- `RandomObjectPositioner` **instancia** los objetos requeridos en posiciones aleatorias del mapa.

### 4.5 Estados emocionales de la muñeca
- Máquina de estados por componentes: **Idle, Watching, Happy, Cry, Angry**.
- La muñeca **observa** al jugador: si la ignora o le da la espalda, sube Enojo/Llanto; si la mira,
  sube Felicidad; puede **rotar** para seguir al jugador.
- La emoción dominante (`Bars.getTopBar()`) define el estado activo y la próxima quest.
- Feedback visual: la fatiga/vignette en pantalla se intensifica con el estado emocional.

### 4.6 Sistemas de la casa (energía, luces, puertas, agua)
- **Panel eléctrico** → restaura energía (flag `power_on`) → habilita interruptores y grupos de luces.
- **Puertas** reutilizables con bloqueo por estado/flag, sonido y apertura por jugador **o por la IA**.
- **Canilla** y otros interactuables narrativos disparan eventos de la intro.

### 4.7 Desafío final: escape / survival
- Ver sección **10. Desafío Final**.

---

## 5. Diseño del Mapa

**Espacio:** una **casa de una/dos plantas** durante una tormenta nocturna, con exterior visible
(lluvia, nubes en movimiento, relámpagos).

**Habitaciones** identificadas con el componente `Piece` (cada una con un `id`), lo que permite que las
quests dirijan al jugador a destinos concretos (cocina, baño, dormitorios, etc.).

**Recorrido (Path Route) de la Intro (lineal y guiado):**
1. **Inicio a oscuras** → el jugador busca y activa el **panel eléctrico** (vuelve la energía).
2. Suena el **teléfono** → contestar (estática + voz + risa de la muñeca).
3. Aparece la **muñeca** → acercarse dispara un golpe en el **baño** + jumpscare + canilla abierta.
4. **Cerrar la canilla** → se enfoca el **espejo** con un mensaje → entra la **música de tensión**.
5. **Fase de escape** → fatiga visual + respiración + aparición del perseguidor.

**Gameplay principal (escena `InGame`):** exploración libre de la casa resolviendo el ciclo de quests
de la muñeca, con objetos repartidos por las habitaciones.

**Intención de ambientación:** el mapa no es un bloque de prueba; busca leerse como un hogar real
deteriorado por la tormenta y la presencia, guiando al jugador por luz, sonido y subtítulos.

---

## 6. Referencias Visuales

- **Tono:** terror psicológico doméstico, nocturno, lluvioso. Referencias del estilo *P.T.*,
  *Visage*, *MADiSON* (casa opresiva, sustos de iluminación, foco en una entidad/objeto inquietante).
- **Paleta:** oscuros fríos (azules/grises de tormenta) cortados por luces cálidas e inestables.
- **Iluminación:** lámparas con flicker/pulso, relámpagos sincronizados con truenos, secciones a oscuras
  que obligan a restaurar energía.
- **Cámara/feedback:** vignette de "fatiga", efecto de "manos" en pantalla al ser capturado, head bob
  y respiración para inmersión corporal.
- **Arte propio:** modelos y texturas creados por el equipo (mobiliario, cocina, la muñeca/"vieja",
  props varios) en `Assets/Art` y `Assets/materials`.

---

## 7. Sistemas Implementados (resumen técnico)

| Dominio | Script(s) principal(es) | Qué hace |
|---|---|---|
| Movimiento | `PlayerMovement`, `PlayerCamera`, `PlayerHeadBob`, `PlayerFootsteps` | FPS con Rigidbody, mirada por Input System, head bob y pasos. |
| Interacción | `PlayerInteraction`, `Interactable`, `GrabbableObject` | Raycast de mirada, prompts, agarre físico, inventario 3 slots. |
| Interactuables | `DoorInteractable`, `FaucetInteractable`, `LightSwitch`, `LightGroupController`, `ElectricalPanelInteractable` | Puertas, canilla, luces y energía. |
| Quests | `Quest`, `StructureQuest`, `typeQuest`, `Piece`, `QuestController`, `RandomObjectPositioner` | Tareas por tipo, datos en ScriptableObject, spawn de objetos. |
| Muñeca | `DollEmotionSystem`, `DollState`, `Idle/Watching/Happy/Cry/Angry`, `DollEmotion`, `Bars`, `TpsDoll` | Máquina de estados emocional + barras + selección de quests. |
| Cámara cinemática | `FixedCameraWithZoom` (Cinemachine opcional) | Focos/zoom durante secuencias, bloquea control. |
| Audio | `MusicManager`, `SFXManager`, `AmbientManager`, `UIAudioManager` | Música en capas, SFX 2D/3D, ambiente + truenos, sonidos UI. |
| FX visuales | `ScreenEffectController`, `RainFXController`, `MovingCloudTexture`, `CameraIdleSway`, `LightPulse/Flicker` | Vignette/overlays, lluvia, nubes, sway, luces inestables. |
| UI | `SubtitleUI`, `PauseMenuController`, inventario, menú 3D (`Menu3DButtons*`, `MenuPlaySequence`) | Subtítulos con prioridad, pausa/opciones, menú diegético. |
| Estado global | `GameController`, `GameStateController` | Persistencia entre escenas, settings (PlayerPrefs), flags narrativas. |
| Narrativa/eventos | `IntroSequenceController`, `DialogueController`, `HintDialogueController`, triggers | Cadena de eventos de la intro, diálogos, pistas, gatillos. |
| Daño | `IDamageable`, `Player_Health`, `Obj_Damage` | Sistema de daño básico por colisión. |

---

## 8. Inteligencia Artificial (IA)

**Perseguidor — "la vieja"** (`PursuerNavMeshController`, requiere `NavMeshAgent`):

- **Persecución:** cada `destinationUpdateRate` segundos hace `agent.SetDestination(player.position)`,
  navegando por el **NavMesh** de la casa.
- **Aceleración progresiva:** la velocidad crece con el tiempo
  `agent.speed = min(maxSpeed, baseSpeed * multiplier^segundos)` (en prefab: multiplicador `1.1`,
  tope `8`), generando presión creciente y evitando que el jugador escape dando vueltas indefinidamente.
- **Apertura de puertas:** raycast frontal (desde la altura del cuerpo) contra `doorMask`; si detecta una
  `DoorInteractable` cerrada, llama `OpenFromAI()` y dispara la animación `Attack`. Hay cooldown para no
  abrir/cerrar en loop.
- **Captura:** si la distancia al jugador ≤ `grabDistance`, detiene el agente, corta los pasos, dispara
  `Attack` y lanza el cierre (`DemoEndController`) tras un breve delay.
- **Animación:** parámetro **`Speed`** (float) alimentado con `agent.velocity.magnitude` para Idle↔Walk,
  y trigger **`Attack`** para golpear puerta/capturar.
- **Audio:** loop de pasos mientras se mueve (`footstepLoopSource`), risa y frase final al capturar.
- **Spawn:** `PursuerSpawnController` aparece a la entidad en `spawnPoint` (warp sobre NavMesh) tras un
  delay, con SFX de aparición.

**IA emocional de la muñeca:** además del perseguidor, la muñeca tiene un comportamiento "inteligente"
de observación (detecta si el jugador la mira o le da la espalda mediante producto punto, ajusta sus
barras y rota para seguirlo) que dirige toda la progresión de quests.

---

## 9. UI

- **Subtítulos** (`SubtitleUI`): sistema central con **prioridades** (`Hint < Environment < Dialogue < Critical`);
  un mensaje de mayor prioridad no es interrumpido por uno menor. Soporta color de texto/fondo por diálogo.
- **Diálogos** (`DialogueController`): pools de líneas por `id`, con delay y duración; usados por quests
  (inicio/éxito/fallo) y por la intro.
- **Pistas** (`HintDialogueController`): pistas automáticas filtradas por flags (`requiredFlags`/`blockedFlags`)
  para guiar sin romper la narrativa.
- **Inventario:** 3 slots con íconos instanciados; resaltado del objeto enfocado.
- **Menú de Pausa / Opciones** (`PauseMenuController`): Tab para pausar; ajustes de **sensibilidad,
  brillo (URP Color Adjustments), y volúmenes** (Master/Music/SFX/Ambient vía AudioMixer), persistidos
  en `PlayerPrefs`. Al pausar aplica **lowpass** al audio y baja/pausa la música.
- **Menú principal 3D** (`Menu3DButtonsController` + `Menu3DButtonVisual`): botones físicos detectados por
  raycast con hover, y una **secuencia cinemática de "Play"** (`MenuPlaySequence`) que titila luces,
  cambia texturas a una versión creepy y termina con un portazo antes de cargar la Intro.

---

## 10. Audio

Diseño en **capas escalonadas** para construir tensión:

- **Música** (`MusicManager`): pista principal en loop + capas paralelas, con **fades** que funcionan
  incluso en pausa (tiempo no escalado). Entra la pista **`tension`** en la revelación del espejo y en
  el escape. En pausa hace ducking o pausa según configuración.
- **Ambiente** (`AmbientManager`): loops de lluvia/viento + **truenos aleatorios** sincronizados con
  **flashes** de luces (relámpagos).
- **SFX** (`SFXManager`): reproducción por **ID** (2D/3D, loops, variantes aleatorias). Eventos:
  teléfono (`PhoneRing`, `PhoneStatic`), muñeca (`DollLaugh`), baño (`BathroomBreak`, `JumpScare`,
  `FaucetLoop`, `CloseFaucet`), final (`PursuerLaugh`, `endPhrase`), interruptores (`LightSwitch`),
  menú (`menu_door_slam`).
- **UI** (`UIAudioManager`): select/back/hover/play.

El audio es **diegético y narrativo**: cada evento de la intro tiene su firma sonora, y la
escalada musical marca el cambio de "cuidado" a "huida".

---

## 11. Desafío Final Integrador

**Tipo:** *Escape sequence + Survival* (persecución).

**Disparo:** al cerrar la canilla y revelarse el mensaje del espejo, `IntroSequenceController` entra en
la **fase de escape** (flag `escape_phase_started`):
- Activa **respiración agitada** y cambia el head bob.
- Dispara el efecto visual **`fatigue`** (vignette/oscurecimiento).
- Asegura la **música `tension`**.
- Llama a `PursuerSpawnController.StartSpawnSequence()` → aparece **la vieja**.

**Durante el escape:**
- La IA persigue por NavMesh, **acelerando** con el tiempo y **abriendo puertas**.
- Si el jugador llega a la salida (`ExitTrigger`) durante la fase, la **puerta se cierra**, lo empuja
  hacia atrás y marca `escape_attempted`: **no se puede escapar** (refuerza la narrativa "aún no es hora de irse").

**Cierre (captura):** cuando la vieja alcanza al jugador (`DemoEndController`):
1. Efecto **`hands`** (manos tapando la visión) + vignette `fatigue`.
2. Risa (`PursuerLaugh`) + frase final (`endPhrase`).
3. Diálogo: *"Aún no es hora de irse querido... Aún no"*.
4. **Fade a negro** y carga de la escena **`GameOver`**.

Integra simultáneamente: IA (NavMesh), animación, audio (música+SFX+pasos), FX de pantalla,
iluminación, triggers, eventos por flags y transición de escena. Es la **culminación** de todos los
sistemas del vertical slice.

---

## 12. Estado Actual del Proyecto

**Terminado y funcional:**
- Intro narrativa completa por eventos (energía → teléfono → muñeca → baño → espejo → escape).
- IA del perseguidor (persecución, aceleración, apertura de puertas, captura).
- Secuencia de cierre y pantalla `GameOver`.
- Sistemas de audio (música/ambiente/SFX/UI), FX (lluvia, nubes, flicker, vignette), iluminación y energía.
- Menú principal 3D con secuencia de Play, y menú de pausa/opciones con settings persistentes.
- Interacción, inventario y interactuables (puertas, canilla, luces, panel).
- Sistema emocional de la muñeca + barras + selección de quests.

**En proceso / a pulir:**
- **Gameplay principal de quests** (escena `InGame`): el ciclo está implementado pero "terminándose";
  hay detalles a cerrar (ver `03_Informe_Testing.md`):
  - El **timer de quests** no descuenta como se espera (`_getTimerDuration()` devuelve el timer actual
    en vez de la duración total).
  - `TpsDoll` localiza el punto pero **no teletransporta** físicamente la muñeca.
  - Algunos nombres de métodos no coinciden con su comportamiento (`restaCryBar` suma).
  - `LightSwitch` con grupo puede invertir el estado dos veces.
- **Cinemachine:** verificar que la virtual camera de foco esté asignada en escena.
- **Build Settings:** agregar `InGame.unity` (figura `Play.unity` deshabilitada).
- **Repositorio:** limpiar carpetas generadas (`Library/`, `Temp/`) del control de versiones.

**Plataforma objetivo:** PC (Windows). Build para itch.io / Drive pendiente.

---

## 13. Ficha técnica rápida

- **Nombre:** Take Care
- **Motor:** Unity 6 — Universal Render Pipeline (URP)
- **Género:** Survival Horror / Terror psicológico — Primera persona
- **Input:** Nuevo Input System (+ teclas clásicas para inventario/debug)
- **Navegación IA:** Unity NavMesh
- **Cámara:** FPS manual + Cinemachine (focos)
- **Escenas:** `Menu` → `Intro` → `InGame` → `GameOver`
- **Equipo:** Giorgio (Sonido) · Walter, Tomás (Modelado y Texturas) · Ángel, Facu, Teo (Programación)
