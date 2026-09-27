using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolumeController : MonoBehaviour{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TMP_Text soundValueText;
    [SerializeField] private AudioMixerGroup soundOutputGroup;

    private const string MusicVolumeParameter = "MusicVolume";
    private const string SoundVolumeParameter = "SoundVolume";
    private const float MaximumMusicGain = 0.6f;
    private const float MutedDecibels = -80f;

    private void Start(){
        if (audioMixer == null || volumeSlider == null){
            Debug.LogError("El AudioMixer o MusicSlider no está asignado.");
            return;
        }

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicVolumeParameter, volumeSlider.value));

        volumeSlider.onValueChanged.AddListener(SetMusicVolume);
        SetMusicVolume(volumeSlider.value);

        if (soundSlider != null && soundOutputGroup != null){
            TowerAttackAudio.SoundOutputGroup = soundOutputGroup;
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SoundVolumeParameter, soundSlider.value));
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
            SetSoundVolume(soundSlider.value);
        }
    }

    private void OnDestroy(){
        if (volumeSlider != null){
            volumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }
        if (soundSlider != null){
            soundSlider.onValueChanged.RemoveListener(SetSoundVolume);
        }
    }

    private void SetMusicVolume(float volume){
        PlayerPrefs.SetFloat(MusicVolumeParameter, volume);
        float volumeInDecibels = volume <= 0f
            ? MutedDecibels
            : Mathf.Log10(volume * MaximumMusicGain) * 20f;

        audioMixer.SetFloat(
            MusicVolumeParameter,
            volumeInDecibels
        );

        if (volumeValueText != null){
            int displayedVolume = Mathf.RoundToInt(volume * 100f);
            volumeValueText.text = displayedVolume.ToString();
        }
    }

    private void SetSoundVolume(float volume){
        PlayerPrefs.SetFloat(SoundVolumeParameter, volume);
        float decibels = volume <= 0f ? MutedDecibels : Mathf.Log10(volume) * 20f;
        audioMixer.SetFloat(SoundVolumeParameter, decibels);

        if (soundValueText != null){
            soundValueText.text = Mathf.RoundToInt(volume * 100f).ToString();
        }
    }
}
