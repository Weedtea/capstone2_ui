using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// SettingsManager: 오디오, 디스플레이(전체화면), 게임 옵션(카메라 흔들림)을 제어하고 저장합니다.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("--- Audio Setup ---")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("--- UI Text Displays ---")]
    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("--- UI Buttons ---")]
    [SerializeField] private Button settingsOpenButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;

    [Header("--- Game & Display Settings ---")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Toggle cameraShakeToggle;

    [Header("--- Modal UI ---")]
    [SerializeField] private GameObject settingsPanel;

    // Camera Shake 상태를 다른 스크립트에서 쉽게 참조할 수 있도록 전역 변수(static) 제공
    public static bool IsCameraShakeEnabled { get; private set; } = true;

    // PlayerPrefs 키 값 정의
    private const string MASTER_VOL_KEY = "MasterVolume";
    private const string BGM_VOL_KEY = "BGMVolume";
    private const string SFX_VOL_KEY = "SFXVolume";
    private const string FULLSCREEN_KEY = "IsFullScreen";
    private const string CAMERASHAKE_KEY = "IsCameraShake";

    private void Awake()
    {
        // 1. 초기화 및 로드 로직
        LoadSettings();
    }

    private void Start()
    {
        // 슬라이더 조절 이벤트 연결
        if (masterSlider) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // 버튼 클릭 이벤트 연결
        if (settingsOpenButton) settingsOpenButton.onClick.AddListener(OpenSettings);
        if (closeButton) closeButton.onClick.AddListener(CloseSettings);
        if (resetButton) resetButton.onClick.AddListener(ResetToDefault);

        // 토글 이벤트 연결
        if (fullScreenToggle) fullScreenToggle.onValueChanged.AddListener(SetFullScreen);
        if (cameraShakeToggle) cameraShakeToggle.onValueChanged.AddListener(SetCameraShake);
    }

    #region 오디오 제어 로직 (0-100 스케일)
    public void SetMasterVolume(float value)
    {
        if (mainMixer)
        {
            float normalizedValue = Mathf.Clamp(value / 100f, 0.0001f, 1f);
            float dB = Mathf.Log10(normalizedValue) * 20f;
            mainMixer.SetFloat("Master", dB);
        }
        if (masterValueText) masterValueText.text = Mathf.RoundToInt(value).ToString() + "%";
        PlayerPrefs.SetFloat(MASTER_VOL_KEY, value);
    }

    public void SetBGMVolume(float value)
    {
        if (mainMixer)
        {
            float normalizedValue = Mathf.Clamp(value / 100f, 0.0001f, 1f);
            float dB = Mathf.Log10(normalizedValue) * 20f;
            mainMixer.SetFloat("BGM", dB);
        }
        if (bgmValueText) bgmValueText.text = Mathf.RoundToInt(value).ToString() + "%";
        PlayerPrefs.SetFloat(BGM_VOL_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        if (mainMixer)
        {
            float normalizedValue = Mathf.Clamp(value / 100f, 0.0001f, 1f);
            float dB = Mathf.Log10(normalizedValue) * 20f;
            mainMixer.SetFloat("SFX", dB);
        }
        if (sfxValueText) sfxValueText.text = Mathf.RoundToInt(value).ToString() + "%";
        PlayerPrefs.SetFloat(SFX_VOL_KEY, value);
    }
    #endregion

    #region 디스플레이 및 사양 제어 로직
    public void SetFullScreen(bool isFull)
    {
        // 전체 화면 모드 설정
        if (isFull)
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        else
            Screen.fullScreenMode = FullScreenMode.Windowed; // 창 모드 전환 시 사용자 조절 가능

        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFull ? 1 : 0);
    }

    public void SetCameraShake(bool isShake)
    {
        IsCameraShakeEnabled = isShake;
        PlayerPrefs.SetInt(CAMERASHAKE_KEY, isShake ? 1 : 0);
    }
    #endregion

    private void LoadSettings()
    {
        // 오디오 로드 (기본값 50)
        float master = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 50f);
        float bgm = PlayerPrefs.GetFloat(BGM_VOL_KEY, 50f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOL_KEY, 50f);

        if (masterSlider) masterSlider.value = master;
        if (bgmSlider) bgmSlider.value = bgm;
        if (sfxSlider) sfxSlider.value = sfx;

        SetMasterVolume(master);
        SetBGMVolume(bgm);
        SetSFXVolume(sfx);

        // 디스플레이 로드 (기본값 전체화면 On)
        bool isFull = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
        if (fullScreenToggle) fullScreenToggle.isOn = isFull;
        SetFullScreen(isFull);

        // 게임 옵션 로드 (기본값 설정 On)
        bool isShake = PlayerPrefs.GetInt(CAMERASHAKE_KEY, 1) == 1;
        if (cameraShakeToggle) cameraShakeToggle.isOn = isShake;
        SetCameraShake(isShake);
    }

    #region 공용 이벤트 핸들링 (Button Clicks)
    public void OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        LoadSettings();
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void ResetToDefault()
    {
        // 모든 설정을 초기 상태로 (볼륨 50, 전체화면 On, 흔들림 On)
        if (masterSlider) masterSlider.value = 50f;
        if (bgmSlider) bgmSlider.value = 50f;
        if (sfxSlider) sfxSlider.value = 50f;

        if (fullScreenToggle) fullScreenToggle.isOn = true;
        if (cameraShakeToggle) cameraShakeToggle.isOn = true;
        
        Debug.Log("모든 설정이 초기화되었습니다.");
    }
    #endregion
}
