using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolumeController : MonoBehaviour{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;

    private const string MusicVolumeParameter = "MusicVolume";

    private void Start(){
        if (audioMixer == null || volumeSlider == null){
            Debug.LogError("El AudioMixer o MusicSlider no está asignado.");
            return;
        }

        volumeSlider.minValue = 0.01f;
        volumeSlider.maxValue = 1f;

        volumeSlider.onValueChanged.AddListener(SetMusicVolume);
        SetMusicVolume(volumeSlider.value);
    }

    private void OnDestroy(){
        if (volumeSlider != null){
            volumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }
    }

    private void SetMusicVolume(float volume){
        float volumeInDecibels = Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(
            MusicVolumeParameter,
            volumeInDecibels
        );

        if (volumeValueText != null){
            int displayedVolume = Mathf.RoundToInt(volume * 100f);
            volumeValueText.text = displayedVolume.ToString();
        }
    }
}