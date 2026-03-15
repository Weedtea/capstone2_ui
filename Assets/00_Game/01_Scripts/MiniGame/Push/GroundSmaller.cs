using UnityEngine;

/// <summary>
/// 밀치기 미니게임의 바닥 관리
/// </summary>
public class GroundSmaller : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private float smallerSpeed = 0.01f;

	private bool _isActive; // 축소 활성화 여부

	/// <summary>
	/// 바닥 축소를 활성화하거나 비활성화한다.
	/// </summary>
	/// <param name="active">true면 축소 시작</param>
	public void SetActive(bool active)
	{
		_isActive = active;
	}

	/// <summary>
	/// 매 물리 프레임마다 바닥 크기를 줄인다. 최소 크기 이하면 무시한다.
	/// </summary>
	private void FixedUpdate()
	{
		if (!_isActive) return;
		if (transform.localScale.x <= 0 || transform.localScale.z <= 0) return;
		transform.localScale -= new Vector3(smallerSpeed, 0, smallerSpeed);
	}
}
