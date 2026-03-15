using UnityEngine;
using UnityEngine.UI;

public class TitleCanvasController : MonoBehaviour
{
    [Header("Left Menu")]
    public Button btnStart;
    public Button btnCustomGame;
    public Button btnTutorial;
    public Button btnLocker;
    public Button btnSettings;
    public Button btnQuit;

    [Header("Bottom Bar")]
    public Button btnShop;

    private void OnEnable()
    {
        if (btnStart != null) btnStart.onClick.AddListener(OnStartClicked);
        if (btnCustomGame != null) btnCustomGame.onClick.AddListener(OnCustomGameClicked);
        if (btnTutorial != null) btnTutorial.onClick.AddListener(OnTutorialClicked);
        if (btnLocker != null) btnLocker.onClick.AddListener(OnLockerClicked);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
        if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);
        if (btnShop != null) btnShop.onClick.AddListener(OnShopClicked);
    }

    private void OnDisable()
    {
        if (btnStart != null) btnStart.onClick.RemoveListener(OnStartClicked);
        if (btnCustomGame != null) btnCustomGame.onClick.RemoveListener(OnCustomGameClicked);
        if (btnTutorial != null) btnTutorial.onClick.RemoveListener(OnTutorialClicked);
        if (btnLocker != null) btnLocker.onClick.RemoveListener(OnLockerClicked);
        if (btnSettings != null) btnSettings.onClick.RemoveListener(OnSettingsClicked);
        if (btnQuit != null) btnQuit.onClick.RemoveListener(OnQuitClicked);
        if (btnShop != null) btnShop.onClick.RemoveListener(OnShopClicked);
    }

    private void OnStartClicked()
    {
        Debug.Log("Pummel Lobby: 게임 시작 (Quick Match)");
    }

    private void OnCustomGameClicked()
    {
        Debug.Log("Pummel Lobby: 커스텀 게임 (Custom Game)");
    }

    private void OnTutorialClicked()
    {
        Debug.Log("Pummel Lobby: 튜토리얼 (Tutorial)");
    }

    private void OnLockerClicked()
    {
        Debug.Log("Pummel Lobby: 내 라커 (Locker)");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("Pummel Lobby: 설정 (Settings)");
    }

    private void OnQuitClicked()
    {
        Debug.Log("Pummel Lobby: 게임 종료 (Exit Game)");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnShopClicked()
    {
        Debug.Log("Pummel Lobby: 호갱 상점 (Item Shop)");
    }
}
