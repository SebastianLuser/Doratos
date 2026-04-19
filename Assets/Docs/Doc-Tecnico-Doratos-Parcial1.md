# Documento Técnico — Doratos (Parcial 1)

> Objetivo: llegar al **30/4** con un prototipo 1v1 online funcional, cerrado, "publicable". Arquitectura **ajustada a la consigna de la materia**: Photon PUN directo, FSM simple, sin capas de abstracción innecesarias.

---

## 1. Alcance del Parcial 1

**Debe tener:**
- Lobby → Room de 2 jugadores (Photon PUN).
- Arena simétrica única con cobertura básica.
- Gladiador con las 5 acciones del High Concept: movimiento top-down, escudo direccional, tiro de lanza, recuperación de lanza, dash.
- Sincronización de posición, rotación, estado del escudo y estado de la lanza.
- **UI con feedback** en cada paso (conectando, buscando sala, esperando oponente, match en curso, victoria/derrota).
- HUD mínimo in-game (cooldown de dash, "tenés la lanza / la lanza está en el piso").
- **Manejo de desconexión**: si el rival se va, se cierra la partida y se declara ganador al que queda (requisito de consigna).
- Pantalla de victoria / derrota por KO **o** por abandono.
- Game loop: match → combate → fin → volver al lobby.

**No entra al Parcial 1:** skins, múltiples arenas, matchmaking avanzado, chat, stats, anti-cheat, APIs externas.

---

## 2. Principios de arquitectura

Se prioriza lo que **la consigna pide** y lo que **se vio en clase**, no elegancia arquitectónica.

1. **Photon PUN directo**: un `NetworkManager : MonoBehaviourPunCallbacks` que hace conexión, lobby, rooms y callbacks. Sin interfaces, sin capas de transporte.
2. **ScriptableObjects** para toda la data balanceable (stats, cooldowns, daños). Cambiar balance = editar SO, no código.
3. **FSM simple** para el jugador: 3 estados (`Idle/Moving`, `Shielding`, `Dashing`). Sin jerarquía, sin HSM.
4. **Comunicación directa por referencias y RPCs**. Nada de EventBus: los scripts que necesitan hablarse se referencian por inspector o `FindObjectOfType` en setup.
5. **MasterClient autoritativo** para lanza y daño (como se vio en clase).
6. **Object pooling** sólo para VFX de impacto (la lanza única no lo necesita; se deja preparado pero no es crítico).
7. **UI reactiva a callbacks de Photon**: cada estado de conexión muestra feedback en pantalla.

Lo que **se descartó** respecto a una versión anterior, para ajustarnos a la consigna:
- Service Locator → no hace falta, son 4 scripts principales.
- EventBus → llamadas directas y RPCs alcanzan.
- `INetworkService` y abstracciones sobre Photon → se usa `PhotonNetwork` y `MonoBehaviourPunCallbacks` directo.
- HSM jerárquica → FSM plana con 3 estados.

---

## 3. Estructura de carpetas

```
Assets/Scripts/
├── Network/
│   ├── NetworkManager.cs          // MonoBehaviourPunCallbacks — conexión, lobby, rooms, desconexiones
│   └── MatchManager.cs            // MasterClient: arranque/fin de match, spawn de lanza, OnPlayerLeftRoom
├── Data/
│   └── SO/ (GladiatorSO, SpearSO, ArenaSO, MatchConfigSO)
├── Gameplay/
│   ├── Player/
│   │   ├── PlayerController.cs    // Input, movimiento, rotación (owner)
│   │   ├── PlayerState.cs         // FSM de 3 estados
│   │   ├── PlayerHealth.cs        // HP; daño aplicado sólo por MC via RPC
│   │   └── PlayerNetworkSync.cs   // IPunObservable: isShielding, dashCooldown
│   ├── Combat/
│   │   ├── Shield.cs              // Cono frontal de bloqueo
│   │   ├── Spear.cs               // Vuelo, colisión, estado (Held/InFlight/Grounded); dueño = MC
│   │   └── SpearPickup.cs         // Trigger de recogida
│   └── Arena/ (ArenaBounds, SpawnPoint)
├── UI/
│   ├── LobbyUI.cs                 // "Conectando...", "Buscando sala...", "Esperando oponente..."
│   ├── HUD.cs                     // Cooldown de dash, estado de la lanza
│   └── EndScreenUI.cs             // "Ganaste" / "Perdiste" / "Oponente desconectado"
└── Utilities/
    ├── SimplePool.cs              // Pool para VFX de impacto
    └── MyLogger.cs
```

Sin carpeta `HSM/`, sin `Services/`, sin `Core/Bootstrap/`. Una escena de Boot, una de Lobby, una de Arena.

---

## 4. NetworkManager — Photon directo

`NetworkManager` es un **MonoBehaviour que hereda de `MonoBehaviourPunCallbacks`** (como se vio en clase). Vive en la escena de Lobby y sobrevive vía `DontDestroyOnLoad`.

**Responsabilidades:**
- Conectar al master server (`PhotonNetwork.ConnectUsingSettings()`).
- Unirse al lobby, crear o unirse a una room de 2 jugadores.
- Exponer callbacks que la UI escucha directamente.
- Activar `PhotonNetwork.AutomaticallySyncScene = true` para que el MasterClient cargue la Arena y el resto lo siga.

**Callbacks que vamos a implementar (override):**

| Callback | Qué hace |
|---|---|
| `OnConnectedToMaster()` | `JoinLobby()` + UI "Conectado, buscando sala" |
| `OnJoinedLobby()` | `JoinRandomRoom()`; si falla crea una |
| `OnJoinRandomFailed(...)` | `CreateRoom(null, {MaxPlayers=2})` |
| `OnJoinedRoom()` | UI "Esperando oponente" si sólo hay 1 player; si ya son 2, MasterClient carga escena Arena |
| `OnPlayerEnteredRoom(Player)` | Si somos MC y ya hay 2, cargar Arena |
| **`OnPlayerLeftRoom(Player)`** | **En partida → finalizar match declarando ganador al local; en lobby → volver a "esperando"** |
| `OnDisconnected(DisconnectCause)` | UI de error + botón "Reintentar" |

**Ejemplo acotado (pseudocódigo real):**

```csharp
public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();
    public override void OnJoinedLobby() => PhotonNetwork.JoinRandomRoom();

    public override void OnJoinRandomFailed(short code, string msg)
        => PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });

    public override void OnJoinedRoom()
    {
        LobbyUI.Instance.ShowWaitingForOpponent();
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
            PhotonNetwork.LoadLevel("Arena");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
            PhotonNetwork.LoadLevel("Arena");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // REQUISITO DE LA CONSIGNA: el que queda gana si la partida estaba en curso
        if (MatchManager.Instance != null && MatchManager.Instance.IsInMatch)
            MatchManager.Instance.EndMatchByDisconnect();
        else
            LobbyUI.Instance.ShowWaitingForOpponent();
    }

    public override void OnDisconnected(DisconnectCause cause)
        => LobbyUI.Instance.ShowDisconnected(cause.ToString());
}
```

---

## 5. ScriptableObjects

| SO | Campos | Rol |
|---|---|---|
| `GladiatorSO` | `maxHP`, `moveSpeed`, `rotationSpeed`, `dashDistance`, `dashCooldown`, `dashDurationMs` | Stats del gladiador |
| `SpearSO` | `throwSpeed`, `damage`, `lifetimeMaxSec`, `pickupRadius` | Stats de la lanza |
| `ArenaSO` | `spawnPoints[]`, `bounds`, `coverPrefabs[]` | Data de la arena |
| `MatchConfigSO` | `roundsToWin`, `spawnInvulnerableSec`, `endScreenDelaySec` | Config del match |

Todos los números del balance viven acá.

---

## 6. FSM del jugador — 3 estados

Son tres estados planos, sin jerarquía:

```
PlayerState:
 ├── Default        (Idle / Moving — mismo estado, se distingue por vector de input)
 ├── Shielding      (mantiene input de escudo; bloquea frontal; muev. reducido)
 └── Dashing        (duración corta con invulnerabilidad a la lanza; bloquea otros inputs)
```

**Implementación:** un `enum PlayerStateId { Default, Shielding, Dashing }` en `PlayerState.cs`, un `switch` en `Update()`, y transiciones gatilladas por input o timers. No hace falta clase `IState`, no hace falta stack.

**Reglas:**
- `Default ↔ Shielding`: mientras se mantiene el input de escudo.
- `Default → Dashing`: al presionar dash si `cooldown ≤ 0`. Vuelve a `Default` al terminar `dashDurationMs`.
- `Shielding` y `Dashing` son mutuamente excluyentes: durante uno, se ignora el input del otro.
- `Dead` se maneja como flag booleana en `PlayerHealth`, no como estado de la FSM (queda simple).

---

## 7. Networking — qué sincroniza quién

| Elemento | Dueño | Sincronización |
|---|---|---|
| Posición/rotación del player | El propio cliente (owner) | `PhotonTransformView` con interpolación |
| Estado del escudo (bool + dirección) | Owner | `IPunObservable` en `PlayerNetworkSync.cs` |
| Cooldown de dash | Owner | Predicho local, replicado por `IPunObservable` |
| **Lanza** | **MasterClient** | `PhotonView` propio; Transform View replicado; estado via RPCs |
| Daño / muerte | **MasterClient** | RPC `ApplyDamage(targetId, amount)` — sólo MC lo procesa |
| Fin de match | MasterClient | RPC `EndMatch(winnerId)` |

**Por qué MC autoritativo para lanza y daño:**
- Así se vio en clase y es lo que pide la consigna.
- Evita el clásico desync "yo te pegué" vs "yo bloqueé".
- Simple de implementar en el tiempo del parcial.

**Instanciación:**
- Player: cada cliente hace `PhotonNetwork.Instantiate("Gladiator", spawn, rot)` al entrar a la Arena.
- Lanza: el MasterClient la instancia al iniciar el match. Prefab en `Resources/Spear.prefab`.

---

## 8. Implementación mecánica por mecánica

### 8.1 Movimiento top-down

- `PlayerController` lee input (WASD + mouse/stick).
- Aplica `Rigidbody.MovePosition` con `moveSpeed` del `GladiatorSO`.
- Rotación hacia el mouse (vista top-down).
- **Red:** `PhotonTransformView` + interpolación. Sólo el owner escribe.

### 8.2 Escudo

- Input "mantener RMB/LT" → `PlayerState` pasa a `Shielding`.
- `Shield.cs` bloquea impactos en cono frontal (`dot(forward, incoming) > 0.5`).
- **Red:** `bool isShielding` + `Vector3 shieldDir` en `PlayerNetworkSync` via `IPunObservable`. El MC lo lee al resolver colisión lanza↔player.
- Moverse con escudo es más lento (multiplier en el FSM).

### 8.3 Tiro de lanza

1. Input "tirar" → owner envía **RPC al MasterClient**: `RequestThrowSpear(origin, direction)`.
2. MC valida: ¿este jugador tiene la lanza?
3. MC pone la lanza en `InFlight`, la suelta del socket y aplica velocidad en su `Rigidbody`.
4. `PhotonTransformView` replica posición a los clientes.
5. El MC detecta colisión:
   - Player sin escudo en dirección → `ApplyDamage` → muerte.
   - Escudo frontal → rebote al piso.
   - Pared → cae.
6. La lanza pasa a `Grounded`.

### 8.4 Recuperación de lanza

- `SpearPickup` = trigger con `pickupRadius`.
- El player que entra, si la lanza está `Grounded`, RPC `RequestPickup(playerId)` al MC.
- MC valida y hace RPC `OnSpearAttached(playerId)` → todos los clientes parentan la lanza al socket del player.

### 8.5 Dash

- Input "dash" → si `cooldown ≤ 0` y estado = `Default`:
  - Owner entra a `Dashing` local (responsive).
  - Envía RPC `OnDashStarted(direction)` para que los otros vean el VFX.
  - Durante `dashDurationMs`: velocidad × N, ignora colisión con lanza.
- No se pide permiso al MC: la autoridad del daño queda en el MC leyendo posiciones ya sincronizadas.

---

## 9. UI con feedback — requisito de la consigna

La UI reacciona a los callbacks de `NetworkManager` y al estado del match. Hay 3 vistas:

### 9.1 LobbyUI

Panel único con texto de estado + botón "Conectar". Estados mostrados:

| Estado | Texto | Cuándo |
|---|---|---|
| `Idle` | "Presioná para conectar" | Al abrir el juego |
| `Connecting` | "Conectando al servidor..." | Tras `Connect()` |
| `InLobby` | "Buscando sala..." | `OnJoinedLobby` |
| `CreatingRoom` | "Creando sala..." | `OnJoinRandomFailed` |
| `WaitingOpponent` | "Esperando oponente (1/2)" | `OnJoinedRoom` con 1 player |
| `StartingMatch` | "Empezando partida..." | 2 players, MC cargando escena |
| `Disconnected` | "Desconectado: {causa}" + botón "Reintentar" | `OnDisconnected` |

### 9.2 HUD (in-game)

- Barra de HP propia (y del rival, chico, en una esquina).
- Ícono de lanza: ✔ tenés / ✘ no tenés / 📍 en el piso.
- Cooldown de dash (radial o número).
- Texto fugaz "¡Bloqueado!" cuando el escudo para un tiro.

### 9.3 EndScreenUI

| Caso | Texto |
|---|---|
| Ganaste por KO | "¡Victoria!" |
| Perdiste por KO | "Derrota" |
| **Ganaste por abandono** | **"Tu oponente se desconectó — Victoria"** |
| Error/desconexión propia | "Perdiste la conexión" |

Timer de `endScreenDelaySec` y botón "Volver al lobby" (que hace `PhotonNetwork.LeaveRoom()`).

---

## 10. Manejo de desconexión — requisito de la consigna

**Caso principal:** un jugador se va durante la partida.

1. Photon dispara `OnPlayerLeftRoom(Player otherPlayer)` en `NetworkManager`.
2. `NetworkManager` consulta `MatchManager.IsInMatch`.
3. Si estaba en match → llama `MatchManager.EndMatchByDisconnect()`:
   - Marca al player local como ganador.
   - Muestra `EndScreenUI` con "Tu oponente se desconectó — Victoria".
   - Cierra la room tras `endScreenDelaySec` (`PhotonNetwork.LeaveRoom()`).
4. Si no estaba en match (lobby/espera) → vuelve a "Esperando oponente".

**Caso secundario:** se desconecta uno mismo.
- `OnDisconnected(cause)` → `LobbyUI.ShowDisconnected(cause)`.
- Si estaba en partida, al reconectar vuelve al lobby, no a la room anterior.

**Caso del MasterClient que se va:**
- Photon promueve automáticamente a otro MC (`OnMasterClientSwitched`).
- Para el parcial 1, como son 2 jugadores, si el MC se va es lo mismo que "oponente desconectado" → el que queda gana.

---

## 11. Optimización (lo mínimo que hace falta)

- **Pool para VFX de impacto** (`SimplePool`). La lanza única no se poolea.
- **`Physics.RaycastNonAlloc`** si se agregan raycasts (no hay planeados).
- **Qué NO sincronizar:** animaciones cosméticas, VFX, audio → se disparan por RPCs puntuales (`OnSpearHit`, `OnDashStarted`), no por streaming.
- **Tick de Photon:** 10/s por default para Transform. Subir a 20 sólo si la lanza se ve mal.
- **Interpolación activa** en `PhotonTransformView` de player y lanza. Sin extrapolación.
- **Regla simple de `Update()`:** cada script hace el suyo. No vale la pena un UpdateService central para 4 componentes.

---

## 12. Plan de implementación — clase por clase hasta 30/4

Hoy es **16/4**. Clases restantes antes del parcial: **23/4 (práctica/repaso)**. Fechas de trabajo fuera de clase: 17, 18, 19, 20, 21, 22, 24, 25, 26, 27, 28, 29.

| Bloque | Fecha objetivo | Entregable |
|---|---|---|
| **Setup + NetworkManager** | 17–18/4 | Proyecto Unity + Photon PUN conectando, `NetworkManager` con todos los callbacks, LobbyUI con feedback |
| **Room + spawn** | 19–20/4 | Crear/unirse a room de 2, spawn de 2 players en Arena, `PhotonTransformView` |
| **Movimiento + escudo** | 21–22/4 | WASD + rotación + escudo cono, FSM de 3 estados, stats desde `GladiatorSO` |
| **Lanza (core)** | 23–24/4 | Tiro, vuelo autoritativo en MC, impacto, muerte, pickup |
| **Dash + HUD** | 25–26/4 | Dash con cooldown, HUD de HP/lanza/dash, VFX básicos |
| **OnPlayerLeftRoom + EndScreen** | 27/4 | Desconexión declara ganador, EndScreenUI con los 3 casos |
| **Match loop + QA** | 28–29/4 | Volver al lobby tras end, testing con 2 PCs, fix latencia, balance |
| **Entrega** | **30/4** | Build jugable |

---

## 13. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Autoridad de la lanza se desincroniza | MC único dueño; los demás sólo visualizan |
| Dash "teletransporta" visto por el otro cliente | Interpolación + suavizado; el dash es corto, aceptable |
| Escudo bloquea y MC cree que no | `isShielding` en `IPunObservable` con envío frecuente |
| Desconexión no cierra la partida | `OnPlayerLeftRoom` testeado explícitamente con 2 PCs (matar proceso) |
| Bugs de carga de escena | `AutomaticallySyncScene = true` + MC carga Arena |
| Scope creep (mapas, skins, etc.) | Congelado por este doc hasta post-parcial |

---

## 14. Checklist de entrega

- [ ] 2 clientes se conectan y juegan una partida completa
- [ ] Lobby muestra feedback de cada estado de conexión
- [ ] HUD con HP, estado de lanza y cooldown de dash
- [ ] Daño aplicado por MC, no por cliente atacante
- [ ] Lanza recuperable por ambos
- [ ] Escudo bloquea frontalmente, no por atrás
- [ ] Pantalla de victoria por KO
- [ ] **Pantalla de victoria por desconexión del rival** (OnPlayerLeftRoom)
- [ ] Botón "Volver al lobby" funcional tras fin de match
- [ ] Build exportada (PC Windows)
- [ ] Video corto de prueba (opcional pero recomendado)

---

## Referencias cruzadas

- Photon PUN: contenido de las 3 clases de la materia (`MonoBehaviourPunCallbacks`, MasterClient autoritativo, `AutomaticallySyncScene`, `PhotonNetwork.Instantiate` con prefabs en `Resources/`).
- Consigna: requiere manejo explícito de `OnPlayerLeftRoom()` para cerrar la partida o declarar ganador.
- High Concept: `High Concept — Doratos.md` en esta carpeta.
