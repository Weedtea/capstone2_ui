using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 연결 상태를 감시하고, 끊어진 경우 자동 재연결을 시도한다.
/// DontDestroyOnLoad 오브젝트로, MultiplayLobbyManager에서 세션 접속 성공 시 생성된다.
/// NetworkRunner와 별개의 GameObject에 존재하여, Runner가 파괴되어도 재연결을 시도할 수 있다.
/// </summary>
public class NetworkConnectionMonitor : MonoBehaviour, INetworkRunnerCallbacks
{
	private const int MAX_RETRY_COUNT = 5;
	private const float RETRY_INTERVAL = 3f;
	private const string MENU_SCENE_NAME = "MenuScene";

	public static NetworkConnectionMonitor Instance { get; private set; }

	private string _cachedSessionName;       // 재연결용 세션 이름
	private string _cachedNickname;          // 재연결용 닉네임

	private GameMode _cachedGameMode;        // Host 또는 Client
	private NetworkRunner _runner;           // 현재 활성 NetworkRunner 참조
	private NetworkRunner _runnerPrefab;     // 재연결 시 Instantiate할 프리팹 참조
	private bool _isReconnecting;            // 재연결 시도 중 여부
	private bool _isIntentionalShutdown;     // 의도적 종료(메뉴 이동 등)일 때 재연결 방지

	/// <summary>
	/// 싱글턴 등록. 중복 시 자신을 파괴한다.
	/// </summary>
	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	/// <summary>
	/// 세션 정보를 캐싱하고 현재 Runner에 콜백을 등록한다.
	/// MultiplayLobbyManager에서 세션 접속 성공 후 호출한다.
	/// </summary>
	/// <param name="runner">현재 NetworkRunner</param>
	/// <param name="sessionName">세션(방) 코드</param>
	/// <param name="nickname">로컬 플레이어 닉네임</param>
	/// <param name="gameMode">Host 또는 Client</param>
	/// <param name="runnerPrefab">재연결 시 사용할 NetworkRunner 프리팹. null이면 Resources에서 로드 시도.</param>
	public void Initialize(NetworkRunner runner, string sessionName, string nickname, GameMode gameMode, NetworkRunner runnerPrefab = null)
	{
		_runner = runner;
		_cachedSessionName = sessionName;
		_cachedNickname = nickname;
		_cachedGameMode = gameMode;
		_isIntentionalShutdown = false;
		_isReconnecting = false;

		if (runnerPrefab != null)
			_runnerPrefab = runnerPrefab;

		runner.AddCallbacks(this);
	}

	/// <summary>
	/// 의도적 종료임을 표시한다. 메뉴로 돌아가는 등 재연결이 불필요한 경우 호출.
	/// </summary>
	public void MarkIntentionalShutdown()
	{
		_isIntentionalShutdown = true;
	}

	/// <summary>
	/// 모든 코루틴을 중단하고 내부 상태를 초기화한다.
	/// 메뉴 씬에서 새 세션을 시작하기 전에 호출하여 이전 세션의 잔여 상태를 제거한다.
	/// </summary>
	public void Reset()
	{
		StopAllCoroutines();
		_isReconnecting = false;
		_isIntentionalShutdown = true;
		_runner = null;
	}

	/// <summary>
	/// 현재 Runner 참조를 갱신한다. 재연결 성공 시 사용.
	/// </summary>
	/// <param name="runner">새로운 NetworkRunner</param>
	public void UpdateRunner(NetworkRunner runner)
	{
		_runner = runner;
		runner.AddCallbacks(this);
	}

	/// <summary>
	/// 서버로부터 연결이 끊겼을 때 호출. 클라이언트 전용.
	/// 네트워크 끊김 아이콘을 표시하고 자동 재연결을 시도한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="reason">연결 해제 사유</param>
	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
	{
		if (_isIntentionalShutdown || _isReconnecting)
			return;

		Debug.Log($"[NetworkConnectionMonitor] 서버 연결 끊김: {reason}");

		if (NetworkNotificationUI.Instance != null)
			NetworkNotificationUI.Instance.ShowNetworkDisconnectIcon(true);

		StartCoroutine(AutoReconnectCoroutine());
	}

	/// <summary>
	/// Runner가 종료되었을 때 호출. 의도적 종료가 아니면 메뉴로 이동한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="shutdownReason">종료 사유</param>
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		if (_isIntentionalShutdown || _isReconnecting)
			return;

		Debug.Log($"[NetworkConnectionMonitor] Runner 종료: {shutdownReason}");

		if (NetworkNotificationUI.Instance != null)
		{
			NetworkNotificationUI.Instance.ShowNetworkDisconnectIcon(false);
			NetworkNotificationUI.Instance.ShowMessage("연결이 종료되었습니다.", Color.white, 3f);
		}

		StartCoroutine(ReturnToMenuDelayed(3f));
	}

	/// <summary>
	/// 자동 재연결을 최대 MAX_RETRY_COUNT회 시도한다.
	/// 성공 시 아이콘을 숨기고, 실패 시 메시지를 표시한 뒤 메뉴로 이동한다.
	/// </summary>
	/// <returns>코루틴 열거자</returns>
	private IEnumerator AutoReconnectCoroutine()
	{
		_isReconnecting = true;

		for (int attempt = 1; attempt <= MAX_RETRY_COUNT; attempt++)
		{
			Debug.Log($"[NetworkConnectionMonitor] 재연결 시도 {attempt}/{MAX_RETRY_COUNT}");
			yield return new WaitForSeconds(RETRY_INTERVAL);

			bool success = false;
			var task = TryReconnect();
			while (!task.IsCompleted)
				yield return null;

			success = task.Result;

			if (success)
			{
				Debug.Log("[NetworkConnectionMonitor] 재연결 성공");
				_isReconnecting = false;

				if (NetworkNotificationUI.Instance != null)
					NetworkNotificationUI.Instance.ShowNetworkDisconnectIcon(false);
				yield break;
			}
		}

		_isReconnecting = false;
		Debug.LogWarning("[NetworkConnectionMonitor] 재연결 실패 — 메뉴로 이동");

		if (NetworkNotificationUI.Instance != null)
		{
			NetworkNotificationUI.Instance.ShowNetworkDisconnectIcon(false);
			NetworkNotificationUI.Instance.ShowMessage("연결이 끊어졌습니다.", Color.white, 3f);
		}

		yield return new WaitForSeconds(3f);
		CleanupAndLoadMenu();
	}

	/// <summary>
	/// 캐싱된 세션 정보로 새 Runner를 생성하여 재접속을 시도한다.
	/// </summary>
	/// <returns>성공 여부</returns>
	private async System.Threading.Tasks.Task<bool> TryReconnect()
	{
		if (_runner != null && _runner.IsRunning)
			await _runner.Shutdown();

		if (_runner != null)
		{
			Destroy(_runner.gameObject);
			_runner = null;
		}

		var prefab = _runnerPrefab != null ? _runnerPrefab : Resources.Load<NetworkRunner>("NetworkRunner");
		if (prefab == null)
		{
			Debug.LogWarning("[NetworkConnectionMonitor] NetworkRunner 프리팹을 찾을 수 없어 재연결 실패");
			return false;
		}

		_runner = Instantiate(prefab);
		DontDestroyOnLoad(_runner.gameObject);

		EnsureRunnerComponents(_runner);
		_runner.AddCallbacks(this);

		var lobbyCallbacks = _runner.gameObject.GetComponent<LobbyRunnerCallbacks>();
		if (lobbyCallbacks == null)
			lobbyCallbacks = _runner.gameObject.AddComponent<LobbyRunnerCallbacks>();
		_runner.AddCallbacks(lobbyCallbacks);

		var sceneManager = _runner.GetComponent<INetworkSceneManager>();
		var objectProvider = _runner.GetComponent<INetworkObjectProvider>();
		var authValues = new AuthenticationValues(_cachedNickname);

		var args = new StartGameArgs
		{
			GameMode = _cachedGameMode,
			SessionName = _cachedSessionName,
			AuthValues = authValues,
			SceneManager = sceneManager,
			ObjectProvider = objectProvider
		};

		var result = await _runner.StartGame(args);
		if (!result.Ok)
		{
			Debug.LogWarning($"[NetworkConnectionMonitor] 재연결 StartGame 실패: {result.ShutdownReason}");
			if (_runner != null)
			{
				await _runner.Shutdown();
				Destroy(_runner.gameObject);
				_runner = null;
			}
			return false;
		}

		LobbyNicknameRegistry.Register(_runner.LocalPlayer, _cachedNickname);
		return true;
	}

	/// <summary>
	/// Runner에 씬 매니저와 오브젝트 프로바이더가 없으면 기본 컴포넌트를 추가한다.
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
	/// 일정 시간 후 메뉴 씬으로 이동하는 코루틴.
	/// </summary>
	/// <param name="delay">대기 시간(초)</param>
	/// <returns>코루틴 열거자</returns>
	private IEnumerator ReturnToMenuDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		CleanupAndLoadMenu();
	}

	/// <summary>
	/// Runner를 정리하고 메뉴 씬을 로드한다.
	/// 진행 중인 재연결 코루틴도 모두 중단한다.
	/// </summary>
	private void CleanupAndLoadMenu()
	{
		StopAllCoroutines();
		_isIntentionalShutdown = true;
		_isReconnecting = false;

		if (_runner != null && _runner.IsRunning)
			_ = _runner.Shutdown();
		if (_runner != null)
		{
			Destroy(_runner.gameObject);
			_runner = null;
		}

		LobbyNicknameRegistry.Clear();
		SceneManager.LoadScene(MENU_SCENE_NAME);
	}

	/// <summary>
	/// 파괴 시 싱글턴 참조를 정리한다.
	/// </summary>
	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	// ── INetworkRunnerCallbacks 미사용 메서드 ──────────────────────────

	/// <summary>
	/// INetworkRunnerCallbacks 미사용 메서드.
	/// </summary>
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
#pragma warning disable UNT0006
	public void OnConnectedToServer(NetworkRunner runner) { }
#pragma warning restore UNT0006
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
