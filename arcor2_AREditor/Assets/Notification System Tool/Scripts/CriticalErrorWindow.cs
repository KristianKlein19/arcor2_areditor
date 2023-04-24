using System;
using System.Collections;
using UnityEngine;
using Base;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using SystemAction = System.Action;

public class CriticalErrorWindow : Singleton<CriticalErrorWindow>, INotificationDisplay
{
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private TMP_Text windowTitleText;
    [SerializeField] private TMP_Text windowsBodyText;
    [SerializeField] private AudioClip errorSound;

    private AudioSource _audioSource;

    private AndroidSharing androidSharing;

    private Button _saveBtn;

    private Button _shareBtn;

    private Button _logoutCloseButton;

    private TMP_Text _notificationDetail;

    private GameObject _notificationInstance;

    private string textToLog;

    private CriticalErrorWindow()
    {
    }

    private void Start()
    {
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>();
        }

        _audioSource = GetComponent<AudioSource>();

        if (notificationPrefab == null)
        {
            Debug.LogError("Notification prefab is not assigned");
        }

        _notificationInstance = Instantiate(notificationPrefab, parentCanvas.transform, false);
        _notificationInstance.SetActive(false);

        // Get buttons from the instantiated prefab
        _saveBtn = FindComponentInChildrenByName<Button>(_notificationInstance, "SaveButton");
        _shareBtn = FindComponentInChildrenByName<Button>(_notificationInstance, "ShareButton");
        _logoutCloseButton = FindComponentInChildrenByName<Button>(_notificationInstance, "LogoutButton");

        if (_saveBtn == null || _shareBtn == null || _logoutCloseButton == null)
        {
            Debug.LogError("One or more buttons not found in the instantiated prefab");
            return;
        }

        _saveBtn.onClick.AddListener(OnSaveButtonClicked);
        _shareBtn.onClick.AddListener(OnShareButtonClicked);
        _logoutCloseButton.onClick.AddListener(OnCloseLogoutButtonClicked);

        androidSharing = new AndroidSharing();
    }

    private void PlayCriticalErrorSound()
    {
        if (errorSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(errorSound);
        }
    }

    private void OnCloseLogoutButtonClicked()
    {
        Application.Quit();
    }

    private void OnShareButtonClicked()
    {
        Debug.Log(textToLog);
        androidSharing.ShareTextAsLogFile(textToLog);
    }

    private void OnSaveButtonClicked()
    {
        StartCoroutine(androidSharing.SaveLogFileToDevice(textToLog));
    }

    public void DisplayNotification(NotificationManager.Notification notification, SystemAction onHide)
    {
        PlayCriticalErrorSound();

        TMP_Text windowBodyText = FindComponentInChildrenByName<TMP_Text>(_notificationInstance, "HeadBodyText");
        TMP_Text windowsHeadTitleText = FindComponentInChildrenByName<TMP_Text>(_notificationInstance, "TitleBarText");

        windowBodyText.text = notification.Text;
        textToLog = notification.Text;
        windowsHeadTitleText.text = notification.TitleText;

        ActivateDisplayObject(_notificationInstance, () =>
        {
            Debug.Log("Window open.");
        }
        );

    }

    private void ActivateDisplayObject(GameObject notificationInstance, SystemAction onComplete)
    {
        notificationInstance.SetActive(true);
        CanvasGroup canvasGroup = notificationInstance.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            StartCoroutine(FadeIn(canvasGroup, onComplete));
        }
        else
        {
            Debug.LogWarning("CanvasGroup component not found on the notification instance.");
            onComplete?.Invoke();
        }
    }

    private T FindComponentInChildrenByName<T>(GameObject obj, string name) where T : Component
    {
        T[] componentsInChildren = obj.GetComponentsInChildren<T>(true);

        foreach (T component in componentsInChildren)
        {
            if (component.gameObject.name == name)
            {
                return component;
            }
        }

        return null;
    }


    private static IEnumerator FadeIn(CanvasGroup canvasGroup, SystemAction onComplete)
    {
        canvasGroup.gameObject.SetActive(true);
        Tween fadeTween = canvasGroup.DOFade(1f, 0.5f);
        yield return fadeTween.WaitForCompletion();
        onComplete?.Invoke();
    }

    private static IEnumerator FadeOut(CanvasGroup canvasGroup, SystemAction onComplete)
    {
        Tween fadeTween = canvasGroup.DOFade(0f, 0.5f);
        yield return fadeTween.WaitForCompletion();
        onComplete?.Invoke();
    }
}
