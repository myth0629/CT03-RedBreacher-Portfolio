using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    [Header("SFX(효과음)")]
    [SerializeField] private AudioMixer SFX_AudioMixer;
    [SerializeField] private Slider SFX;
    
    [Header("BGM(배경음악)")]
    [SerializeField] private AudioMixer BGM_AudioMixer;
    [SerializeField] private Slider Music;

    private void Start()
    {
        if (Music != null)
        {
            Music.onValueChanged.AddListener(SetBGMVolume);
        }

        if (SFX != null)
        {
            SFX.onValueChanged.AddListener(SetSFXVolume);
        }

        ApplySavedSettings();
    }

    // 설정 바로저장
    private void ApplySavedSettings()
    {
        float bgmVolume = AudioVolumeSettings.GetSavedVolume(
            AudioVolumeSettings.BgmVolumePrefKey,
            AudioVolumeSettings.GetMixerNormalizedVolume(BGM_AudioMixer, AudioVolumeSettings.BgmMixerParameter));
        float sfxVolume = AudioVolumeSettings.GetSavedVolume(
            AudioVolumeSettings.SfxVolumePrefKey,
            AudioVolumeSettings.GetMixerNormalizedVolume(SFX_AudioMixer, AudioVolumeSettings.SfxMixerParameter));
        
        if (Music != null)
        {
            Music.SetValueWithoutNotify(bgmVolume);
        }

        if (SFX != null)
        {
            SFX.SetValueWithoutNotify(sfxVolume);
        }
        
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    public void SetBGMVolume(float volume)
    {
        AudioVolumeSettings.SetSavedVolume(
            BGM_AudioMixer,
            AudioVolumeSettings.BgmMixerParameter,
            AudioVolumeSettings.BgmVolumePrefKey,
            volume);
    }

    public void SetSFXVolume(float volume)
    {
        AudioVolumeSettings.SetSavedVolume(
            SFX_AudioMixer,
            AudioVolumeSettings.SfxMixerParameter,
            AudioVolumeSettings.SfxVolumePrefKey,
            volume);
    }
}

public static class AudioVolumeSettings
{
    public const string BgmVolumePrefKey = "Setting.BGMVolume";
    public const string SfxVolumePrefKey = "Setting.SFXVolume";
    public const string BgmMixerParameter = "BGMVolume";
    public const string SfxMixerParameter = "SFXVolume";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedSettingsAfterSceneLoad()
    {
        ApplySavedSettingsToSceneMixers();
    }

    public static void ApplySavedSettingsToSceneMixers()
    {
        float bgmVolume = GetSavedVolume(BgmVolumePrefKey);
        float sfxVolume = GetSavedVolume(SfxVolumePrefKey);

        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sources.Length; i++)
        {
            AudioMixer mixer = sources[i] != null && sources[i].outputAudioMixerGroup != null
                ? sources[i].outputAudioMixerGroup.audioMixer
                : null;
            ApplyVolumeToMixer(mixer, BgmMixerParameter, bgmVolume);
            ApplyVolumeToMixer(mixer, SfxMixerParameter, sfxVolume);
        }
    }

    public static float GetSavedVolume(string prefKey, float fallback = 1f)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(prefKey, fallback));
    }

    public static void SetSavedVolume(AudioMixer mixer, string parameterName, string prefKey, float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        ApplyVolumeToMixer(mixer, parameterName, clampedVolume);
        PlayerPrefs.SetFloat(prefKey, clampedVolume);
        PlayerPrefs.Save();
    }

    public static void ApplyVolumeToMixer(AudioMixer mixer, string parameterName, float volume)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        float dB = Mathf.Log10(Mathf.Max(0.0001f, Mathf.Clamp01(volume))) * 20f;
        mixer.SetFloat(parameterName, dB);
    }

    public static float GetMixerNormalizedVolume(AudioMixer mixer, string parameterName)
    {
        if (mixer != null && mixer.GetFloat(parameterName, out float currentVolume))
        {
            return Mathf.Pow(10f, currentVolume / 20f);
        }

        return 1f;
    }
}
