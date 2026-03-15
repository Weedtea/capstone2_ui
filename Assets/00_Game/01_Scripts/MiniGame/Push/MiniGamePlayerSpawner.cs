using Fusion;
using UnityEngine;

/// <summary>
/// 밀치기 미니게임용 플레이어 스포너.
/// BaseMiniGamePlayerSpawner를 상속하여 대시 입력을 포함한 입력을 수집한다.
/// </summary>
public class MiniGamePlayerSpawner : BaseMiniGamePlayerSpawner
{
	private bool _dashInput;

	/// <summary>
	/// 스페이스 키 입력을 감지하여 대시 플래그를 설정한다.
	/// </summary>
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
			_dashInput = true;
	}

	/// <summary>
	/// 이동/대시 입력을 수집하여 MiniGameNetworkInput으로 전달한다.
	/// </summary>
	/// <param name="runner">현재 NetworkRunner</param>
	/// <param name="input">입력을 설정할 NetworkInput</param>
	protected override void FillInput(NetworkRunner runner, NetworkInput input)
	{
		var data = new MiniGameNetworkInput
		{
			MoveDirection = new Vector2(
				Input.GetAxisRaw("Horizontal"),
				Input.GetAxisRaw("Vertical")
			),
			DashPressed = _dashInput
		};
		_dashInput = false;
		input.Set(data);
	}
}
