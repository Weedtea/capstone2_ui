using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkRunner에 붙여서 콜백을 MultiplayLobbyManager로 전달한다.
/// 호스트는 플레이어 입/퇴장 시 닉네임 맵을 ReliableData로 전체 브로드캐스트한다.
/// 호스트가 씬 전환을 요청하면 모든 클라이언트에게 ReliableData로 씬 이름을 전송한다.
/// 게임 중 플레이어 이탈 시 인원 수에 따라 게임 무효 또는 퇴장 알림을 처리한다.
/// </summary>
[RequireComponent(typeof(NetworkRunner))]
public class LobbyRunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
	private const string LOBBY_SCENE_NAME = "LobbyScene";
	private const string MENU_SCENE_NAME = "MenuScene";	private const string TITLE_SCENE_NAME = "Title";

	private const float VOID_GAME_RETURN_DELAY = 3f;

	public static readonly ReliableKey SCENE_CHANGE_KEY = ReliableKey.FromInts(2, 0, 0, 0);
	public static readonly ReliableKey GAME_IN_PROGRESS_KEY = ReliableKey.FromInts(3, 0, 0, 0);
	public static readonly ReliableKey VOID_GAME_KEY = ReliableKey.FromInts(4, 0, 0, 0);
	public static readonly ReliableKey PLAYER_LEFT_KEY = ReliableKey.FromInts(5, 0, 0, 0);

	private ILobbyManager _manager;
	private Coroutine _returnToLobbyCoroutine;

	/// <summary>
	/// 로비 매니저를 설정한다. 방 생성/참여 시 러너에 붙은 뒤 호출.
	/// </summary>
	/// <param name="manager">콜백을 받을 매니저</param>
	public void SetManager(ILobbyManager manager)
	{
		_manager = manager;
	}

	/// <summary>
	/// 플레이어 참여 시 호출.
	/// 호스트: 게임 진행 중이면 접속을 거부하고, 아니면 닉네임 등록 후 브로드캐스트.
	/// 클라이언트: 매니저에 알림만 전달.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">참여한 PlayerRef</param>
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		if (runner.IsServer)
		{
			string nickname = runner.GetPlayerUserId(player);
			if (!string.IsNullOrEmpty(nickname))
				LobbyNicknameRegistry.Register(player, nickname);

			if (IsInGameScene())
			{
				RejectLateJoiner(runner, player);
				return;
			}

			BroadcastNicknames(runner);
		}

		if (_manager != null && player != runner.LocalPlayer)
		{
			string nickname = LobbyNicknameRegistry.GetNickname(player)
				?? runner.GetPlayerUserId(player)
				?? "Unknown";
			_manager.HandlePlayerJoined(nickname);
		}
	}

	/// <summary>
	/// 플레이어 퇴장 시 호출.
	/// 로비 씬이면 기존 처리, 게임 씬이면 인원 수에 따라 게임 무효 또는 퇴장 알림.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">퇴장한 PlayerRef</param>
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		string leftNickname = LobbyNicknameRegistry.GetNickname(player) ?? "Unknown";

		LobbyNicknameRegistry.Remove(player);

		if (IsInGameScene())
		{
			HandlePlayerLeftDuringGame(runner, player, leftNickname);
			return;
		}

		if (runner.IsServer)
			BroadcastNicknames(runner);
	}

	/// <summary>
	/// ReliableData 수신 콜백. 닉네임, 씬 전환, 게임 진행 중 거부, 게임 무효, 퇴장 알림을 처리한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">송신자 PlayerRef</param>
	/// <param name="key">데이터 식별 키</param>
	/// <param name="data">수신된 바이트 데이터</param>
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
	{
		if (key == LobbyNicknameRegistry.NICKNAME_KEY)
		{
			LobbyNicknameRegistry.DeserializeAll(data);
		}
		else if (key == SCENE_CHANGE_KEY)
		{
			string sceneName = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
			SceneManager.LoadScene(sceneName);
		}
		else if (key == GAME_IN_PROGRESS_KEY)
		{
			HandleGameInProgressReject(runner);
		}
		else if (key == VOID_GAME_KEY)
		{
			HandleVoidGameReceived();
		}
		else if (key == PLAYER_LEFT_KEY)
		{
			string nickname = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
			HandlePlayerLeftNotification(nickname);
		}
	}

	/// <summary>
	/// 호스트가 모든 클라이언트에게 씬 전환 시그널을 전송한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="sceneName">이동할 씬 이름</param>
	public void BroadcastSceneChange(NetworkRunner runner, string sceneName)
	{
		byte[] payload = Encoding.UTF8.GetBytes(sceneName);
		foreach (PlayerRef p in runner.ActivePlayers)
		{
			if (p == runner.LocalPlayer)
				continue;
			runner.SendReliableDataToPlayer(p, SCENE_CHANGE_KEY, payload);
		}
	}

	/// <summary>
	/// 현재 활성 씬이 게임(미니게임) 씬인지 판별한다.
	/// LobbyScene이 아니고 MenuScene이 아니면 게임 씬으로 간주한다.
	/// </summary>
	/// <returns>게임 씬이면 true</returns>
	private bool IsInGameScene()
	{
		string currentScene = SceneManager.GetActiveScene().name;
		if (currentScene == LOBBY_SCENE_NAME) return false;
		if (currentScene == MENU_SCENE_NAME) return false;
		if (currentScene == TITLE_SCENE_NAME) return false;
		return true;
	}

	/// <summary>
	/// 게임 진행 중 접속한 플레이어를 거부한다.
	/// GAME_IN_PROGRESS_KEY를 전송하고 서버에서 연결을 해제한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">거부할 PlayerRef</param>
	private void RejectLateJoiner(NetworkRunner runner, PlayerRef player)
	{
		Debug.Log($"[LobbyRunnerCallbacks] 게임 진행 중 접속 거부: {player}");
		runner.SendReliableDataToPlayer(player, GAME_IN_PROGRESS_KEY, Array.Empty<byte>());
		runner.Disconnect(player);
	}

	/// <summary>
	/// 게임 중 플레이어 이탈 시 남은 인원 수에 따라 분기 처리한다.
	/// 1명 남음(1v1): 게임 무효. 2명 이상 남음(3+): 퇴장 알림.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">이탈한 PlayerRef</param>
	/// <param name="leftNickname">이탈한 플레이어 닉네임</param>
	private void HandlePlayerLeftDuringGame(NetworkRunner runner, PlayerRef player, string leftNickname)
	{
		int remainingCount = runner.ActivePlayers.Count();

		Debug.Log($"[LobbyRunnerCallbacks] 게임 중 플레이어 이탈: {leftNickname}, 남은 인원: {remainingCount}");

		if (remainingCount <= 1)
		{
			HandleVoidGame1v1(runner, leftNickname);
		}
		else
		{
			HandlePlayerLeftMulti(runner, leftNickname);
		}

		if (runner.IsServer)
			BroadcastNicknames(runner);
	}

	/// <summary>
	/// 1v1 게임에서 상대가 이탈했을 때 처리.
	/// 호스트는 VOID_GAME_KEY를 브로드캐스트하고 로컬에서도 게임을 무효 처리한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="leftNickname">이탈한 플레이어 닉네임</param>
	private void HandleVoidGame1v1(NetworkRunner runner, string leftNickname)
	{
		if (runner.IsServer)
		{
			foreach (PlayerRef p in runner.ActivePlayers)
			{
				if (p == runner.LocalPlayer)
					continue;
				runner.SendReliableDataToPlayer(p, VOID_GAME_KEY, Array.Empty<byte>());
			}
		}

		HandleVoidGameReceived();
	}

	/// <summary>
	/// 3인 이상 게임에서 한 명이 이탈했을 때 처리.
	/// 호스트는 PLAYER_LEFT_KEY에 닉네임을 담아 브로드캐스트한다.
	/// 호스트 자신도 로컬에서 퇴장 알림을 표시한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="leftNickname">이탈한 플레이어 닉네임</param>
	private void HandlePlayerLeftMulti(NetworkRunner runner, string leftNickname)
	{
		if (runner.IsServer)
		{
			byte[] payload = Encoding.UTF8.GetBytes(leftNickname);
			foreach (PlayerRef p in runner.ActivePlayers)
			{
				if (p == runner.LocalPlayer)
					continue;
				runner.SendReliableDataToPlayer(p, PLAYER_LEFT_KEY, payload);
			}
		}

		HandlePlayerLeftNotification(leftNickname);
	}

	/// <summary>
	/// "게임이 시작된 방입니다." 메시지를 표시하고 연결을 해제한 뒤 메뉴로 이동한다.
	/// 클라이언트에서 GAME_IN_PROGRESS_KEY 수신 시 호출된다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	private void HandleGameInProgressReject(NetworkRunner runner)
	{
		Debug.Log("[LobbyRunnerCallbacks] 게임이 시작된 방 — 접속 거부됨");

		if (NetworkNotificationUI.Instance != null)
			NetworkNotificationUI.Instance.ShowMessage("게임이 시작된 방입니다.", Color.white, 3f);

		if (NetworkConnectionMonitor.Instance != null)
			NetworkConnectionMonitor.Instance.MarkIntentionalShutdown();

		StartCoroutine(ShutdownAndLoadMenu(runner, 3f));
	}

	/// <summary>
	/// "비정상적인 접근입니다." 메시지를 표시하고 게임을 무효 처리한 뒤 로비로 이동한다.
	/// VOID_GAME_KEY 수신 시 또는 1v1 이탈 감지 시 호출된다.
	/// </summary>
	private void HandleVoidGameReceived()
	{
		Debug.Log("[LobbyRunnerCallbacks] 게임 무효 — 대기실로 이동");

		if (PushGameManager.Instance != null)
			PushGameManager.Instance.VoidGame();

		if (NetworkNotificationUI.Instance != null)
			NetworkNotificationUI.Instance.ShowMessage("비정상적인 접근입니다.", Color.white, VOID_GAME_RETURN_DELAY);

		if (_returnToLobbyCoroutine != null)
			StopCoroutine(_returnToLobbyCoroutine);
		_returnToLobbyCoroutine = StartCoroutine(ReturnToLobbyDelayed(VOID_GAME_RETURN_DELAY));
	}

	/// <summary>
	/// "닉네임 님이 접속을 종료하였습니다." 빨간색 메시지를 표시한다.
	/// PLAYER_LEFT_KEY 수신 시 호출된다. 게임은 계속 진행된다.
	/// </summary>
	/// <param name="nickname">이탈한 플레이어 닉네임</param>
	private void HandlePlayerLeftNotification(string nickname)
	{
		string message = $"{nickname} 님이 접속을 종료하였습니다.";
		Debug.Log($"[LobbyRunnerCallbacks] {message}");

		if (NetworkNotificationUI.Instance != null)
			NetworkNotificationUI.Instance.ShowMessage(message, Color.red, 5f);
	}

	/// <summary>
	/// 일정 시간 후 로비 씬으로 이동하는 코루틴.
	/// </summary>
	/// <param name="delay">대기 시간(초)</param>
	/// <returns>코루틴 열거자</returns>
	private IEnumerator ReturnToLobbyDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		SceneManager.LoadScene(LOBBY_SCENE_NAME);
		_returnToLobbyCoroutine = null;
	}

	/// <summary>
	/// Runner를 종료하고 일정 시간 후 메뉴 씬으로 이동하는 코루틴.
	/// </summary>
	/// <param name="runner">종료할 NetworkRunner</param>
	/// <param name="delay">대기 시간(초)</param>
	/// <returns>코루틴 열거자</returns>
	private IEnumerator ShutdownAndLoadMenu(NetworkRunner runner, float delay)
	{
		yield return new WaitForSeconds(delay);

		if (runner != null && runner.IsRunning)
			_ = runner.Shutdown();

		LobbyNicknameRegistry.Clear();
		SceneManager.LoadScene(MENU_SCENE_NAME);
	}

	/// <summary>
	/// 레지스트리의 전체 닉네임 맵을 모든 원격 플레이어에게 전송한다. 호스트 전용.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	private void BroadcastNicknames(NetworkRunner runner)
	{
		byte[] payload = LobbyNicknameRegistry.SerializeAll();
		foreach (PlayerRef p in runner.ActivePlayers)
		{
			if (p == runner.LocalPlayer)
				continue;
			runner.SendReliableDataToPlayer(p, LobbyNicknameRegistry.NICKNAME_KEY, payload);
		}
	}

	/// <summary>
	/// INetworkRunnerCallbacks 미사용 메서드.
	/// </summary>
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
#pragma warning disable UNT0006
	public void OnConnectedToServer(NetworkRunner runner) { }
	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
#pragma warning restore UNT0006
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
