# Cableado de la escena InGame (victoria / derrota / intro)

Guía paso a paso para armar en Unity todo lo que se programó: la secuencia de intro,
la condición de victoria (felicidad), la de derrota (timer + Pursuer) y la salida.

Scripts involucrados:
- `InGameSequenceController` (nuevo) -> orquesta todo el flujo.
- `WinExitTrigger` (nuevo) -> detecta cuando el jugador cruza la puerta al ganar.
- `Quest` y `QuestController` (ya modificados) -> avisan al flow cuando se completa/falla una misión.
- `PursuerSpawnController`, `DemoEndController`, `DoorInteractable`, `DialogueController` (ya existen).

> Antes de empezar: abrí la escena `Assets/Scenes/InGame.unity`.

---

## 0. Velocidad del jugador (ya quedó arreglado)

No hay ningún script que baje la velocidad en runtime. El "arranca lento" venía de los
valores **serializados**, que pisaban el default del script (`walkSpeed = 4`):

- `Player.prefab` tenía `walkSpeed 1.5 / sprint 2.5 / crouch 0.75`.
- En `InGame` el override del jugador era `walk 3 / sprint 4 / crouch 2`.

Ya los subí a **walk 4 / sprint 6.5 / crouch 2** en el prefab y en el override de InGame.
Si querés otra sensación, tocá los campos de `PlayerMovement` en el **prefab `Player`**
(así vale para todas las escenas) y no en cada escena.

---

## 1. Objeto controlador del flujo (`InGameSequenceController`)

Es el cerebro de la escena. Todo lo demás se le cuelga.

1. En la jerarquía: clic derecho → **Create Empty**. Nombralo `GameFlow`.
2. Dejalo en posición `(0,0,0)` (no importa, no se mueve).
3. Add Component → **InGameSequenceController**.

Campos a completar (los explico todos en el paso final, primero creamos lo que falta).

---

## 2. Imagen de fade (pantalla negra del intro y de la victoria)

El intro arranca en negro y aclara; la victoria hace lo inverso. Usa una `Image` UI.

1. Si **ya existe un Canvas** en la escena, usalo. Si no: clic derecho → **UI → Canvas**.
   - Canvas en `Render Mode: Screen Space - Overlay`.
2. Sobre el Canvas: clic derecho → **UI → Image**. Nombrala `FadeImage`.
3. En el `Rect Transform` de `FadeImage`: anchors **stretch/stretch** (alt+shift al elegir
   el preset) para que tape toda la pantalla (left/right/top/bottom = 0).
4. En el componente `Image`: color **negro**, alpha **255** (la dejamos opaca; el script
   la aclara solo al iniciar).
5. Asegurate de que `FadeImage` esté **por encima** del resto del UI del HUD en la jerarquía
   (último hijo del Canvas) para que tape todo.

> El script la fuerza a negro en `Awake`, antes del primer frame visible. Si igual ves un
> flash, revisá que `fadeImage` esté asignada en `GameFlow`.

### UI apagada durante la intro

Si querés que el Canvas arranque limpio, dejá activo solo lo que necesitás para la intro
(`Content`/subtítulos y `FadeImage`) y mandá el resto al campo `uiObjectsHiddenDuringIntro`
del `GameFlow`. El flow los apaga al arrancar y los vuelve a prender cuando terminan el
diálogo + los 5 segundos.

---

## 3. Diálogo de intro (`DialogueController`)

En la escena ya hay un objeto `Dialogue` con el componente `DialogueController`.

1. Seleccioná el objeto `Dialogue`.
2. En `Pools` (lista de `DialoguePool`), agregá un elemento nuevo:
   - **Id**: `ingame_intro`  (tiene que coincidir con el campo del flow).
   - **Lines**: agregá las líneas del monólogo del sótano. Por cada línea:
     - `text`: el texto.
     - `delayBefore`: segundos de espera antes de mostrarla (opcional).
     - `duration`: cuántos segundos queda en pantalla.
   - El flow espera a que terminen **todas** las líneas (suma `delayBefore + duration`)
     antes de contar los 5 segundos.
3. Verificá que también exista el pool **`captureMessege`** (lo usa el final por captura).
   Si no está, crealo igual con el mensaje de cuando te atrapan.

> Si no ponés ninguna línea, el intro igual funciona: hace el fade y espera los 5 seg.

---

## 4. La muñeca arranca apagada (`DollEmotionSystem`)

El flow enciende el sistema de la muñeca recién cuando empiezan las quests, así no pide
misiones durante el intro.

1. Buscá el objeto de la muñeca (`TpsDoll` o el que tenga el componente `DollEmotionSystem`).
2. En el Inspector, **destildá** el checkbox del componente `DollEmotionSystem`
   (que arranque deshabilitado). El flow lo prende solo.

> No desactives el GameObject entero, solo el componente `DollEmotionSystem`.

---

## 5. Puerta de salida (`DoorInteractable`)

Es la puerta que se abre sola al llegar a 100 de felicidad.

1. Elegí la puerta que va a ser la salida (o creala con su `DoorInteractable`).
2. En su `DoorInteractable`:
   - `canOpen` = **true**.
   - `Requirement Type` = **CustomFlag**.
   - `Custom Flag Name` = **`escape_door_unlocked`**.
   - `Starts Opened` = false.
   - `Close On Player Contact` = false.
3. Anotá mentalmente este objeto: lo vas a arrastrar al campo `escapeDoor` del flow.

Así queda bloqueada para el jugador al principio, aunque `canOpen` esté en true. Al llegar
a 100 de felicidad, el `GameFlow` prende la flag `escape_door_unlocked` y recién ahí la
abre con `OpenFromAI()`.

---

## 6. Trigger de victoria detrás de la puerta (`WinExitTrigger`)

Esto es lo que te confundía: el campo **`flow`** del `WinExitTrigger` es simplemente la
**referencia al objeto `GameFlow`** (el que tiene el `InGameSequenceController`). Nada más.

1. Clic derecho → **Create Empty**. Nombralo `WinExitTrigger`.
2. Ponelo **justo detrás / en el umbral** de la puerta de salida (donde el jugador pasaría
   al escapar).
3. Add Component → **Box Collider**. Marcá **Is Trigger** (el script igual lo fuerza).
   Ajustá el tamaño para que cubra el paso de la puerta.
4. Add Component → **WinExitTrigger**.
5. Campos:
   - `flow`: **arrastrá el objeto `GameFlow`** (el del `InGameSequenceController`).
   - `playerTag`: dejalo en `Player` (el jugador tiene que tener ese Tag).

> Solo dispara la victoria si **ya** llegaste a 100 (la puerta se abrió). Si lo cruzás antes,
> no hace nada.

---

## 7. Camino de derrota: Pursuer + Spawn + final por captura

Hoy la escena `InGame` **no tiene** ni el Pursuer ni el `DemoEndController` (solo están en
`Intro`). Hay que traerlos.

### 7a. Pursuer
1. Arrastrá `Assets/Prefabs/Pursuer.prefab` a la jerarquía de `InGame`.
2. **Desactivá el GameObject** del Pursuer (checkbox de arriba a la izquierda en el Inspector).
   Tiene que arrancar apagado: el spawner lo prende cuando se acaba el timer.
3. En su componente `PursuerNavMeshController`:
   - `player`: podés dejarlo vacío (se autobusca por el Tag `Player`) o arrastrar el jugador.
   - `captureEndController`: lo asignás en el paso 7c.
4. Importante: tiene `NavMeshAgent`, así que la escena necesita un **NavMesh bakeado** que
   cubra el sótano/recorrido. (Window → AI → Navigation → Bake, si no está hecho.)

### 7b. Punto de spawn
1. Create Empty → `PursuerSpawnPoint`. Ubicalo donde querés que aparezca el Pursuer
   (sobre el NavMesh, lejos del jugador).

### 7c. DemoEndController (final por captura)
1. Create Empty → `CaptureEnd`. Add Component → **DemoEndController**.
2. Campos:
   - `fadeImage`: podés reusar la misma `FadeImage` del paso 2 (o una propia).
   - `endSceneName`: el nombre de la escena de fin (ej. `GameOver` / `EndGame`, la misma
     que usa la demo). Tiene que estar en Build Settings.
   - `captureMessageDialogueId`: `captureMessege` (el pool del paso 3).
   - El resto (`captureLaughSfxId`, `endPhraseSfxId`, `handsEffectId`, etc.) dejalos como vienen
     por default, siempre que existan los managers (`SFXManager`, `ScreenEffectController`).
3. Volvé al **Pursuer** → `PursuerNavMeshController` → `captureEndController`:
   arrastrá este objeto `CaptureEnd`.

### 7d. SpawnController (ya existe, hay que activarlo)
1. Seleccioná el objeto `SpawnController` de la escena (tiene `PursuerSpawnController`).
2. **Activá el componente** (hoy está deshabilitado / `m_Enabled: 0`). Tildá el checkbox.
   El GameObject tiene que estar activo.
3. Campos:
   - `pursuerObject`: arrastrá el **Pursuer** (paso 7a).
   - `spawnPoint`: arrastrá `PursuerSpawnPoint` (paso 7b).
   - `spawnDelay`: dejalo bajo (0–1). El timer de 10 min ya es la espera real.
   - `spawnOnlyOnce`: true.

---

## 8. Verificación de las quests (felicidad + timer por misión)

Ya está cableado por código: `Quest` busca el `InGameSequenceController` solo y le avisa.
No tenés que arrastrar nada acá, pero revisá:

1. Que haya **4 misiones** definidas (cada completada suma 30; 4 × 30 = 120 → con 4 ya ganás
   pasando los 100). Si querés que sea exacto 100, dejá 4 misiones igual: el flow clampea a 100.
2. **Timer por misión**: para que fallar reste felicidad, cada `StructureQuest` tiene que tener
   `TimerDuration > 0`. Si una misión tiene timer 0, nunca falla sola (solo suma).
3. No hace falta tocar `QuestController`: ya tiene el guard para no spamear cuando el timer es 0.

---

## 9. Escenas en Build Settings

`File → Build Settings → Scenes In Build`. Tienen que estar agregadas:
- `InGame`
- La escena de **victoria** (la que pongas en `winSceneName`, por defecto `Win`).
- La escena de **derrota/fin** (la que pongas en `endSceneName` del `DemoEndController`).

Si los nombres no coinciden exactamente con los de las escenas, no carga (revisá mayúsculas).

---

## 10. Volver al `GameFlow` y completar el `InGameSequenceController`

Ahora sí, seleccioná `GameFlow` y completá todos los campos:

**Referencias**
- `playerMovement`: arrastrá el jugador (el que tiene `PlayerMovement`).
- `dollEmotionSystem`: arrastrá la muñeca (la del componente `DollEmotionSystem`, paso 4).
- `pursuerSpawn`: arrastrá `SpawnController` (paso 7d).
- `escapeDoor`: arrastrá la puerta de salida (paso 5).
- `escapeDoorFlagName`: `escape_door_unlocked`.

**Fade in**
- `fadeImage`: arrastrá `FadeImage` (paso 2).
- `fadeInDuration`: 3 (o lo que quieras de fade lento).
- `uiObjectsHiddenDuringIntro`: arrastrá acá los objetos de UI que no querés ver durante la intro.
  No metas `FadeImage` ni el objeto que muestre subtítulos/dialogo.

**Intro**
- `introDialogueId`: `ingame_intro` (paso 3).
- `lockPlayerDuringIntro`: **false** (el jugador se puede mover durante la intro).
- `delayBeforeQuests`: **0** (apenas termina la intro ya podés activar acercándote a la muñeca).
- `introFinishedFlag`: `intro_finished` (flag que se prende al terminar la intro; útil para HintDialogue).

**Activación por cercanía a la muñeca**
- `dollApproachDistance`: 3 (radio para que, terminada la intro, al acercarte a la muñeca arranquen las quests).
- `questsStartedFlag`: `quests_started` (flag que se prende al arrancar las quests; útil para HintDialogue).

> Flujo nuevo: termina la intro → se prende `intro_finished` → te acercás a la muñeca →
> arrancan las quests al instante (sin la espera larga del idle), arranca el timer y se
> prende `quests_started`.
- `delayBeforeQuests`: 5 (los 5 segundos pedidos antes de arrancar las quests).

**Victoria (felicidad)**
- `happinessPerMission`: 30.
- `happinessLostOnFail`: 30.
- `happinessToWin`: 100.
- `escapeMessage`: "La muñeca esta feliz y te abrio la puerta. RAPIDO! ESCAPA!".
- `escapeMessageDuration`: 6.
- `winSceneName`: `Win` (o el nombre real de tu escena de victoria).

**Derrota (timer)**
- `questPhaseDuration`: 600 (10 minutos).
- `timerLabel`: opcional. Si querés mostrar el tiempo, creá un `TextMeshPro - Text (UI)` en el
  Canvas y arrastralo acá. Si lo dejás vacío, el timer corre igual pero no se ve.

---

## 11. Prueba rápida (orden esperado)

1. Play en `InGame`.
2. Pantalla negra → aclara (fade in lento). Ya te podés mover.
3. Aparece el diálogo del sótano. Al terminar, esperan 5 seg.
4. Recuperás el control, la muñeca empieza a pedir misiones y arranca el timer de 10 min.
5. **Victoria**: completás misiones (cada una +30). Al llegar a 100, se abre la puerta, sale
   el mensaje de escape. Cruzás el `WinExitTrigger` → fade out → escena `Win`.
6. **Derrota**: si no escapás antes de los 10 min, aparece el Pursuer. Si te atrapa → mensaje,
   risa, fade y escena de fin.

### Si algo no anda
- "No aparece el Pursuer": revisá que `SpawnController` esté activo, con `pursuerObject` y
  `spawnPoint` asignados, y que el Pursuer arranque **desactivado** sobre el NavMesh.
- "No gana al cruzar la puerta": el `WinExitTrigger` necesita el `flow` asignado y el jugador
  con Tag `Player`; además solo funciona si ya se llegó a 100.
- "No se abre la puerta": `canOpen` true, `Requirement Type = CustomFlag`, `Custom Flag Name = escape_door_unlocked`
  y el mismo nombre en `escapeDoorFlagName` del `GameFlow`.
- "No carga la escena de victoria/fin": el nombre tiene que estar en Build Settings y escrito igual.
