using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveUI : MonoBehaviour{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text nextWaveText;
    [SerializeField] private Button startWaveButton;

    private WaveManager waveManager;
    private TMP_Text startWaveButtonText;

    private void Start(){
        waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager == null){
            Debug.LogWarning("WaveManager was not found.");
            return;
        }

        startWaveButtonText =
            startWaveButton.GetComponentInChildren<TMP_Text>();
    }

    private void Update(){
        if (waveManager == null){
            return;
        }

        waveText.text =
            $"Wave {waveManager.CurrentWaveNumber}";

        bool waitingForFirstWave =
            waveManager.WaitingForFirstWave;

        bool waitingForNextWave =
            waveManager.WaitingForNextWave;

        if (waitingForFirstWave){
            nextWaveText.gameObject.SetActive(false);
            startWaveButton.gameObject.SetActive(true);

            if (startWaveButtonText != null){
                startWaveButtonText.text = "START WAVE";
            }

            return;
        }

        if (waitingForNextWave){
            nextWaveText.gameObject.SetActive(true);
            startWaveButton.gameObject.SetActive(true);

            int seconds =
                Mathf.CeilToInt(waveManager.NextWaveTimer);

            nextWaveText.text =
                $"Next wave in: {seconds}";

            if (startWaveButtonText != null){
                startWaveButtonText.text = "START NOW";
            }

            return;
        }

        nextWaveText.gameObject.SetActive(false);
        startWaveButton.gameObject.SetActive(false);
    }

    public void StartNextWaveNow(){
        if (waveManager == null){
            return;
        }

        waveManager.StartNextWaveNow();
    }
}
