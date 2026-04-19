using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenUI : MonoBehaviour
{
    public static EndScreenUI Instance;

    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI resultText;
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

    public void ShowVictory()
    {
        ShowResult("¡Victoria!");
    }

    public void ShowDefeat()
    {
        ShowResult("Derrota");
    }

    public void ShowDisconnectWin()
    {
        ShowResult("Tu oponente se desconectó\n¡Victoria!");
    }

    public void ShowConnectionLost()
    {
        ShowResult("Perdiste la conexión");
    }

    private void ShowResult(string text)
    {
        if (resultText != null) resultText.text = text;
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
