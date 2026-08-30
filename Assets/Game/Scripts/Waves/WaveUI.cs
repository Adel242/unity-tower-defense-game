using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveUI : MonoBehaviour{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text nextWaveText;
    [SerializeField] private Button startWaveButton;

    private void Update(){
        if (waveManager == null){
            return;
        }

        waveText.text = $"Wave {waveManager.CurrentWaveNumber}";

        bool waiting = waveManager.WaitingForNextWave;

        nextWaveText.gameObject.SetActive(waiting);
        startWaveButton.gameObject.SetActive(waiting);

        if (waiting){
            int seconds = Mathf.CeilToInt(waveManager.NextWaveTimer);

            nextWaveText.text = $"Next wave in: {seconds}";
        }
    }

    public void StartNextWaveNow(){
        waveManager.StartNextWaveNow();
    }
}