using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SetupPartyTitleUI
{
    [MenuItem("Tools/Setup Party Title UI")]
    public static void Setup()
    {
        // 1. Create TitleCanvas and EventSystem
        var canvasGO = new GameObject("TitleCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Basic solid background color for the prototype look
        // Distinctive Pummel Party muted dark blue background
        Color bgColor = new Color(0.18f, 0.22f, 0.28f); 
        Color bottomBarColor = new Color(1, 1, 1, 0); // Translucent container
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // 2. Main Background Panel
        var bgPanelGO = new GameObject("MainBackground");
        bgPanelGO.transform.SetParent(canvasGO.transform, false);
        var bgRect = bgPanelGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var bgImg = bgPanelGO.AddComponent<Image>();
        bgImg.color = bgColor;

        // 3. Left Side Menu Buttons
        var menuContainer = new GameObject("SideMenuContainer");
        menuContainer.transform.SetParent(canvasGO.transform, false);
        var menuRect = menuContainer.AddComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0, 0.5f);
        menuRect.anchorMax = new Vector2(0, 0.5f);
        menuRect.pivot = new Vector2(0, 0.5f);
        menuRect.sizeDelta = new Vector2(600, 700);
        menuRect.anchoredPosition = new Vector2(150, 50); // Offset from left edge

        var vlg = menuContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        string[] menuItems = { "game start", "", "tutorial", "locker", "settings", "exit game" };
        Color[] btnColors = { 
            new Color(0.9f, 0.4f, 0.2f),    // Vibrant Orange for Game Start
            Color.clear,                    // Spacer
            new Color(0.6f, 0.4f, 0.6f),    // Muted Purple
            new Color(0.8f, 0.6f, 0.2f),    // Muted Yellow
            new Color(0.6f, 0.4f, 0.2f),    // Bronze/Orange
            new Color(0.4f, 0.4f, 0.4f)     // Dark Gray
        };

        for (int i = 0; i < menuItems.Length; i++)
        {
            var item = menuItems[i];
            if (string.IsNullOrEmpty(item))
            {
                // Spacer
                var spacer = new GameObject("Spacer");
                spacer.transform.SetParent(menuContainer.transform, false);
                spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
                continue;
            }

            CreateTextButton(item, menuContainer.transform, defaultSprite, btnColors[i]);
        }

        // 4. Bottom Bar
        var bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(canvasGO.transform, false);
        var bbRect = bottomBar.AddComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(0, 0);
        bbRect.anchorMax = new Vector2(1, 0);
        bbRect.pivot = new Vector2(0.5f, 0);
        bbRect.sizeDelta = new Vector2(0, 80);
        bbRect.anchoredPosition = Vector2.zero;
        var bbImg = bottomBar.AddComponent<Image>();
        bbImg.color = bottomBarColor;

        // --- Bottom Left Items (Item Shop, Currency) ---
        var bbLeft = new GameObject("BottomLeftGroup");
        bbLeft.transform.SetParent(bottomBar.transform, false);
        var bblRect = bbLeft.AddComponent<RectTransform>();
        bblRect.anchorMin = new Vector2(0, 0.5f); bblRect.anchorMax = new Vector2(0, 0.5f);
        bblRect.pivot = new Vector2(0, 0.5f);
        bblRect.sizeDelta = new Vector2(400, 60);
        bblRect.anchoredPosition = new Vector2(20, 0);

        var hlgLeft = bbLeft.AddComponent<HorizontalLayoutGroup>();
        hlgLeft.spacing = 10;
        hlgLeft.childAlignment = TextAnchor.MiddleLeft;
        hlgLeft.childControlHeight = false;
        hlgLeft.childControlWidth = false;

        CreateBottomLabel("★  item shop", bbLeft.transform, defaultSprite, new Vector2(180, 55), new Color(0.9f, 0.7f, 0.1f), Color.black);
        CreateBottomLabel("★  999k", bbLeft.transform, defaultSprite, new Vector2(140, 55), new Color(0.2f, 0.25f, 0.3f), Color.white);

        // --- Bottom Right Items (Profile, Progress) ---
        var bbRight = new GameObject("BottomRightGroup");
        bbRight.transform.SetParent(bottomBar.transform, false);
        var bbrRect = bbRight.AddComponent<RectTransform>();
        bbrRect.anchorMin = new Vector2(1, 0.5f); bbrRect.anchorMax = new Vector2(1, 0.5f);
        bbrRect.pivot = new Vector2(1, 0.5f);
        bbrRect.sizeDelta = new Vector2(500, 70);
        bbrRect.anchoredPosition = new Vector2(-20, 0);

        // Add panel background for profile area
        var bbrImg = bbRight.AddComponent<Image>();
        bbrImg.sprite = defaultSprite;
        bbrImg.type = Image.Type.Sliced;
        bbrImg.color = new Color(0.12f, 0.15f, 0.2f); // Darker blue slate panel

        // Profile Avatar Circle
        var avatar = new GameObject("AvatarCircle");
        avatar.transform.SetParent(bbRight.transform, false);
        var avatarRect = avatar.AddComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0, 0.5f); avatarRect.anchorMax = new Vector2(0, 0.5f); // Anchor Left
        avatarRect.pivot = new Vector2(0, 0.5f);
        avatarRect.sizeDelta = new Vector2(50, 50);
        avatarRect.anchoredPosition = new Vector2(25, 0); // 25 from left internal edge
        var avatarImg = avatar.AddComponent<Image>();
        avatarImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // Circular
        avatarImg.color = new Color(0.8f, 0.5f, 0.2f); // Orange avatar bg
        
        var avatarStar = new GameObject("StarText");
        avatarStar.transform.SetParent(avatar.transform, false);
        var starRect = avatarStar.AddComponent<RectTransform>();
        starRect.anchorMin = Vector2.zero; starRect.anchorMax = Vector2.one;
        starRect.offsetMin = Vector2.zero; starRect.offsetMax = Vector2.zero;
        var starTmp = avatarStar.AddComponent<TextMeshProUGUI>();
        starTmp.text = "★";
        starTmp.fontSize = 30;
        starTmp.color = Color.white;
        starTmp.alignment = TextAlignmentOptions.Center;

        // Name
        var nameItem = new GameObject("NameText");
        nameItem.transform.SetParent(bbRight.transform, false);
        var nameRect = nameItem.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f); nameRect.anchorMax = new Vector2(0, 0.5f);
        nameRect.pivot = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(200, 30);
        nameRect.anchoredPosition = new Vector2(90, 0); // Align after avatar
        var nameTmp = nameItem.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "name";
        nameTmp.fontSize = 20;
        nameTmp.color = Color.white;
        nameTmp.fontStyle = FontStyles.Bold;

        // Progress Bar (BG)
        var pbBg = new GameObject("ProgressBarBG");
        pbBg.transform.SetParent(bbRight.transform, false);
        var pbBgRect = pbBg.AddComponent<RectTransform>();
        pbBgRect.anchorMin = new Vector2(0, 0.5f); pbBgRect.anchorMax = new Vector2(0, 0.5f);
        pbBgRect.pivot = new Vector2(0, 1);
        pbBgRect.sizeDelta = new Vector2(150, 12);
        pbBgRect.anchoredPosition = new Vector2(90, -5);
        var pbbImg = pbBg.AddComponent<Image>();
        pbbImg.color = new Color(0.08f, 0.10f, 0.12f); // Very dark track

        // Progress Bar (Fill)
        var pbFill = new GameObject("ProgressBarFill");
        pbFill.transform.SetParent(pbBg.transform, false);
        var pbFillRect = pbFill.AddComponent<RectTransform>();
        pbFillRect.anchorMin = new Vector2(0, 0); pbFillRect.anchorMax = new Vector2(0, 1); // Anchor left
        pbFillRect.pivot = new Vector2(0, 0.5f);
        pbFillRect.sizeDelta = new Vector2(80, 0); // Partial fill
        pbFillRect.anchoredPosition = Vector2.zero;
        var pbfImg = pbFill.AddComponent<Image>();
        pbfImg.color = new Color(0.2f, 0.8f, 0.3f); // Vibrant light green

        // Star Icon and 0/5
        var rankStatus = new GameObject("RankStatus");
        rankStatus.transform.SetParent(bbRight.transform, false);
        var rsRect = rankStatus.AddComponent<RectTransform>();
        rsRect.anchorMin = new Vector2(1, 0.5f); rsRect.anchorMax = new Vector2(1, 0.5f);
        rsRect.pivot = new Vector2(1, 0.5f);
        rsRect.sizeDelta = new Vector2(120, 50);
        rsRect.anchoredPosition = new Vector2(-20, 0); // 20 from right internal edge
        
        var rsTmp = rankStatus.AddComponent<TextMeshProUGUI>();
        rsTmp.text = "★ 0/5";
        rsTmp.fontSize = 28;
        rsTmp.color = new Color(0.9f, 0.8f, 0.2f); // Gold star text
        rsTmp.fontStyle = FontStyles.Bold;
        rsTmp.alignment = TextAlignmentOptions.Right;

        // 5. Build Popups
        CreateRoomPopups(canvasGO.transform, defaultSprite);

        // 6. Attach controller
        canvasGO.AddComponent<PartyTitleController>();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Prototype Title UI");
    }

    private static void CreateRoomPopups(Transform parent, Sprite bgSprite)
    {
        // Add a full screen dim panel
        var dimPanel = new GameObject("DimPanel");
        dimPanel.transform.SetParent(parent, false);
        var dimRect = dimPanel.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero; dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero; dimRect.offsetMax = Vector2.zero;
        var dimImg = dimPanel.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f); // 70% opacity black
        dimPanel.SetActive(false); // Hidden by default

        // Game Start Popup Panel
        var gameStartPopup = CreatePopupPanel("GameStartPopup", dimPanel.transform, bgSprite, new Vector2(500, 360));
        CreatePopupTitle("Game Start", gameStartPopup);
        var gsCreateBtn = CreatePopupButton("Create Room", gameStartPopup, bgSprite, new Color(0.9f, 0.4f, 0.2f), new Vector2(0, 50));
        var gsJoinBtn = CreatePopupButton("Join Room", gameStartPopup, bgSprite, new Color(0.2f, 0.6f, 0.4f), new Vector2(0, -30));
        var gsCloseBtn = CreatePopupButton("Close", gameStartPopup, bgSprite, new Color(0.4f, 0.4f, 0.4f), new Vector2(0, -120));
        gsCloseBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
        gsCloseBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>().fontSize = 24;

        // Create Room Popup Panel
        var createRoomPopup = CreatePopupPanel("CreateRoomPopup", dimPanel.transform, bgSprite, new Vector2(600, 450));
        CreatePopupTitle("Create Room", createRoomPopup);
        
        // Players Dropdown Area
        CreateRoomLabel("Players", createRoomPopup, new Vector2(-150, 40));
        var dropdownBg = CreatePopupFieldBg("DropdownBg", createRoomPopup, bgSprite, new Vector2(200, 50), new Vector2(50, 40));
        
        var ptxtGO = new GameObject("Text");
        ptxtGO.transform.SetParent(dropdownBg.transform, false);
        var ptxtRect = ptxtGO.AddComponent<RectTransform>();
        ptxtRect.anchorMin = Vector2.zero; ptxtRect.anchorMax = Vector2.one;
        ptxtRect.offsetMin = Vector2.zero; ptxtRect.offsetMax = Vector2.zero;
        
        var playersTmp = ptxtGO.AddComponent<TextMeshProUGUI>();
        playersTmp.text = "4 Players  ▼";
        playersTmp.fontSize = 24;
        playersTmp.color = Color.white;
        playersTmp.alignment = TextAlignmentOptions.Center;
        playersTmp.fontStyle = FontStyles.Bold;
        dropdownBg.AddComponent<TMP_Dropdown>(); // Adding simple component for structure
        
        // Public/Private Toggle Area
        CreateRoomLabel("Visibility", createRoomPopup, new Vector2(-150, -40));
        var toggleBg = CreatePopupFieldBg("ToggleBg", createRoomPopup, bgSprite, new Vector2(200, 50), new Vector2(50, -40));
        
        var ttxtGO = new GameObject("Text");
        ttxtGO.transform.SetParent(toggleBg.transform, false);
        var ttxtRect = ttxtGO.AddComponent<RectTransform>();
        ttxtRect.anchorMin = Vector2.zero; ttxtRect.anchorMax = Vector2.one;
        ttxtRect.offsetMin = Vector2.zero; ttxtRect.offsetMax = Vector2.zero;
        
        var toggleTmp = ttxtGO.AddComponent<TextMeshProUGUI>();
        toggleTmp.text = "Public";
        toggleTmp.fontSize = 24;
        toggleTmp.color = Color.white;
        toggleTmp.alignment = TextAlignmentOptions.Center;
        toggleTmp.fontStyle = FontStyles.Bold;
        toggleBg.AddComponent<Toggle>(); // Structure
        
        // Create Button
        var crCloseBtn = CreatePopupButton("Close", createRoomPopup, bgSprite, new Color(0.4f, 0.4f, 0.4f), new Vector2(-130, -140));
        crCloseBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 60);
        var createBtn = CreatePopupButton("Create", createRoomPopup, bgSprite, new Color(0.2f, 0.6f, 0.4f), new Vector2(130, -140));
        createBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 60);

        // Join Room Popup Panel
        var joinRoomPopup = CreatePopupPanel("JoinRoomPopup", dimPanel.transform, bgSprite, new Vector2(600, 400));
        joinRoomPopup.SetActive(false); // Just so they don't overlap if both are active under DimPanel
        CreatePopupTitle("Join Room", joinRoomPopup);
        
        CreateRoomLabel("Enter Room Code", joinRoomPopup, new Vector2(0, 40));
        var inputFieldBg = CreatePopupFieldBg("InputFieldBg", joinRoomPopup, bgSprite, new Vector2(300, 60), new Vector2(0, -20));
        
        var itxtGO = new GameObject("Text");
        itxtGO.transform.SetParent(inputFieldBg.transform, false);
        var itxtRect = itxtGO.AddComponent<RectTransform>();
        itxtRect.anchorMin = Vector2.zero; itxtRect.anchorMax = Vector2.one;
        itxtRect.offsetMin = Vector2.zero; itxtRect.offsetMax = Vector2.zero;
        
        var inputTmp = itxtGO.AddComponent<TextMeshProUGUI>();
        inputTmp.text = "123456"; // Placeholder showing 6 digits
        inputTmp.fontSize = 30;
        inputTmp.color = new Color(0.7f, 0.7f, 0.7f);
        inputTmp.alignment = TextAlignmentOptions.Center;
        inputTmp.fontStyle = FontStyles.Bold;
        
        var inputField = inputFieldBg.AddComponent<TMP_InputField>();
        inputField.textComponent = inputTmp;
        inputField.characterLimit = 6;
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber; // Num pad only

        // Join Button
        var jrCloseBtn = CreatePopupButton("Close", joinRoomPopup, bgSprite, new Color(0.4f, 0.4f, 0.4f), new Vector2(-130, -120));
        jrCloseBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 60);
        var joinBtn = CreatePopupButton("Join", joinRoomPopup, bgSprite, new Color(0.25f, 0.45f, 0.65f), new Vector2(130, -120));
        joinBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 60);
    }

    private static GameObject CreatePopupPanel(string name, Transform parent, Sprite bgSprite, Vector2 size)
    {
        var popupPanel = new GameObject(name);
        popupPanel.transform.SetParent(parent, false);
        var rect = popupPanel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        
        var img = popupPanel.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.18f, 0.22f, 0.28f); // Base dark blue
        
        // Inner border/style
        var outline = popupPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.12f, 0.15f);
        outline.effectDistance = new Vector2(4, -4);
        
        return popupPanel;
    }

    private static void CreatePopupTitle(string title, GameObject popupPanel)
    {
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(popupPanel.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1); titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(400, 60);
        titleRect.anchoredPosition = new Vector2(0, -30);
        
        var tmp = titleGO.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 40;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }

    private static void CreateRoomLabel(string text, GameObject parent, Vector2 position)
    {
        var labelGO = new GameObject(text + "_Label");
        labelGO.transform.SetParent(parent.transform, false);
        var rect = labelGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);
        rect.anchoredPosition = position;
        
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.color = new Color(0.8f, 0.8f, 0.8f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private static GameObject CreatePopupFieldBg(string name, GameObject parent, Sprite bgSprite, Vector2 size, Vector2 position)
    {
        var bgGO = new GameObject(name);
        bgGO.transform.SetParent(parent.transform, false);
        var rect = bgGO.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        var img = bgGO.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.12f, 0.15f, 0.2f); // Darker field bg
        return bgGO;
    }

    private static GameObject CreatePopupButton(string text, GameObject parent, Sprite bgSprite, Color bgColor, Vector2 position)
    {
        var btnGO = new GameObject(text + "_Button");
        btnGO.transform.SetParent(parent.transform, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 60);
        rect.anchoredPosition = position;
        
        var img = btnGO.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = bgColor;
        
        btnGO.AddComponent<Button>();
        
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        return btnGO;
    }

    private static Button CreateTextButton(string text, Transform parent, Sprite bgSprite, Color bgColor)
    {
        var btnGO = new GameObject(text + "_Button");
        btnGO.transform.SetParent(parent, false);
        var rect = btnGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 60);

        // Add visible rounded image background
        var img = btnGO.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = bgColor;

        var btn = btnGO.AddComponent<Button>();

        // Text needs to be on a child object
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 35; // slightly smaller to fit padding
        tmp.color = Color.white; 
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        // Adding letter spacing to match prototype
        tmp.characterSpacing = 5f; 

        return btn;
    }

    private static void CreateBottomLabel(string text, Transform parent, Sprite bgSprite, Vector2 size, Color bgColor, Color textColor)
    {
        var go = new GameObject(text + "_Box");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = bgColor; 

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }
}
