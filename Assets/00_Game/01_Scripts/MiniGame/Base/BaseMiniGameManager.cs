using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 미니게임 공용 순서 상태 정의.
/// </summary>
public enum MiniGamePhase : int
{
	WaitingForPlayers = 0,
	TitleDrop = 1,
	Countdown = 2,
	Playing = 3,
	Finished = 4
}

/// <summary>
/// 미니게임 매니저 베이스 클래스.
/// TitleDrop -> Countdown -> Playing -> Finished 흐름, 로비 복귀, VoidGame을 공용으로 처리한다.
/// 파생 클래스는 CheckPlayingState()에서 게임별 승패 조건을 구현하고,
/// 종료 조건 충족 시 FinishGame()을 호출한다.
/// </summary>
public abstract class BaseMiniGameManager : MonoBehaviour
{
	public static BaseMiniGameManager Instance { get; private set; }

	protected const float RESULT_DISPLAY_DURATION = 3f;
	protected const string LOBBY_SCENE_NAME = "LobbyScene";

	[Header("Timing")]
	[SerializeField] private float titleDuration = 2f;
	[SerializeField] private float countdownInterval = 1f;
	[SerializeField] private float goDuration = 1f;

	protected MiniGamePhase _phase;
	protected int _countdownValue;
	private float _phaseTimer;

	public bool IsPlaying => _phase == MiniGamePhase.Playing; //현재 게임이 Playing 상태인지 반환.
	public MiniGamePhase CurrentPhase => _phase; //현재 순서 상태를 반환.

	/// <summary>
	/// 싱글턴 등록. 파생 클래스에서 오버라이드 시 base.Awake() 호출 필수.
	/// </summary>
	protected virtual void Awake()
	{
		Instance = this;
	}

	/// <summary>
	/// TitleDrop 연출을 시작한다. 파생 클래스에서 오버라이드 시 base.Start() 호출 필수.
	/// </summary>
	protected virtual void Start()
	{
		_phase = MiniGamePhase.TitleDrop;
		_countdownValue = 5;
		_phaseTimer = titleDuration;
		OnTitleDrop();
	}

	/// <summary>
	/// 매 프레임 Phase에 따라 타이머 갱신, 카운트다운 전환, Playing 상태 감시를 수행한다.
	/// </summary>
	protected virtual void Update()
	{
		if (_phase == MiniGamePhase.Finished)
			return;

		if (_phase == MiniGamePhase.Playing)
		{
			CheckPlayingState();
			return;
		}

		_phaseTimer -= Time.deltaTime;
		if (_phaseTimer > 0f) return;

		switch (_phase)
		{
			case MiniGamePhase.TitleDrop:
				EnterCountdown();
				break;
			case MiniGamePhase.Countdown:
				AdvanceCountdown();
				break;
		}
	}

	/// <summary>
	/// Playing 중 게임별 승패/종료 조건을 확인한다. 파생 클래스에서 구현.
	/// 종료 조건 충족 시 FinishGame()을 호출할 것.
	/// </summary>
	protected abstract void CheckPlayingState();

	/// <summary>
	/// 게임을 정상 종료한다. Finished로 전환 후 로비 복귀를 예약한다.
	/// </summary>
	protected void FinishGame()
	{
		if (_phase == MiniGamePhase.Finished)
			return;

		_phase = MiniGamePhase.Finished;
		OnGameFinished();

		var runner = FindFirstObjectByType<NetworkRunner>();
		if (runner != null && runner.IsServer)
			StartCoroutine(ReturnAllToLobby(runner));
	}

	/// <summary>
	/// 게임을 무효 처리한다. 플레이어 이탈 등 비정상 종료 시 호출.
	/// </summary>
	public void VoidGame()
	{
		if (_phase == MiniGamePhase.Finished)
			return;

		_phase = MiniGamePhase.Finished;
		OnGameVoided();
	}

	/// <summary>
	/// TitleDrop 종료 후 카운트다운 단계로 진입한다.
	/// </summary>
	private void EnterCountdown()
	{
		_phase = MiniGamePhase.Countdown;
		_countdownValue = 5;
		_phaseTimer = countdownInterval;
		OnCountdownStart(_countdownValue);
	}

	/// <summary>
	/// 카운트다운을 1 감소시킨다. 0 이하가 되면 Playing으로 전환한다.
	/// </summary>
	private void AdvanceCountdown()
	{
		_countdownValue--;

		if (_countdownValue < 0)
		{
			_phase = MiniGamePhase.Playing;
			OnPlayingStart();
		}
		else
		{
			_phaseTimer = _countdownValue == 0 ? goDuration : countdownInterval;
			OnCountdownUpdate(_countdownValue);
		}
	}

	/// <summary>
	/// 결과 표시 후 모든 플레이어를 로비 씬으로 복귀시킨다. 호스트에서만 실행.
	/// </summary>
	/// <param name="runner">현재 NetworkRunner</param>
	/// <returns>코루틴 열거자</returns>
	private IEnumerator ReturnAllToLobby(NetworkRunner runner)
	{
		yield return new WaitForSeconds(RESULT_DISPLAY_DURATION);

		if (runner != null && runner.IsServer)
		{
			var callbacks = runner.GetComponent<LobbyRunnerCallbacks>();
			if (callbacks != null)
				callbacks.BroadcastSceneChange(runner, LOBBY_SCENE_NAME);
		}

		yield return null;
		SceneManager.LoadScene(LOBBY_SCENE_NAME);
	}

	// ── Phase 전환 이벤트 (파생 클래스에서 오버라이드) ──────────────

	/// <summary>
	/// TitleDrop 연출 시점에 호출된다.
	/// </summary>
	protected virtual void OnTitleDrop() { }

	/// <summary>
	/// 카운트다운이 시작될 때 호출된다.
	/// </summary>
	/// <param name="initialValue">최초 카운트다운 값</param>
	protected virtual void OnCountdownStart(int initialValue) { }

	/// <summary>
	/// 카운트다운 값이 갱신될 때 호출된다.
	/// </summary>
	/// <param name="value">남은 카운트다운 값</param>
	protected virtual void OnCountdownUpdate(int value) { }

	/// <summary>
	/// Playing 상태 진입 시 호출된다.
	/// </summary>
	protected virtual void OnPlayingStart() { }

	/// <summary>
	/// 게임이 정상 종료될 때 호출된다.
	/// </summary>
	protected virtual void OnGameFinished() { }

	/// <summary>
	/// 게임이 무효 처리될 때 호출된다.
	/// </summary>
	protected virtual void OnGameVoided() { }

	/// <summary>
	/// 파괴 시 싱글턴 참조를 정리한다.
	/// </summary>
	protected virtual void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
}
