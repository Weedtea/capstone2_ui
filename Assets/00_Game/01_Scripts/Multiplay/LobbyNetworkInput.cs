using Fusion;
using UnityEngine;

/// <summary>
/// LobbyScene에서 플레이어 이동 입력을 네트워크로 전달하기 위한 구조체.
/// </summary>
public struct LobbyNetworkInput : INetworkInput
{
	/// <summary>
	/// WASD 이동 방향
	/// </summary>
	public Vector2 MoveDirection;
}
