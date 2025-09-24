// Assets/Scripts/Garden/GardenSession.cs
using System;
using UnityEngine;

namespace ClashFarm.Garden
{
    public sealed class GardenSession : MonoBehaviour
    {
        public static GardenSession I { get; private set; }

        [Header("Server Time (UTC)")]
        public long ServerTimeAtLoginMs;
        public float RealtimeAtLoginS;
        #if UNITY_EDITOR
        [Header("Debug (Editor)")]
        [Tooltip("Додатковий зсув часу в секундах для QA (лише в Editor).")]
        public int DebugTimeOffsetSec = 0;
        #endif

        [Header("Plots (max 12)")]
        public PlotState[] Plots = new PlotState[12];

        public bool IsReady { get; private set; }
        public event Action OnReady;

        [Header("Player Identity (temp)")]
        public string PlayerName;          // TODO: заповни з твого PlayerSession
        public string PlayerSerialCode;    // TODO: заповни з твого PlayerSession
        private bool _isInitializing;


        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < Plots.Length; i++)
                Plots[i] = new PlotState { SlotIndex = i, Unlocked = i < 3 }; // дефолт до першого /state
        }

        public long NowServerLikeMs()
        {
            // базове "серверне" зараз = час логіну + пройдений realtime
            long ms = ServerTimeAtLoginMs + (long)((Time.realtimeSinceStartup - RealtimeAtLoginS) * 1000f);

            // у Editor дозволяємо QA зсув
            #if UNITY_EDITOR
            ms += (long)DebugTimeOffsetSec * 1000L;
            #endif

            return ms;
        }

        /// <summary> Ініт з головного лоадера. Тягне /state і заповнює Plots. </summary>
        public async void Init()
        {
            if (IsReady || _isInitializing) return;
            _isInitializing = true;
            try
            {
                var resp = await GardenApi.GetStateAsync(PlayerName, PlayerSerialCode);
                ServerTimeAtLoginMs = resp.serverTimeMs;
                RealtimeAtLoginS = Time.realtimeSinceStartup;

                for (int i = 0; i < Plots.Length; i++)
                    Plots[i].Unlocked = i < resp.unlockedSlots;

                if (resp.plots != null)
                    foreach (var p in resp.plots)
                        MergeFromDto(p);

                IsReady = true;
                OnReady?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"GardenSession.Init failed: {e}");
            }
            finally { _isInitializing = false; }
        }

        public void MergeFromDto(GardenApi.PlotDto p)
        {
            if (p.slot < 0 || p.slot >= Plots.Length) return;
            var s = Plots[p.slot] ?? new PlotState { SlotIndex = p.slot };
            s.Unlocked = true;

            s.PlantedID = p.plantedId;
            s.Stage = (byte)Mathf.Clamp(p.stage, 0, 3);
            s.OnPlanted = s.Stage > 0;
            s.NeedWater = p.needsWater;
            s.Weeds = p.hasWeeds;

            s.OnPlantedTimeMs = p.onPlantedTime;
            s.NextStageTimeMs = p.nextStageTime;
            s.TimeEndGrowthMs = p.timeEndGrowth;

            s.SellPrice = p.sellPrice;

            Plots[p.slot] = s;
        }

        [Serializable]
        public class PlotState
        {
            [Header("Slot")]
            public int SlotIndex;
            public bool Unlocked;

            [Header("State")]
            public bool OnPlanted;
            public bool NeedWater;   // полив ще не зроблено в поточній стадії
            public bool Weeds;

            [Header("Plant")]
            public int PlantedID;    // 0 якщо пусто
            [Range(0,3)] public byte Stage; // 0 пусто, 1 seed, 2 plant, 3 grown

            [Header("Timing (unix ms, UTC)")]
            public long OnPlantedTimeMs;
            public long NextStageTimeMs; // 0 якщо Stage==0 або 3
            public long TimeEndGrowthMs; // 0 якщо Stage==0

            [Header("Economy")]
            public int SellPrice;

            // внутрішні технічні поля, якщо знадобляться в UI чи дебагу
            [NonSerialized] public int WeedExtraSecApplied;
        }
    }
}
