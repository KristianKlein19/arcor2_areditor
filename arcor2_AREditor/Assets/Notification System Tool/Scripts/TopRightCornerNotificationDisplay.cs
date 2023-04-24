using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using SystemAction = System.Action;

namespace Base
{
    public class TopRightCornerNotificationDisplay : Singleton<TopRightCornerNotificationDisplay>, INotificationDisplay
    {
        [SerializeField] private GameObject notificationErrorPrefab;

        [SerializeField] private GameObject notificationWarningPrefab;

        [SerializeField] private GameObject notificationSuccessPrefab;

        [SerializeField] private Canvas parentCanvas;

        private TMP_Text _notificationDetail;

        private GameObject _notificationInstance;

        private GameObject _prefabToUse;

        private Slider _slider;

        private readonly float _fadingOffset = 1f;

        private float _displayTime;

        private Dictionary<NotificationManager.NotificationType, GameObject> _notificationInstances;

        private TopRightCornerNotificationDisplay()
        {
        }

        private void Start()
        {
            if (notificationErrorPrefab == null || notificationWarningPrefab == null || notificationSuccessPrefab == null)
            {
                Debug.LogError("Notification prefab is not assigned.");
                return;
            }

            _notificationInstances = new Dictionary<NotificationManager.NotificationType, GameObject>
            {
                { NotificationManager.NotificationType.Error, Instantiate(notificationErrorPrefab, parentCanvas.transform, false) },
                { NotificationManager.NotificationType.Warning, Instantiate(notificationWarningPrefab, parentCanvas.transform, false) },
                { NotificationManager.NotificationType.Success, Instantiate(notificationSuccessPrefab, parentCanvas.transform, false) }
            };

            foreach (GameObject instance in _notificationInstances.Values)
            {
                instance.SetActive(false);
            }
        }

        private GameObject GetPooledNotification(NotificationManager.NotificationType type)
        {
            return _notificationInstances.TryGetValue(type, out GameObject notificationInstance) ? notificationInstance : null;
        }

        public void DisplayNotification(NotificationManager.Notification notification, SystemAction onHide)
        {
            GameObject notificationInstance = GetPooledNotification(notification.Type);
            if (notificationInstance == null)
            {
                Debug.LogError("Notification instance not created.");
                return;
            }

            _notificationDetail = notificationInstance.GetComponentInChildren<TMP_Text>();
            if (_notificationDetail == null)
            {
                Debug.LogError("TMP_Text component not found on the prefab.");
                return;
            }

            // Setting notification detail text
            _notificationDetail.text = notification.Text;

            _slider = notificationInstance.GetComponentInChildren<Slider>();
            _displayTime = EvaluateDisplayTime(notification.Text);
            _slider.maxValue = _displayTime;
            _slider.value = _slider.maxValue;

            ActivateDisplayObject(notificationInstance,() =>
            {
                StartCoroutine(HideMessageAfterDelay(() =>
                {
                    DeactivateDisplayObject(notificationInstance,() =>
                    {
                        onHide?.Invoke();
                    });
                }));
            });
        }

        private IEnumerator HideMessageAfterDelay(SystemAction onHide)
        {
            float elapsedTime = 0f;

            while (elapsedTime + 0.1f < _displayTime)
            {
                elapsedTime += Time.deltaTime;
                _slider.value = _displayTime - elapsedTime;
                yield return null;
            }

            onHide?.Invoke();
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
            Debug.Log("Object is activating");
        }

        private void DeactivateDisplayObject(GameObject notificationInstance, SystemAction onComplete)
        {
            CanvasGroup canvasGroup = notificationInstance.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadeOut(canvasGroup, () =>
                {
                    notificationInstance.SetActive(false);
                    onComplete?.Invoke();
                }));
            }
            else
            {
                Debug.LogWarning("CanvasGroup component not found on the notification instance.");
                notificationInstance.SetActive(false);
                onComplete?.Invoke();
            }
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

        private float EvaluateDisplayTime(string notificationText)
        {
            int textLenght = notificationText.Length;
            _displayTime = Mathf.Clamp(textLenght, 2000, 7000) / 1000 + _fadingOffset;

            return _displayTime;
        }
    }
}
