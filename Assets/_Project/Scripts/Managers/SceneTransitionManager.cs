using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// Sahneler arasi Fade (ekan kararma/aydinlanma) gecislerini yoneten Singleton Manager.
/// Canvas ve Image nesneleri KOD ILE olusturulur, sahnelere bagli degildir.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Settings")]
    public float fadeDuration = 0.5f;

    private CanvasGroup fadeCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.parent = null;
        DontDestroyOnLoad(gameObject);

        // Canvas ve BlackScreen'i KOD ILE olustur ki sahnelerle birlikte yok olmasin.
        CreateFadeCanvas();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void CreateFadeCanvas()
    {
        // Yeni Canvas olustur
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform); // Manager'in altina al - DontDestroyOnLoad ile beraber gelsin
        
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Her seyin ustunde olsun

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Siyah Image olustur
        GameObject imageGO = new GameObject("BlackScreen");
        imageGO.transform.SetParent(canvasGO.transform, false);

        Image img = imageGO.AddComponent<Image>();
        img.color = Color.black;

        // Ekrani tamamen kapla
        RectTransform rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // CanvasGroup ekle (alpha kontrolu icin)
        fadeCanvasGroup = imageGO.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Her yeni sahne yuklendiginde ekran siyahtan aydinliga acisin
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, fadeDuration, null));
    }

    public void FadeIn(Action onComplete = null)
    {
        if (fadeCanvasGroup != null)
            StartCoroutine(FadeRoutine(1f, 0f, fadeDuration, onComplete));
        else
            onComplete?.Invoke();
    }

    public void FadeOut(Action onComplete = null)
    {
        if (fadeCanvasGroup != null)
            StartCoroutine(FadeRoutine(0f, 1f, fadeDuration, onComplete));
        else
            onComplete?.Invoke();
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Action onComplete)
    {
        if (fadeCanvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = endAlpha;

        if (endAlpha <= 0f)
            fadeCanvasGroup.blocksRaycasts = false;

        onComplete?.Invoke();
    }
}
