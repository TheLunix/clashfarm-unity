using UnityEngine;
using UnityEngine.UI;

public class UniversalBackButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject mainMenuPanel; // панель головного меню (щоб ховати кнопку)

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnBackClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnBackClicked);
    }

    private void Update()
    {
        if (button == null || mainMenuPanel == null)
            return;

        // якщо активна панель головного меню – кнопку ховаємо
        bool isOnMain = mainMenuPanel.activeInHierarchy;
        if (button.gameObject.activeSelf == isOnMain)
            button.gameObject.SetActive(!isOnMain);
    }

    private void OnBackClicked()
    {
        if (Navigation.Instance != null)
        {
            Navigation.Instance.GoBack();
        }
        else
        {
            Debug.LogWarning("[UniversalBackButton] Navigation.Instance is null – нема кому робити GoBack()");
        }
    }
}
