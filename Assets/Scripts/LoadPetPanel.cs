using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LoadPetPanel : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public Text TextChar, HPChar, PetKill, NamePet, HideText; 
    public GameObject ButtonHide, ButtonTraining, ButtonCollars, ButtonCommands, PanelInfo, PanelTraining; 
    public Button _ButtonHide, _ButtonTraining, _ButtonCollars, _ButtonCommands;

    public void Load()
    {
        // Налаштування обробників подій для кнопок
        _ButtonHide.onClick.AddListener(() => StartCoroutine(PetHide()));
        _ButtonTraining.onClick.AddListener(PetTraining);
        _ButtonCollars.onClick.AddListener(PetInventory);
        _ButtonCommands.onClick.AddListener(PetCommands);

        // Відображення інформації про питомця в залежності від статусу
        if (Player.PetActive == 1 || Player.PetActive == 2)
        {
            NamePet.text = "Твій: " + Player.PetName;
            TextChar.text = "Сила: " + Player.PetPower +
                "\nЗахист: " + Player.PetProtect +
                "\nЛовкість: " + Player.PetDexterity +
                "\nМайстерність: " + Player.PetSkill +
                "\nЖивучість: " + Player.PetVitality;
            HPChar.text = "Здоров'я: " + Player.PetHP +
                "\nЗдор. макс.: " + (Player.PetHP * 15) +
                "\nЗдор. відновл.: " + ((Player.PetHP * 15) / 10) + " в годину";
            PetKill.text = "Убито звірів: " + Player.PetKills +
                "\nЗа кожних 10 убитих звірів питомець отримує +3 до майстерності, максимум +30 до майстерності.";
            HideText.text = (Player.PetActive == 1) ? "Сховати" : "Випустити";

            // Активувати кнопки
            ButtonHide.SetActive(true);
            ButtonTraining.SetActive(true);
            ButtonCollars.SetActive(true);
            ButtonCommands.SetActive(true);
        }
        else if (Player.PetActive == 3)
        {
            NamePet.text = "Твій: " + Player.PetName;
            TextChar.text = "Сила: " + Player.PetPower +
                "\nЗахист: " + Player.PetProtect +
                "\nЛовкість: " + Player.PetDexterity +
                "\nМайстерність: " + Player.PetSkill +
                "\nЖивучість: " + Player.PetVitality;
            HPChar.text = "Ваш питомець - мертвий! \nВоскресіть його, щоб взяти його з собою.";
            PetKill.text = "Убито звірів: " + Player.PetKills +
                "\nЗа кожних 10 убитих звірів питомець отримує +3 до майстерності, максимум +30 до майстерності.";
            HideText.text = "Воскресити";

            // Активувати чи деактивувати кнопки в залежності від статусу питомця
            ButtonHide.SetActive(true);
            ButtonTraining.SetActive(Player.PetActive == 1);
            ButtonCollars.SetActive(Player.PetActive == 1);
            ButtonCommands.SetActive(Player.PetActive == 1);
        }
    }

    private IEnumerator PetHide()
    {
        // Зміна статусу питомця (приховати або випустити)
        if (Player.PetActive == 1)
        {
            Player.PetActive = 2;
            yield return StartCoroutine(UpdateCellAccount("pet", Player.PetActive.ToString(), Player.pID.ToString()));
            HideText.text = "Випустити";
            Player.ReloadInfoBar();
        }
        else
        {
            Player.PetActive = 1;
            yield return StartCoroutine(UpdateCellAccount("pet", Player.PetActive.ToString(), Player.pID.ToString()));
            HideText.text = "Сховати";
            Player.ReloadInfoBar();
        }
    }

    private void PetTraining()
    {
        PanelInfo.SetActive(false);
        PanelTraining.SetActive(true);
    }

    private void PetInventory()
    {
        // Дії при натисканні на кнопку "Інвентар"
    }

    private void PetCommands()
    {
        // Дії при натисканні на кнопку "Команди"
    }

    private IEnumerator UpdateCellAccount(string cellname, string value, string id)
    {
        // Оновлення значення клітинки для облікового запису
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCell", cellname);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PlayerID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }
}