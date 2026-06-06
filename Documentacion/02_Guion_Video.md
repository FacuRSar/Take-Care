# Take Care — Guion del Video de Entrega (Segundo Parcial)

> Duración máxima: **5 minutos**. Sigue la **estructura sugerida por la cátedra** (Intro / Desarrollo / Cierre).
> Participan **todos** los integrantes: cada uno se presenta (nombre y apellido), explica su **rol**,
> **qué sistemas desarrolló** y **cómo los implementó en Unity**.
> El **audio debe escucharse correctamente**. Mostrar gameplay real mientras se narra.

**Equipo:**
- **Giorgio** → Sonido
- **Walter** y **Tomás** → Diseño de modelos y texturas
- **Ángel**, **Facu** y **Teo** → Programación

> Nota: el reparto exacto de "quién programó qué" está sugerido abajo entre los tres programadores.
> **Ajústenlo a la realidad** antes de grabar (cada uno debe hablar de lo que efectivamente hizo).

---

## Distribución de tiempo (objetivo ~5:00)

| Bloque | Tiempo | Contenido |
|---|---|---|
| Intro | 0:00 – 0:45 | Nombre del juego, de qué trata, objetivo, evolución vs. 1er parcial. |
| Desarrollo | 0:45 – 4:15 | Gameplay + cada integrante explica su sistema. |
| Cierre | 4:15 – 5:00 | Organización del equipo, problemas, soluciones, aprendizajes. |

---

## BLOQUE 1 — INTRO (0:00 – 0:45)

**En pantalla:** menú principal 3D de *Take Care* → apretar "Play" → secuencia creepy (titileo,
texturas alteradas, portazo) → primeros segundos de la Intro.

**Narración (puede leerla un integrante, p. ej. Ángel):**

> "Nuestro juego se llama **Take Care**. Es un **survival horror en primera persona** hecho en Unity.
> El jugador pasa una noche de tormenta en una casa donde debe **cuidar a una muñeca** que tiene
> emociones propias: hay que mantenerla contenta completando tareas… porque si se descontrola, la casa
> reacciona y una presencia empieza a perseguirte.
>
> El **objetivo del jugador** es sobrevivir: atender las tareas de la muñeca, explorar la casa y escapar
> del desafío final.
>
> Respecto al **Primer Parcial**, evolucionamos de un greybox de prueba a un **vertical slice** con
> arte y texturas propias, **audio en capas**, **IA con NavMesh**, **efectos visuales**, **secuencias
> cinemáticas** y un **desafío final integrador**."

**Tip de edición:** que la frase de "una presencia empieza a perseguirte" caiga justo cuando se ve un
flash de relámpago o el primer plano del perseguidor.

---

## BLOQUE 2 — DESARROLLO (0:45 – 4:15)

> Mostrar **gameplay continuo** de fondo mientras cada integrante habla de su parte.
> Idealmente cada uno aparece (cámara o voz) cuando se muestra **su** sistema.

### 2.1 Recorrido del escenario y Path Route (mostrar primero)
**En pantalla:** recorrido de la Intro: casa a oscuras → panel eléctrico → vuelve la luz → teléfono →
muñeca → baño/jumpscare → espejo → arranque del escape.

**Narración breve:**
> "El recorrido está **guiado por eventos**: arrancás a oscuras, restaurás la energía en el panel
> eléctrico, suena el teléfono, aparece la muñeca y todo escala hasta la persecución final."

---

### 2.2 Ángel — Programación: Jugador, Cámara e Interacción
**En pantalla:** caminar/correr/agacharse, agarrar un objeto, abrir una puerta, inventario, prompts.

**Guion (Ángel):**
> "Soy **Ángel [Apellido]**, parte de **programación**. Desarrollé el **control del jugador y la
> interacción**.
>
> El movimiento usa **Rigidbody con CapsuleCollider** y el **nuevo Input System**: tengo caminar,
> correr y agacharme con velocidades distintas, más **head bob** y **pasos** que cambian según la
> velocidad. La cámara en primera persona la manejo leyendo el delta del mouse, con un filtro para
> evitar saltos bruscos.
>
> La **interacción es por raycast** desde la cámara: detecto objetos `Interactable` o agarrables, muestro
> un prompt y, al apretar E, ejecuto la acción. Los **objetos agarrables** siguen la mano con física
> (por velocidad, no pegados), y el **inventario de 3 slots** guarda objetos registrando **flags
> globales**, que después usan las quests."

---

### 2.3 Facu — Programación: Quests, Muñeca y Sistema Emocional
**En pantalla:** la muñeca cambiando de estado, una quest apareciendo (subtítulo), recoger/entregar
objetos, barras emocionales (si hay debug visible).

**Guion (Facu):**
> "Soy **Facu [Apellido]**, de **programación**. Hice el **núcleo de gameplay**: el **sistema de quests**
> y el **sistema emocional de la muñeca**.
>
> Las quests están definidas como **ScriptableObject**, así el equipo puede crear tareas desde el
> Inspector: cada una tiene un tipo (**recolectar**, **ir a una habitación** o **entregar objetos**),
> diálogos y un efecto emocional. Los objetos requeridos se **instancian** en posiciones aleatorias del
> mapa.
>
> La muñeca es una **máquina de estados** (Idle, Watching, Happy, Cry, Angry) con **tres barras
> emocionales**. Según cómo el jugador la trate —si la mira, la ignora, o falla tareas— cambia la
> emoción dominante, y **esa emoción decide la próxima quest**. Es lo que conecta el comportamiento del
> jugador con la progresión del juego."

---

### 2.4 Teo — Programación: IA, Secuencias e Iluminación/Estados
**En pantalla:** el perseguidor saliendo a escena, persiguiendo, abriendo una puerta, la captura;
luces que fallan, panel de energía.

**Guion (Teo):**
> "Soy **Teo [Apellido]**, de **programación**. Me encargué de la **IA del perseguidor** y de las
> **secuencias y estados de la casa**.
>
> La 'vieja' usa **NavMesh**: persigue al jugador actualizando su destino, **acelera con el tiempo**
> hasta un tope para que no puedas escapar dando vueltas, y **abre puertas** detectándolas con un raycast
> frontal. Cuando te alcanza, dispara la **animación de ataque** y arranca el cierre.
>
> Toda la **intro** es una cadena de **eventos por flags y corutinas** (`GameStateController`): energía,
> teléfono, muñeca, baño, espejo y el escape. También armé el **sistema de energía e iluminación**: el
> panel eléctrico habilita las luces, que tienen flicker y pulsos para la atmósfera."

> *(Si el reparto real es distinto, intercambien los párrafos 2.2/2.3/2.4 según corresponda.)*

---

### 2.5 Giorgio — Sonido
**En pantalla:** subir un poco el audio en la edición: lluvia, truenos, teléfono, risa de la muñeca,
música de tensión entrando, portazo del menú.

**Guion (Giorgio):**
> "Soy **Giorgio [Apellido]** y me encargué del **sonido**. Diseñé el **paisaje sonoro de terror** en
> capas: el **ambiente** constante (lluvia y viento) con **truenos aleatorios** sincronizados con los
> relámpagos; los **efectos** puntuales por evento (teléfono, estática, risa de la muñeca, jumpscare del
> baño, el portazo del menú); y la **música**, que entra como **pista de tensión** en los momentos clave
> y hace fade.
>
> Trabajé con los managers de audio del proyecto, organizando los sonidos por **identificadores** y
> mezclándolos con un **AudioMixer** (master, música, SFX y ambiente), que además se puede ajustar desde
> el menú de opciones y se **filtra al pausar**."

---

### 2.6 Walter y Tomás — Modelos y Texturas
**En pantalla:** paneo por la casa, mobiliario, la cocina, la muñeca/"vieja", props; mostrar materiales
y texturas.

**Guion (Walter):**
> "Soy **Walter [Apellido]**. Junto a Tomás hicimos el **modelado 3D**: la casa, el mobiliario, los props
> y el personaje de la muñeca/'vieja'. Buscamos que el espacio se lea como un **hogar real y opresivo**,
> no como un nivel de prueba."

**Guion (Tomás):**
> "Soy **Tomás [Apellido]**. Me enfoqué en **texturas y materiales**: madera, granito de la cocina, metal,
> el vestido de la muñeca, etc., cuidando la **cohesión visual** y que todo funcione con la **iluminación
> nocturna** y los efectos de tormenta. El salto del greybox a este acabado es la mayor evolución visual
> respecto al primer parcial."

---

### 2.7 Sistemas integrados (montaje rápido, ~20s)
**En pantalla (cortes rápidos mientras una voz enumera):** Animaciones (perseguidor, puertas) ·
Cinemachine/focos de cámara · UI (subtítulos, inventario, pausa) · Partículas (lluvia) · Eventos
(intro) · IA · Audio.

> "Todo esto se **integra** en la experiencia: animación, Cinemachine, UI, partículas, iluminación,
> eventos y audio trabajando juntos."

---

### 2.8 Desafío final (clímax, ~25s)
**En pantalla:** la fase de escape completa: fatiga en pantalla, respiración, la vieja persiguiendo,
intento de salida bloqueado, captura → fade → `GameOver`.

**Narración:**
> "El **desafío final** es una **secuencia de escape y supervivencia**: cuando la tensión llega al límite,
> aparece la presencia y te persigue por la casa. La salida **está bloqueada** —'aún no es hora de irse'—
> y si te atrapa, termina con el cierre y la pantalla de Game Over. Es la **culminación** que integra IA,
> audio, efectos visuales, iluminación y eventos."

---

## BLOQUE 3 — CIERRE (4:15 – 5:00)

**En pantalla:** pantalla con créditos del equipo o gameplay tranquilo de fondo.

Responder las 4 preguntas de la consigna (puede repartirse entre integrantes):

**¿Cómo se organizaron como equipo?**
> "Dividimos por especialidad: **sonido** (Giorgio), **arte 3D y texturas** (Walter y Tomás) y
> **programación** (Ángel, Facu y Teo), coordinando por **GitHub**. Cada programador tomó un dominio
> (jugador/interacción, quests/muñeca, IA/secuencias) para trabajar en paralelo sin pisarnos."

**¿Qué problemas encontraron?**
> "Integrar tantos sistemas sin que choquen (eventos, flags, audio), **conflictos en Git** por escenas
> y archivos generados, y ajustar el **balance** del sistema emocional de la muñeca y la **dificultad** de
> la persecución. También bugs de animación (la vieja 'deslizándose' por un clip sin loop) y de timing
> de las quests."

**¿Cómo los resolvieron?**
> "Centralizamos el estado en un **GameStateController** con flags y eventos, lo que desacopló los
> sistemas. Para la IA ajustamos la **aceleración y la apertura de puertas**, y arreglamos las
> animaciones configurando el **loop** correcto. El audio lo unificamos por **IDs** y AudioMixer."

**¿Qué aprendieron?**
> "A **integrar** sistemas en una experiencia coherente y no solo a sumarlos sueltos: a usar NavMesh,
> Cinemachine, el Input System, ScriptableObjects, AudioMixer y URP, y sobre todo a **trabajar en equipo
> con Git** sobre un mismo proyecto de Unity."

---

## Checklist de grabación (no olvidar)

- [ ] Cada integrante dice **nombre y apellido** + **rol** + **sistema** + **cómo lo implementó**.
- [ ] Se ve **gameplay real** (no solo slides).
- [ ] Se muestran: gameplay, **path route**, diseño del mapa, **IA**, **audio**, **UI**, **Cinemachine**,
      **animaciones**, **eventos** y **sistema final**.
- [ ] **El audio se escucha bien** (subir música/SFX en la edición; narración clara por encima).
- [ ] Dura **≤ 5:00**.
- [ ] Al final o en la ficha de itch.io: **controles, objetivo, bugs conocidos, créditos**.

## Controles (mostrar en pantalla en algún momento)
- **WASD**: moverse · **Shift**: correr · **Ctrl**: agacharse
- **Mouse**: mirar · **E**: interactuar / agarrar · **1/2/3**: slots de inventario
- **Tab**: pausa / opciones
