using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Manages the achievement system, tracking unlocks and displaying Steam/Xbox style notifications.
public class AchievementManager : MonoBehaviour {

    public static AchievementManager Instance { get; private set; }

    [System.Serializable]
    public class Achievement {
        public string id;
        public string title;
        public string description;
        public string emoji;
        public bool unlocked;

        public Achievement(string id, string title, string description, string emoji) {
            this.id = id;
            this.title = title;
            this.description = description;
            this.emoji = emoji;
            this.unlocked = false;
        }
    }

    [Header("UI Prefab (Opcional)")]
    [SerializeField] private GameObject achievementPopupPrefab;

    private Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
    private Queue<Achievement> notificationQueue = new Queue<Achievement>();
    private bool isShowingNotification = false;
    private int mathSolvesThisRound = 0;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAchievements();
        } else {
            Destroy(gameObject);
        }
    }

    private void InitializeAchievements() {
        AddAchievement("math_first_unlock", "Desbloqueo Matemático", "¡Desbloqueaste tu primera casilla bloqueada!", "🏆");
        AddAchievement("math_mult_unlock", "Genio del Producto", "¡Resolviste una multiplicación correctamente!", "✖");
        AddAchievement("math_div_unlock", "Fracción Precisa", "¡Resolviste una división correctamente!", "➗");
        AddAchievement("math_speed_unlock", "Calculadora Veloz", "¡Resolviste un reto en menos de 3.5 segundos!", "⚡");
        AddAchievement("math_mod_unlock", "Módulo Maestro", "¡Resolviste un módulo correctamente!", "🧠");
        AddAchievement("math_triple_unlock", "Defensa de Redes", "¡Desbloqueaste 3 casillas en la misma ronda!", "🛡");
        AddAchievement("level_2_reach", "Racha de Cables", "¡Completaste el primer nivel con éxito!", "⭐");
    }

    private void AddAchievement(string id, string title, string description, string emoji) {
        achievements[id] = new Achievement(id, title, description, emoji);
    }

    public void UnlockAchievement(string id) {
        if (achievements.TryGetValue(id, out Achievement ach)) {
            if (!ach.unlocked) {
                ach.unlocked = true;
                Debug.Log($"[ACHIEVEMENT UNLOCKED] {ach.title}: {ach.description}");
                notificationQueue.Enqueue(ach);

                if (!isShowingNotification) {
                    StartCoroutine(ProcessNotificationQueue());
                }
            }
        }
    }

    public void IncrementSolvesThisRound() {
        mathSolvesThisRound++;
        if (mathSolvesThisRound >= 3) {
            UnlockAchievement("math_triple_unlock");
        }
    }

    public void ResetSolvesThisRound() {
        mathSolvesThisRound = 0;
    }

    private IEnumerator ProcessNotificationQueue() {
        isShowingNotification = true;

        while (notificationQueue.Count > 0) {
            Achievement ach = notificationQueue.Dequeue();
            yield return StartCoroutine(ShowNotificationCoroutine(ach));
        }

        isShowingNotification = false;
    }

    private IEnumerator ShowNotificationCoroutine(Achievement ach) {
        GameObject popupGO = null;

        if (achievementPopupPrefab != null) {
            popupGO = Instantiate(achievementPopupPrefab);
            var texts = popupGO.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts) {
                if (t.gameObject.name.Contains("Title") || t.text.Contains("LOGRO")) {
                    t.text = "¡LOGRO DESBLOQUEADO!";
                } else if (t.gameObject.name.Contains("Name") || t.gameObject.name.Contains("Achievement")) {
                    t.text = ach.emoji + " " + ach.title;
                }
            }
        } else {
            popupGO = CreateDynamicPopupUI(ach);
        }

        if (popupGO == null) {
            yield break;
        }

        RectTransform rect = popupGO.GetComponent<RectTransform>();
        
        float startX = 340f;
        float endX = -20f;
        float yPos = 20f; // Padding from bottom

        rect.anchoredPosition = new Vector2(startX, yPos);

        // Slide In
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            float currentX = Mathf.Lerp(startX, endX, t);
            rect.anchoredPosition = new Vector2(currentX, yPos);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(endX, yPos);

        // Wait
        yield return new WaitForSeconds(3.5f);

        // Slide Out
        elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            float currentX = Mathf.Lerp(endX, startX, t);
            rect.anchoredPosition = new Vector2(currentX, yPos);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(startX, yPos);

        // Cleanup
        Destroy(popupGO);
    }

    private GameObject CreateDynamicPopupUI(Achievement ach) {
        GameObject canvasGO = GameObject.Find("AchievementCanvas");
        if (canvasGO == null) {
            canvasGO = new GameObject("AchievementCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            DontDestroyOnLoad(canvasGO);
        }

        GameObject popup = new GameObject("AchievementPopup");
        popup.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = popup.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(310f, 65f);

        Image bgImage = popup.AddComponent<Image>();
        bgImage.color = new Color(0.06f, 0.06f, 0.08f, 0.94f);

        Outline outline = popup.AddComponent<Outline>();
        outline.effectColor = new Color(1.0f, 0.82f, 0.15f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject iconGO = new GameObject("IconText");
        iconGO.transform.SetParent(popup.transform, false);
        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(45f, 45f);
        iconRect.anchoredPosition = new Vector2(12f, 0f);

        TextMeshProUGUI iconText = iconGO.AddComponent<TextMeshProUGUI>();
        iconText.text = ach.emoji;
        iconText.fontSize = 26;
        iconText.alignment = TextAlignmentOptions.Center;

        GameObject subGO = new GameObject("SubheaderText");
        subGO.transform.SetParent(popup.transform, false);
        RectTransform subRect = subGO.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0f, 1f);
        subRect.offsetMin = new Vector2(65f, -28f);
        subRect.offsetMax = new Vector2(-10f, -8f);

        TextMeshProUGUI subText = subGO.AddComponent<TextMeshProUGUI>();
        subText.text = "¡LOGRO DESBLOQUEADO!";
        subText.fontSize = 10;
        subText.color = new Color(1.0f, 0.82f, 0.15f);
        subText.fontStyle = FontStyles.Bold;

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(popup.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(65f, -57f);
        titleRect.offsetMax = new Vector2(-10f, -28f);

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = ach.title;
        titleText.fontSize = 13;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;

        return popup;
    }
}
