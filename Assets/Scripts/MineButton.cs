using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MineButton : MonoBehaviour
{
    public GameObject MinePanel, PanelMain, Personage, ButtonMine, ButtonCancel, Button;
    public Image Fone;
    public Sprite[] FoneSprite;
    public Text _ButtonMine;
    public LoadAndUpdateAccount Player;
    public MineSystem Mine;
    public TextMeshProUGUI MineInfo, InfoText;
    private float _timeLeft = 0f;
    private string TimeInfo;
    
    public void OpenMinePanel()
    {
        // Відкриття панелі шахти
        MinePanel.SetActive(true);
        PanelMain.SetActive(false);
        Personage.SetActive(false);
        Fone.sprite = FoneSprite[1];

        if (Player.pTimeToEndMine != "0")
        {
            if (!Player.IsMineToday)
            {
                System.DateTime time = System.DateTime.Parse(Player.pTimeToNextMine);
                _timeLeft = (int)(time - System.DateTime.Now).TotalSeconds;
                System.DateTime mtime = System.DateTime.Parse(Player.pTimeToEndMine);
                Mine._timeMineLeft = (int)(mtime - System.DateTime.Now).TotalSeconds;
                Mine.mTimer();
                InfoText.text = "Ви спустились в шахту\nЗалишилось часу на видобуток - " + Mine.MineTimeInfo +
                    "\nВидобуто: <sprite=1> " + Player.pMinedGold + "/" + Player.pMaxMinegGold + " золота";

                if (System.DateTime.Now > time)
                {
                    Button.SetActive(false);
                    ButtonMine.SetActive(true);
                    ButtonCancel.SetActive(true);
                    MineInfo.text = "Ви спустились до місця золота.\nШвидше добувайте!";
                }

                StartCoroutine(StartTimer());
                _ButtonMine.text = "Вийти";
            }
            else
            {
                int mining = Player.pMinedGold;
                MineInfo.text = "Сьогодні ви вже були в шахті!\nВи видобули: <sprite=1> " + mining + " золота.";
                Button.SetActive(false);
            }
        }
    }

    public void CloseMinePanel()
    {
        // Закриття панелі шахти
        MinePanel.SetActive(false);
        PanelMain.SetActive(true);
        Personage.SetActive(true);
        Fone.sprite = FoneSprite[0];
    }

    private IEnumerator StartTimer()
    {
        // Таймер для шахти
        while (_timeLeft > 0)
        {
            _timeLeft -= Time.deltaTime;
            UpdateTimeText();
            yield return null;
        }
    }

    private void UpdateTimeText()
    {
        if (_timeLeft < 0)
        {
            _timeLeft = 0;
            Button.SetActive(false);
            ButtonMine.SetActive(true);
            ButtonCancel.SetActive(true);
            MineInfo.text = "Ви спустились до місця золота.\nШвидше добувайте!";
        }

        DisplayTime(_timeLeft);
        string TextMined = (Player.pMinedGold > 0) ? "Ви успішно видобули <sprite=1> " + Player.pLvl + " золота." : "";
        MineInfo.text = TextMined + "\nВи спускаєтесь до нового місця золота, залишилось часу - " + TimeInfo;
    }

    void DisplayTime(float timeToDisplay)
    {
        // Відображення залишкового часу у вигляді "хвилини:секунди"
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
        TimeInfo = remaining.ToString(@"mm\:ss");
    }
}