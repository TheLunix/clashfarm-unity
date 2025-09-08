using UnityEngine;
using UnityEngine.UI;

public sealed class GardenPreloadStarter : MonoBehaviour
{
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject spinner;

    void Start()
    {
        // візьми з PlayerSession — підстав свої поля:
        var name   = PlayerSession.I?.Data?.nickname   ?? "";
        var serial = PlayerSession.I?.Data?.serialcode ?? "";

        GardenStateCache.I.OnReady += HandleReady;
        GardenStateCache.I.PreloadByCredentials(name, serial);

        if (statusText) statusText.text = "Loading garden data…";
        if (spinner)    spinner.SetActive(true);
    }

    void HandleReady()
    {
        if (statusText) statusText.text = "Ready";
        if (spinner)    spinner.SetActive(false);
        // тут можеш розблокувати кнопку "Город"
    }

    void OnDestroy()
    {
        if (GardenStateCache.I != null) GardenStateCache.I.OnReady -= HandleReady;
    }
}
