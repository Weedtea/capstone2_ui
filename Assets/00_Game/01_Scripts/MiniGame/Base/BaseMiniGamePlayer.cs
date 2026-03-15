using Fusion;
using UnityEngine;

/// <summary>
/// 미니게임용 네트워크 플레이어 베이스 클래스.
/// Host Authority 기반 Rigidbody 물리, 위치/회전 동기화, 보간, 탈락 상태를 공용으로 처리한다.
/// 파생 클래스는 ProcessTick()에서 게임별 입력/물리 로직을 구현한다.
/// </summary>
public abstract class BaseMiniGamePlayer : NetworkBehaviour
{
	[Networked] public NetworkBool IsEliminated { get; set; }
	[Networked] protected Vector3 NetPosition { get; set; }
	[Networked] protected Quaternion NetRotation { get; set; }

	protected Rigidbody _rb;

	[Header("Interpolation")]
	[SerializeField] private float interpolationSpeed = 20f;

	/// <summary>
	/// Fusion 스폰 시 호출. Rigidbody 초기화, 태그 설정, 비권한 클라이언트는 키네마틱 처리.
	/// </summary>
	public override void Spawned()
	{
		_rb = GetComponent<Rigidbody>();
		if (_rb != null)
			_rb.constraints = RigidbodyConstraints.FreezeRotation;

		gameObject.tag = "Player";

		if (!HasStateAuthority && _rb != null)
			_rb.isKinematic = true;

		OnPlayerSpawned();
	}

	/// <summary>
	/// Spawned 이후 파생 클래스에서 추가 초기화를 수행할 수 있다.
	/// </summary>
	protected virtual void OnPlayerSpawned() { }

	/// <summary>
	/// Fusion 틱마다 호출. Playing 여부, 탈락 여부를 확인한 뒤 게임별 로직을 실행한다.
	/// </summary>
	public override void FixedUpdateNetwork()
	{
		if (!HasStateAuthority) return;

		if (!IsGamePlaying())
		{
			SyncTransform();
			return;
		}

		if (IsEliminated)
		{
			SyncTransform();
			return;
		}

		ProcessTick();
		SyncTransform();
	}

	/// <summary>
	/// 게임이 Playing 상태인지 확인한다. BaseMiniGameManager가 없으면 Playing으로 간주.
	/// </summary>
	/// <returns>Playing 상태면 true</returns>
	protected virtual bool IsGamePlaying()
	{
		return BaseMiniGameManager.Instance == null || BaseMiniGameManager.Instance.IsPlaying;
	}

	/// <summary>
	/// 게임별 입력 처리, 이동, 판정 등을 수행한다. 파생 클래스에서 구현.
	/// HasStateAuthority, IsPlaying, IsEliminated 체크가 완료된 상태에서 호출된다.
	/// </summary>
	protected abstract void ProcessTick();

	/// <summary>
	/// 현재 Rigidbody 위치/회전을 네트워크 속성에 반영한다.
	/// </summary>
	protected void SyncTransform()
	{
		if (_rb != null)
		{
			NetPosition = _rb.position;
			NetRotation = _rb.rotation;
		}
	}

	/// <summary>
	/// 매 렌더 프레임마다 비권한 클라이언트의 오브젝트를 동기화 위치로 보간한다.
	/// </summary>
	public override void Render()
	{
		if (HasStateAuthority) return;

		float t = interpolationSpeed * Time.deltaTime;
		transform.position = Vector3.Lerp(transform.position, NetPosition, t);
		transform.rotation = Quaternion.Slerp(transform.rotation, NetRotation, t);
	}
}
