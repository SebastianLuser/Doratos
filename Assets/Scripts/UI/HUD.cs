using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance;

    [Header("Network Status")]
    [SerializeField] private GameObject networkStatusPanel;
    [SerializeField] private TextMeshProUGUI networkStatusText;
    private float networkStatusTimer;

    [Header("Health Bars")]
    [SerializeField] private Image[] healthFills = new Image[4];  // imagen Fill de cada barra
    [SerializeField] private TextMeshProUGUI[] playerLabels = new TextMeshProUGUI[4];
    [SerializeField] private PlayerColorsSO playerColors;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Series Standings (TAB)")]
    [SerializeField] private GameObject seriesPanel;
    [SerializeField] private TextMeshProUGUI seriesText;

    private PlayerHealth localHealth;
    private PlayerState localState;
    private List<PlayerHealth> allHealths = new List<PlayerHealth>();
    private int localSlot = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnError        += HandleNetworkError;
            NetworkManager.Instance.OnStateChanged += HandleNetworkState;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnError        -= HandleNetworkError;
            NetworkManager.Instance.OnStateChanged -= HandleNetworkState;
        }
    }

    private void HandleNetworkError(string message)
    {
        ShowNetworkStatus(message, 4f);
    }

    private void HandleNetworkState(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Disconnected:
                ShowNetworkStatus("No connection with the server", 6f);
                if (EndScreenUI.Instance != null)
                    EndScreenUI.Instance.ShowConnectionLost();
                break;
            case ConnectionState.Connecting:
                ShowNetworkStatus("Connecting...", 3f);
                break;
            case ConnectionState.WaitingOpponent:
                ShowNetworkStatus("Awaiting opponent...", 3f);
                break;
        }
    }

    public void ShowNetworkStatus(string message, float duration = 4f)
    {
        if (networkStatusText == null) return;
        networkStatusText.text = message;
        networkStatusTimer = duration;
        if (networkStatusPanel != null) networkStatusPanel.SetActive(true);
    }

    public void UpdateTimer(float remaining, bool fireActive)
    {
        if (timerText == null) return;

        if (fireActive)
        {
            timerText.text = "FIRE";
            timerText.color = Color.red;
        }
        else
        {
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{minutes}:{seconds:00}";
            timerText.color = remaining <= 10f ? Color.yellow : Color.white;
        }
    }

    private void Update()
    {
        UpdateHealthBars();
        UpdateNetworkStatus();

        if (Input.GetKeyDown(KeyCode.Tab) && seriesPanel != null)
        {
            bool show = !seriesPanel.activeSelf;
            seriesPanel.SetActive(show);
            if (show) RefreshSeriesStandings();
        }
    }

    private void RefreshSeriesStandings()
    {
        if (seriesText == null || !PhotonNetwork.InRoom) return;

        int killLimit = NetworkManager.Instance != null ? NetworkManager.Instance.KillLimit : 0;
        string header = killLimit > 0 ? $"Series — first to {killLimit} kills\n" : "Series\n";
        string lines = "";
        foreach (var p in PhotonNetwork.PlayerList)
        {
            int sk = NetworkManager.Instance != null ? NetworkManager.Instance.GetSeriesKills(p) : 0;
            string you = p.IsLocal ? " (you)" : "";
            lines += $"\n{p.NickName}{you}: {sk}" + (killLimit > 0 ? $"/{killLimit}" : "");
        }
        seriesText.text = header + lines;
    }

    public void RegisterLocalPlayer(PlayerHealth health, PlayerState state)
    {
        localHealth = health;
        localState = state;
        localSlot = AssignSlot(health);
    }

    public void RegisterEnemy(PlayerHealth health)
    {
        AssignSlot(health);
    }

    private int AssignSlot(PlayerHealth health)
    {
        if (allHealths.Contains(health)) return allHealths.IndexOf(health);
        if (allHealths.Count >= 4) return -1;

        allHealths.Add(health);
        int slot = allHealths.Count - 1;

        if (slot < playerLabels.Length && playerLabels[slot] != null)
        {
            var owner = PhotonNetwork.CurrentRoom?.GetPlayer(health.photonView.OwnerActorNr);
            playerLabels[slot].text = owner?.NickName ?? "Player " + (slot + 1);
        }

        if (slot < healthFills.Length && healthFills[slot] != null)
            healthFills[slot].color = playerColors.SlotColors[slot];

        return slot;
    }

    private void UpdateHealthBars()
    {
        for (int i = 0; i < healthFills.Length; i++)
        {
            if (healthFills[i] == null) continue;

            var h = i < allHealths.Count ? allHealths[i] : null;

            if (h != null)
            {
                healthFills[i].fillAmount = Mathf.Clamp01(h.HealthNormalized);
                healthFills[i].color = h.IsDead ? Color.gray : playerColors.SlotColors[i];
            }
            else
            {
                healthFills[i].fillAmount = 0f;
            }
        }
    }

    private void UpdateNetworkStatus()
    {
        if (networkStatusTimer <= 0f) return;
        networkStatusTimer -= Time.deltaTime;
        if (networkStatusTimer <= 0f)
        {
            if (networkStatusPanel != null) networkStatusPanel.SetActive(false);
        }
    }


}
