using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenUI : MonoBehaviour
{
    public static EndScreenUI Instance;

    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button lobbyButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HidePanel();
    }

    private void Start()
    {
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnLobbyClicked);
    }

    private void OnDestroy()
    {
        if (lobbyButton != null)
            lobbyButton.onClick.RemoveListener(OnLobbyClicked);
    }

    // Mid-series round win — no lobby button, shows progress
    public void ShowRoundVictory(int wins, int killLimit)
    {
        string sub = killLimit > 0 ? $"{wins}/{killLimit} victories" : $"{wins} victories";
        ShowResult("¡Round won!", sub, showButton: false);
    }

    // Mid-series round loss — no lobby button
    public void ShowRoundDefeat(int wins, int killLimit)
    {
        string sub = killLimit > 0 ? $"Leader: {wins}/{killLimit}" : "";
        ShowResult("Round lost", sub, showButton: false);
    }

    // Series over
    public void ShowSeriesVictory()
    {
        ShowResult("¡You won the series!", "", showButton: true);
    }

    public void ShowSeriesDefeat()
    {
        ShowResult("You lost the series", "", showButton: true);
    }

    public void ShowDisconnectWin()
    {
        ShowResult("Opponent disconnected\n¡Victory!", "", showButton: false);
    }

    public void ShowConnectionLost()
    {
        ShowResult("You lost connection", "", showButton: true);
    }

    private void ShowResult(string title, string subtitle, bool showButton)
    {
        if (resultText != null) resultText.text = title;
        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }
        if (lobbyButton != null) lobbyButton.gameObject.SetActive(showButton);
        ShowPanel();
    }

    private void ShowPanel()
    {
        if (panel == null) return;
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        if (panel == null) return;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }

    private void OnLobbyClicked()
    {
        if (MatchManager.Instance != null)
            MatchManager.Instance.ReturnToLobby();
    }
}
