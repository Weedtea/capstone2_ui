using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 모든 씬에서 동작하는 DontDestroyOnLoad 글로벌 알림 UI.
/// 상단 중앙에 알림 메시지, 우측 상단에 네트워크 끊김 아이콘을 표시한다.
/// Unity 에디터에서 프리팹으로 만들고, Inspector에서 참조를 연결한다.
/// </summary>
public class NetworkNotificationUI : MonoBehaviour
{
	private const float DEFAULT_DURATION = 3f;
	private const float FADE_DURATION = 0.5f;

	public static NetworkNotificationUI Instance { get; private set; }

	[Header("메시지 (상단 중앙)")]
	[SerializeField] private CanvasGroup messageGroup;     // 메시지 영역 CanvasGroup (페이드 제어)
	[SerializeField] private TextMeshProUGUI messageText;  // 알림 텍스트

	[Header("네트워크 끊김 아이콘 (우측 상단)")]
	[SerializeField] private GameObject disconnectIconRoot; // 끊김 아이콘 루트 오브젝트

	private Coroutine _messageCoroutine;

	/// <summary>
	/// 싱글턴 등록. 중복 시 자신을 파괴한다.
	/// </summary>
	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (messageGroup != null)
			messageGroup.gameObject.SetActive(false);
		if (disconnectIconRoot != null)
			disconnectIconRoot.SetActive(false);
	}

	/// <summary>
	/// 상단 중앙에 알림 메시지를 표시한다. duration 후 자동 페이드아웃.
	/// </summary>
	/// <param name="text">표시할 메시지</param>
	/// <param name="color">텍스트 색상</param>
	/// <param name="duration">표시 시간(초). 0 이하면 기본값 사용</param>
	public void ShowMessage(string text, Color color, float duration = DEFAULT_DURATION)
	{
		if (messageText == null || messageGroup == null) return;

		messageText.text = text;
		messageText.color = color;
		messageGroup.alpha = 1f;
		messageGroup.gameObject.SetActive(true);

		if (_messageCoroutine != null)
			StopCoroutine(_messageCoroutine);
		_messageCoroutine = StartCoroutine(FadeOutAfter(duration));
	}

	/// <summary>
	/// 우측 상단 네트워크 끊김 아이콘의 표시 여부를 설정한다.
	/// </summary>
	/// <param name="show">true면 표시, false면 숨김</param>
	public void ShowNetworkDisconnectIcon(bool show)
	{
		if (disconnectIconRoot != null)
			disconnectIconRoot.SetActive(show);
	}

	/// <summary>
	/// 메시지를 즉시 숨긴다.
	/// </summary>
	public void HideMessage()
	{
		if (_messageCoroutine != null)
		{
			StopCoroutine(_messageCoroutine);
			_messageCoroutine = null;
		}

		if (messageGroup != null)
			messageGroup.gameObject.SetActive(false);
	}

	/// <summary>
	/// 일정 시간 후 메시지를 페이드아웃하는 코루틴.
	/// </summary>
	/// <param name="duration">페이드 시작까지 대기 시간</param>
	/// <returns>코루틴 열거자</returns>
	private IEnumerator FadeOutAfter(float duration)
	{
		yield return new WaitForSeconds(duration);

		float elapsed = 0f;
		while (elapsed < FADE_DURATION)
		{
			elapsed += Time.unscaledDeltaTime;
			messageGroup.alpha = 1f - (elapsed / FADE_DURATION);
			yield return null;
		}

		messageGroup.gameObject.SetActive(false);
		_messageCoroutine = null;
	}

	/// <summary>
	/// 파괴 시 싱글턴 참조를 정리한다.
	/// </summary>
	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
}
