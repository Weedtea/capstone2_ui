using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메뉴에서 멀티플레이(방 생성/참여) UI와 Photon Fusion 2 연동을 담당한다.
/// 닉네임 입력, 방 생성(랜덤 코드), 방 참여(코드 입력). 성공 시 LobbyScene으로 이동.
/// </summary>
public class MultiplayLobbyManager : MonoBehaviour, ILobbyManager
{
	private const int ROOM_CODE_LENGTH = 6;
	private const string ROOM_CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
	private const string LOBBY_SCENE_NAME = "LobbyScene";

	[Header("Runner")]
	[SerializeField] private NetworkRunner networkRunnerPrefab;

	[Header("Multiplay Panel")]
	[SerializeField] private GameObject multiplayPanel;
	[SerializeField] private TMP_InputField nicknameInput;
	[SerializeField] private Button createRoomButton;
	[SerializeField] private Button joinRoomButton;

	[Header("Join Room UI")]
	[SerializeField] private GameObject joinRoomSection;
	[SerializeField] private TMP_InputField joinCodeInput;
	[SerializeField] private Button confirmJoinButton;
	[SerializeField] private Button backFromJoinButton;

	[Header("Network UI")]
	[SerializeField] private NetworkNotificationUI notificationUIPrefab;

	private NetworkRunner _runner;
	private bool _isConnecting;
	private static bool _isLoadingLobby;
	private string _lastSessionName; // 마지막 접속 세션 이름 (ConnectionMonitor 전달용)
	private string _lastNickname;    // 마지막 사용 닉네임 (ConnectionMonitor 전달용)

	/// <summary>
	/// 버튼 클릭 리스너를 등록하고, 글로벌 알림 UI가 없으면 생성한다.
	/// 이전 세션에서 남은 NetworkRunner가 있으면 정리한다.
	/// </summary>
	private void Awake()
	{
		if (createRoomButton != null)
			createRoomButton.onClick.AddListener(OnCreateRoomClicked);
		if (joinRoomButton != null)
			joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
		if (confirmJoinButton != null)
			confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
		if (backFromJoinButton != null)
			backFromJoinButton.onClick.AddListener(HideJoinSection);

		EnsureNotificationUI();
		CleanupStaleRunners();
	}

	/// <summary>
	/// 파괴 시 러너를 종료한다. 로비 씬 전환 중이면 유지.
	/// </summary>
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

	/// <summary>
	/// 메뉴에서 멀티플레이 버튼을 눌렀을 때 호출.
	/// </summary>
	public void OpenMultiplayPanel()
	{
		if (multiplayPanel != null)
			multiplayPanel.SetActive(true);
		HideJoinSection();
	}

	/// <summary>
	/// 멀티플레이 패널을 닫는다.
	/// </summary>
	public void CloseMultiplayPanel()
	{
		if (multiplayPanel != null)
			multiplayPanel.SetActive(false);
	}

	/// <summary>
	/// 방 생성 버튼을 눌렀을 때 호출. 닉네임's room + 랜덤 코드로 방 생성.
	/// </summary>
	public async void OnCreateRoomClicked()
	{
		if (_isConnecting)
			return;

		string nickname = GetNicknameOrDefault();
		if (string.IsNullOrWhiteSpace(nickname))
		{
			Debug.LogWarning("닉네임을 입력해 주세요.");
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
			Debug.LogWarning("방 생성에 실패했습니다.");
	}

	/// <summary>
	/// 방 참여 버튼을 눌렀을 때 호출. 참여 코드 입력 UI 표시.
	/// </summary>
	public void OnJoinRoomClicked()
	{
		ShowJoinSection();
	}

	/// <summary>
	/// 참여 코드 입력 후 확인 버튼을 눌렀을 때 호출. 해당 코드로 접속.
	/// </summary>
	public async void OnConfirmJoinClicked()
	{
		if (_isConnecting)
			return;

		string nickname = GetNicknameOrDefault();
		if (string.IsNullOrWhiteSpace(nickname))
		{
			Debug.LogWarning("닉네임을 입력해 주세요.");
			return;
		}

		string code = joinCodeInput != null ? joinCodeInput.text?.Trim().ToUpperInvariant() : string.Empty;
		if (string.IsNullOrEmpty(code))
		{
			Debug.LogWarning("참여 코드를 입력해 주세요.");
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
			Debug.LogWarning("방 참여에 실패했습니다. 코드를 확인해 주세요.");
	}

	/// <summary>
	/// 다른 플레이어가 참여했을 때 LobbyRunnerCallbacks에서 호출. LobbyScene에서 참여자 목록으로 표시되므로 여기선 처리하지 않음.
	/// </summary>
	/// <param name="nickname">참여한 플레이어의 닉네임(또는 UserId)</param>
	public void HandlePlayerJoined(string nickname)
	{
	}

	/// <summary>
	/// Host로 세션 생성. Photon Cloud 사용 시 SessionName이 참여 코드가 됨.
	/// </summary>
	private async Task<bool> StartHostAsync(string roomDisplayName, string sessionName, string nickname)
	{
		if (networkRunnerPrefab == null)
		{
			Debug.LogError("MultiplayLobbyManager: NetworkRunner Prefab이 할당되지 않았습니다.");
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

	/// <summary>
	/// Client로 지정한 세션 코드로 참여.
	/// </summary>
	private async Task<bool> StartClientAsync(string sessionName, string nickname)
	{
		if (networkRunnerPrefab == null)
		{
			Debug.LogError("MultiplayLobbyManager: NetworkRunner Prefab이 할당되지 않았습니다.");
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

	/// <summary>
	/// 기존 Runner가 있으면 종료하고 파괴한다. 새 세션 시작 전 반드시 호출.
	/// </summary>
	/// <returns>비동기 Task</returns>
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

	/// <summary>
	/// 러너에 씬 매니저·오브젝트 프로바이더가 없으면 기본 컴포넌트를 붙인다.
	/// </summary>
	/// <param name="runner">대상 NetworkRunner</param>
	private void EnsureRunnerComponents(NetworkRunner runner)
	{
		if (runner.GetComponent<INetworkSceneManager>() == null)
			runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
		if (runner.GetComponent<INetworkObjectProvider>() == null)
			runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
	}

	/// <summary>
	/// 러너에 LobbyRunnerCallbacks를 붙이고, Fusion 콜백으로 등록한 뒤 매니저를 전달한다.
	/// </summary>
	/// <param name="runner">대상 NetworkRunner</param>
	private void AttachLobbyCallbacks(NetworkRunner runner)
	{
		var bridge = runner.gameObject.GetComponent<LobbyRunnerCallbacks>();
		if (bridge == null)
			bridge = runner.gameObject.AddComponent<LobbyRunnerCallbacks>();
		runner.AddCallbacks(bridge);
		bridge.SetManager(this);
	}

	/// <summary>
	/// 참여 코드로 쓸 랜덤 6자리 문자열을 생성한다.
	/// </summary>
	/// <returns>대문자+숫자 조합 6자리 코드</returns>
	private static string GenerateRoomCode()
	{
		char[] chars = new char[ROOM_CODE_LENGTH];
		for (int i = 0; i < ROOM_CODE_LENGTH; i++)
			chars[i] = ROOM_CODE_CHARS[UnityEngine.Random.Range(0, ROOM_CODE_CHARS.Length)];
		return new string(chars);
	}

	/// <summary>
	/// 닉네임 입력값을 반환하며, 비어 있으면 "Player"를 반환한다.
	/// </summary>
	/// <returns>닉네임 문자열</returns>
	private string GetNicknameOrDefault()
	{
		if (nicknameInput != null && !string.IsNullOrWhiteSpace(nicknameInput.text))
			return nicknameInput.text.Trim();
		return "Player";
	}

	/// <summary>
	/// 방 참여(코드 입력) UI를 표시하고, 메인 버튼들을 숨긴다.
	/// </summary>
	private void ShowJoinSection()
	{
		if (joinRoomSection != null)
			joinRoomSection.SetActive(true);
		if (joinCodeInput != null)
			joinCodeInput.text = string.Empty;

		SetMainButtonsVisible(false);
	}

	/// <summary>
	/// 방 참여 UI를 숨기고, 메인 버튼들을 다시 표시한다.
	/// </summary>
	private void HideJoinSection()
	{
		if (joinRoomSection != null)
			joinRoomSection.SetActive(false);

		SetMainButtonsVisible(true);
	}

	/// <summary>
	/// 메인 UI 요소(닉네임 입력, 방 생성/참여 버튼)의 표시 여부를 일괄 설정한다.
	/// </summary>
	/// <param name="visible">표시 여부</param>
	private void SetMainButtonsVisible(bool visible)
	{
		if (nicknameInput != null)
			nicknameInput.gameObject.SetActive(visible);
		if (createRoomButton != null)
			createRoomButton.gameObject.SetActive(visible);
		if (joinRoomButton != null)
			joinRoomButton.gameObject.SetActive(visible);
	}

	/// <summary>
	/// 방 생성/참여/확인 버튼의 interactable을 일괄 설정한다.
	/// </summary>
	/// <param name="interactable">활성화 여부</param>
	private void SetButtonsInteractable(bool interactable)
	{
		if (createRoomButton != null) createRoomButton.interactable = interactable;
		if (joinRoomButton != null) joinRoomButton.interactable = interactable;
		if (confirmJoinButton != null) confirmJoinButton.interactable = interactable;
	}

	/// <summary>
	/// 이전 세션에서 남아있는 DontDestroyOnLoad NetworkRunner를 찾아 정리한다.
	/// 재연결 실패 후 메뉴로 복귀했을 때 잔여 Runner가 새 접속을 방해하는 것을 방지.
	/// </summary>
	private void CleanupStaleRunners()
	{
		var existingRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
		foreach (var runner in existingRunners)
		{
			if (runner == networkRunnerPrefab)
				continue;

			Debug.Log($"[MultiplayLobbyManager] 잔여 NetworkRunner 정리: {runner.gameObject.name}");
			if (runner.IsRunning)
				_ = runner.Shutdown();
			Destroy(runner.gameObject);
		}

		if (NetworkConnectionMonitor.Instance != null)
			NetworkConnectionMonitor.Instance.Reset();
	}

	/// <summary>
	/// 글로벌 알림 UI 프리팹이 씬에 없으면 Instantiate한다.
	/// DontDestroyOnLoad로 유지되므로 최초 1회만 생성된다.
	/// </summary>
	private void EnsureNotificationUI()
	{
		if (NetworkNotificationUI.Instance != null)
			return;

		if (notificationUIPrefab == null)
		{
			Debug.LogWarning("MultiplayLobbyManager: NotificationUI Prefab이 할당되지 않았습니다.");
			return;
		}

		Instantiate(notificationUIPrefab);
	}

	/// <summary>
	/// NetworkConnectionMonitor를 생성하거나 기존 인스턴스를 갱신하여 세션 정보를 전달한다.
	/// </summary>
	/// <param name="gameMode">Host 또는 Client</param>
	private void InitializeConnectionMonitor(GameMode gameMode)
	{
		if (NetworkConnectionMonitor.Instance == null)
		{
			var go = new GameObject("NetworkConnectionMonitor");
			go.AddComponent<NetworkConnectionMonitor>();
		}

		NetworkConnectionMonitor.Instance.Initialize(_runner, _lastSessionName, _lastNickname, gameMode, networkRunnerPrefab);
	}
}
