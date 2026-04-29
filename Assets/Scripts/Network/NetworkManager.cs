using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public enum ConnectionState
{
    Idle,
    Connecting,
    InLobby,
    CreatingRoom,
    WaitingOpponent,
    StartingMatch,
    Disconnected
}

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    public ConnectionState State { get; private set; } = ConnectionState.Idle;

    public event System.Action<ConnectionState> OnStateChanged;
    public event System.Action<List<RoomInfo>> OnRoomListChanged;
    public event System.Action<string> OnError;
    public event System.Action OnRoomPlayersChanged;
    public event System.Action OnSeriesUpdated;

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    public List<RoomInfo> GetCachedRoomList() => new List<RoomInfo>(cachedRoomList);

    // --- Series properties ---

    public int KillLimit
    {
        get
        {
            if (!PhotonNetwork.InRoom) return 0;
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            return props.ContainsKey("KL") ? (int)props["KL"] : 0;
        }
    }

    public int GetSeriesKills(Player player)
    {
        if (player == null) return 0;
        return player.CustomProperties.ContainsKey("SK") ? (int)player.CustomProperties["SK"] : 0;
    }

    public void AddSeriesKill(int winnerActorNr, out int newTotal)
    {
        var winner = PhotonNetwork.CurrentRoom?.GetPlayer(winnerActorNr);
        if (winner == null) { newTotal = 0; return; }
        int current = GetSeriesKills(winner);
        newTotal = current + 1;
        winner.SetCustomProperties(new Hashtable { { "SK", newTotal } });
    }

    // Called after a round ends when the series isn't over — stays in the room
    public void ReturnToRoom()
    {
        SetState(ConnectionState.WaitingOpponent);
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
            PhotonNetwork.LoadLevel("Lobby");
        }
    }

    // --------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void SetNickname(string nickname)
    {
        PhotonNetwork.NickName = nickname;
    }

    public void Connect()
    {
        if (State != ConnectionState.Idle && State != ConnectionState.Disconnected) return;
        SetState(ConnectionState.Connecting);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom(string roomName, int killLimit = 0)
    {
        if (State != ConnectionState.InLobby) return;
        SetState(ConnectionState.CreatingRoom);
        var customProps = new Hashtable { { "KL", killLimit } };
        var options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = customProps,
            CustomRoomPropertiesForLobby = new string[] { "KL" }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void StartMatch()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount < 2) return;

        // Increment series ID so all clients reset their series kills
        int sid = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SID")
            ? (int)PhotonNetwork.CurrentRoom.CustomProperties["SID"] : 0;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "SID", sid + 1 } });

        SetState(ConnectionState.StartingMatch);
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("Arena");
    }

    public void JoinRoom(string roomName)
    {
        if (State != ConnectionState.InLobby) return;
        SetState(ConnectionState.Connecting);
        PhotonNetwork.JoinRoom(roomName);
    }

    private void SetState(ConnectionState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }

    public override void OnConnectedToMaster()
    {
        SetState(ConnectionState.InLobby);
        PhotonNetwork.JoinLobby();
    }

    public override void OnLeftRoom()
    {
        SetState(ConnectionState.Idle);
    }

    public override void OnJoinedLobby()
    {
        SetState(ConnectionState.InLobby);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 4)
        {
            SetState(ConnectionState.StartingMatch);
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.LoadLevel("Arena");
            }
        }
        else
        {
            SetState(ConnectionState.WaitingOpponent);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        OnRoomListChanged?.Invoke(cachedRoomList);
    }

    private void UpdateCachedRoomList(List<RoomInfo> changes)
    {
        foreach (var room in changes)
        {
            int index = cachedRoomList.FindIndex(r => r.Name == room.Name);
            if (index >= 0)
                cachedRoomList.RemoveAt(index);

            if (room.RemovedFromList || !room.IsVisible)
                continue;

            cachedRoomList.Add(room);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetState(ConnectionState.InLobby);
        OnError?.Invoke("Error al crear sala: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetState(ConnectionState.InLobby);
        string msg = returnCode == ErrorCode.GameFull    ? "La sala está llena" :
                     returnCode == ErrorCode.GameClosed  ? "Hay una partida en progreso" :
                     "Error al unirse: " + message;
        OnError?.Invoke(msg);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        OnRoomPlayersChanged?.Invoke();

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 4)
        {
            SetState(ConnectionState.StartingMatch);
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.LoadLevel("Arena");
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        OnRoomPlayersChanged?.Invoke();

        if (MatchManager.Instance != null && MatchManager.Instance.IsInMatch)
        {
            if (PhotonNetwork.IsMasterClient)
                MatchManager.Instance.NotifyPlayerLeft(otherPlayer.ActorNumber);
        }
        else
        {
            SetState(ConnectionState.WaitingOpponent);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Notify UI that master changed (e.g. start button visibility may change)
        OnRoomPlayersChanged?.Invoke();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("SID"))
        {
            // New series started — reset this player's kills
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "SK", 0 } });
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("SK"))
            OnSeriesUpdated?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        cachedRoomList.Clear();
        SetState(ConnectionState.Disconnected);

        string msg = cause switch
        {
            DisconnectCause.None                        => "Desconectado del servidor",
            DisconnectCause.ServerTimeout               => "Sin respuesta del servidor",
            DisconnectCause.ClientTimeout               => "Se perdió la conexión",
            DisconnectCause.DisconnectByServerLogic     => "Desconectado por el servidor",
            DisconnectCause.DisconnectByClientLogic     => "Desconectado por el cliente",
            DisconnectCause.MaxCcuReached               => "Servidor lleno, intentá más tarde",
            DisconnectCause.InvalidAuthentication       => "Error de autenticación",
            DisconnectCause.ExceptionOnConnect          => "No se pudo conectar al servidor",
            DisconnectCause.Exception                   => "Error de conexión inesperado",
            _                                           => $"Sin conexión ({cause})"
        };

        OnError?.Invoke(msg);
    }
}
