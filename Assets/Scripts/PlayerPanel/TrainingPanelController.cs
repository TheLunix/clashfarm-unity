using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainingPanelController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text headerTitle; // "Training / Тренування"
    [SerializeField] private Button btnClose;      // хрестик
    [SerializeField] private Button btnSwitch;     // Персонаж ↔ Тварина (поки disabled)
    [SerializeField] private TMP_Text switchLabel; // текст кнопки (поки "Pet (soon)")

    [Header("Rows (Player)")]
    [SerializeField] private StatRowView rowPower;
    [SerializeField] private StatRowView rowSkill;
    [SerializeField] private StatRowView rowSurvivability;
    [SerializeField] private StatRowView rowProtection;
    [SerializeField] private StatRowView rowDexterity;

    [Header("Icons")]
    [SerializeField] private Sprite iconPower;
    [SerializeField] private Sprite iconSkill;
    [SerializeField] private Sprite iconSurvivability;
    [SerializeField] private Sprite iconProtection;
    [SerializeField] private Sprite iconDexterity;

    [Header("Toasts (optional)")]
    [SerializeField] private CanvasGroup toastCg;
    [SerializeField] private TMP_Text toastText;
    [SerializeField, Range(0.1f, 2f)] private float toastFade = 0.2f;
    [SerializeField, Range(0.5f, 3f)] private float toastHold = 1.0f;

    // локальні прапорці “йде апгрейд”
    private bool busyPower, busySkill, busySurv, busyProt, busyDex;
    private Coroutine toastRoutine;

    void Awake()
    {
        if (btnClose)  btnClose.onClick.AddListener(ClosePanel);
        if (btnSwitch)
        {
            btnSwitch.interactable = false;
            if (switchLabel) switchLabel.text = "Pet (soon)";
        }
        if (toastCg) { toastCg.alpha = 0f; toastCg.gameObject.SetActive(false); }
    }

    void OnEnable() => RefreshAllRows();

    private void ClosePanel()
    {
        // якщо панеллю керує PlayerSceneController — можна дернути його CloseCurrent()
        gameObject.SetActive(false);
    }

    private PlayerInfo Data => PlayerSession.I?.Data;

    private void RefreshAllRows()
    {
        if (headerTitle) headerTitle.text = "Тренування";

        if (Data == null)
        {
            BindDisabled(rowPower, iconPower, "Power", "—");
            BindDisabled(rowSkill, iconSkill, "Skill", "—");
            BindDisabled(rowSurvivability, iconSurvivability, "Survivability", "—");
            BindDisabled(rowProtection, iconProtection, "Protection", "—");
            BindDisabled(rowDexterity, iconDexterity, "Dexterity", "—");
            return;
        }

        int green = Data.playergreen;

        BindRow(rowPower, iconPower, "Power", "Increases damage",
            StatCostClient.Stat.Power, Data.playerpower, green, UpgradePower);

        BindRow(rowSkill, iconSkill, "Skill", "Increases critical chance",
            StatCostClient.Stat.Skill, Data.playerskill, green, UpgradeSkill);

        BindRow(rowSurvivability, iconSurvivability, "Survivability", "Increases max HP",
            StatCostClient.Stat.Survivability, Data.playersurvivability, green, UpgradeSurvivability);

        BindRow(rowProtection, iconProtection, "Protection", "Reduces incoming damage",
            StatCostClient.Stat.Protection, Data.playerprotection, green, UpgradeProtection);

        BindRow(rowDexterity, iconDexterity, "Dexterity", "Increases dodge chance",
            StatCostClient.Stat.Dexterity, Data.playerdexterity, green, UpgradeDexterity);
    }

    private void BindRow(StatRowView row, Sprite icon, string title, string desc,
                         StatCostClient.Stat stat, int level, int playerGreen,
                         System.Action onUpgrade)
    {
        if (!row) return;

        int price = StatCostClient.GetPrice(stat, level);
        bool canAfford = playerGreen >= price && price > 0;
        row.Bind(icon, title, desc, level, price, canAfford, onUpgrade);
    }

    private void BindDisabled(StatRowView row, Sprite icon, string title, string desc)
    {
        if (!row) return;
        row.Bind(icon, title, desc, 0, 0, false, null);
        row.ShowBusy(true);
    }

    // === Upgrade handlers ===
    private void UpgradePower()         => StartCoroutine(DoUpgrade(StatCostClient.Stat.Power,         rowPower));
    private void UpgradeSkill()         => StartCoroutine(DoUpgrade(StatCostClient.Stat.Skill,         rowSkill));
    private void UpgradeSurvivability() => StartCoroutine(DoUpgrade(StatCostClient.Stat.Survivability, rowSurvivability));
    private void UpgradeProtection()    => StartCoroutine(DoUpgrade(StatCostClient.Stat.Protection,    rowProtection));
    private void UpgradeDexterity()     => StartCoroutine(DoUpgrade(StatCostClient.Stat.Dexterity,     rowDexterity));

    // гет/сет busy без ref-параметрів
    private bool IsBusy(StatCostClient.Stat s) => s switch
    {
        StatCostClient.Stat.Power         => busyPower,
        StatCostClient.Stat.Skill         => busySkill,
        StatCostClient.Stat.Survivability => busySurv,
        StatCostClient.Stat.Protection    => busyProt,
        StatCostClient.Stat.Dexterity     => busyDex,
        _ => false
    };
    private void SetBusy(StatCostClient.Stat s, bool v)
    {
        switch (s)
        {
            case StatCostClient.Stat.Power:         busyPower = v; break;
            case StatCostClient.Stat.Skill:         busySkill = v; break;
            case StatCostClient.Stat.Survivability: busySurv  = v; break;
            case StatCostClient.Stat.Protection:    busyProt  = v; break;
            case StatCostClient.Stat.Dexterity:     busyDex   = v; break;
        }
    }

    private IEnumerator DoUpgrade(StatCostClient.Stat stat, StatRowView row)
    {
        if (row == null || Data == null) yield break;
        if (IsBusy(stat)) yield break;

        SetBusy(stat, true);
        row.ShowBusy(true);

        // актуальні значення
        int level = GetLevel(stat, Data);
        int price = StatCostClient.GetPrice(stat, level);

        if (price <= 0)
        {
            ShowToast("Base level reached");
            row.ShowBusy(false);
            SetBusy(stat, false);
            yield break;
        }

        if (Data.playergreen < price)
        {
            ShowToast("Not enough green");
            row.ShowBusy(false);
            SetBusy(stat, false);
            yield break;
        }

        // оптимістично оновлюємо відображення рядка (без зміни PlayerSession/інфобару)
        row.UpdateLevelAndPrice(level + 1, StatCostClient.GetPrice(stat, level + 1), Data.playergreen - price >= 0);

        // виклик сервера (nickname/serialcode беремо з Data)
        string statKey = StatCostClient.ToWireKey(stat);
        var task = ApiClient.PostUpgradeAsync(Data.nickname, Data.serialcode, statKey, level);
        while (!task.IsCompleted) yield return null;

        var info = task.Result;
        if (info == null)
        {
            // ролбек у рядку
            row.UpdateLevelAndPrice(level, price, true);
            ShowToast("Upgrade failed");
            row.ShowBusy(false);
            SetBusy(stat, false);
            yield break;
        }

        // застосувати серверну істину (інфобар/HP/інші поля оновляться через твої підписки)
        PlayerSession.I.Apply(info);

        // перемалювати всі рядки з нових даних
        RefreshAllRows();
        ShowToast("Upgraded +1");

        row.ShowBusy(false);
        SetBusy(stat, false);
    }

    private static int GetLevel(StatCostClient.Stat stat, PlayerInfo data)
    {
        return stat switch
        {
            StatCostClient.Stat.Power         => data.playerpower,
            StatCostClient.Stat.Skill         => data.playerskill,
            StatCostClient.Stat.Survivability => data.playersurvivability,
            StatCostClient.Stat.Protection    => data.playerprotection,
            StatCostClient.Stat.Dexterity     => data.playerdexterity,
            _ => 0
        };
    }

    // ===== Toasts =====
    private void ShowToast(string text)
    {
        if (!toastCg || !toastText) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(text));
    }

    private IEnumerator ToastRoutine(string text)
    {
        toastText.text = text;
        toastCg.gameObject.SetActive(true);

        float t = 0f;
        while (t < toastFade) { t += Time.unscaledDeltaTime; toastCg.alpha = t / toastFade; yield return null; }
        toastCg.alpha = 1f;

        yield return new WaitForSecondsRealtime(toastHold);

        t = 0f;
        while (t < toastFade) { t += Time.unscaledDeltaTime; toastCg.alpha = 1f - t / toastFade; yield return null; }
        toastCg.alpha = 0f;
        toastCg.gameObject.SetActive(false);
        toastRoutine = null;
    }
}
