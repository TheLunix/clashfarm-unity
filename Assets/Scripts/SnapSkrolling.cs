using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SnapSkrolling : MonoBehaviour
{
    private string NickName, SerialCode, PlayerLvl, nick = "Name", code = "SerialCode", lvl = "Lvl", jsonformat, PlayerID, id = "ID";
    [Header("Controllers")]
    public int CountItems;
    [Range(1, 500)]
    public int ItemOffset;
    [Range(0f, 20f)]
    public float SnapSpeed;
    [Range(0f, 5f)]
    public float ScaleOffset;
    [Header("Other Items")]
    public GameObject PrefabItem;
    public ScrollRect ScrollRect;
    private GameObject[] Item;
    public int Type;
    private Vector2[] ItemPos, ItemScale;
    private RectTransform ContentRect;
    private Vector2 ContentVector;
    private int SelectItem = 0;
    private bool IsScrolling;
    ItemJS[] Items;

    void Start()
    {
        this.NickName = PlayerPrefs.GetString(nick);
        this.PlayerLvl = PlayerPrefs.GetString(lvl);
        this.SerialCode = PlayerPrefs.GetString(code);
        this.PlayerID = PlayerPrefs.GetString(id);
        ContentRect = GetComponent<RectTransform>();
        StartCoroutine(ItemsLoad());
    }

    private IEnumerator ItemsLoad()
    {
        yield return StartCoroutine(LoadItems());
        Item = new GameObject[CountItems];
        ItemPos = new Vector2[CountItems];
        ItemScale = new Vector2[CountItems];

        for (int i = CountItems - 1; i > -1; i--)
        {
            Item[i] = Instantiate(PrefabItem, transform, false);

            GameObject NItem = Item[i].transform.Find("ItemName").gameObject;
            Text NameItem = NItem.GetComponent<Text>();
            NameItem.text = Items[i].name;

            GameObject ItemID = Item[i].transform.Find("ItemID").gameObject;
            Text ItemIDText = ItemID.GetComponent<Text>();
            ItemIDText.text = Items[i].id.ToString();

            GameObject PItem = Item[i].transform.Find("ItemPrice").gameObject;
            Text prPrewiew = PItem.GetComponent<Text>();
            prPrewiew.text = Items[i].price.ToString();

            GameObject ItemPrewiew = Item[i].transform.Find("ItemPrewiew").gameObject;
            Image ImPrewiew = ItemPrewiew.GetComponent<Image>();
            ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id.ToString());

            if (i == CountItems - 1) continue;
            Item[i].transform.localPosition = new Vector2(Item[i + 1].transform.localPosition.x + PrefabItem.GetComponent<RectTransform>().sizeDelta.x + ItemOffset,
            Item[i].transform.localPosition.y);
            ItemPos[i] = -Item[i].transform.localPosition;
        }
    }

    private IEnumerator LoadItems()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadItemsCount", "Yes");
        FindDataBase.AddField("PlayerLevel", this.PlayerLvl);
        FindDataBase.AddField("ItemType", Type);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        jsonformat = "{\"Items\":" + jsonformat + "}";
        Items = JsonHelper.FromJson<ItemJS>(jsonformat);
        CountItems = Items.Length;
        if (CountItems == 0) this.enabled = false;
        www.Dispose();
    }

    private void FixedUpdate()
    {
        float nearestPos = float.MaxValue;

        for (int i = CountItems - 1; i > -1; i--)
        {
            float distance = Mathf.Abs(ContentRect.anchoredPosition.x - ItemPos[i].x);

            if (distance < nearestPos)
            {
                nearestPos = distance;

                if (CountItems == 2)
                {
                    SelectItem = 0;
                }
                else
                {
                    if (i == 0) { SelectItem = i + 1; }
                    else if (i == CountItems - 1) { SelectItem = i - 1; }
                    else { SelectItem = i; }
                }
            }

            float scale = Mathf.Clamp(1 / (distance / ItemOffset) * ScaleOffset, 0.5f, 1f);
            ItemScale[i].x = Mathf.SmoothStep(Item[i].transform.localScale.x, scale, 10 * Time.fixedDeltaTime);
            ItemScale[i].y = Mathf.SmoothStep(Item[i].transform.localScale.y, scale, 10 * Time.fixedDeltaTime);
            Item[i].transform.localScale = ItemScale[i];
        }

        float ScrollVelocity = Mathf.Abs(ScrollRect.velocity.x);

        if (ScrollVelocity < 800 && !IsScrolling) ScrollRect.inertia = false;
        if (IsScrolling || ScrollVelocity > 800) return;

        if (CountItems > 1)
        {
            ScrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            ContentVector.x = Mathf.SmoothStep(ContentRect.anchoredPosition.x, ItemPos[SelectItem].x, SnapSpeed * Time.fixedDeltaTime);
            ContentRect.anchoredPosition = ContentVector;
        }
        else { ScrollRect.movementType = ScrollRect.MovementType.Clamped; }
    }

    public void Skrolling(bool scroll)
    {
        IsScrolling = scroll;

        if (scroll) ScrollRect.inertia = true;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }

    [Serializable]
    public class ItemJS
    {
        public string name;
        public int id, price;
    }
}