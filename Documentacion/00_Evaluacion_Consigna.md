# Take Care — Evaluación de la Consigna (Segundo Parcial)

> Documento de control interno. Cruza cada punto pedido por la cátedra contra el estado real
> del proyecto (código y escenas verificados). Sirve como checklist previo a la entrega.

**Leyenda de estado:**
- ✅ Implementado y verificado en el proyecto.
- 🟡 Implementado parcialmente / funciona pero conviene pulir o cablear.
- 🔴 Falta o es entregable externo (no es código): hay que generarlo/subirlo.

---

## 1. Sistemas Base (evolución del Primer Parcial)

| Punto | Estado | Dónde está en el proyecto |
|---|---|---|
| Organización del proyecto | ✅ | `Assets/Scripts` ordenado por dominios (Player, Interaction, Quest, Doll, Controllers, Intro, Ambient, UI, Core, World). |
| Uso de GitHub y trabajo colaborativo | 🟡 | Repo activo. Verificar commits significativos y limpiar `Library/` del control de versiones (ver Testing). |
| High Concept actualizado | ✅ | Ver `01_GDD.md`. |
| Movimiento del personaje | ✅ | `PlayerMovement.cs` (Rigidbody + Input System: caminar/correr/agachar). |
| Física y colisiones | ✅ | Rigidbody/CapsuleCollider en player; `GrabbableObject` con física; `Obj_Damage` por colisión. |
| Triggers y eventos | ✅ | `DollProximityTrigger`, `ExitTrigger`, `PhoneTrigger`, `OneShotSoundTrigger`, `GameStateController` (flags + evento `OnFlagChanged`). |
| Cámara funcional | ✅ | `PlayerCamera.cs` (FPS manual) + `FixedCameraWithZoom.cs` (focos/cinemática). |
| Prefabs | ✅ | Perseguidor, objetos agarrables, íconos de inventario, etc. |
| Raycast | ✅ | `PlayerInteraction` (interacción por mirada), perseguidor abre puertas por raycast, menú 3D por raycast. |
| Sistema de daño (si aplica) | 🟡 | `IDamageable` + `Player_Health` + `Obj_Damage` existen; poco usado en el slice (el cierre es por captura, no por daño). |
| Instanciación de objetos | ✅ | `RandomObjectPositioner` instancia objetos de quest; inventario instancia íconos UI. |

---

## 2. Evolución del Proyecto (vs. Primer Parcial)

| Punto | Estado | Observación |
|---|---|---|
| Paso de Greyboxing a escenario avanzado | 🟡 | Modelos y texturas propios en `Assets/Art/Modelos` y `Assets/materials`. Confirmar que la escena `InGame` esté vestida (no greybox). |
| Construcción final | 🟡 | El gameplay principal "se está terminando aún"; el cierre (end) ya está. |
| Uso de materiales y texturas | ✅ | Texturas propias (cocina, muebles, vestido de la muñeca, granito, etc.). |
| Construcción del terreno / nivel | 🟡 | Casa interior + exterior con tormenta. Confirmar navegación completa. |
| Mejora del diseño de nivel | ✅ | Recorrido guiado en Intro; habitaciones identificadas con `Piece`. |
| Path Route (rutas de juego) | ✅ | Intro es un recorrido lineal por eventos; quests dirigen al jugador por habitaciones. |
| Progresión jugable | ✅ | Intro → gameplay de quests emocionales → desafío final (persecución). |
| Mayor cohesión visual | ✅ | Lluvia, nubes, flicker de luces, vignette/fatiga, paleta de terror. |
| Mejoras en interacción y feedback | ✅ | Prompts, subtítulos con prioridad, SFX por evento, resaltado de objetos. |

---

## 3. Sistemas Integrados Obligatorios

| Sistema | Estado | Implementación verificada |
|---|---|---|
| Sistema de Animación | ✅ | Animator del perseguidor (Idle/Walk/Attack por parámetro `Speed` + trigger `Attack`); animaciones de puertas/palanca por interpolación. |
| Audio — Música | ✅ | `MusicManager` (capas, fades, ducking en pausa; pista `tension`). |
| Audio — Ambientales | ✅ | `AmbientManager` (lluvia/viento, truenos aleatorios + flash de luces). |
| Audio — Feedback sonoro | ✅ | `SFXManager` (2D/3D, loops, variantes) + `UIAudioManager`. |
| Sistema de UI | ✅ | `SubtitleUI` (prioridades), `PauseMenuController` (pausa + opciones), inventario, menú 3D. |
| Cámaras con Cinemachine | 🟡 | `FixedCameraWithZoom` soporta `CinemachineCamera` para focos. **Verificar que la virtual camera esté asignada en escena** (si no, usa fallback manual). |
| Iluminación | ✅ | Sistema de energía (`ElectricalPanel` → flag `power_on` → `LightGroupController`), flicker, pulsos, rayos. |
| Inteligencia Artificial (IA) | ✅ | `PursuerNavMeshController` (NavMeshAgent: persigue, abre puertas, acelera, captura). |
| Sistema de Partículas | ✅ | `RainFXController` (lluvia por ParticleSystem) + visuales de tormenta. |
| Eventos y secuencias de gameplay | ✅ | `IntroSequenceController` (cadena de eventos por flags y corutinas), `MenuPlaySequence`. |

---

## 4. Desafío Final Integrador

| Punto | Estado | Detalle |
|---|---|---|
| Cierre jugable / desafío final | ✅ | **Escape sequence + Survival**: tras cerrar la canilla y revelarse el espejo, arranca la fase de escape (`escape_phase_started`): respiración agitada, fatiga visual, música de tensión y aparición del perseguidor (`PursuerSpawnController`). |
| Integra múltiples sistemas | ✅ | Combina IA (NavMesh), animación, audio (música + SFX + pasos), FX de pantalla (`fatigue`, `hands`), iluminación, triggers (`ExitTrigger`) y carga de escena (`GameOver`). |
| Funciona como culminación | ✅ | La captura dispara `DemoEndController` → diálogo final → fade → `GameOver`. |

---

## 5. Entregables

| Entregable | Estado | Acción pendiente |
|---|---|---|
| **Video (máx. 5 min)** | 🔴 | Grabar y editar siguiendo `02_Guion_Video.md`. Cada integrante se presenta y explica su rol/sistema. Audio audible. |
| **Build / itch.io** | 🔴 | Generar build Windows (o WebGL) y subir a itch.io o Drive. Aclarar "Segundo Parcial", controles, objetivo, bugs conocidos, créditos. |
| **Repositorio GitHub** | 🟡 | Existe. Limpiar (gitignore para `Library/`, `Temp/`), confirmar commits significativos y estructura ordenada. |
| **GDD** | ✅ | `01_GDD.md` (este paquete). |
| **Informe de Testing** | ✅ | `03_Informe_Testing.md` (este paquete). |

---

## 6. Veredicto general

**El proyecto cumple técnicamente con la gran mayoría de los contenidos evaluados.** Todos los
"Sistemas Integrados Obligatorios" tienen implementación real y verificable en código, y hay un
desafío final integrador funcional (escape + captura). La evolución respecto al Primer Parcial está
presente (arte propio, audio en capas, IA, FX, secuencias).

**Para asegurar la aprobación, lo que falta es principalmente entregable externo y pulido, no código nuevo:**

1. 🔴 **Video** (guion listo en `02_Guion_Video.md`).
2. 🔴 **Build publicada** (itch.io / Drive) con su ficha (controles, objetivo, bugs, créditos).
3. 🟡 **Limpieza del repo** (gitignore de carpetas generadas, commits ordenados).
4. 🟡 **Verificar en escena**: que la `CinemachineCamera` de foco esté asignada y que el build
   apunte a la escena correcta (en `EditorBuildSettings` figura `Play.unity` deshabilitada; la escena
   de gameplay real es `InGame.unity` — **agregarla al Build Settings**).
5. 🟡 **Cerrar el gameplay principal** (las quests están casi listas; ver bugs conocidos en Testing).

> Riesgo principal de la consigna: "La entrega incompleta implica desaprobación directa". Por eso los
> puntos 🔴 (video + build + links funcionando) son tan importantes como el código.
