using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    private const string BgmVolumePrefKey = "Setting.BGMVolume";
    private const string SfxVolumePrefKey = "Setting.SFXVolume";
    
    [Header("SFX(효과음)")]
    [SerializeField] private AudioMixer SFX_AudioMixer;
    [SerializeField] private Slider SFX;
    
    [Header("BGM(배경음악)")]
    [SerializeField] private AudioMixer BGM_AudioMixer;
    [SerializeField] private Slider Music;

    private void Start()
    {
        Music.onValueChanged.AddListener(SetBGMVolume);
        SFX.onValueChanged.AddListener(SetSFXVolume);

        ApplySavedSettings();
    }

    // 설정 바로저장
    private void ApplySavedSettings()
    {
        float bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumePrefKey, GetMixerNormalizedVolume(BGM_AudioMixer, "BGMVolume")));
        float sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumePrefKey, GetMixerNormalizedVolume(SFX_AudioMixer, "SFXVolume")));
        
        Music.SetValueWithoutNotify(bgmVolume);
        SFX.SetValueWithoutNotify(sfxVolume);
        
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }
    
    private float GetMixerNormalizedVolume(AudioMixer mixer, string parameterName)
    {
        if (mixer != null && mixer.GetFloat(parameterName, out float currentVolume))
        {
            return Mathf.Pow(10f, currentVolume / 20f);
        }

        return 1f;
    }
    
    public void SetBGMVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        float dB = Mathf.Log10(Mathf.Max(0.0001f, clampedVolume)) * 20f;

        BGM_AudioMixer.SetFloat("BGMVolume", dB);
        PlayerPrefs.SetFloat(BgmVolumePrefKey, clampedVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        float dB = Mathf.Log10(Mathf.Max(0.0001f, clampedVolume)) * 20f;

        SFX_AudioMixer.SetFloat("SFXVolume", dB);
        PlayerPrefs.SetFloat(SfxVolumePrefKey, clampedVolume);
        PlayerPrefs.Save();
    }
}
