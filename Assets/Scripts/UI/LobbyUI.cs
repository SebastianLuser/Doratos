using System.Collections;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("Nickname")]
    [SerializeField] private TMP_InputField nicknameInput;

    [Header("Connection")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button testSoloButton;

    [Header("Room Creation")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createRoomButton;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomEntryPrefab;

    [Header("In Room")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomInfoText;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private Button startButton;

    [Header("Stats")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TextMeshProUGUI statsText;

    private int totalKills;
    private int totalDeaths;
    private Coroutine refreshCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        totalKills = PlayerPrefs.GetInt("Stats_Kills", 0);
        totalDeaths = PlayerPrefs.GetInt("Stats_Deaths", 0);
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnStateChanged -= HandleStateChanged;
            NetworkManager.Instance.OnRoomListChanged -= HandleRoomListChanged;
            NetworkManager.Instance.OnError -= HandleError;
            NetworkManager.Instance.OnRoomPlayersChanged -= HandleRoomPlayersChanged;
        }
    }

    private void Start()
    {
        connectButton.onClick.AddListener(OnConnectClicked);
        if (retryButton != null)
            retryButton.onClick.AddListener(OnConnectClicked);
        if (testSoloButton != null)
            testSoloButton.onClick.AddListener(OnTestSoloClicked);
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (leaveRoomButton != null)
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (nicknameInput != null)
        {
            string saved = PlayerPrefs.GetString("PlayerNickname", "");
            nicknameInput.text = saved;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnStateChanged += HandleStateChanged;
            NetworkManager.Instance.OnRoomListChanged += HandleRoomListChanged;
            NetworkManager.Instance.OnError += HandleError;
            NetworkManager.Instance.OnRoomPlayersChanged += HandleRoomPlayersChanged;

            if (NetworkManager.Instance.State != ConnectionState.Idle)
                HandleStateChanged(NetworkManager.Instance.State);
            else
                ShowIdle();
        }
        else
        {
            ShowIdle();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && statsPanel != null)
            statsPanel.SetActive(!statsPanel.activeSelf);
    }

    private void OnConnectClicked()
    {
        ApplyNickname();
        NetworkManager.Instance.Connect();
    }

    private void OnTestSoloClicked()
    {
        ApplyNickname();
        NetworkManager.Instance.ConnectOffline();
    }

    private void OnCreateRoomClicked()
    {
        if (roomNameInput == null) return;
        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = PhotonNetwork.NickName + "_Arena";
        }
        NetworkManager.Instance.CreateRoom(roomName);
    }

    private void OnLeaveRoomClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    private void OnStartClicked()
    {
        NetworkManager.Instance.StartMatch();
    }

    private void ApplyNickname()
    {
        if (nicknameInput == null) return;
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick))
            nick = "Gladiador" + Random.Range(100, 999);
        NetworkManager.Instance.SetNickname(nick);
        PlayerPrefs.SetString("PlayerNickname", nick);
    }

    private void HandleStateChanged(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Idle:
                ShowIdle();
                break;
            case ConnectionState.Connecting:
                SetStatus("Conectando al servidor...");
                ShowConnectionUI(false);
                break;
            case ConnectionState.InLobby:
                SetStatus("En el lobby — Creá o elegí una sala");
                ShowLobbyUI(true);
                ShowRoomUI(false);
                HandleRoomListChanged(NetworkManager.Instance.GetCachedRoomList());
                if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
                refreshCoroutine = StartCoroutine(AutoRefreshRoomList());
                break;
            case ConnectionState.CreatingRoom:
                SetStatus("Creando sala...");
                ShowLobbyUI(false);
                break;
            case ConnectionState.WaitingOpponent:
                ShowRoomUI(true);
                ShowLobbyUI(false);
                UpdateRoomInfo();
                UpdateStartButton();
                break;
            case ConnectionState.StartingMatch:
                SetStatus("¡Arena completa! Cargando...");
                ShowConnectionUI(false);
                ShowLobbyUI(false);
                ShowRoomUI(false);
                break;
            case ConnectionState.Disconnected:
                SetStatus("Desconectado del servidor");
                ShowConnectionUI(false);
                ShowLobbyUI(false);
                ShowRoomUI(false);
                if (retryButton != null) retryButton.gameObject.SetActive(true);
                if (refreshCoroutine != null) { StopCoroutine(refreshCoroutine); refreshCoroutine = null; }
                break;
        }
    }

    private void HandleRoomListChanged(List<RoomInfo> rooms)
    {
        if (roomListContent == null || roomEntryPrefab == null) return;

        for (int i = roomListContent.childCount - 1; i >= 0; i--)
        {
            var child = roomListContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        foreach (var room in rooms)
        {
            var entry = Instantiate(roomEntryPrefab, roomListContent);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{room.Name}  ({room.PlayerCount}/{room.MaxPlayers})";

            var button = entry.GetComponent<Button>();
            if (button != null)
            {
                string roomName = room.Name;
                button.onClick.AddListener(() => NetworkManager.Instance.JoinRoom(roomName));
            }
        }
    }

    private void HandleRoomPlayersChanged()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.State == ConnectionState.WaitingOpponent)
        {
            UpdateRoomInfo();
            UpdateStartButton();
        }
    }

    private void UpdateStartButton()
    {
        if (startButton == null) return;
        bool canStart = PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount >= 2;
        startButton.gameObject.SetActive(canStart);
    }

    private void HandleError(string message)
    {
        SetStatus(message);
    }

    private void UpdateRoomInfo()
    {
        if (roomInfoText == null || !PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;
        string players = "";
        foreach (var p in PhotonNetwork.PlayerList)
            players += $"\n  - {p.NickName}";

        roomInfoText.text = $"Sala: {room.Name}\nGladiadores ({room.PlayerCount}/{room.MaxPlayers}):{players}";
    }

    private void ShowIdle()
    {
        SetStatus("Ingresá tu nombre y conectate");
        ShowConnectionUI(true);
        ShowLobbyUI(false);
        ShowRoomUI(false);
    }

    private void ShowConnectionUI(bool show)
    {
        if (connectButton != null) connectButton.gameObject.SetActive(show);
        if (testSoloButton != null) testSoloButton.gameObject.SetActive(show);
        if (nicknameInput != null) nicknameInput.gameObject.SetActive(show);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
    }

    private void ShowLobbyUI(bool show)
    {
        if (roomNameInput != null) roomNameInput.gameObject.SetActive(show);
        if (createRoomButton != null) createRoomButton.gameObject.SetActive(show);
        if (roomListContent != null)
        {
            var scroll = roomListContent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (scroll != null)
                scroll.gameObject.SetActive(show);
            else
                roomListContent.gameObject.SetActive(show);
        }
    }

    private void ShowRoomUI(bool show)
    {
        if (roomPanel != null) roomPanel.SetActive(show);
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    public void AddKill()
    {
        totalKills++;
        PlayerPrefs.SetInt("Stats_Kills", totalKills);
        UpdateStatsText();
    }

    public void AddDeath()
    {
        totalDeaths++;
        PlayerPrefs.SetInt("Stats_Deaths", totalDeaths);
        UpdateStatsText();
    }

    private void UpdateStatsText()
    {
        if (statsText != null)
            statsText.text = $"Kills: {totalKills}\nDeaths: {totalDeaths}";
    }

    private IEnumerator AutoRefreshRoomList()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            if (NetworkManager.Instance != null && NetworkManager.Instance.State == ConnectionState.InLobby)
                HandleRoomListChanged(NetworkManager.Instance.GetCachedRoomList());
        }
    }
}
