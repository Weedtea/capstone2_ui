#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class LockerUIBuilder
{
    [MenuItem("Tools/Build Locker UI")]
    public static void BuildUI()
    {
        GameObject dimPanel = null;
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach(var c in canvases) {
            if(c.name == "TitleCanvas") {
                Transform dp = c.transform.Find("DimPanel");
                if (dp != null) { dimPanel = dp.gameObject; break; }
            }
        }

        if (dimPanel == null) { Debug.LogError("DimPanel not found!"); return; }

        // 삭제 (기존 거 덮어쓰기 방지)
        Transform oldLocker = dimPanel.transform.Find("LockerPopup");
        if(oldLocker != null) GameObject.DestroyImmediate(oldLocker.gameObject);

        // 1. LockerPopup
        GameObject lockerPopup = new GameObject("LockerPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LockerManager));
        lockerPopup.transform.SetParent(dimPanel.transform, false);
        RectTransform rt = lockerPopup.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        lockerPopup.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.98f);

        LockerManager manager = lockerPopup.GetComponent<LockerManager>();

        // 2. Title & Close
        GameObject title = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        title.transform.SetParent(lockerPopup.transform, false);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(0, 100);
        titleRT.anchoredPosition = new Vector2(0, -20);
        var tmpTitle = title.GetComponent<TextMeshProUGUI>();
        tmpTitle.text = "Costumes";
        tmpTitle.alignment = TextAlignmentOptions.Center;
        tmpTitle.fontSize = 50;
        tmpTitle.color = Color.white;

        GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(lockerPopup.transform, false);
        var closeRT = closeBtnObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.sizeDelta = new Vector2(60, 60);
        closeRT.anchoredPosition = new Vector2(-20, -20);
        closeBtnObj.GetComponent<Image>().color = new Color(0.77f, 0.15f, 0.15f, 1f); // Strong Red
        
        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        var ctxtRT = closeTextObj.GetComponent<RectTransform>();
        ctxtRT.anchorMin = Vector2.zero; ctxtRT.anchorMax = Vector2.one; ctxtRT.sizeDelta = Vector2.zero; ctxtRT.anchoredPosition = Vector2.zero;
        var ctxt = closeTextObj.GetComponent<TextMeshProUGUI>();
        ctxt.text = "X"; ctxt.alignment = TextAlignmentOptions.Center; ctxt.color = Color.white; ctxt.fontSize = 30;

        // 3. ScrollView + Grid
        GameObject scrollView = new GameObject("ScrollView_Grid", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollView.transform.SetParent(lockerPopup.transform, false);
        var svRT = scrollView.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0.05f, 0.05f);
        svRT.anchorMax = new Vector2(0.95f, 0.82f);
        svRT.sizeDelta = Vector2.zero;
        svRT.anchoredPosition = Vector2.zero;
        scrollView.GetComponent<Image>().color = new Color(0,0,0,0.3f);
        var scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollView.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one; vpRT.sizeDelta = Vector2.zero; vpRT.anchoredPosition = Vector2.zero;
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        content.transform.SetParent(viewport.transform, false);
        var ctRT = content.GetComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1); ctRT.pivot = new Vector2(0.5f, 1);
        ctRT.sizeDelta = new Vector2(0, 800); ctRT.anchoredPosition = Vector2.zero;
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 160);
        grid.spacing = new Vector2(30, 30);
        grid.padding = new RectOffset(30, 30, 30, 30);
        
        scrollRect.viewport = vpRT;
        scrollRect.content = ctRT;

        // 4. Prefab Template (Hidden)
        GameObject slotTemplate = new GameObject("LockerSlot_Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(LockerSlot));
        slotTemplate.transform.SetParent(lockerPopup.transform, false);
        slotTemplate.SetActive(false); // Hide template
        var slotImage = slotTemplate.GetComponent<Image>();
        slotImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var outline = slotTemplate.GetComponent<Outline>();
        outline.enabled = false;
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(6, -6);
        
        GameObject slotIcon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slotIcon.transform.SetParent(slotTemplate.transform, false);
        var iconRT = slotIcon.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one; iconRT.sizeDelta = new Vector2(-20, -20); iconRT.anchoredPosition = Vector2.zero;
        var iconImg = slotIcon.GetComponent<Image>();

        var lockerSlot = slotTemplate.GetComponent<LockerSlot>();
        var fields = lockerSlot.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach(var f in fields) {
            if(f.Name == "costumeImage") f.SetValue(lockerSlot, iconImg);
            if(f.Name == "slotButton") f.SetValue(lockerSlot, slotTemplate.GetComponent<Button>());
            if(f.Name == "equipOutline") f.SetValue(lockerSlot, outline);
        }

        // 5. Purchase Modal
        GameObject purchaseModal = new GameObject("PurchaseModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        purchaseModal.transform.SetParent(lockerPopup.transform, false);
        var pmRT = purchaseModal.GetComponent<RectTransform>();
        pmRT.anchorMin = Vector2.zero; pmRT.anchorMax = Vector2.one; pmRT.sizeDelta = Vector2.zero; pmRT.anchoredPosition = Vector2.zero;
        purchaseModal.GetComponent<Image>().color = new Color(0,0,0,0.85f);

        GameObject modalWindow = new GameObject("ModalWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalWindow.transform.SetParent(purchaseModal.transform, false);
        var mwRT = modalWindow.GetComponent<RectTransform>();
        mwRT.anchorMin = new Vector2(0.5f, 0.5f); mwRT.anchorMax = new Vector2(0.5f, 0.5f);
        mwRT.pivot = new Vector2(0.5f, 0.5f);
        mwRT.sizeDelta = new Vector2(600, 400); mwRT.anchoredPosition = Vector2.zero;
        modalWindow.GetComponent<Image>().color = new Color(0.2f,0.2f,0.25f,1f);

        GameObject msgText = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        msgText.transform.SetParent(modalWindow.transform, false);
        var msgRT = msgText.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.1f, 0.6f); msgRT.anchorMax = new Vector2(0.9f, 0.9f); msgRT.sizeDelta = Vector2.zero; msgRT.anchoredPosition = Vector2.zero;
        var tmpMsg = msgText.GetComponent<TextMeshProUGUI>();
        tmpMsg.text = "Buy this costume?"; tmpMsg.alignment = TextAlignmentOptions.Center; tmpMsg.color = Color.white; tmpMsg.fontSize = 32;

        GameObject priceText = new GameObject("PriceText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        priceText.transform.SetParent(modalWindow.transform, false);
        var prcRT = priceText.GetComponent<RectTransform>();
        prcRT.anchorMin = new Vector2(0.1f, 0.4f); prcRT.anchorMax = new Vector2(0.9f, 0.6f); prcRT.sizeDelta = Vector2.zero; prcRT.anchoredPosition = Vector2.zero;
        var tmpPrc = priceText.GetComponent<TextMeshProUGUI>();
        tmpPrc.text = "Price: 500 G"; tmpPrc.alignment = TextAlignmentOptions.Center; tmpPrc.color = Color.yellow; tmpPrc.fontSize = 28;

        GameObject buyBtn = new GameObject("BuyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buyBtn.transform.SetParent(modalWindow.transform, false);
        var bRT = buyBtn.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.15f, 0.1f); bRT.anchorMax = new Vector2(0.45f, 0.3f); bRT.sizeDelta = Vector2.zero; bRT.anchoredPosition = Vector2.zero;
        buyBtn.GetComponent<Image>().color = new Color(0.13f, 0.38f, 0.68f, 1f); // Blue
        GameObject buyTxt = new GameObject("BuyTxt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        buyTxt.transform.SetParent(buyBtn.transform, false);
        var btRT = buyTxt.GetComponent<RectTransform>(); btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one; btRT.sizeDelta = Vector2.zero; btRT.anchoredPosition = Vector2.zero;
        var btTmp = buyTxt.GetComponent<TextMeshProUGUI>(); btTmp.text = "Yes"; btTmp.alignment = TextAlignmentOptions.Center; btTmp.color = Color.white; btTmp.fontSize = 24;

        GameObject canBtn = new GameObject("CancelButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        canBtn.transform.SetParent(modalWindow.transform, false);
        var cRT = canBtn.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.55f, 0.1f); cRT.anchorMax = new Vector2(0.85f, 0.3f); cRT.sizeDelta = Vector2.zero; cRT.anchoredPosition = Vector2.zero;
        canBtn.GetComponent<Image>().color = new Color(0.77f, 0.15f, 0.15f, 1f); // Red
        GameObject canTxt = new GameObject("CanTxt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        canTxt.transform.SetParent(canBtn.transform, false);
        var ctRT2 = canTxt.GetComponent<RectTransform>(); ctRT2.anchorMin = Vector2.zero; ctRT2.anchorMax = Vector2.one; ctRT2.sizeDelta = Vector2.zero; ctRT2.anchoredPosition = Vector2.zero;
        var ctTmp = canTxt.GetComponent<TextMeshProUGUI>(); ctTmp.text = "No"; ctTmp.alignment = TextAlignmentOptions.Center; ctTmp.color = Color.white; ctTmp.fontSize = 24;

        purchaseModal.SetActive(false);

        // 6. 매니저 세팅 
        var m_fields = manager.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach(var f in m_fields) {
            if(f.Name == "scrollRectContent") f.SetValue(manager, ctRT);
            if(f.Name == "slotPrefab") f.SetValue(manager, slotTemplate);
            if(f.Name == "purchaseModalPanel") f.SetValue(manager, purchaseModal);
            if(f.Name == "modalMessageText") f.SetValue(manager, tmpMsg);
            if(f.Name == "modalPriceText") f.SetValue(manager, tmpPrc);
            if(f.Name == "buyButton") f.SetValue(manager, buyBtn.GetComponent<Button>());
            if(f.Name == "cancelButton") f.SetValue(manager, canBtn.GetComponent<Button>());
            if(f.Name == "costumeDatabase") f.SetValue(manager, new System.Collections.Generic.List<CostumeData>() {
                new CostumeData() { costumeID = "Costume_Basic", price = 0 },
                new CostumeData() { costumeID = "Costume_Crown", price = 500 },
                new CostumeData() { costumeID = "Costume_Glasses", price = 300 },
                new CostumeData() { costumeID = "Costume_Sword", price = 1500 },
                new CostumeData() { costumeID = "Costume_Shield", price = 800 },
                new CostumeData() { costumeID = "Costume_Boots", price = 400 },
                new CostumeData() { costumeID = "Costume_Cape", price = 1000 },
                new CostumeData() { costumeID = "Costume_Mask", price = 600 },
                new CostumeData() { costumeID = "Costume_Armor", price = 2500 }
            });
        }
        
        lockerPopup.SetActive(false);

        // Sidebar 버튼 링킹 (자동 등록용 스크립트 기능 추가) 구동 확인용
        Selection.activeGameObject = lockerPopup;
        Debug.Log("Locker UI Built Successfully!");
    }
}
#endif
