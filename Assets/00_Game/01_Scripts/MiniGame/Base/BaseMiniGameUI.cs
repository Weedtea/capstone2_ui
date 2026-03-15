using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 미니게임 UI 베이스 클래스.
/// 타이틀 드롭, 카운트다운, 시작, 탈락/승리 결과 연출을 공용으로 처리한다.
/// 파생 클래스는 GameTitleText 등을 오버라이드하여 게임별 문구를 지정한다.
/// </summary>
public abstract class BaseMiniGameUI : MonoBehaviour
{
	public static BaseMiniGameUI Instance { get; private set; }

	[Header("Title / Countdown")]
	[SerializeField] private RectTransform titleRect;
	[SerializeField] private TextMeshProUGUI titleLabel;
	[SerializeField] private RectTransform countdownRect;
	[SerializeField] private TextMeshProUGUI countdownLabel;

	[Header("Result")]
	[SerializeField] private RectTransform resultRect;
	[SerializeField] private TextMeshProUGUI resultLabel;

	[Header("Title Animation")]
	[SerializeField] private float dropStartOffsetY = 800f;
	[SerializeField] private float dropDuration = 0.8f;
	[SerializeField] private Ease dropEase = Ease.OutBounce;

	[Header("Countdown Animation")]
	[SerializeField] private float countdownPunchScale = 1.5f;
	[SerializeField] private float countdownAnimDuration = 0.4f;

	private Vector2 _titleOriginalPos;
	private Sequence _activeSequence;

	protected abstract string GameTitleText { get; } //게임 타이틀 텍스트. 파생 클래스에서 오버라이드.
	protected virtual string EliminatedText => "탈락!"; //탈락 시 표시할 텍스트.
	protected virtual Color EliminatedColor => new Color(1f, 0.3f, 0.3f); //탈락 시 텍스트 색상.
	protected virtual string WinText => "승리!"; //승리 시 표시할 텍스트.
	protected virtual Color WinColor => new Color(1f, 0.85f, 0f); //승리 시 텍스트 색상.

	/// <summary>
	/// 싱글턴 등록 및 UI 요소 초기화.
	/// </summary>
	protected virtual void Awake()
	{
		Instance = this;
		_titleOriginalPos = titleRect.anchoredPosition;

		titleLabel.gameObject.SetActive(false);
		countdownLabel.gameObject.SetActive(false);
		resultLabel.gameObject.SetActive(false);
	}

	/// <summary>
	/// 게임 타이틀이 화면 위에서 아래로 바운스하며 드롭된다.
	/// </summary>
	public void PlayTitleDrop()
	{
		titleLabel.gameObject.SetActive(true);
		titleLabel.text = GameTitleText;
		titleLabel.alpha = 0f;

		Vector2 startPos = _titleOriginalPos + Vector2.up * dropStartOffsetY;
		titleRect.anchoredPosition = startPos;
		titleRect.localScale = Vector3.one;

		_activeSequence?.Kill();
		_activeSequence = DOTween.Sequence()
			.Append(titleRect.DOAnchorPos(_titleOriginalPos, dropDuration).SetEase(dropEase))
			.Join(titleLabel.DOFade(1f, dropDuration * 0.3f))
			.Append(titleRect.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6));
	}

	/// <summary>
	/// 타이틀을 페이드아웃하고 카운트다운 텍스트를 표시한다.
	/// </summary>
	/// <param name="initialValue">최초 카운트다운 숫자</param>
	public void TransitionToCountdown(int initialValue)
	{
		_activeSequence?.Kill();

		if (titleLabel.gameObject.activeSelf)
		{
			_activeSequence = DOTween.Sequence()
				.Append(titleLabel.DOFade(0f, 0.3f))
				.AppendCallback(() =>
				{
					titleLabel.gameObject.SetActive(false);
					countdownLabel.gameObject.SetActive(true);
					AnimateCountdownValue(initialValue);
				});
		}
		else
		{
			countdownLabel.gameObject.SetActive(true);
			AnimateCountdownValue(initialValue);
		}
	}

	/// <summary>
	/// 카운트다운 숫자를 업데이트한다.
	/// </summary>
	/// <param name="value">남은 카운트다운 값</param>
	public void UpdateCountdown(int value)
	{
		AnimateCountdownValue(value);
	}

	/// <summary>
	/// "시작!" 텍스트를 잠시 보여준 뒤 페이드아웃한다.
	/// </summary>
	public void PlayGoAndHide()
	{
		_activeSequence?.Kill();
		_activeSequence = DOTween.Sequence()
			.AppendInterval(0.3f)
			.Append(countdownLabel.DOFade(0f, 0.5f))
			.AppendCallback(() => countdownLabel.gameObject.SetActive(false));
	}

	/// <summary>
	/// 로컬 플레이어가 탈락했을 때 탈락 텍스트를 표시한다.
	/// </summary>
	public void ShowEliminated()
	{
		ShowResultText(EliminatedText, EliminatedColor);
	}

	/// <summary>
	/// 게임 종료 시 결과를 표시한다. 승리자에게는 승리 텍스트를 표시.
	/// </summary>
	/// <param name="isWinner">로컬 플레이어가 승리자인지</param>
	public void ShowResult(bool isWinner)
	{
		if (isWinner)
			ShowResultText(WinText, WinColor);
	}

	/// <summary>
	/// 카운트다운 값에 맞는 텍스트와 색상을 설정하고 펀치 스케일 애니메이션을 재생한다.
	/// </summary>
	/// <param name="value">표시할 카운트다운 값</param>
	private void AnimateCountdownValue(int value)
	{
		bool isGo = value <= 0;
		countdownLabel.text = isGo ? "시작!" : value.ToString();
		countdownLabel.color = isGo ? new Color(1f, 0.85f, 0f) : Color.white;
		countdownLabel.alpha = 1f;

		countdownRect.DOKill();
		countdownRect.localScale = Vector3.one * countdownPunchScale;
		countdownRect.DOScale(1f, countdownAnimDuration).SetEase(Ease.OutBack);
	}

	/// <summary>
	/// 결과 텍스트를 펀치 스케일 + 페이드인 애니메이션으로 표시한다.
	/// </summary>
	/// <param name="text">표시할 텍스트</param>
	/// <param name="color">텍스트 색상</param>
	protected void ShowResultText(string text, Color color)
	{
		resultLabel.gameObject.SetActive(true);
		resultLabel.text = text;
		resultLabel.color = color;
		resultLabel.alpha = 0f;

		resultRect.localScale = Vector3.one * 2f;

		resultRect.DOKill();
		DOTween.Sequence()
			.Append(resultLabel.DOFade(1f, 0.3f))
			.Join(resultRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
	}

	/// <summary>
	/// 파괴 시 활성 시퀀스를 정리하고 싱글턴 참조를 해제한다.
	/// </summary>
	protected virtual void OnDestroy()
	{
		_activeSequence?.Kill();
		if (Instance == this)
			Instance = null;
	}
}
