using Fusion;
using UnityEngine;

/// <summary>
/// LobbyScene에서 네트워크 동기화된 캐릭터 이동을 처리한다.
/// Fusion FixedUpdateNetwork 틱마다 입력을 읽어 NetworkCharacterController로 이동시킨다.
/// </summary>
[RequireComponent(typeof(NetworkCharacterController))]
public class LobbyPlayerController : NetworkBehaviour
{
	private NetworkCharacterController _ncc; // Fusion 캐릭터 컨트롤러 캐시

	/// <summary>
	/// Fusion이 오브젝트를 스폰한 직후 호출.
	/// </summary>
	public override void Spawned()
	{
		_ncc = GetComponent<NetworkCharacterController>();
	}

	/// <summary>
	/// Fusion 틱마다 호출. 입력을 읽어 캐릭터를 이동시킨다.
	/// </summary>
	public override void FixedUpdateNetwork()
	{
		Vector3 direction = Vector3.zero;

		if (GetInput(out LobbyNetworkInput input))
		{
			direction = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
		}

		_ncc.Move(direction);
	}
}
