using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public class GardenFirst : MonoBehaviour
{
    [Header("Dialogue / Typewriter")]
    public TypewriterEffect Anim;

    [Header("Sprites")]
    public Sprite Plot;       // суха грядка
    public Sprite Plot_wet;   // полита грядка
    public Sprite Mushroom;   // врожай (гриб)

    [Header("Scene Hooks")]
    public GameObject Garden1;
    public GameObject Garden2;
    public GameObject Garden3;
    public GameObject Reward;     // Панель з нагородою
    public TMP_Text RewardText;   // Текст усередині Reward
    public GameObject Button;     // додаткова кнопка (не обовʼязково)

    [Header("Localization")]
    public string StringTable = "Tutorial";
    public float AfterTypeDelay = 0.5f;

    [Header("Debug")]
    public int Progress = 0;

    void Start()
    {
        // Базова ініціалізація
        DisableAllGardenButtons();
        HideAllArrows();
        HideAllPlants();
        if (Reward) Reward.SetActive(false);
        if (Button) Button.SetActive(false);

        // підпишемося на маркери від тайпрайтера
        if (Anim != null)
        {
            Anim.OnMarker -= OnTypeMarker;
            Anim.OnMarker += OnTypeMarker;
        }

        // Стартовий діалог
        PlayLineAndThen("tutorial.intro.start", AfterTypeDelay, () =>
        {
            ToggleArrow(Garden1, true);
            EnableButton(Garden1, true);
        });
    }

    public void Progressed()
    {
        switch (Progress)
        {
            // ПОСАДКА
            case 0:
                SetPlantVisible(Garden1, true);
                ToggleArrow(Garden1, false);
                ToggleArrow(Garden2, true);
                EnableButton(Garden1, false);
                EnableButton(Garden2, true);
                Progress = 1;
                break;

            case 1:
                SetPlantVisible(Garden2, true);
                ToggleArrow(Garden2, false);
                ToggleArrow(Garden3, true);
                EnableButton(Garden2, false);
                EnableButton(Garden3, true);
                Progress = 2;
                break;

            case 2:
                SetPlantVisible(Garden3, true);
                ToggleArrow(Garden3, false);
                EnableButton(Garden3, false);
                Progress = 3;
                goto case 3;

            // ДІАЛОГ: Полив
            case 3:
                PlayLineAndThen("tutorial.water.prompt", AfterTypeDelay, () =>
                {
                    ToggleArrow(Garden1, true);
                    EnableButton(Garden1, true);
                });
                Progress = 4;
                break;

            // ПОЛИВ 1→2→3
            case 4:
                SetPlotSprite(Garden1, Plot_wet);
                ToggleArrow(Garden1, false);
                ToggleArrow(Garden2, true);
                EnableButton(Garden1, false);
                EnableButton(Garden2, true);
                Progress = 5;
                break;

            case 5:
                SetPlotSprite(Garden2, Plot_wet);
                ToggleArrow(Garden2, false);
                ToggleArrow(Garden3, true);
                EnableButton(Garden2, false);
                EnableButton(Garden3, true);
                Progress = 6;
                break;

            case 6:
                SetPlotSprite(Garden3, Plot_wet);
                SetPlantVisible(Garden3, true);
                ToggleArrow(Garden3, false);
                EnableButton(Garden3, false);
                Progress = 7;
                goto case 7;

            // ДІАЛОГ: ВЖУХ+ріст+потім збір
            case 7:
                // УВАГА: тут текст містить маркер <fx:grow/> одразу після слова ВЖУХ!!!
                // Коли друк дійде до маркера — спрацює OnTypeMarker("grow") → рослини "виросли".
                PlayLine("tutorial.grow.message");

                // Після ЗАВЕРШЕННЯ друку + невелика пауза — показуємо стрілку на Garden1
                OnTypeCompletedThen(2.0f, () =>
                {
                    ToggleArrow(Garden1, true);
                    EnableButton(Garden1, true);
                });

                Progress = 8;
                break;

            // ЗБІР 1→2→3
            case 8:
                SetPlantVisible(Garden1, false);
                ToggleArrow(Garden1, false);
                ToggleArrow(Garden2, true);
                EnableButton(Garden1, false);
                EnableButton(Garden2, true);
                Progress = 9;
                break;

            case 9:
                SetPlantVisible(Garden2, false);
                ToggleArrow(Garden2, false);
                ToggleArrow(Garden3, true);
                EnableButton(Garden2, false);
                EnableButton(Garden3, true);
                Progress = 10;
                break;

            case 10:
                SetPlantVisible(Garden3, false);
                ToggleArrow(Garden3, false);
                EnableButton(Garden3, false);
                Progress = 11;
                goto case 11;

            // ФІНАЛ + нагорода
            case 11:
                PlayLineAndThen("tutorial.outro.part1", AfterTypeDelay, () =>
                {
                    PlayLineAndThen("tutorial.outro.part2", AfterTypeDelay, () =>
                    {
                        if (Reward)
                        {
                            Reward.SetActive(true);
                            if (Button) Button.SetActive(true);
                            UpdateRewardText();
                        }
                    });
                });
                break;
        }
    }

    // ======== Маркери з TypewriterEffect (реакції під час друку) ========
    void OnTypeMarker(string marker)
    {
        if (string.IsNullOrEmpty(marker)) return;

        switch (marker)
        {
            case "grow":
                // Це викликається ТОЧНО після слова "ВЖУХ!!!" (або його аналогу в інших мовах),
                // де ти поставиш <fx:grow/> у локалізованому тексті.
                SetPlantSprite(Garden1, Mushroom);
                SetPlotSprite(Garden1, Plot);

                SetPlantSprite(Garden2, Mushroom);
                SetPlotSprite(Garden2, Plot);

                SetPlantSprite(Garden3, Mushroom);
                SetPlotSprite(Garden3, Plot);
                break;
        }
    }

    // ---------------- Локалізація + текст нагороди ----------------
    void UpdateRewardText()
    {
        if (RewardText == null) return;
        StartCoroutine(CoUpdateRewardText());
    }

    IEnumerator CoUpdateRewardText()
    {
        var loc = new LocalizedString(StringTable, "reward.text");
        var handle = loc.GetLocalizedStringAsync();
        yield return handle; // без використання AsyncOperationHandle<T> в сигнатурах
        RewardText.text = handle.Result ?? "reward.text";
    }

    // ---------------- Друк локалізованих рядків ----------------
    void PlayLineAndThen(string key, float afterTypeDelay, Action action)
    {
        StartCoroutine(CoPlayLineAndThen(key, afterTypeDelay, action));
    }

    void PlayLine(string key)
    {
        StartCoroutine(CoPlayLineAndThen(key, 0f, null));
    }

    IEnumerator CoPlayLineAndThen(string key, float afterTypeDelay, Action action)
    {
        var loc = new LocalizedString(StringTable, key);
        var h = loc.GetLocalizedStringAsync();
        while (!h.IsDone) yield return null;

        var text = h.Result ?? key;
        if (Anim != null) Anim.Play(text);

        if (action != null)
        {
            void handler()
            {
                Anim.OnCompleted -= handler;
                StartCoroutine(Delay(afterTypeDelay, action));
            }
            Anim.OnCompleted -= handler;
            Anim.OnCompleted += handler;
        }
    }

    void OnTypeCompletedThen(float delay, Action action)
    {
        if (Anim == null || action == null) return;

        void handler()
        {
            Anim.OnCompleted -= handler;
            StartCoroutine(Delay(delay, action));
        }
        Anim.OnCompleted -= handler;
        Anim.OnCompleted += handler;
    }

    IEnumerator Delay(float seconds, Action onComplete)
    {
        yield return new WaitForSeconds(seconds);
        onComplete?.Invoke();
    }

    // ---------------- UI допоміжні ----------------
    void EnableButton(GameObject go, bool state)
    {
        if (!go) return;
        var b = go.GetComponent<Button>();
        if (b) b.enabled = state;
    }

    void ToggleArrow(GameObject go, bool state)
    {
        if (!go) return;
        var arrow = go.transform.Find("arrow");
        if (arrow) arrow.gameObject.SetActive(state);
    }

    void HideAllArrows()
    {
        ToggleArrow(Garden1, false);
        ToggleArrow(Garden2, false);
        ToggleArrow(Garden3, false);
    }

    void HideAllPlants()
    {
        SetPlantVisible(Garden1, false);
        SetPlantVisible(Garden2, false);
        SetPlantVisible(Garden3, false);
    }

    void DisableAllGardenButtons()
    {
        EnableButton(Garden1, false);
        EnableButton(Garden2, false);
        EnableButton(Garden3, false);
    }

    void SetPlotSprite(GameObject go, Sprite s)
    {
        if (!go || !s) return;
        var img = go.GetComponent<Image>();
        if (img) { img.sprite = s; return; }
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr) sr.sprite = s;
    }

    void SetPlantVisible(GameObject go, bool state)
    {
        if (!go) return;
        var plant = go.transform.Find("plant");
        if (!plant) return;
        plant.gameObject.SetActive(state);
    }

    void SetPlantSprite(GameObject go, Sprite s)
    {
        if (!go || !s) return;
        var plant = go.transform.Find("plant");
        if (!plant) return;
        var img = plant.GetComponent<Image>();
        if (img) { img.sprite = s; return; }
        var sr = plant.GetComponent<SpriteRenderer>();
        if (sr) sr.sprite = s;
    }
}
