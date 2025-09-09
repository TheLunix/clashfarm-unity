using System.Collections;
using UnityEngine;

public static class GardenClickGate
{
    public static bool Ready { get; private set; } = true;

    public static void Arm(MonoBehaviour host, int frames = 2)
    {
        if (host == null) { Ready = true; return; }
        host.StartCoroutine(Co(frames));
    }

    static IEnumerator Co(int frames)
    {
        Ready = false;
        for (int i = 0; i < frames; i++)
            yield return null;
        Ready = true;
    }
}
