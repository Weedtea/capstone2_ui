using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// 미니게임용 플레이어 Spawner 베이스 클래스.
/// 접속 플레이어의 스폰/디스폰, 콜백 등록, 기존 플레이어 스폰을 공용으로 처리한다.
/// 파생 클래스는 FillInput()만 구현하여 게임별 입력을 전달한다.
/// </summary>
public abstract class BaseMiniGamePlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
	[Header("Player")]
	[SerializeField] private NetworkPrefabRef playerPrefab;

	[Header("Spawn Points")]
	[SerializeField] private Transform[] spawnPoints;

	private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
	protected NetworkRunner _runner;

	protected virtual float FallbackSpawnY => 1f; //폴백 스폰 위치의 Y 오프셋. 파생 클래스에서 필요 시 오버라이드.

	/// <summary>
	/// NetworkRunner를 찾아 콜백을 등록하고, 이미 접속한 플레이어를 스폰한다.
	/// </summary>
	protected virtual void Start()
	{
		_runner = FindObjectOfType<NetworkRunner>();
		if (_runner == null)
		{
			Debug.LogWarning($"{GetType().Name}: NetworkRunner를 찾을 수 없습니다.");
			return;
		}

		_runner.AddCallbacks(this);
		SpawnExistingPlayers();
	}

	/// <summary>
	/// 파괴 시 NetworkRunner에서 콜백을 해제한다.
	/// </summary>
	protected virtual void OnDestroy()
	{
		if (_runner != null)
			_runner.RemoveCallbacks(this);
	}

	/// <summary>
	/// 게임별 입력을 수집하여 NetworkInput에 설정한다. 파생 클래스에서 구현.
	/// </summary>
	/// <param name="runner">현재 NetworkRunner</param>
	/// <param name="input">입력을 설정할 NetworkInput</param>
	protected abstract void FillInput(NetworkRunner runner, NetworkInput input);

	// ── INetworkRunnerCallbacks ──────────────────────────────────────

	/// <summary>
	/// 플레이어 참여 시 호출. 호스트가 캐릭터를 스폰한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">참여한 PlayerRef</param>
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		if (!runner.IsServer) return;
		if (!_spawnedPlayers.ContainsKey(player))
			SpawnPlayer(runner, player);
	}

	/// <summary>
	/// 플레이어 퇴장 시 호출. 호스트가 해당 캐릭터를 디스폰한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">퇴장한 PlayerRef</param>
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		if (!runner.IsServer) return;
		if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
		{
			runner.Despawn(obj);
			_spawnedPlayers.Remove(player);
		}
	}

	/// <summary>
	/// 매 틱마다 입력을 수집하여 Fusion에 전달한다. FillInput()으로 위임.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="input">Fusion이 채울 NetworkInput</param>
	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		FillInput(runner, input);
	}

	/// <summary>
	/// 씬 로드 시점에 이미 접속된 플레이어를 모두 스폰한다.
	/// </summary>
	private void SpawnExistingPlayers()
	{
		if (_runner == null || !_runner.IsServer)
			return;

		foreach (PlayerRef player in _runner.ActivePlayers)
		{
			if (!_spawnedPlayers.ContainsKey(player))
				SpawnPlayer(_runner, player);
		}
	}

	/// <summary>
	/// 지정 플레이어의 캐릭터를 스폰한다. 호스트에서만 호출.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">스폰할 PlayerRef</param>
	private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
	{
		Vector3 spawnPos = GetSpawnPosition(_spawnedPlayers.Count);
		NetworkObject obj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
		if (obj != null)
			_spawnedPlayers[player] = obj;
	}

	/// <summary>
	/// 입장 순서(현재 스폰 수)에 따라 스폰 위치를 반환한다.
	/// spawnPoints 배열이 비어 있으면 원점 + 오프셋으로 폴백한다.
	/// </summary>
	/// <param name="spawnIndex">현재까지 스폰된 플레이어 수 (0부터 시작)</param>
	/// <returns>월드 스폰 좌표</returns>
	protected virtual Vector3 GetSpawnPosition(int spawnIndex)
	{
		if (spawnPoints != null && spawnPoints.Length > 0)
		{
			int index = spawnIndex % spawnPoints.Length;
			return spawnPoints[index].position;
		}

		float offset = spawnIndex * 2f;
		return new Vector3(offset, FallbackSpawnY, 0f);
	}

	// ── 미사용 콜백 ────────────────────────────────

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
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
