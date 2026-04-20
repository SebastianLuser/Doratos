using System.Collections.Generic;
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

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    public List<RoomInfo> GetCachedRoomList() => new List<RoomInfo>(cachedRoomList);

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

    public void ConnectOffline()
    {
        if (State != ConnectionState.Idle && State != ConnectionState.Disconnected) return;
        SetState(ConnectionState.Connecting);
        PhotonNetwork.OfflineMode = true;
    }

    public void CreateRoom(string roomName)
    {
        if (State != ConnectionState.InLobby) return;
        SetState(ConnectionState.CreatingRoom);
        var options = new RoomOptions { MaxPlayers = 4, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void StartMatch()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount < 2) return;
        SetState(ConnectionState.StartingMatch);
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        if (PhotonNetwork.IsMasterClient)
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
        if (PhotonNetwork.OfflineMode)
        {
            if (State == ConnectionState.Connecting)
            {
                PhotonNetwork.CreateRoom("OfflineRoom");
                return;
            }
            PhotonNetwork.OfflineMode = false;
            SetState(ConnectionState.Idle);
            return;
        }
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

            if (room.RemovedFromList || !room.IsVisible || !room.IsOpen)
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
        OnError?.Invoke("Error al unirse: " + message);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom.PlayerCount >= 4)
        {
            SetState(ConnectionState.StartingMatch);
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel("Arena");
        }
        else
        {
            SetState(ConnectionState.WaitingOpponent);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        OnRoomPlayersChanged?.Invoke();

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 4)
        {
            SetState(ConnectionState.StartingMatch);
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel("Arena");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        OnRoomPlayersChanged?.Invoke();

        if (MatchManager.Instance != null && MatchManager.Instance.IsInMatch)
        {
            MatchManager.Instance.EndMatchByDisconnect();
        }
        else
        {
            SetState(ConnectionState.WaitingOpponent);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        cachedRoomList.Clear();
        SetState(ConnectionState.Disconnected);
        OnError?.Invoke("Desconectado: " + cause);
    }
}
