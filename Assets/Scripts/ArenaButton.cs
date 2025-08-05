using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ArenaButton : MonoBehaviour
{
    public GameObject Arena, PanelMain, Personage, InfoBattleBar, BattleGnolls, BattleInfo, ObjectErrorText;
    public Image Fone;
    public Sprite[] FoneSprite;
    public Text ErrorText;
    public LoadAndUpdateAccount Player;

    private float TimerLeft;

    public void OpenArena()
    {
        if (Player.IsActiveGuard)
        {
            DisplayErrorMessage("Вы охраняете околицы!");
        }
        else if (Player.pHP < 30)
        {
            DisplayErrorMessage("Для сражения требуется иметь хотя-бы 30 здоровья");
        }
        else if (Player.BattleCount < 1)
        {
            DisplayErrorMessage("Для сражения требуется иметь хотя-бы 1 бой");
        }
        else
        {
            SetArenaActive(true);
        }
    }

    public void CloseArena()
    {
        SetArenaActive(false);
    }

    private void DisplayErrorMessage(string message)
    {
        ObjectErrorText.SetActive(true);
        ErrorText.text = message;
        TimerLeft = 10;
        StartCoroutine(StartTimer());
    }

    private void SetArenaActive(bool active)
    {
        Arena.SetActive(active);
        PanelMain.SetActive(!active);
        Personage.SetActive(!active);
        InfoBattleBar.SetActive(!active);
        BattleGnolls.SetActive(!active);
        BattleInfo.SetActive(!active);
        Fone.sprite = active ? FoneSprite[1] : FoneSprite[0];
    }

    private IEnumerator StartTimer()
    {
        while (TimerLeft > 0)
        {
            TimerLeft -= Time.deltaTime;
            UpdateTime();
            yield return null;
        }
        ObjectErrorText.SetActive(false);
    }

    private void UpdateTime()
    {
        TimerLeft = Mathf.Max(0, TimerLeft);
    }
}