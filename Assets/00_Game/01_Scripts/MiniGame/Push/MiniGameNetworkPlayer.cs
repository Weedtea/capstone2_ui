using Fusion;
using UnityEngine;

/// <summary>
/// 밀치기 미니게임 네트워크 플레이어.
/// BaseMiniGamePlayer를 상속하여 대시, 넉백, 낙하 탈락 로직을 구현한다.
/// </summary>
public class MiniGameNetworkPlayer : BaseMiniGamePlayer
{
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 5f;

	[Header("Dash")]
	[SerializeField] private float dashPower = 10f;
	[SerializeField] private float dashDuration = 0.3f;
	[SerializeField] private float dashCooldown = 1f;

	[Header("Hit")]
	[SerializeField] private float hitPower = 5f;

	[Header("Fail")]
	[SerializeField] private float fallThreshold = -5f;

	[Networked] private TickTimer DashTimer { get; set; }
	[Networked] private TickTimer DashCooldownTimer { get; set; }
	[Networked] public NetworkBool IsDashing { get; set; }
	[Networked] public NetworkBool IsHitting { get; set; }

	/// <summary>
	/// 대시, 이동, 낙하 탈락 처리를 수행한다.
	/// </summary>
	protected override void ProcessTick()
	{
		if (IsDashing && DashTimer.Expired(Runner))
		{
			_rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
			IsDashing = false;
		}

		if (GetInput(out MiniGameNetworkInput input))
		{
			if (input.DashPressed && !IsDashing && DashCooldownTimer.ExpiredOrNotRunning(Runner))
			{
				IsDashing = true;
				DashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
				DashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
				_rb.AddForce(transform.forward * dashPower, ForceMode.VelocityChange);
			}

			if (!IsDashing && !IsHitting)
			{
				Vector3 moveDir = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
				Vector3 moveVel = moveDir * moveSpeed;
				_rb.linearVelocity = new Vector3(moveVel.x, _rb.linearVelocity.y, moveVel.z);

				if (moveDir.sqrMagnitude > 0.0001f)
					transform.rotation = Quaternion.LookRotation(moveDir);
			}
		}

		if (_rb.position.y < fallThreshold)
			IsEliminated = true;
	}

	/// <summary>
	/// Player 충돌 시 넉백 판정, Ground 충돌 시 넉백 해제.
	/// 탈락한 플레이어는 충돌을 무시한다.
	/// </summary>
	/// <param name="collision">충돌 정보</param>
	private void OnCollisionEnter(Collision collision)
	{
		if (!HasStateAuthority) return;
		if (IsEliminated) return;

		if (collision.gameObject.CompareTag("Player"))
		{
			var other = collision.gameObject.GetComponent<MiniGameNetworkPlayer>();
			if (other == null || other.IsEliminated) return;

			bool shouldKnockback = !IsDashing || (IsDashing && other.IsDashing);
			if (shouldKnockback)
			{
				Vector3 dir = (transform.position - collision.transform.position).normalized;
				dir.y = 1f;
				_rb.AddForce(dir * hitPower, ForceMode.Impulse);
				IsHitting = true;
			}
		}

		if (IsHitting && collision.gameObject.CompareTag("Ground"))
		{
			_rb.linearVelocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;
			IsHitting = false;
		}
	}
}
