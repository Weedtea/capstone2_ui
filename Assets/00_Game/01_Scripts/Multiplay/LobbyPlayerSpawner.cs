using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// LobbyScene에 배치하여 접속한 플레이어마다 네트워크 캐릭터를 스폰/디스폰하고,
/// 로컬 WASD 입력을 Fusion에 전달한다.
/// NetworkRunner에 콜백을 등록하며, 씬 파괴 시 자동 해제한다.
/// </summary>
public class LobbyPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
	[Header("Player")]
	[SerializeField] private NetworkPrefabRef playerPrefab;

	[Header("Spawn Points")]
	[SerializeField] private Transform[] spawnPoints;
	private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();   // PlayerRef -> 스폰된 NetworkObject 매핑

	private NetworkRunner _runner; // 현재 씬의 NetworkRunner 참조

	/// <summary>
	/// NetworkRunner를 찾아 콜백을 등록하고, 이미 접속한 플레이어를 스폰한다.
	/// </summary>
	private void Start()
	{
		_runner = FindObjectOfType<NetworkRunner>();
		if (_runner == null)
		{
			Debug.LogWarning("LobbyPlayerSpawner: NetworkRunner를 찾을 수 없습니다.");
			return;
		}

		_runner.AddCallbacks(this);
		SpawnExistingPlayers();
	}

	/// <summary>
	/// 파괴 시 NetworkRunner에서 콜백을 해제한다.
	/// </summary>
	private void OnDestroy()
	{
		if (_runner != null)
			_runner.RemoveCallbacks(this);
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
		Vector3 spawnPos = GetSpawnPosition(player);
		NetworkObject obj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
		if (obj != null)
			_spawnedPlayers[player] = obj;
	}

	/// <summary>
	/// 입장 순서에 따라 스폰 위치를 반환한다.
	/// spawnPoints 배열이 비어 있으면 원점 + 오프셋으로 폴백한다.
	/// </summary>
	/// <param name="player">위치를 결정할 PlayerRef</param>
	/// <returns>월드 스폰 좌표</returns>
	private Vector3 GetSpawnPosition(PlayerRef player)
	{
		if (spawnPoints != null && spawnPoints.Length > 0)
		{
			int index = _spawnedPlayers.Count % spawnPoints.Length;
			return spawnPoints[index].position;
		}

		float offset = _spawnedPlayers.Count * 2f;
		return new Vector3(offset, 0f, 0f);
	}

	// ── INetworkRunnerCallbacks ──────────────────────────────────────

	/// <summary>
	/// 플레이어 참여 시 호출. 호스트가 캐릭터를 스폰한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="player">참여한 PlayerRef</param>
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		if (!runner.IsServer)
			return;

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
		if (!runner.IsServer)
			return;

		if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
		{
			runner.Despawn(obj);
			_spawnedPlayers.Remove(player);
		}
	}

	/// <summary>
	/// 매 틱마다 로컬 WASD 입력을 수집하여 Fusion에 전달한다.
	/// </summary>
	/// <param name="runner">NetworkRunner</param>
	/// <param name="input">Fusion이 채울 NetworkInput</param>
	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		var data = new LobbyNetworkInput
		{
			MoveDirection = new Vector2(
				Input.GetAxisRaw("Horizontal"),
				Input.GetAxisRaw("Vertical")
			)
		};
		input.Set(data);
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
