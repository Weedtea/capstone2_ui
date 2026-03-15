using UnityEngine;
using UnityEngine.UI;

public class PartyTitleController : MonoBehaviour
{
    private GameObject dimPanel;
    private GameObject gameStartPopup;
    private GameObject createRoomPopup;
    private GameObject joinRoomPopup;

    void Start()
    {
        // Find panels
        var dimPanelTrans = transform.Find("DimPanel");
        if (dimPanelTrans == null) return;
        dimPanel = dimPanelTrans.gameObject;

        gameStartPopup = dimPanelTrans.Find("GameStartPopup")?.gameObject;
        createRoomPopup = dimPanelTrans.Find("CreateRoomPopup")?.gameObject;
        joinRoomPopup = dimPanelTrans.Find("JoinRoomPopup")?.gameObject;

        // Hook up side menu buttons
        var leftMenu = transform.Find("SideMenuContainer");
        if (leftMenu != null)
        {
            var gameStartBtn = leftMenu.Find("game start_Button")?.GetComponent<Button>();
            if (gameStartBtn != null) gameStartBtn.onClick.AddListener(ShowGameStartPopup);
        }

        // Hook up Game Start popup buttons
        if (gameStartPopup != null)
        {
            gameStartPopup.transform.Find("Create Room_Button")?.GetComponent<Button>()?.onClick.AddListener(ShowCreateRoomPopup);
            gameStartPopup.transform.Find("Join Room_Button")?.GetComponent<Button>()?.onClick.AddListener(ShowJoinRoomPopup);
            gameStartPopup.transform.Find("Close_Button")?.GetComponent<Button>()?.onClick.AddListener(ClosePopups);
        }

        // Hook up Create Room popup buttons
        if (createRoomPopup != null)
        {
            createRoomPopup.transform.Find("Close_Button")?.GetComponent<Button>()?.onClick.AddListener(ShowGameStartPopup);
        }

        // Hook up Join Room popup buttons
        if (joinRoomPopup != null)
        {
            joinRoomPopup.transform.Find("Close_Button")?.GetComponent<Button>()?.onClick.AddListener(ShowGameStartPopup);
        }

        // Add close logic to dim panel background
        var dimBtn = dimPanel.GetComponent<Button>();
        if (dimBtn == null) dimBtn = dimPanel.AddComponent<Button>();
        dimBtn.onClick.AddListener(ClosePopups);

        // Ensure everything is closed at start
        ClosePopups();
    }

    public void ShowGameStartPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(true);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
    }

    public void ShowCreateRoomPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(true);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
    }

    public void ShowJoinRoomPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(true);
    }

    public void ClosePopups()
    {
        dimPanel.SetActive(false);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
    }
}