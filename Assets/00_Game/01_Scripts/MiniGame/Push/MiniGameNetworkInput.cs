using Fusion;
using UnityEngine;

/// <summary>
/// 밀치기 미니게임의 네트워크 입력 구조체.
/// MiniGamePlayerSpawner에서 수집하여 Fusion에 전달된다.
/// </summary>
public struct MiniGameNetworkInput : INetworkInput
{
	public Vector2 MoveDirection; //WASD 이동 방향

	public NetworkBool DashPressed; //대시 입력 여부
}
