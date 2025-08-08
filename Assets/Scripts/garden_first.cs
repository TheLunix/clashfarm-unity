using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class GardenFirst : MonoBehaviour
{
    public TypewriterEffect Anim;
    public Sprite Plot, Plot_wet, Mushroom;
    public int Progress = 0;
    public GameObject Garden1, Garden2, Garden3, Reward, Button;

    private void Start()
    {

        StartCoroutine(StartTimer(7f, () =>
        {
            Garden1.transform.Find("arrow").gameObject.SetActive(true);
            Button button = Garden1.GetComponent<Button>();
            button.enabled = true;
        }));
    }
    public void Progressed()
    {
        switch (Progress)
        {
            case 0:
                Garden1.transform.Find("plant").gameObject.SetActive(true);
                Garden1.transform.Find("arrow").gameObject.SetActive(false);
                Garden2.transform.Find("arrow").gameObject.SetActive(true);
                Button button = Garden1.GetComponent<Button>();
                button.enabled = false;
                button = Garden2.GetComponent<Button>();
                button.enabled = true;
                Progress = 1;
                break;
            case 1:
                Garden2.transform.Find("plant").gameObject.SetActive(true);
                Garden2.transform.Find("arrow").gameObject.SetActive(false);
                Garden3.transform.Find("arrow").gameObject.SetActive(true);
                button = Garden2.GetComponent<Button>();
                button.enabled = false;
                button = Garden3.GetComponent<Button>();
                button.enabled = true;
                Progress = 2;
                break;
            case 2:
                Garden3.transform.Find("plant").gameObject.SetActive(true);
                Garden3.transform.Find("arrow").gameObject.SetActive(false);
                button = Garden3.GetComponent<Button>();
                button.enabled = false;
                Progress = 3;
                Progressed();
                break;
            case 3:
                Anim.fullRawText = "Чудово! \nТепер потрібно їх полити.";
                Anim.Start();
                StartCoroutine(StartTimer(2.5f, () =>
                {
                    Garden1.transform.Find("arrow").gameObject.SetActive(true);
                    Button button = Garden1.GetComponent<Button>();
                    button.enabled = true;
                }));
                Progress = 4;
                break;
            case 4:
                Garden1.GetComponent<Image>().sprite = Plot_wet;
                Garden1.transform.Find("arrow").gameObject.SetActive(false);
                Garden2.transform.Find("arrow").gameObject.SetActive(true);
                button = Garden1.GetComponent<Button>();
                button.enabled = false;
                button = Garden2.GetComponent<Button>();
                button.enabled = true;
                Progress = 5;
                break;
            case 5:
                Garden2.GetComponent<Image>().sprite = Plot_wet;
                Garden2.transform.Find("arrow").gameObject.SetActive(false);
                Garden3.transform.Find("arrow").gameObject.SetActive(true);
                button = Garden2.GetComponent<Button>();
                button.enabled = false;
                button = Garden3.GetComponent<Button>();
                button.enabled = true;
                Progress = 6;
                break;
            case 6:
                Garden3.GetComponent<Image>().sprite = Plot_wet;
                Garden3.transform.Find("plant").gameObject.SetActive(true);
                Garden3.transform.Find("arrow").gameObject.SetActive(false);
                button = Garden3.GetComponent<Button>();
                button.enabled = false;
                Progress = 7;
                Progressed();
                break;
            case 7:
                Anim.fullRawText = "В тебе чудово виходить! \nА тепер дивись.... ВЖУХ!!! \nІ все виросло, скажи прекрасно, да? \nТепер портібно зібрати врожай!";
                Anim.Start();
                StartCoroutine(StartTimer(2.5f, () =>
                {
                    Garden1.transform.Find("plant").GetComponent<SpriteRenderer>().sprite = Mushroom;
                    Garden1.GetComponent<Image>().sprite = Plot;
                    Garden2.transform.Find("plant").GetComponent<SpriteRenderer>().sprite = Mushroom;
                    Garden2.GetComponent<Image>().sprite = Plot;
                    Garden3.transform.Find("plant").GetComponent<SpriteRenderer>().sprite = Mushroom;
                    Garden3.GetComponent<Image>().sprite = Plot;
                }));
                StartCoroutine(StartTimer(8f, () =>
                {
                    Garden1.transform.Find("arrow").gameObject.SetActive(true);
                    Button button = Garden1.GetComponent<Button>();
                    button.enabled = true;
                }));
                Progress = 8;
                break;
            case 8:
                Garden1.transform.Find("plant").gameObject.SetActive(false);
                Garden1.transform.Find("arrow").gameObject.SetActive(false);
                Garden2.transform.Find("arrow").gameObject.SetActive(true);
                button = Garden1.GetComponent<Button>();
                button.enabled = false;
                button = Garden2.GetComponent<Button>();
                button.enabled = true;
                Progress = 9;
                break;
            case 9:
                Garden2.transform.Find("plant").gameObject.SetActive(false);
                Garden2.transform.Find("arrow").gameObject.SetActive(false);
                Garden3.transform.Find("arrow").gameObject.SetActive(true);
                button = Garden2.GetComponent<Button>();
                button.enabled = false;
                button = Garden3.GetComponent<Button>();
                button.enabled = true;
                Progress = 10;
                break;
            case 10:
                Garden3.transform.Find("plant").gameObject.SetActive(false);
                Garden3.transform.Find("arrow").gameObject.SetActive(false);
                button = Garden3.GetComponent<Button>();
                button.enabled = false;
                Progress = 11;
                Progressed();
                break;
            case 11:
                Anim.fullRawText = "Так... На разі дай це сюди! \nА тобі... \nО, в мене в карманах щось завалялось...";
                Anim.Start();
                
                StartCoroutine(StartTimer(4.5f, () =>
                {
                    Anim.fullRawText = "Все!  Тримай!  \nБувай здоровий!";
                    Anim.Start();
                    StartCoroutine(StartTimer(1.4f, () =>
                    {
                        Reward.gameObject.SetActive(true);
                        //Button.gameObject.SetActive(true);
                    }));
                }));
                break;
        }
    }

    IEnumerator StartTimer(float seconds, System.Action onComplete)
    {
        yield return new WaitForSeconds(seconds);
        onComplete?.Invoke();
    }
}