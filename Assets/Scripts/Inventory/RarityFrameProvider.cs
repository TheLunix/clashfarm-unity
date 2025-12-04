using UnityEngine;

public class RarityFrameProvider : MonoBehaviour
{
    public static RarityFrameProvider Instance { get; private set; }

    [Header("Frames by ItemLevel")]
    [SerializeField] private Sprite basicFrame;       // 1–3
    [SerializeField] private Sprite advancedFrame;    // 4–6
    [SerializeField] private Sprite expertFrame;      // 7–9
    [SerializeField] private Sprite improvedFrame;    // 10–12
    [SerializeField] private Sprite legendaryFrame;   // 13+

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // виправлення твого попереднього варнінга
        DontDestroyOnLoad(transform.root.gameObject);
    }

    /// <summary>
    /// Отримати рамку тільки по ItemLevel.
    /// Rarity більше НЕ використовується.
    /// </summary>
    public Sprite GetFrame(int itemLevel)
    {
        if (itemLevel <= 3)
            return basicFrame;

        if (itemLevel <= 6)
            return advancedFrame;

        if (itemLevel <= 9)
            return expertFrame;

        if (itemLevel <= 12)
            return improvedFrame;

        return legendaryFrame;
    }
}
