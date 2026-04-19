using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance;

    [Header("Spear")]
    [SerializeField] private TextMeshProUGUI spearStateText;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    private float feedbackTimer;

    [Header("Health Bars")]
    [SerializeField] private Image[] healthBars = new Image[4];
    [SerializeField] private TextMeshProUGUI[] playerLabels = new TextMeshProUGUI[4];

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;

    private PlayerHealth localHealth;
    private PlayerState localState;
    private List<PlayerHealth> allHealths = new List<PlayerHealth>();
    private List<DummyHealth> dummyHealths = new List<DummyHealth>();
    private Spear spearRef;
    private int localSlot = -1;

    public static readonly Color[] SlotColors = new Color[]
    {
        new Color(0.2f, 0.6f, 1f),
        new Color(1f, 0.3f, 0.3f),
        new Color(0.3f, 1f, 0.3f),
        new Color(1f, 0.85f, 0.2f)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void UpdateTimer(float remaining, bool fireActive)
    {
        if (timerText == null) return;

        if (fireActive)
        {
            timerText.text = "FUEGO";
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
        UpdateSpearState();
        UpdateFeedback();
    }

    public void RegisterLocalPlayer(PlayerHealth health, PlayerState state)
    {
        localHealth = health;
        localState = state;
        localSlot = AssignSlot(health);
        ApplyPlayerColor(health.transform, localSlot);
    }

    public void RegisterEnemy(PlayerHealth health)
    {
        int slot = AssignSlot(health);
        ApplyPlayerColor(health.transform, slot);
    }

    public void RegisterDummy(DummyHealth dummy)
    {
        if (!dummyHealths.Contains(dummy))
            dummyHealths.Add(dummy);

        int slot = allHealths.Count + dummyHealths.Count - 1;
        if (slot < SlotColors.Length)
            ApplyPlayerColor(dummy.transform, slot);
    }

    public void RegisterSpear(Spear spear)
    {
        spearRef = spear;
    }

    private int AssignSlot(PlayerHealth health)
    {
        if (allHealths.Contains(health)) return allHealths.IndexOf(health);
        if (allHealths.Count >= 4) return -1;
        allHealths.Add(health);
        int slot = allHealths.Count - 1;
        if (slot < playerLabels.Length && playerLabels[slot] != null)
            playerLabels[slot].text = "Player " + (slot + 1);
        return slot;
    }

    private void UpdateHealthBars()
    {
        int barIndex = 0;

        for (int i = 0; i < allHealths.Count && barIndex < healthBars.Length; i++)
        {
            var h = allHealths[i];
            if (healthBars[barIndex] == null) { barIndex++; continue; }

            if (h != null)
            {
                healthBars[barIndex].fillAmount = h.HealthNormalized;
                healthBars[barIndex].color = h.IsDead ? Color.gray : SlotColors[barIndex];
            }
            barIndex++;
        }

        foreach (var dh in dummyHealths)
        {
            if (barIndex >= healthBars.Length) break;
            if (healthBars[barIndex] != null && dh != null)
            {
                healthBars[barIndex].fillAmount = dh.HealthNormalized;
                healthBars[barIndex].color = dh.IsDead ? Color.gray : SlotColors[barIndex];

                if (playerLabels[barIndex] != null)
                    playerLabels[barIndex].text = "Player " + (barIndex + 1);
            }
            barIndex++;
        }

        for (int i = barIndex; i < healthBars.Length; i++)
        {
            if (healthBars[i] != null)
                healthBars[i].fillAmount = 0f;
        }
    }

    private void UpdateSpearState()
    {
        if (spearStateText == null) return;

        if (spearRef == null)
        {
            spearStateText.text = "SIN LANZA";
            return;
        }

        switch (spearRef.State)
        {
            case SpearState.Held:
                bool isMine = localHealth != null && spearRef.HolderActorNr == localHealth.photonView.OwnerActorNr;
                spearStateText.text = isMine ? "ARMADO" : "ENEMIGO ARMADO";
                spearStateText.color = isMine ? Color.green : Color.red;
                break;
            case SpearState.InFlight:
                spearStateText.text = "EN VUELO";
                spearStateText.color = Color.yellow;
                break;
            case SpearState.Grounded:
                spearStateText.text = "EN EL PISO";
                spearStateText.color = Color.white;
                break;
        }
    }

    private void UpdateFeedback()
    {
        if (feedbackText == null) return;

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f)
                feedbackText.text = "";
        }
    }

    public void ShowFeedback(string text, float duration = 1.5f)
    {
        if (feedbackText == null) return;
        feedbackText.text = text;
        feedbackTimer = duration;
    }

    private void ApplyPlayerColor(Transform playerTransform, int slot)
    {
        if (slot < 0 || slot >= SlotColors.Length) return;

        Color color = SlotColors[slot];
        var body = playerTransform.Find("Body");
        if (body != null)
        {
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(renderer.material);
                mat.color = color;
                renderer.material = mat;
            }
        }
    }
}
