using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hub sahnesinde etkilesime girilebilir objelere eklenen trigger script.
/// Oyuncu yaklasinca "[E] ..." yazisi cikar, E'ye basinca belirtilen aksiyonu tetikler.
/// 
/// Prompt, Screen Space Canvas uzerinde gosterilir — her zaman render edilir.
/// </summary>
public class HubInteractable : MonoBehaviour
{
    public enum InteractionType
    {
        OpenShop,
        StartRun,
        GoToMainMenu,
        OpenBlacksmith
    }

    [Header("Settings")]
    public InteractionType interactionType = InteractionType.OpenShop;

    [Header("Prompt")]
    [Tooltip("Prompt yazisi. Bos birakirsan otomatik yazilir.")]
    public string promptText = "";

    [Header("Detection")]
    [Tooltip("Oyuncunun etkilesim mesafesi")]
    public float interactionRadius = 2.5f;

    private bool playerInRange = false;
    private Transform playerTransform;

    // Screen Space prompt
    private GameObject promptObj;
    private TextMeshProUGUI promptTMP;
    private Canvas uiCanvas;
    private Camera mainCam;
    private bool forcedHidden = false; // Shop acikken prompt gizlenir

    private void Start()
    {
        mainCam = Camera.main;
        FindOrCreateCanvas();
        CreateScreenPrompt();

        if (promptObj != null)
            promptObj.SetActive(false);
    }

    /// <summary>
    /// Tum HubInteractable prompt'larini gizle (shop acilinca cagirilir).
    /// </summary>
    public static void HideAllPrompts()
    {
        foreach (var interactable in FindObjectsByType<HubInteractable>(FindObjectsSortMode.None))
        {
            interactable.forcedHidden = true;
            if (interactable.promptObj != null)
                interactable.promptObj.SetActive(false);
        }
    }

    /// <summary>
    /// Tum HubInteractable prompt'larini tekrar gosterilebilir yap (shop kapaninca cagirilir).
    /// </summary>
    public static void ShowAllPrompts()
    {
        foreach (var interactable in FindObjectsByType<HubInteractable>(FindObjectsSortMode.None))
        {
            interactable.forcedHidden = false;
            if (interactable.promptObj != null)
                interactable.promptObj.SetActive(interactable.playerInRange && !ModalUIManager.HasOpenModal);
        }
    }

    private void Update()
    {
        // Player'i bul
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        // Mesafe kontrolu
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactionRadius;

        if (inRange && !playerInRange)
        {
            playerInRange = true;
            if (promptObj != null && !forcedHidden && !ModalUIManager.HasOpenModal) promptObj.SetActive(true);
        }
        else if (!inRange && playerInRange)
        {
            playerInRange = false;
            if (promptObj != null) promptObj.SetActive(false);
        }

        if (promptObj != null && ModalUIManager.HasOpenModal && promptObj.activeSelf)
            promptObj.SetActive(false);

        // Prompt pozisyonunu guncelle (objenin ustunde takip etsin)
        if (playerInRange && promptObj != null && mainCam != null)
        {
            Vector3 worldPos = transform.position + new Vector3(0f, 0.7f, 0f);
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
            promptObj.transform.position = screenPos;
        }

        // E tusuna bas
        if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    private void OnDestroy()
    {
        if (promptObj != null)
            Destroy(promptObj);
    }

    private void Interact()
    {
        switch (interactionType)
        {
            case InteractionType.OpenShop:
                if (HubManager.Instance != null)
                    HubManager.Instance.ToggleShop();
                break;

            case InteractionType.StartRun:
                if (HubManager.Instance != null)
                    HubManager.Instance.StartRun();
                break;

            case InteractionType.GoToMainMenu:
                if (HubManager.Instance != null)
                    HubManager.Instance.GoToMainMenu();
                break;

            case InteractionType.OpenBlacksmith:
                if (HubManager.Instance != null)
                    HubManager.Instance.ToggleBlacksmith();
                break;
        }
    }

    /// <summary>
    /// Sahnedeki Screen Space Canvas'i bul veya yoksa olustur.
    /// </summary>
    private void FindOrCreateCanvas()
    {
        // Sahnede "PromptCanvas" adli bir canvas ara
        GameObject existing = GameObject.Find("PromptCanvas");
        if (existing != null)
        {
            uiCanvas = existing.GetComponent<Canvas>();
            if (uiCanvas != null) return;
        }

        // Yoksa yeni bir Screen Space Overlay canvas olustur
        GameObject canvasObj = new GameObject("PromptCanvas");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100; // Modal panellerin altinda, normal hub ustunde

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// Screen Space Canvas uzerinde prompt text olusturur.
    /// </summary>
    private void CreateScreenPrompt()
    {
        if (uiCanvas == null) return;

        promptObj = new GameObject("Prompt_" + gameObject.name);
        promptObj.transform.SetParent(uiCanvas.transform, false);

        // Arkaplan
        Image bg = promptObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // Boyut
        RectTransform rect = promptObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 40f);
        rect.pivot = new Vector2(0.5f, 0f); // Alt orta

        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptObj.transform, false);
        promptTMP = textObj.AddComponent<TextMeshProUGUI>();
        promptTMP.text = GetDefaultPromptText();
        promptTMP.fontSize = 22f;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.color = Color.yellow;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 2f);
        textRect.offsetMax = new Vector2(-5f, -2f);
    }

    private string GetDefaultPromptText()
    {
        if (!string.IsNullOrEmpty(promptText)) return promptText;

        switch (interactionType)
        {
            case InteractionType.OpenShop:
                return "[E] Dukkani Ac";
            case InteractionType.StartRun:
                return "[E] Savasa Basla";
            case InteractionType.GoToMainMenu:
                return "[E] Ana Menu";
            case InteractionType.OpenBlacksmith:
                return "[E] Demirci";
            default:
                return "[E] Etkilesim";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
