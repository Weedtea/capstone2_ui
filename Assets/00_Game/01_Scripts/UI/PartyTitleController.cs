using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PartyTitleController : MonoBehaviour, ILobbyManager
{
    private const int ROOM_CODE_LENGTH = 6;
    private const string ROOM_CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const string LOBBY_SCENE_NAME = "LobbyScene";

    [Header("Fusion Runner")]
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private NetworkNotificationUI notificationUIPrefab;

    [Header("UI Popups")]
    private GameObject dimPanel;
    private GameObject gameStartPopup;
    private GameObject createRoomPopup;
    private GameObject joinRoomPopup;
    private GameObject tutorialPopup;
    private GameObject exitConfirmPopup;
    private GameObject settingPopup;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField createNicknameInput;
    [SerializeField] private TMP_InputField joinNicknameInput;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("Action Buttons")]
    [SerializeField] private Button confirmCreateButton;
    [SerializeField] private Button confirmJoinButton;

    private NetworkRunner _runner;
    private bool _isConnecting;
    private static bool _isLoadingLobby;
    private string _lastSessionName;
    private string _lastNickname;

    void Start()
    {
        // Find panels
        var dimPanelTrans = transform.Find("DimPanel");
        if (dimPanelTrans == null) return;
        dimPanel = dimPanelTrans.gameObject;

        gameStartPopup = dimPanelTrans.Find("GameStartPopup")?.gameObject;
        createRoomPopup = dimPanelTrans.Find("CreateRoomPopup")?.gameObject;
        joinRoomPopup = dimPanelTrans.Find("JoinRoomPopup")?.gameObject;
        tutorialPopup = dimPanelTrans.Find("TutorialPopup")?.gameObject;
        exitConfirmPopup = dimPanelTrans.Find("ExitConfirmPopup")?.gameObject;
        settingPopup = dimPanelTrans.Find("SettingPopup")?.gameObject;

        // Hook up side menu buttons
        var leftMenu = transform.Find("SideMenuContainer");
        if (leftMenu != null)
        {
            var gameStartBtn = leftMenu.Find("game start_Button")?.GetComponent<Button>();
            if (gameStartBtn != null) gameStartBtn.onClick.AddListener(ShowGameStartPopup);

            var settingBtn = leftMenu.Find("settings_Button")?.GetComponent<Button>();
            if (settingBtn != null) settingBtn.onClick.AddListener(ShowSettingPopup);

            var tutorialBtn = leftMenu.Find("tutorial_Button")?.GetComponent<Button>();
            if (tutorialBtn != null) tutorialBtn.onClick.AddListener(ShowTutorialPopup);

            var exitBtn = leftMenu.Find("exit game_Button")?.GetComponent<Button>();
            if (exitBtn != null) exitBtn.onClick.AddListener(ShowExitConfirmPopup);
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

        // Hook up Tutorial popup buttons
        if (tutorialPopup != null)
        {
            tutorialPopup.transform.Find("Close_Button")?.GetComponent<Button>()?.onClick.AddListener(ClosePopups);
        }

        // Hook up Exit Confirm popup buttons
        if (exitConfirmPopup != null)
        {
            exitConfirmPopup.transform.Find("Confirm_Button")?.GetComponent<Button>()?.onClick.AddListener(OnConfirmExitClicked);
            exitConfirmPopup.transform.Find("Cancel_Button")?.GetComponent<Button>()?.onClick.AddListener(ClosePopups);
        }

        // Hook up Setting popup buttons
        if (settingPopup != null)
        {
            settingPopup.transform.Find("Close_Button")?.GetComponent<Button>()?.onClick.AddListener(ClosePopups);
        }

        // Hook up Networking buttons (if assigned via inspector)
        if (confirmCreateButton != null) confirmCreateButton.onClick.AddListener(OnCreateRoomClicked);
        if (confirmJoinButton != null) confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);

        // Add close logic to dim panel background
        var dimBtn = dimPanel.GetComponent<Button>();
        if (dimBtn == null) dimBtn = dimPanel.AddComponent<Button>();
        dimBtn.onClick.AddListener(OnDimPanelClicked);

        // Networking setup
        EnsureNotificationUI();
        CleanupStaleRunners();

        // Ensure everything is closed at start
        ClosePopups();
    }

    private void OnDestroy()
    {
        if (_isLoadingLobby)
        {
            _isLoadingLobby = false;
            return;
        }
        if (_runner != null && _runner.IsRunning)
            _ = _runner.Shutdown();
    }

    #region UI Logic

    public void ShowGameStartPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(true);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
        if (tutorialPopup) tutorialPopup.SetActive(false);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(false);
        if (settingPopup) settingPopup.SetActive(false);
    }

    public void ShowCreateRoomPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(true);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
        if (tutorialPopup) tutorialPopup.SetActive(false);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(false);
        if (settingPopup) settingPopup.SetActive(false);
    }

    public void ShowJoinRoomPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(true);
        if (tutorialPopup) tutorialPopup.SetActive(false);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(false);
        if (settingPopup) settingPopup.SetActive(false);
    }

    public void ShowTutorialPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
        if (tutorialPopup) tutorialPopup.SetActive(true);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(false);
        if (settingPopup) settingPopup.SetActive(false);
    }

    public void ShowExitConfirmPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
        if (tutorialPopup) tutorialPopup.SetActive(false);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(true);
        if (settingPopup) settingPopup.SetActive(false);
    }

    public void ShowSettingPopup()
    {
        dimPanel.SetActive(true);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
        if (tutorialPopup) tutorialPopup.SetActive(false);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(false);
        if (settingPopup) settingPopup.SetActive(true);
    }

    public void ClosePopups()
    {
        dimPanel.SetActive(false);
        if (gameStartPopup) gameStartPopup.SetActive(false);
        if (createRoomPopup) createRoomPopup.SetActive(false);
        if (joinRoomPopup) joinRoomPopup.SetActive(false);
        if (tutorialPopup) tutorialPopup.SetActive(false);
        if (exitConfirmPopup) exitConfirmPopup.SetActive(false);
        if (settingPopup) settingPopup.SetActive(false);
    }

    private void OnDimPanelClicked()
    {
        // Settings popup should only be closed via its X button
        if (settingPopup != null && settingPopup.activeSelf) return;
        
        ClosePopups();
    }

    private void OnConfirmExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Networking Logic

    public async void OnCreateRoomClicked()
    {
        if (_isConnecting) return;

        string nickname = GetNicknameOrDefault(createNicknameInput);
        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("Please enter a nickname.");
            return;
        }

        string roomCode = GenerateRoomCode();
        string roomDisplayName = $"{nickname}'s room";
        _isConnecting = true;
        SetButtonsInteractable(false);

        LobbyNicknameRegistry.Clear();

        bool success = await StartHostAsync(roomDisplayName, roomCode, nickname);
        _isConnecting = false;
        SetButtonsInteractable(true);

        if (success)
        {
            _lastSessionName = roomCode;
            _lastNickname = nickname;

            LobbyNicknameRegistry.Register(_runner.LocalPlayer, nickname);
            InitializeConnectionMonitor(GameMode.Host);

            _isLoadingLobby = true;
            SceneManager.LoadScene(LOBBY_SCENE_NAME);
        }
        else
            Debug.LogWarning("Failed to create a room.");
    }

    public async void OnConfirmJoinClicked()
    {
        if (_isConnecting) return;

        string nickname = GetNicknameOrDefault(joinNicknameInput);
        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("Please enter a nickname.");
            return;
        }

        string code = joinCodeInput != null ? joinCodeInput.text?.Trim().ToUpperInvariant() : string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Please enter a room code.");
            return;
        }

        _isConnecting = true;
        SetButtonsInteractable(false);

        LobbyNicknameRegistry.Clear();

        bool success = await StartClientAsync(code, nickname);
        _isConnecting = false;
        SetButtonsInteractable(true);

        if (success)
        {
            _lastSessionName = code;
            _lastNickname = nickname;

            LobbyNicknameRegistry.Register(_runner.LocalPlayer, nickname);
            InitializeConnectionMonitor(GameMode.Client);

            _isLoadingLobby = true;
            SceneManager.LoadScene(LOBBY_SCENE_NAME);
        }
        else
            Debug.LogWarning("Failed to join the room. Please check the code.");
    }

    private async Task<bool> StartHostAsync(string roomDisplayName, string sessionName, string nickname)
    {
        if (networkRunnerPrefab == null)
        {
            Debug.LogError("PartyTitleController: NetworkRunner Prefab이 할당되지 않았습니다.");
            return false;
        }

        await ShutdownExistingRunner();

        _runner = Instantiate(networkRunnerPrefab);
        DontDestroyOnLoad(_runner.gameObject);

        EnsureRunnerComponents(_runner);
        AttachLobbyCallbacks(_runner);

        INetworkSceneManager sceneManager = _runner.GetComponent<INetworkSceneManager>();
        INetworkObjectProvider objectProvider = _runner.GetComponent<INetworkObjectProvider>();

        AuthenticationValues authValues = new AuthenticationValues(nickname);

        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            PlayerCount = 10,
            AuthValues = authValues,
            SceneManager = sceneManager,
            ObjectProvider = objectProvider
        };

        StartGameResult result = await _runner.StartGame(args);
        if (result.Ok == false)
        {
            Debug.LogWarning($"Fusion StartGame (Host) 실패: {result.ShutdownReason}");
            if (_runner != null)
            {
                await _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }
            return false;
        }

        return true;
    }

    private async Task<bool> StartClientAsync(string sessionName, string nickname)
    {
        if (networkRunnerPrefab == null)
        {
            Debug.LogError("PartyTitleController: NetworkRunner Prefab이 할당되지 않았습니다.");
            return false;
        }

        await ShutdownExistingRunner();

        _runner = Instantiate(networkRunnerPrefab);
        DontDestroyOnLoad(_runner.gameObject);

        EnsureRunnerComponents(_runner);
        AttachLobbyCallbacks(_runner);

        INetworkSceneManager sceneManager = _runner.GetComponent<INetworkSceneManager>();
        INetworkObjectProvider objectProvider = _runner.GetComponent<INetworkObjectProvider>();

        AuthenticationValues authValues = new AuthenticationValues(nickname);

        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            AuthValues = authValues,
            SceneManager = sceneManager,
            ObjectProvider = objectProvider
        };

        StartGameResult result = await _runner.StartGame(args);
        if (result.Ok == false)
        {
            Debug.LogWarning($"Fusion StartGame (Client) 실패: {result.ShutdownReason}");
            if (_runner != null)
            {
                await _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }
            return false;
        }

        return true;
    }

    private async Task ShutdownExistingRunner()
    {
        if (_runner != null && _runner.IsRunning)
            await _runner.Shutdown();

        if (_runner != null)
        {
            Destroy(_runner.gameObject);
            _runner = null;
        }
    }

    private void EnsureRunnerComponents(NetworkRunner runner)
    {
        if (runner.GetComponent<INetworkSceneManager>() == null)
            runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        if (runner.GetComponent<INetworkObjectProvider>() == null)
            runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
    }

    private void AttachLobbyCallbacks(NetworkRunner runner)
    {
        var bridge = runner.gameObject.GetComponent<LobbyRunnerCallbacks>();
        if (bridge == null)
            bridge = runner.gameObject.AddComponent<LobbyRunnerCallbacks>();
        runner.AddCallbacks(bridge);
        // LobbyRunnerCallbacks.SetManager is handled by LobbyRunnerCallbacks finding it or through interface if updated.
    }

    private static string GenerateRoomCode()
    {
        char[] chars = new char[ROOM_CODE_LENGTH];
        for (int i = 0; i < ROOM_CODE_LENGTH; i++)
            chars[i] = ROOM_CODE_CHARS[UnityEngine.Random.Range(0, ROOM_CODE_CHARS.Length)];
        return new string(chars);
    }

    private string GetNicknameOrDefault(TMP_InputField input)
    {
        if (input != null && !string.IsNullOrWhiteSpace(input.text))
            return input.text.Trim();
        return "Player";
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (confirmCreateButton != null) confirmCreateButton.interactable = interactable;
        if (confirmJoinButton != null) confirmJoinButton.interactable = interactable;
    }

    private void CleanupStaleRunners()
    {
        var existingRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (var runner in existingRunners)
        {
            if (runner == networkRunnerPrefab) continue;
            if (runner.IsRunning) _ = runner.Shutdown();
            Destroy(runner.gameObject);
        }

        if (NetworkConnectionMonitor.Instance != null)
            NetworkConnectionMonitor.Instance.Reset();
    }

    private void EnsureNotificationUI()
    {
        if (NetworkNotificationUI.Instance != null) return;
        if (notificationUIPrefab == null) return;
        Instantiate(notificationUIPrefab);
    }

    private void InitializeConnectionMonitor(GameMode gameMode)
    {
        if (NetworkConnectionMonitor.Instance == null)
        {
            var go = new GameObject("NetworkConnectionMonitor");
            go.AddComponent<NetworkConnectionMonitor>();
        }

        NetworkConnectionMonitor.Instance.Initialize(_runner, _lastSessionName, _lastNickname, gameMode, networkRunnerPrefab);
    }

    public void HandlePlayerJoined(string nickname)
    {
        // This can be used for UI notifications if needed.
    }

    #endregion
}