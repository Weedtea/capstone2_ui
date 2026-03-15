using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LobbyScene에서 상단에 참여 코드, 하단에 참여자 닉네임을 표시한다.
/// 호스트에게만 게임 시작 버튼을 보여주고, 클릭 시 모든 플레이어를 미니게임 씬으로 이동시킨다.
/// </summary>
public class LobbySceneUI : MonoBehaviour
{
	private const string MINIGAME_SCENE_NAME = "MiniGameTest";

	[Header("참여 코드")]
	[SerializeField] private TextMeshProUGUI roomCodeTxt;

	[Header("참여자 닉네임")]
	[SerializeField] private TextMeshProUGUI player1Text;
	[SerializeField] private TextMeshProUGUI player2Text;
	[SerializeField] private TextMeshProUGUI player3Text;
	[SerializeField] private TextMeshProUGUI player4Text;

	[Header("게임 시작")]
	[SerializeField] private Button startGameButton;

	private NetworkRunner _runner;
	private float _refreshTimer;           // 참여자 목록 갱신 타이머
	private const float REFRESH_INTERVAL = 0.5f;
	private bool _isStartButtonBound;      // 시작 버튼 리스너 등록 여부

	/// <summary>
	/// NetworkRunner를 찾고 게임 시작 버튼을 숨긴다.
	/// </summary>
	private void Awake()
	{
		_runner = FindObjectOfType<NetworkRunner>();

		if (startGameButton != null)
			startGameButton.gameObject.SetActive(false);
	}

	/// <summary>
	/// 매 프레임 러너 상태를 확인하고, 시작 버튼 표시 및 참여자 목록을 주기적으로 갱신한다.
	/// </summary>
	private void Update()
	{
		if (_runner == null) _runner = FindObjectOfType<NetworkRunner>();

		if (_runner == null || !_runner.IsRunning)
			return;

		UpdateStartButtonVisibility();

		_refreshTimer += Time.deltaTime;
		if (_refreshTimer < REFRESH_INTERVAL)
			return;
		_refreshTimer = 0f;

		RefreshRoomCode();
		RefreshPlayerList();
	}

	/// <summary>
	/// 호스트에게만 게임 시작 버튼을 표시하고, 최초 1회 클릭 리스너를 등록한다.
	/// </summary>
	private void UpdateStartButtonVisibility()
	{
		if (startGameButton == null) return;

		bool isHost = _runner.IsServer;
		if (startGameButton.gameObject.activeSelf != isHost)
			startGameButton.gameObject.SetActive(isHost);

		if (isHost && !_isStartButtonBound)
		{
			startGameButton.onClick.AddListener(OnStartGameClicked);
			_isStartButtonBound = true;
		}
	}

	/// <summary>
	/// 게임 시작 버튼 클릭 시 호출. 모든 클라이언트에 씬 전환을 브로드캐스트하고 미니게임 씬을 로드한다.
	/// </summary>
	private void OnStartGameClicked()
	{
		if (_runner == null || !_runner.IsServer) return;

		startGameButton.interactable = false;

		var callbacks = _runner.GetComponent<LobbyRunnerCallbacks>();
		if (callbacks != null)
			callbacks.BroadcastSceneChange(_runner, MINIGAME_SCENE_NAME);

		SceneManager.LoadScene(MINIGAME_SCENE_NAME);
	}

	/// <summary>
	/// 상단 참여 코드(roomCodeTxt)를 러너 세션 이름으로 갱신한다.
	/// </summary>
	private void RefreshRoomCode()
	{
		if (roomCodeTxt == null)
			return;
		if (_runner.SessionInfo.IsValid)
			roomCodeTxt.text = _runner.SessionInfo.Name;
		else
			roomCodeTxt.text = string.Empty;
	}

	/// <summary>
	/// 하단 Player1~4에 접속한 플레이어 닉네임을 순서대로 채운다. 없으면 빈 문자열.
	/// </summary>
	private void RefreshPlayerList()
	{
		TextMeshProUGUI[] playerTexts = { player1Text, player2Text, player3Text, player4Text };
		int index = 0;
		foreach (PlayerRef player in _runner.ActivePlayers)
		{
			if (index >= playerTexts.Length)
				break;

			// 레지스트리 우선 조회, 없으면 GetPlayerUserId fallback
			string nickname = LobbyNicknameRegistry.GetNickname(player);
			if (string.IsNullOrEmpty(nickname))
				nickname = _runner.GetPlayerUserId(player);
			if (string.IsNullOrEmpty(nickname))
				nickname = "Unknown";

			if (playerTexts[index] != null)
				playerTexts[index].text = nickname;
			index++;
		}
		for (; index < playerTexts.Length; index++)
		{
			if (playerTexts[index] != null)
				playerTexts[index].text = string.Empty;
		}
	}
}
