#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HUDBuilderTemp
{
    [MenuItem("Tools/Build New HUD")]
    public static void BuildUI()
    {
        // Find existing HUD_Canvas
        GameObject canvas = GameObject.Find("HUD_Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUDManager));
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvas.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
        }

        HUDManager manager = canvas.GetComponent<HUDManager>();
        
        Transform throwBtn = canvas.transform.Find("ThrowButton");
        if (throwBtn != null) GameObject.DestroyImmediate(throwBtn.gameObject);

        
        // --- 1-3. Top-Left: Player Info (Color, HP, Rank) ---
        Transform topBar = canvas.transform.Find("TopBar");
        if (topBar != null) GameObject.DestroyImmediate(topBar.gameObject);

        GameObject playerListPanel = new GameObject("PlayerListPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        playerListPanel.transform.SetParent(canvas.transform, false);
        var plRT = playerListPanel.GetComponent<RectTransform>();
        plRT.anchorMin = new Vector2(0.02f, 0.6f); 
        plRT.anchorMax = new Vector2(0.25f, 0.98f);
        plRT.offsetMin = Vector2.zero; plRT.offsetMax = Vector2.zero;
        
        var vlg = playerListPanel.GetComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.spacing = 20;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        List<PlayerInfoSlot> slots = new List<PlayerInfoSlot>();
        string[] pNames = { "Player 1", "Player 2", "Player 3", "Player 4" };
        Color[] pColors = { Color.red, Color.blue, Color.green, Color.yellow };

        for (int i = 0; i < 4; i++)
        {
            GameObject pSlot = new GameObject($"PlayerSlot_{i+1}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            pSlot.transform.SetParent(playerListPanel.transform, false);
            pSlot.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f); // Dark background
            var hlg = pSlot.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true; hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false; hlg.spacing = 15;
            hlg.padding = new RectOffset(10, 10, 10, 10);

            // Color Indicator
            GameObject colorInd = new GameObject("ColorIndicator", typeof(RectTransform), typeof(Image));
            colorInd.transform.SetParent(pSlot.transform, false);
            var cRT = colorInd.GetComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(40, 40);
            Image cImg = colorInd.GetComponent<Image>();
            cImg.color = pColors[i];

            // Name
            GameObject nameTxt = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameTxt.transform.SetParent(pSlot.transform, false);
            var nRT = nameTxt.GetComponent<RectTransform>();
            nRT.sizeDelta = new Vector2(120, 40);
            var nTmp = nameTxt.GetComponent<TextMeshProUGUI>();
            nTmp.text = pNames[i]; nTmp.color = Color.white; nTmp.fontSize = 24; nTmp.alignment = TextAlignmentOptions.Left;

            // HP
            GameObject hpTxt = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hpTxt.transform.SetParent(pSlot.transform, false);
            var hRT = hpTxt.GetComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(80, 40);
            var hTmp = hpTxt.GetComponent<TextMeshProUGUI>();
            hTmp.text = "HP: 100"; hTmp.color = new Color(0.4f, 1f, 0.4f); hTmp.fontSize = 24; hTmp.alignment = TextAlignmentOptions.Center;

            // Rank
            GameObject rankTxt = new GameObject("RankText", typeof(RectTransform), typeof(TextMeshProUGUI));
            rankTxt.transform.SetParent(pSlot.transform, false);
            var rRT = rankTxt.GetComponent<RectTransform>();
            rRT.sizeDelta = new Vector2(60, 40);
            var rTmp = rankTxt.GetComponent<TextMeshProUGUI>();
            rTmp.text = "-th"; rTmp.color = Color.yellow; rTmp.fontSize = 24; rTmp.alignment = TextAlignmentOptions.Right;

            PlayerInfoSlot slot = new PlayerInfoSlot();
            slot.colorIndicator = cImg;
            slot.nameText = nTmp;
            slot.hpText = hTmp;
            slot.rankText = rTmp;
            slots.Add(slot);
        }

        // --- 5. Bottom-Center: Personal Inventory (5 slots) ---
        Transform oldInv = canvas.transform.Find("InventoryPanel");
        if (oldInv != null) GameObject.DestroyImmediate(oldInv.gameObject);

        GameObject invPanel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        invPanel.transform.SetParent(canvas.transform, false);
        var invRT = invPanel.GetComponent<RectTransform>();
        invRT.anchorMin = new Vector2(0.5f, 0f);
        invRT.anchorMax = new Vector2(0.5f, 0f);
        invRT.pivot = new Vector2(0.5f, 0f);
        invRT.sizeDelta = new Vector2(600, 100);
        invRT.anchoredPosition = new Vector2(0, 50); // Move 50px up from bottom

        var invHLG = invPanel.GetComponent<HorizontalLayoutGroup>();
        invHLG.childAlignment = TextAnchor.MiddleCenter;
        invHLG.childControlHeight = true; invHLG.childControlWidth = false;
        invHLG.childForceExpandHeight = true; invHLG.childForceExpandWidth = false;
        invHLG.spacing = 20;

        List<Image> invSlots = new List<Image>();
        for (int i = 0; i < 5; i++)
        {
            GameObject slotBg = new GameObject($"InvSlot_{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotBg.transform.SetParent(invPanel.transform, false);
            Image bgImg = slotBg.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // Dark Background
            var le = slotBg.GetComponent<LayoutElement>();
            le.preferredWidth = 100; // LayoutElement correctly sets width when childControlWidth is false

            GameObject itemIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));

            itemIcon.transform.SetParent(slotBg.transform, false);
            var iconRT = itemIcon.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one; 
            iconRT.offsetMin = new Vector2(10, 10); iconRT.offsetMax = new Vector2(-10, -10);
            Image iImg = itemIcon.GetComponent<Image>();
            iImg.color = new Color(1, 1, 1, 0.2f); // Empty state
            invSlots.Add(iImg);
        }

        // --- 6. Top-Right: Cooldown Timer (15s) ---
        Transform oldTimer = canvas.transform.Find("TimerPanel");
        if (oldTimer != null) GameObject.DestroyImmediate(oldTimer.gameObject);

        GameObject timerPanel = new GameObject("TimerPanel", typeof(RectTransform), typeof(Image));
        timerPanel.transform.SetParent(canvas.transform, false);
        var tRT = timerPanel.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.85f, 0.85f);
        tRT.anchorMax = new Vector2(0.98f, 0.98f);
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        timerPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        GameObject timerTxtObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerTxtObj.transform.SetParent(timerPanel.transform, false);
        var ttRT = timerTxtObj.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = Vector2.zero; ttRT.offsetMax = Vector2.zero;
        var tTmp = timerTxtObj.GetComponent<TextMeshProUGUI>();
        tTmp.text = "15s"; tTmp.color = Color.white; tTmp.fontSize = 60; tTmp.alignment = TextAlignmentOptions.Center;

        // Assgin to Manager
        var m_fields = manager.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach(var f in m_fields) {
            if(f.Name == "playerSlots") f.SetValue(manager, slots);
            if(f.Name == "inventorySlots") f.SetValue(manager, invSlots);
            if(f.Name == "turnTimerText") f.SetValue(manager, tTmp);
        }
        
        Debug.Log("HUD Built Successfully!");
    }
}
#endif