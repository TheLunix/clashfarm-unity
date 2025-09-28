using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerSceneController : MonoBehaviour
{
    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Sprite defaultAvatar;

    [Header("Left Column Buttons")]
    [SerializeField] private Button btnStatistic;
    [SerializeField] private Button btnTraining;
    [SerializeField] private Button btnEquipment;

    [Header("Right Column Buttons")]
    [SerializeField] private Button btnGifts;
    [SerializeField] private Button btnAchievements;
    [SerializeField] private Button btnProfile;

    [Header("Options")]
    [SerializeField] private bool playClickSound = false;
    [SerializeField] private AudioSource uiAudio;

    void Awake()
    {
        if (avatarImage && defaultAvatar && avatarImage.sprite == null)
            avatarImage.sprite = defaultAvatar;

        if (btnStatistic)    btnStatistic.onClick.AddListener(OpenStatistic);
        if (btnTraining)     btnTraining.onClick.AddListener(OpenTraining);
        if (btnEquipment)    btnEquipment.onClick.AddListener(OpenEquipment);

        if (btnGifts)        btnGifts.onClick.AddListener(OpenGifts);
        if (btnAchievements) btnAchievements.onClick.AddListener(OpenAchievements);
        if (btnProfile)      btnProfile.onClick.AddListener(OpenProfile);
    }

    void OnDestroy()
    {
        if (btnStatistic)    btnStatistic.onClick.RemoveListener(OpenStatistic);
        if (btnTraining)     btnTraining.onClick.RemoveListener(OpenTraining);
        if (btnEquipment)    btnEquipment.onClick.RemoveListener(OpenEquipment);
        if (btnGifts)        btnGifts.onClick.RemoveListener(OpenGifts);
        if (btnAchievements) btnAchievements.onClick.RemoveListener(OpenAchievements);
        if (btnProfile)      btnProfile.onClick.RemoveListener(OpenProfile);
    }

    private void OpenStatistic()    { Click(); /* TODO */ }
    private void OpenTraining()     { Click(); /* TODO */ }
    private void OpenEquipment()    { Click(); /* TODO */ }
    private void OpenGifts()        { Click(); /* TODO */ }
    private void OpenAchievements() { Click(); /* TODO */ }
    private void OpenProfile()      { Click(); /* TODO */ }

    private void Click()
    {
        if (playClickSound && uiAudio) uiAudio.Play();
    }

    public void SetAvatar(Sprite s)
    {
        if (avatarImage && s) avatarImage.sprite = s;
    }
}
