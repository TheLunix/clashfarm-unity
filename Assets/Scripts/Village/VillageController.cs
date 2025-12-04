using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClashFarm.Village
{
    // Перелік локацій всередині панелі Village
    public enum LocationType
    {
        Druid,      // Друїд
        Witch,      // Відьма
        Blacksmith, // Коваль
        Monk,       // Монах
        Home,       // Дім
        Clan,       // Клан
        Shop,       // Магазин
        Market,     // Базар
        Livestock,  // Тваринник
        Outskirts,  // Околиці
        Travel,     // Подорож
        Mine        // Шахта
    }

    [Serializable]
    public class LocationEntry
    {
        [Header("Ідентифікатор локації")]
        public LocationType type;

        [Header("Панель локації (вмикаємо/вимикаємо)")]
        public GameObject panel;

        [Header("Опційна кнопка (якщо хочеш автопідв'язку)")]
        public Button button;

        [Tooltip("Повторним кліком по кнопці — закрити панель (залишитись у Village-хабі)")]
        public bool toggleOnSecondClick = false;
    }

    public class VillageController : MonoBehaviour
    {
        [Header("Хаби верхнього рівня (ті самі панелі, що ти вже маєш у сцені)")]
        [SerializeField] private GameObject mainHub;    // твій mainMenuPanel / мейн-хаб
        [SerializeField] private GameObject villageHub; // панель-сцена Village (контейнер кнопок і підпанелей)

        [Header("Локації всередині Village")]
        [SerializeField] private List<LocationEntry> locations = new();

        [Header("Loading Controller (опційно)")]
        [Tooltip("Обʼєкт із скриптом LoadingController — виклики StartLoading/StopLoading через SendMessage")]
        [SerializeField] private GameObject loadingController;

        // Поточна відкрита локація всередині Village
        private LocationEntry _current;

        private void OnEnable()
        {
            // Автопідв'язка кнопок до OpenLocation
            foreach (var loc in locations)
            {
                if (loc?.button == null) continue;
                var captured = loc.type;
                loc.button.onClick.RemoveAllListeners();
                loc.button.onClick.AddListener(() => OpenLocation(captured));
            }

            // На старті всі внутрішні панелі вимкнені
            CloseAllInternal();
        }

        // ================== ПУБЛІЧНІ МЕТОДИ ДЛЯ КНОПОК (без параметрів) ==================

        // Вхід у Village-хаб (кнопка "Село" з мейну)
        public void OpenVillageHub()
        {
            BeginLoading();
            try
            {
                if (mainHub) mainHub.SetActive(false);
                if (villageHub) villageHub.SetActive(true);
                CloseAllInternal();
                _current = null;
            }
            finally { EndLoading(); }

            if (Navigation.Instance != null)
                Navigation.Instance.OnVillageHubOpened();
        }

        // Вихід назад у мейн
        public void ReturnToMain()
        {
            BeginLoading();
            try
            {
                CloseAllInternal();
                _current = null;
                if (villageHub) villageHub.SetActive(false);
                if (mainHub) mainHub.SetActive(true);
            }
            finally { EndLoading(); }

            if (Navigation.Instance != null)
                Navigation.Instance.OnMainMenuOpened();
        }

        // ——— ЛОКАЦІЇ (готові до призначення в OnClick) ———
        public void OpenMonk()       => OpenLocation(LocationType.Monk);
        public void OpenDruid()      => OpenLocation(LocationType.Druid);
        public void OpenWitch()      => OpenLocation(LocationType.Witch);
        public void OpenBlacksmith() => OpenLocation(LocationType.Blacksmith);
        public void OpenHome()       => OpenLocation(LocationType.Home);
        public void OpenClan()       => OpenLocation(LocationType.Clan);
        public void OpenShop()       => OpenLocation(LocationType.Shop);
        public void OpenMarket()     => OpenLocation(LocationType.Market);
        public void OpenLivestock()  => OpenLocation(LocationType.Livestock);

        // Закрити поточну локацію, залишитись у Village-хабі
        public void CloseCurrent()   => CloseCurrentInternal();

        // ================== ОСНОВНА ЛОГІКА ==================

        public void OpenLocation(LocationType type)
        {
            // гарантуємо, що Village-хаб увімкнений
            if (villageHub != null && !villageHub.activeSelf)
                OpenVillageHub();

            var entry = locations.FirstOrDefault(l => l != null && l.type == type);
            if (entry == null || entry.panel == null)
            {
                Debug.LogWarning($"[VillageController] Не знайдено панелі для: {type}");
                return;
            }

            // toggle на повторний клік
            if (_current == entry && entry.toggleOnSecondClick)
            {
                CloseCurrentInternal();
                return;
            }

            BeginLoading();
            try
            {
                CloseAllInternal();
                entry.panel.SetActive(true);
                _current = entry;
            }
            finally { EndLoading(); }

            // Повідомляємо Navigation, що ми тепер у конкретній локації
            if (Navigation.Instance != null)
                Navigation.Instance.OnVillageLocationOpened(type);
        }

        public void OpenOutskirts()
        {
            if (BlockIfInTravel()) return;
            OpenLocation(LocationType.Outskirts);
        }

        public void OpenMine()
        {
            if (BlockIfInTravel()) return;
            OpenLocation(LocationType.Mine);
        }

        // Подорож себе саму відкривати може
        public void OpenTravel()
        {
            OpenLocation(LocationType.Travel);
        }

        // ================== УТИЛІТИ ==================
        private void CloseAllInternal()
        {
            foreach (var loc in locations)
                if (loc?.panel) loc.panel.SetActive(false);
        }

        private void CloseCurrentInternal()
        {
            BeginLoading();
            try
            {
                if (_current?.panel) _current.panel.SetActive(false);
                _current = null;
            }
            finally { EndLoading(); }
        }

        private void BeginLoading()
        {
            if (loadingController)
                loadingController.SendMessage("StartLoading", SendMessageOptions.DontRequireReceiver);
        }
        private void EndLoading()
        {
            if (loadingController)
                loadingController.SendMessage("StopLoading", SendMessageOptions.DontRequireReceiver);
        }

        private bool IsTravelActiveClient()
        {
            if (PlayerSession.I == null || PlayerSession.I.Data == null) return false;

            var info = PlayerSession.I.Data;
            if (string.IsNullOrEmpty(info.timetoendhike) || info.timetoendhike == "0") return false;

            if (!DateTime.TryParse(info.timetoendhike, null,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var endUtc))
                return false;

            return DateTime.UtcNow < endUtc;
        }

        private bool BlockIfInTravel()
        {
            if (IsTravelActiveClient())
            {
                Debug.LogWarning("[Village] Гравець у подорожі — ця локація недоступна.");
                // тут пізніше можна повісити popup "Спочатку завершіть подорож"
                return true;
            }
            return false;
        }
    }
}
