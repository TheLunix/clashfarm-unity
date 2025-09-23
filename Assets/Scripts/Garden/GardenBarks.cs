namespace ClashFarm.Garden
{
    using UnityEngine;
    using TMPro;

    public sealed class GardenBarks : MonoBehaviour
    {
        public static GardenBarks I { get; private set; }
        void Awake(){ if (I!=null && I!=this){Destroy(gameObject);return;} I=this; }

        [Header("UI")]
        public TMP_Text bubbleText;
        public GameObject root;
        public float showSeconds = 2.5f;

        [Header("Lines")]
        [TextArea] public string[] onPlant = { "Посадив! Нумо ростити.", "Будь ласка, виростай швиденько." };
        [TextArea] public string[] onWater = { "Полив! Ом-ном-ном для рослин.", "Крапля за краплею — буде врожай." };
        [TextArea] public string[] onWeed  = { "Бур’ян — геть!", "Чисто як у операційній." };
        [TextArea] public string[] onHarvest = { "Урожай в кишені!", "Оце так кабачок!" };
        [TextArea] public string[] idle = { "Одного разу я виростив моркву ось таку!" };

        public void SayPlant()   => Say(onPlant);
        public void SayWater()   => Say(onWater);
        public void SayWeed()    => Say(onWeed);
        public void SayHarvest() => Say(onHarvest);
        public void SayIdle()    => Say(idle);

        void Say(string[] pool)
        {
            if (bubbleText == null || pool == null || pool.Length == 0) return;
            bubbleText.text = pool[Random.Range(0, pool.Length)];
            if (root) root.SetActive(true);
            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), showSeconds);
        }

        void Hide(){ if (root) root.SetActive(false); }
    }
}
