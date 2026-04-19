#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class TempLobbySetup
{
    [MenuItem("Tools/Setup Lobby UI")]
    public static void SetupLobbyUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas found"); return; }
        var canvasRT = canvas.GetComponent<RectTransform>();

        // Reposition existing elements
        var statusText = GameObject.Find("StatusText");
        if (statusText != null)
        {
            var rt = statusText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = new Vector2(700f, 50f);
            var tmp = statusText.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.alignment = TextAlignmentOptions.Center; tmp.fontSize = 28; }
        }

        // --- NICKNAME INPUT ---
        var nicknameInput = CreateInputField(canvasRT, "NicknameInput", new Vector2(0f, -100f), new Vector2(350f, 45f), "Tu nombre de gladiador...");

        // Reposition ConnectButton
        var connectButton = GameObject.Find("ConnectButton");
        if (connectButton != null)
        {
            var rt = connectButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(-100f, -160f);
            rt.sizeDelta = new Vector2(200f, 50f);
            var img = connectButton.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = new Color(0.2f, 0.6f, 0.2f, 1f); }
            var txt = connectButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) { txt.text = "CONECTAR"; txt.fontSize = 22; txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white; }
        }

        // Reposition TestSoloButton
        var testSoloButton = GameObject.Find("TestSoloButton");
        if (testSoloButton != null)
        {
            var rt = testSoloButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(100f, -160f);
            rt.sizeDelta = new Vector2(200f, 50f);
        }

        // Reposition RetryButton
        var retryButton = GameObject.Find("RetryButton");
        if (retryButton != null)
        {
            var rt = retryButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -160f);
            rt.sizeDelta = new Vector2(200f, 50f);
        }

        // --- LOBBY SECTION (room creation + room list) ---
        // Room Name Input
        var roomNameInput = CreateInputField(canvasRT, "RoomNameInput", new Vector2(-100f, -240f), new Vector2(300f, 45f), "Nombre de la sala...");
        roomNameInput.gameObject.SetActive(false);

        // Create Room Button
        var createRoomBtn = CreateButton(canvasRT, "CreateRoomButton", new Vector2(110f, -240f), new Vector2(160f, 45f), "CREAR SALA", new Color(0.7f, 0.5f, 0.1f, 1f));
        createRoomBtn.SetActive(false);

        // Room List Header
        var roomListHeader = CreateText(canvasRT, "RoomListHeader", new Vector2(0f, -300f), new Vector2(500f, 35f), "Salas disponibles:", 22, TextAlignmentOptions.Left);
        roomListHeader.SetActive(false);

        // Room List Scroll Area
        var scrollGO = new GameObject("RoomListScroll");
        scrollGO.transform.SetParent(canvasRT, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.5f, 1f);
        scrollRT.anchorMax = new Vector2(0.5f, 1f);
        scrollRT.anchoredPosition = new Vector2(0f, -450f);
        scrollRT.sizeDelta = new Vector2(500f, 250f);

        var scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Content
        var contentGO = new GameObject("RoomListContent");
        contentGO.transform.SetParent(scrollRT, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(10, 10, 5, 5);

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.viewport = scrollRT;
        scrollGO.SetActive(false);

        // No Rooms Text (placeholder when empty)
        var noRoomsText = CreateText(contentRT, "NoRoomsText", Vector2.zero, new Vector2(480f, 40f), "No hay salas — ¡Creá una!", 20, TextAlignmentOptions.Center);
        noRoomsText.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f, 1f);

        // --- ROOM ENTRY PREFAB ---
        var roomEntryPrefab = CreateButton(canvasRT, "RoomEntryPrefab", Vector2.zero, new Vector2(480f, 45f), "Sala (0/4)", new Color(0.2f, 0.2f, 0.3f, 1f));
        var reTxt = roomEntryPrefab.GetComponentInChildren<TextMeshProUGUI>();
        if (reTxt != null) { reTxt.alignment = TextAlignmentOptions.Left; reTxt.fontSize = 20; }
        var reLayout = roomEntryPrefab.AddComponent<LayoutElement>();
        reLayout.preferredHeight = 45f;

        // Save as prefab
        string prefabPath = "Assets/Resources/Prefabs/RoomEntry.prefab";
        PrefabUtility.SaveAsPrefabAsset(roomEntryPrefab, prefabPath);
        Object.DestroyImmediate(roomEntryPrefab);
        var roomEntryPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        // --- ROOM PANEL (in-room view) ---
        var roomPanel = new GameObject("RoomPanel");
        roomPanel.transform.SetParent(canvasRT, false);
        var rpRT = roomPanel.AddComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(0.5f, 0.5f);
        rpRT.anchorMax = new Vector2(0.5f, 0.5f);
        rpRT.anchoredPosition = new Vector2(0f, 0f);
        rpRT.sizeDelta = new Vector2(500f, 300f);

        var rpBg = roomPanel.AddComponent<Image>();
        rpBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        roomPanel.SetActive(false);

        // Room info text
        var roomInfoText = CreateText(rpRT, "RoomInfoText", new Vector2(0f, 50f), new Vector2(450f, 200f), "Sala: ...\nEsperando gladiadores...", 22, TextAlignmentOptions.TopLeft);
        roomInfoText.GetComponent<TextMeshProUGUI>().color = Color.white;

        // Leave Room Button
        var leaveBtn = CreateButton(rpRT, "LeaveRoomButton", new Vector2(0f, -120f), new Vector2(200f, 45f), "SALIR", new Color(0.7f, 0.2f, 0.2f, 1f));

        // --- STATS PANEL ---
        var statsPanel = new GameObject("StatsPanel");
        statsPanel.transform.SetParent(canvasRT, false);
        var spRT = statsPanel.AddComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0.5f, 0.5f);
        spRT.anchorMax = new Vector2(0.5f, 0.5f);
        spRT.anchoredPosition = new Vector2(0f, 0f);
        spRT.sizeDelta = new Vector2(350f, 200f);

        var spBg = statsPanel.AddComponent<Image>();
        spBg.color = new Color(0f, 0f, 0f, 0.9f);
        statsPanel.SetActive(false);

        var statsTitle = CreateText(spRT, "StatsTitle", new Vector2(0f, 60f), new Vector2(300f, 40f), "ESTADÍSTICAS", 26, TextAlignmentOptions.Center);
        statsTitle.GetComponent<TextMeshProUGUI>().color = Color.yellow;

        var statsText = CreateText(spRT, "StatsText", new Vector2(0f, -10f), new Vector2(300f, 100f), "Kills: 0\nDeaths: 0", 22, TextAlignmentOptions.Center);
        statsText.GetComponent<TextMeshProUGUI>().color = Color.white;

        // Tab hint
        var tabHint = CreateText(canvasRT, "TabHint", new Vector2(0f, -20f), new Vector2(300f, 30f), "[TAB] Estadísticas", 16, TextAlignmentOptions.Center);
        tabHint.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        tabHint.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        tabHint.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
        tabHint.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f, 1f);

        // --- ASSIGN REFERENCES TO LOBBYUI ---
        var lobbyUIGO = GameObject.Find("LobbyUI");
        if (lobbyUIGO == null)
        {
            // Try canvas
            var lobbyUI = canvas.GetComponent<LobbyUI>();
            if (lobbyUI == null)
            {
                lobbyUIGO = canvas;
                lobbyUI = canvas.GetComponent<LobbyUI>();
            }
            if (lobbyUI != null) lobbyUIGO = lobbyUI.gameObject;
        }

        var lobbyComp = lobbyUIGO != null ? lobbyUIGO.GetComponent<LobbyUI>() : null;
        if (lobbyComp == null)
        {
            // Search all objects
            lobbyComp = Object.FindAnyObjectByType<LobbyUI>();
        }

        if (lobbyComp != null)
        {
            var so = new SerializedObject(lobbyComp);

            so.FindProperty("nicknameInput").objectReferenceValue = nicknameInput.GetComponent<TMP_InputField>();
            so.FindProperty("roomNameInput").objectReferenceValue = roomNameInput.GetComponent<TMP_InputField>();
            so.FindProperty("createRoomButton").objectReferenceValue = createRoomBtn.GetComponent<Button>();

            // For room list - we need the content transform and the prefab
            so.FindProperty("roomListContent").objectReferenceValue = contentRT;
            so.FindProperty("roomEntryPrefab").objectReferenceValue = roomEntryPrefabAsset;

            so.FindProperty("roomPanel").objectReferenceValue = roomPanel;
            so.FindProperty("roomInfoText").objectReferenceValue = roomInfoText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("leaveRoomButton").objectReferenceValue = leaveBtn.GetComponent<Button>();

            so.FindProperty("statsPanel").objectReferenceValue = statsPanel;
            so.FindProperty("statsText").objectReferenceValue = statsText.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedProperties();
            Debug.Log("LobbyUI references assigned!");
        }
        else
        {
            Debug.LogError("LobbyUI component not found!");
        }

        EditorUtility.SetDirty(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("Lobby UI setup complete!");
    }

    static GameObject CreateInputField(RectTransform parent, string name, Vector2 pos, Vector2 size, string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Text Area
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(rt, false);
        var taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10f, 5f);
        taRT.offsetMax = new Vector2(-10f, -5f);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(taRT, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;
        var phText = phGO.AddComponent<TextMeshProUGUI>();
        phText.text = placeholder;
        phText.fontSize = 20;
        phText.fontStyle = FontStyles.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        phText.alignment = TextAlignmentOptions.Left;

        // Text
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(taRT, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Left;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRT;
        input.textComponent = txt;
        input.placeholder = phText;
        input.characterLimit = 20;

        return go;
    }

    static GameObject CreateButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = bgColor;

        go.AddComponent<Button>();

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(rt, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(5f, 0f);
        txtRT.offsetMax = new Vector2(-5f, 0f);
        txtGO.AddComponent<CanvasRenderer>();
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        return go;
    }

    static GameObject CreateText(RectTransform parent, string name, Vector2 pos, Vector2 size, string content, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.AddComponent<CanvasRenderer>();
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = alignment;

        return go;
    }
}
#endif
