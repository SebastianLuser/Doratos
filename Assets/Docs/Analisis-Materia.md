# Análisis — Juegos en Red (UADE, 1C 2026)

## Datos del cursado

- **Docente:** Nahuel Reimondo
- **Modalidad:** Presencial jueves 18:30–22:00 + 1 encuentro remoto sábado 30/5 (9–13h)
- **Carga:** 68 hs, 17 clases (12/3 al 2/7) + final regular 23/7
- **Stack confirmado:** Unity + Photon (PUN por contexto de las clases)
- **Proyecto:** 2 juegos grupales (uno por parcial), grupos hasta 5 alumnos, ~18 estudiantes en total

## Cronograma resumido

| Fecha | Hito |
|-------|------|
| 12/3 | Clase 1 — Protocolos y arquitecturas |
| 19/3 | Clase 2 — Estado, Rooms, interacción |
| 26/3 | Práctica |
| 9/4 | Autoridad, métricas, ping |
| 16/4 | Definición 1° parcial |
| **30/4** | **1° PARCIAL** |
| 14/5 | Otras herramientas de Photon |
| 21/5 | Optimización |
| 28/5 | Serialización, encriptación, anti-cheats |
| 30/5 | Remoto — definición 2° parcial |
| 4/6 | APIs |
| 11/6 | Live Ops |
| **18/6** | **2° PARCIAL** |
| 2/7 | Recuperatorio / final adelantado |
| **23/7** | **FINAL REGULAR** |

---

## Reconstrucción cronológica de las 3 clases grabadas

Las VTT no están ordenadas por nombre de archivo. Por contenido, el orden real es:

### Clase A — `Reunión en _General_ (2).vtt` (la más temprana)
**Temas:** Internet, IP, puertos, TCP vs UDP, arquitecturas cliente-servidor vs híbrida, introducción a Photon (crear app en dashboard, App ID), primer acercamiento a estado replicado y sincronización.
**Analogías clave:** UDP = radio/streaming (tirar datos sin confirmar), TCP = llamada telefónica (con confirmación).
**Calidad de audio:** baja, muchos errores de ASR.

### Clase B — `Reunión en _General_ .vtt` (intermedia)
**Temas:** Photon PUN en profundidad — `PhotonNetwork.Instantiate` vs `Instantiate` de Unity, carpeta `Resources`, PhotonView, sincronización de Transform, MasterClient para lógica centralizada, generación procedural de escenario, diseño sincrónico vs asincrónico.
**Práctica asignada:** Prototipo de **Tanques** multijugador (hasta 4 jugadores, escenario procedural via MasterClient, disparo con daño sincronizado, último en pie gana).

### Clase C — `Reunión en _General_ (1).vtt` (la más reciente)
**Temas:** Revisión de bugs del juego de Tanques (escena que no carga para el 2° jugador), sistema de daño en red (¿quién aplica el daño? autoridad), `PhotonTransformView` con interpolación/extrapolación, Rooms en profundidad (RoomOptions, visibilidad, MaxPlayers), callbacks (`OnRoomListUpdate`), `AutomaticallySyncScene`, Lobby con lista de rooms.
**Próxima clase anunciada:** RPC + definición formal del 1° parcial.

---

## Progresión temática observada

1. **Fundamentos de red** (TCP/UDP, arquitecturas) →
2. **Photon básico** (conexión, rooms, instanciación en red) →
3. **Sincronización y autoridad** (MasterClient, PhotonView, interpolación) →
4. **Próximo:** RPC, optimización, serialización, anti-cheat, Live Ops, APIs

El docente avanza desde lo conceptual hacia lo práctico, con **Tanques** como proyecto-puente para bajar los conceptos a código antes del primer parcial.

---

## Conceptos críticos identificados (todos los VTT)

- **MasterClient:** lógica que corre una sola vez (escenarios, timers, drops). `PhotonNetwork.IsMasterClient`.
- **Autoridad:** quién decide el estado válido. Patrón recurrente en todas las clases.
- **Qué sincronizar:** criterio de diseño — menos tráfico de red = mejor. Preferir inputs sobre posiciones cuando se puede.
- **Prefabs de red:** deben estar en `Resources/` y se buscan por nombre exacto.
- **`AutomaticallySyncScene = true`** para que los clientes sigan al master.
- **Interpolación/extrapolación** para suavizar movimiento con latencia.

---

## Recomendaciones del docente (transversales)

- Arrancar el proyecto ya — "los tiempos siempre son peores de lo que uno estima".
- No aparecer la última clase pidiendo ayuda de golpe.
- Scope acotado: mejor un juego **completo y publicable** que uno lleno de features sin terminar.
- Traer dudas concretas, no ponerse a laburar durante la consulta.
- Evitar "modificar vida a la fuerza" — usar RPC y autoridad bien definida.

---

## Relación con el proyecto Doratos

Doratos (gladiadores 1v1 top-down con Photon) encaja bien con el alcance pedido:

- **Scope acotado:** 2 jugadores, 1 arena, 5 acciones → "completo y publicable".
- **Cubre temas clave:** Rooms, autoridad (¿quién decide si la lanza impacta?), sincronización de proyectil, MasterClient (¿quién spawnea la lanza inicial?), interpolación para movimiento y dash.
- **Decisiones de red a resolver:**
  - La lanza: ¿objeto de red persistente o se destruye al impactar? ¿quién tiene autoridad del daño — el que tira o el que recibe?
  - Dash: ¿se sincroniza la posición o el input+cooldown?
  - Escudo: ¿bloqueo autoritativo del que defiende o del que dispara?
- **Posibles extensiones para 2° parcial:** matchmaking mejorado, chat, anti-cheat básico, Live Ops (stats de partida), APIs (leaderboard).

---

## Datos faltantes / ambigüedades

- Criterios de aprobación/regularidad no figuran en el cronograma (probablemente en el programa aparte).
- TPs intermedios entre parciales no están detallados en el PDF.
- Fechas exactas de entrega del proyecto (más allá de parciales) no declaradas.
- Audio VTT con muchos errores de ASR — algunos términos técnicos quedaron inferidos por contexto.
