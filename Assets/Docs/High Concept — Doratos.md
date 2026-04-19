# High Concept — Doratos

# HIGH CONCEPT DOCUMENT

## Project Name: Doratos
* * *

## Premise

**Game Premise**
Doratos es un juego multijugador online 1v1 de gladiadores con vista top-down donde dos jugadores se enfrentan en una arena con una sola lanza y un escudo. Cada tiro es un compromiso: si fallás, tenés que ir a buscarla mientras el otro te caza. La tensión está en decidir cuándo atacar, cuándo cubrirte y cuándo correr.

**Game Genres**
Arena PvP online, combate táctico top-down, multijugador 1v1.
* * *

## Introduction

**Basic Argument Framework**
Dos gladiadores, una arena, una lanza. No hay historia: hay instinto, timing y la certeza de que la próxima decisión define el combate. Cada partida es corta, intensa y decisiva.

**Basic Gameplay Description**

*   **Tirar la lanza:** Único ataque ofensivo a distancia. Alto daño, riesgo alto: si la tirás y fallás, quedás desarmado.
*   **Recuperar la lanza:** Una vez tirada hay que ir físicamente a buscarla. Momento de máxima vulnerabilidad.
*   **Defenderse con el escudo:** Bloquea proyectiles desde el frente. Posicionamiento y timing son clave.
*   **Dash:** Movimiento rápido con cooldown para esquivar, reposicionar o cazar al oponente desarmado.
*   **Multijugador online:** Partidas 1v1 por red usando Photon.

**What Makes the Game Unique**

*   Una sola lanza compartida: el recurso ofensivo es físico, no infinito. Tirarla es apostar.
*   Loop de riesgo/recompensa puro: atacar te desarma, defender te da el control.
*   Vista top-down con lectura total del campo: no hay sorpresas, hay decisiones.
*   Partidas cortas, skill-based, sin RNG ni progresión entre partidas.
* * *

## Mechanics

**Main Mechanics**

*   **Movimiento top-down:** WASD / stick. Vista cenital, lectura 360° del escenario.
*   **Escudo:** Bloqueo direccional. Cubre al jugador frente a proyectiles mientras esté levantado. Limita movilidad/visión al usarlo.
*   **Lanza (tiro):** Proyectil recto con velocidad alta. Mata o hiere con un impacto. Una sola lanza por jugador.
*   **Lanza (recuperación):** Si falla o es bloqueada, queda en el piso. El dueño tiene que ir a recogerla. Puede ser robada por el rival.
*   **Dash:** Desplazamiento corto y veloz con cooldown. Usos: esquive, cierre de distancia, huida post-tiro.
*   **Red (Photon):** Sincronización de posición, inputs y estado de la lanza entre los dos clientes.

**Core Gameplay Loop**

1. Matchmaking / conexión via Photon
2. Spawn de ambos jugadores en arena con lanza y escudo
3. Lectura de distancia y posición del rival
4. Decisión: atacar, defender, dashear o posicionarse
5. Tiro de lanza → impacto / bloqueo / falla
6. Si falla: correr a recuperarla antes que el rival
7. Close-combat o reposicionamiento
8. Victoria / derrota por KO
9. Nueva ronda
* * *

## Art Sheet

**Art Style**
Top-down con estilo low-poly / pixel-poly, siluetas claras y legibles. Prioridad: leer al instante dónde está el jugador, dónde está la lanza y cuál es la dirección del escudo.

**Color Palette**
Tonos tierra de arena romana: ocres, arenas, piedra. Contraste fuerte entre los dos gladiadores (ej: rojo vs azul) para identificación inmediata. Sangre y partículas como feedback visual principal.
* * *

## Expected Audience

*   Jugadores de PvP competitivo y arena fighters (Brawlhalla, Nidhogg, Duck Game).
*   Fans de combate skill-based con mecánicas simples pero profundas (Nidhogg 2, Towerfall).
*   Público casual-competitivo que busca partidas rápidas 1v1 online.
*   Rango etario: 13+.
* * *

## MVP Scope

*   **Modo de juego:** 1v1 online via Photon, best-of uno o mejor de tres.
*   **Arenas:** 1 — Una arena simétrica con cobertura básica.
*   **Personajes:** 1 gladiador jugable (mismo para ambos jugadores, diferenciados por color).
*   **Mecánicas activas:** Movimiento, escudo, tiro de lanza, recuperación de lanza, dash.
*   **Matchmaking:** Quick match básico por sala Photon.
*   **UI:** Lobby, HUD de combate (cooldown de dash, estado de lanza), pantalla de victoria.
* * *

## Development Sheet

**Why This Game?**

*   **Nicho claro:** PvP 1v1 top-down con recurso único compartido (la lanza) no está explotado.
*   **Scope acotado:** Dos jugadores, una arena, cinco acciones. MVP alcanzable.
*   **Escalable:** Fácil agregar arenas, skins, modos (2v2, capture the spear, torneo), power-ups.
*   **Competitivo por diseño:** Mecánicas simples + decisiones profundas = potencial e-sport / streaming.

**Possible Challenges**

*   Netcode en Photon: latencia y sincronización del estado de la lanza (quién la tiene, dónde está, quién la puede levantar).
*   Balance entre ofensiva y defensa: que el escudo no haga el juego estático.
*   Feedback claro en vista top-down de direccionalidad del escudo y del tiro.
*   Matchmaking y flujo de sala con pocos jugadores conectados.

**Development Engine**
Unity

**Development Platforms**

*   PC (Windows) — build principal
*   Posible expansión a Mac / Linux / WebGL

**Hardware Requirements**

*   PC con conexión a internet estable
*   Teclado + mouse o gamepad
* * *

## Monetization

Desarrollo inicial como proyecto competitivo / portfolio. Lanzamiento en Steam como venta única a precio bajo, con posible modelo free-to-play + cosméticos (skins de gladiador, escudos, efectos de lanza) si la base de jugadores lo justifica. Sin pay-to-win: todo lo monetizable es visual.
