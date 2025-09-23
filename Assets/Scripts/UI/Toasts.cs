namespace ClashFarm.Garden
{
    using UnityEngine;
    using TMPro;

    public sealed class Toasts : MonoBehaviour
    {
        public static Toasts I { get; private set; }
        void Awake(){ if (I!=null && I!=this){Destroy(gameObject);return;} I=this; }

        public CanvasGroup group;
        public TMP_Text text;
        public float showTime = 2f;
        public float fadeSpeed = 6f;

        public void Show(string msg)
        {
            if (text==null || group==null) { Debug.Log(msg); return; }
            text.text = msg;
            StopAllCoroutines();
            StartCoroutine(CoShow());
        }

        System.Collections.IEnumerator CoShow()
        {
            group.alpha = 0; group.gameObject.SetActive(true);
            while (group.alpha < 1f){ group.alpha += Time.deltaTime * fadeSpeed; yield return null; }
            yield return new WaitForSeconds(showTime);
            while (group.alpha > 0f){ group.alpha -= Time.deltaTime * fadeSpeed; yield return null; }
            group.gameObject.SetActive(false);
        }
    }
}
