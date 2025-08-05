using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerRegister : MonoBehaviour
{
    public InputField NickInput;
    public Text MessageText;
    public GameObject MessageBox;

    private string SerialCode;

    private const string ServerURL = "https://api.clashfarm.com/api/player";

    public void RegisterPlayer()
    {
        string nick = NickInput.text;

        if (string.IsNullOrEmpty(nick))
        {
            DisplayMessage("Придумайте нік!");
            return;
        }

        if (nick.Length <= 3)
        {
            DisplayMessage("Довжина ніка має бути більше 3 символів!");
            return;
        }

        StartCoroutine(CheckNickNameInAccounts());
    }

    private void DisplayMessage(string message)
    {
        MessageText.text = message;
        MessageBox.SetActive(true);
    }

    private IEnumerator CheckNickNameInAccounts()
    {
        WWWForm formData = new WWWForm();
        formData.AddField("PlayerName", NickInput.text);

        using (UnityWebRequest www = UnityWebRequest.Post(ServerURL + "/checkname", formData))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Помилка: " + www.error);
                yield break;
            }

            if (www.downloadHandler.text == "1")
            {
                DisplayMessage("Даний нік вже використовується, придумайте інший!");
            }
            else
            {
                PlayerPrefs.SetString("Name", NickInput.text);
                PlayerPrefs.Save();
                GenerateSerialCode();
            }
        }
    }

    private void GenerateSerialCode(int stringLength = 10)
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        SerialCode = new string(Enumerable.Repeat(characters, stringLength)
                                        .Select(s => s[random.Next(s.Length)]).ToArray());
        Debug.Log("SerialCode: " + SerialCode);
        StartCoroutine(CheckSerialCodeInAccounts());
    }

    private IEnumerator CheckSerialCodeInAccounts()
    {
        WWWForm formData = new WWWForm();
        formData.AddField("PlayerSerialCode", SerialCode);

        using (UnityWebRequest www = UnityWebRequest.Post(ServerURL + "/checkserial", formData))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Помилка: " + www.error);
                yield break;
            }

            if (www.downloadHandler.text == "1")
            {
                GenerateSerialCode(); // Якщо код не унікальний — згенерувати ще раз
            }
            else
            {
                PlayerPrefs.SetString("SerialCode", SerialCode);
                PlayerPrefs.Save();
                StartCoroutine(RegisterAccount());
            }
        }
    }

    private IEnumerator RegisterAccount()
    {
        WWWForm formData = new WWWForm();
        formData.AddField("PlayerName", NickInput.text);
        formData.AddField("PlayerSerialCode", SerialCode);

        using (UnityWebRequest www = UnityWebRequest.Post(ServerURL + "/register", formData))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Помилка: " + www.error);
            }
            else if (www.downloadHandler.text == "0")
            {
                SceneManager.LoadScene(1); // або індекс 1
            }
            else
            {
                DisplayMessage("Не вдалося зареєструватися!");
            }
        }
    }
}
