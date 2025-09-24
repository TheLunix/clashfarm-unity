using UnityEngine;

namespace ClashFarm.Garden
{
    public sealed class GardenSfx : MonoBehaviour
    {
        public static GardenSfx I { get; private set; }

        [Header("Audio")]
        public AudioSource source;
        [Range(0f, 1f)] public float volume = 0.9f;
        [Tooltip("Випадкова варіація висоти тону для 'живості' звуку")]
        public Vector2 pitchJitter = new Vector2(0.98f, 1.02f);

        [Header("Clips")]
        public AudioClip sfxPlant;
        public AudioClip sfxWater;
        public AudioClip sfxWeed;
        public AudioClip sfxHarvest;
        public AudioClip sfxUnlock;
        public AudioClip sfxError;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            //DontDestroyOnLoad(gameObject);
        }

        void Play(AudioClip clip)
        {
            if (!source || !clip) return;
            float old = source.pitch;
            source.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
            source.PlayOneShot(clip, volume);
            source.pitch = old;
        }

        public void PlayPlant()   => Play(sfxPlant);
        public void PlayWater()   => Play(sfxWater);
        public void PlayWeed()    => Play(sfxWeed);
        public void PlayHarvest() => Play(sfxHarvest);
        public void PlayUnlock()  => Play(sfxUnlock);
        public void PlayError()   => Play(sfxError);
    }
}
