using UnityEngine;

/// <summary>
/// 밀치기 미니게임의 게임 매니저.
/// BaseMiniGameManager의 Phase 흐름을 상속하고,
/// 낙하 탈락 기반 승패 판정과 바닥 축소 제어를 수행한다.
/// </summary>
public class PushGameManager : BaseMiniGameManager
{
	private const float ELIMINATION_CHECK_INTERVAL = 0.25f;

	[Header("References")]
	[SerializeField] private GroundSmaller groundSmaller;

	private float _eliminationCheckTimer;
	private bool _hasShownEliminated;

	/// <summary>
	/// 바닥 축소를 비활성화하고 베이스 Start를 호출한다.
	/// </summary>
	protected override void Start()
	{
		if (groundSmaller != null)
			groundSmaller.SetActive(false);

		base.Start();
	}

	/// <summary>
	/// TitleDrop 연출을 시작한다.
	/// </summary>
	protected override void OnTitleDrop()
	{
		BaseMiniGameUI.Instance?.PlayTitleDrop();
	}

	/// <summary>
	/// 카운트다운 전환 시 UI를 갱신한다.
	/// </summary>
	/// <param name="initialValue">최초 카운트다운 값</param>
	protected override void OnCountdownStart(int initialValue)
	{
		BaseMiniGameUI.Instance?.TransitionToCountdown(initialValue);
	}

	/// <summary>
	/// 카운트다운 값 변경 시 UI를 갱신한다.
	/// </summary>
	/// <param name="value">남은 카운트다운 값</param>
	protected override void OnCountdownUpdate(int value)
	{
		BaseMiniGameUI.Instance?.UpdateCountdown(value);
	}

	/// <summary>
	/// Playing 진입 시 시작 연출 및 바닥 축소를 활성화한다.
	/// </summary>
	protected override void OnPlayingStart()
	{
		BaseMiniGameUI.Instance?.PlayGoAndHide();

		if (groundSmaller != null)
			groundSmaller.SetActive(true);
	}

	/// <summary>
	/// Playing 중 주기적으로 탈락 상태를 확인한다.
	/// 로컬 플레이어 탈락 시 UI를 표시하고, 생존자 1명 이하면 종료한다.
	/// </summary>
	protected override void CheckPlayingState()
	{
		_eliminationCheckTimer -= Time.deltaTime;
		if (_eliminationCheckTimer > 0f) return;
		_eliminationCheckTimer = ELIMINATION_CHECK_INTERVAL;

		var players = FindObjectsByType<MiniGameNetworkPlayer>(FindObjectsSortMode.None);
		if (players == null || players.Length == 0) return;

		int aliveCount = 0;
		bool isLocalAlive = false;
		bool isLocalEliminated = false;

		foreach (var player in players)
		{
			if (!player.IsEliminated)
			{
				aliveCount++;
				if (player.HasInputAuthority)
					isLocalAlive = true;
			}
			else if (player.HasInputAuthority)
			{
				isLocalEliminated = true;
			}
		}

		if (isLocalEliminated && !_hasShownEliminated)
		{
			_hasShownEliminated = true;
			BaseMiniGameUI.Instance?.ShowEliminated();
		}

		if (aliveCount <= 1)
		{
			BaseMiniGameUI.Instance?.ShowResult(isLocalAlive);
			FinishGame();
		}
	}

	/// <summary>
	/// 게임 종료 시 바닥 축소를 비활성화한다.
	/// </summary>
	protected override void OnGameFinished()
	{
		if (groundSmaller != null)
			groundSmaller.SetActive(false);
	}

	/// <summary>
	/// 게임 무효 시 바닥 축소를 비활성화한다.
	/// </summary>
	protected override void OnGameVoided()
	{
		if (groundSmaller != null)
			groundSmaller.SetActive(false);
	}
}
