using System;
using System.Collections;
using System.Collections.Generic;
using SystemAction = System.Action;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Base
{
    public class TopRightCornerNotificationIn3DSpace : Singleton<TopRightCornerNotificationIn3DSpace>, INotificationDisplay
    {
        [SerializeField] private GameObject notificationErrorPrefab;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private GameObject messageDetailPrefab;
        [SerializeField] private AudioClip errorSound;

        private AudioSource _audioSource;

        private readonly float _fadingOffset = 3f;

        private Button _iconExpandBtn;

        private Vector3 _iconPosition;

        private readonly Dictionary<Transform, Color> _originalColors = new();

        private Dictionary<Enum, GameObject> _sceneInstances;

        private TMP_Text _notificationDetail;

        private GameObject _notificationInstance;

        private GameObject _iconInstance;

        private GameObject _buttonContainerInstance;

        private GameObject _prefabToUse;

        private GameObject _messageDetailInstance;

        private Slider _slider;

        private Button _expandButton;

        private CanvasGroup _notificationCanvasGroup;

        private CanvasGroup _iconCanvasGroup;

        private Color _originalColor;

        private float _remainingDisplayTime;

        private float _displayTime;

        private Coroutine _hideMessageCoroutine;

        private bool _isFading;

        private Camera arCamera;


        private TopRightCornerNotificationIn3DSpace()
        {
        }

        private void Start()
        {
            arCamera = Camera.main;

            if (GetComponent<AudioSource>() == null)
            {
                gameObject.AddComponent<AudioSource>();
            }

            _audioSource = GetComponent<AudioSource>();

            if (notificationErrorPrefab == null)
            {
                Debug.LogError("Notification prefab is not assigned.");
                return;
            }

            _sceneInstances = new Dictionary<Enum, GameObject>()
            {
                {
                    ObjectTypeEnum.NotificationWindow,
                    Instantiate(notificationErrorPrefab, parentCanvas.transform, false)
                },
                { ObjectTypeEnum.Icon, Instantiate(iconPrefab) },
                { ObjectTypeEnum.MessageDetailPanel, Instantiate(messageDetailPrefab) }
            };

            foreach (GameObject instance in _sceneInstances.Values)
            {
                instance.SetActive(false);
            }

        }

        private enum ObjectTypeEnum
        {
            NotificationWindow,
            Icon,
            MessageDetailPanel
        }

        private GameObject GetPooledNotification(Enum type)
        {
            return _sceneInstances.TryGetValue(type, out GameObject sceneInstance) ? sceneInstance : null;
        }

        private T FindComponentInChildrenByName<T>(GameObject obj, string componentName) where T : Component
        {
            T[] componentsInChildren = obj.GetComponentsInChildren<T>(true);

            foreach (T component in componentsInChildren)
            {
                if (component.gameObject.name == componentName)
                {
                    return component;
                }
            }

            return null;
        }

        private void PlayErrorSound()
        {
            if (errorSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(errorSound);
            }
        }

        public void DisplayNotification(NotificationManager.Notification notification, SystemAction onHide)
        {
            if (_sceneInstances == null)
            {
                Debug.LogError("Dictionary of instances was not created.");
                return;
            }

            PlayErrorSound();

            _notificationInstance = GetPooledNotification(ObjectTypeEnum.NotificationWindow);
            _messageDetailInstance = GetPooledNotification(ObjectTypeEnum.MessageDetailPanel);
            _iconInstance = GetPooledNotification(ObjectTypeEnum.Icon);

            _iconExpandBtn = FindComponentInChildrenByName<Button>(_notificationInstance, "ExpandButton");
            _notificationDetail = _notificationInstance.GetComponentInChildren<TMP_Text>();

            _slider = _notificationInstance.GetComponentInChildren<Slider>();
            _remainingDisplayTime = EvaluateDisplayTime(notification.Text);
            _slider.maxValue = _remainingDisplayTime;
            _slider.value = _slider.maxValue;

            var targetObject = notification.ObjectPosition;

            InstantiateAndRegisterIconObject(notification, targetObject, onHide);

            ActivateDisplayObjects(_notificationInstance, targetObject,() =>
            {
                StartHideMessageCoroutine(notification, notification.ObjectPosition, onHide);
            });
        }

        private void InstantiateAndRegisterIconObject(NotificationManager.Notification notification, Transform targetObject, SystemAction onHide)
        {
            Bounds bounds = targetObject.GetComponent<Renderer>().bounds;
            Vector3 bottomRightCorner = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);


            if (arCamera != null)
            {
                var transform1 = arCamera.transform;
                Vector3 cameraPosition = transform1.position;
                Vector3 cameraDirection = transform1.forward;

                float distanceFromCameraToObject = Vector3.Distance(cameraPosition, bottomRightCorner);
                float lerpFactor = -0.05f;
                float distanceFromObject = distanceFromCameraToObject * lerpFactor;
                _iconPosition = bottomRightCorner + cameraDirection * distanceFromObject;

                // Update the transform of the pooled icon instance
                _iconInstance.transform.position = _iconPosition;
                _iconInstance.transform.rotation = Quaternion.LookRotation(cameraDirection, transform1.up);
                _iconInstance.transform.SetParent(targetObject.transform);
            }

            if (_iconExpandBtn != null)
            {
                _iconExpandBtn.onClick.RemoveAllListeners();
                _iconExpandBtn.onClick.AddListener(() =>
                {
                    ExpandNotification(notification, targetObject, _iconPosition, onHide);
                });
            }


            Icon3DClickHandler clickHandler = _iconInstance.GetComponent<Icon3DClickHandler>();
            if (clickHandler != null)
            {
                clickHandler.OnClick -= () => ExpandNotification(notification, targetObject, _iconPosition, onHide);
                clickHandler.OnClick += () => ExpandNotification(notification, targetObject, _iconPosition, onHide);
            }
            _iconCanvasGroup = _iconInstance.GetComponent<CanvasGroup>();
        }

        private IEnumerator HideMessageAfterDelay(NotificationManager.Notification notification, SystemAction onHide)
        {
            float elapsedTime = 0f;

            while (elapsedTime + 0.1f < _remainingDisplayTime)
            {
                elapsedTime += Time.deltaTime;
                _slider.value = _remainingDisplayTime - elapsedTime;
                yield return null;
            }

            onHide?.Invoke();
        }

        private void ActivateDisplayObjects(GameObject _notificationInstance, Transform targetObject, SystemAction onComplete)
        {
            _notificationInstance.SetActive(true);
            _iconInstance.SetActive(true);

            _notificationCanvasGroup = _notificationInstance.GetComponent<CanvasGroup>();
            Debug.Log(_notificationCanvasGroup);
            Debug.Log(_iconCanvasGroup);

            if (_notificationCanvasGroup != null && _iconCanvasGroup != null)
            {
                StartCoroutine(Fade(_notificationCanvasGroup, _iconCanvasGroup, targetObject, true,() =>
                {
                    onComplete?.Invoke();
                }));
            }
            else
            {
                Debug.LogWarning("CanvasGroup component not found on the notification instance.");
                onComplete?.Invoke();
            }
        }

        private void DeactivateDisplayObject(GameObject _notificationInstance, GameObject _iconInstance, GameObject _messageDetailInstance, Transform targetObject, SystemAction onComplete)
        {
            if (_notificationCanvasGroup != null && _iconCanvasGroup != null)
            {
                StartCoroutine(Fade(_notificationCanvasGroup, _iconCanvasGroup, targetObject, false, () =>
                {
                    _notificationInstance.SetActive(false);
                    _iconInstance.SetActive(false);
                    _messageDetailInstance.SetActive(false);
                    onComplete?.Invoke();
                }));
            }
            else
            {
                Debug.LogWarning("CanvasGroup component not found on the notification instance.");
                _notificationInstance.SetActive(false);
                _iconInstance.SetActive(false);
                _messageDetailInstance.SetActive(false);
                onComplete?.Invoke();
            }
        }

        private void ExpandNotification(NotificationManager.Notification notification, Transform notificationObjectPosition, Vector3 iconPosition, SystemAction onHide)
        {
            // When the animation is running, then detail is not expanded
            if (_isFading)
            {
                return;
            }

            // Display message detail panel
            _messageDetailInstance.SetActive(true);

            TMP_Text textComponent = _messageDetailInstance.GetComponentInChildren<TMP_Text>();
            textComponent.text = notification.DetailText;

            RectTransform panelRectTransform = _messageDetailInstance.GetComponentInChildren<RectTransform>();
            float iconDiameter = _iconInstance.GetComponent<Renderer>().bounds.size.magnitude;

            // Calculate the new position
            var transformVar = arCamera.transform;

            // Transform by icon diameter and container rectangle sizes
            Vector3 iconDown = transformVar.up * (-1.5f * iconDiameter);
            Vector3 iconRight = transformVar.right * (0.5f * panelRectTransform.sizeDelta.x);

            //Vector3 newPosition = iconPosition + new Vector3(0, 0, 0);
            Vector3 newPosition = iconPosition + iconDown + iconRight;

            _messageDetailInstance.transform.position = newPosition;

            // Stop the old coroutine
            if (_hideMessageCoroutine != null)
            {
                StopCoroutine(_hideMessageCoroutine);
            }

            // Reset the remaining display time
            _slider.maxValue = EvaluateDisplayTime(notification.DetailText) * 1.5f;
            _remainingDisplayTime = _slider.maxValue;

            // Restart the coroutine with the updated remaining display time
            StartHideMessageCoroutine(notification, notificationObjectPosition, onHide);
        }


        private void StartHideMessageCoroutine(NotificationManager.Notification notification,
            Transform notificationObjectPosition, SystemAction onHide)
        {
            _hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(notification, () =>
            {
                DeactivateDisplayObject(_notificationInstance, _iconInstance, _messageDetailInstance, notificationObjectPosition, () =>
                {
                    onHide?.Invoke();
                });
            }));
        }


        private void HighlightTargetObject(Transform targetObject, bool highlight)
        {
            if (targetObject != null)
            {
                Renderer objectRenderer = targetObject.GetComponent<Renderer>();
                if (objectRenderer != null)
                {
                    if (highlight)
                    {
                        _originalColors.TryAdd(targetObject, objectRenderer.material.color);
                        objectRenderer.material.color = Color.red;
                    }
                    else
                    {
                        if (_originalColors.TryGetValue(targetObject, out Color originalColor))
                        {
                            objectRenderer.material.color = originalColor;
                        }
                    }
                }
            }
        }

        private IEnumerator Fade(CanvasGroup notificationCanvasGroup, CanvasGroup iconCanvasGroup, Transform targetObject, bool show, SystemAction onComplete)
        {
            _isFading = true;

            float targetAlpha = show ? 1f : 0f;
            if (show)
            {
                iconCanvasGroup.gameObject.SetActive(true);
                notificationCanvasGroup.gameObject.SetActive(true);
                HighlightTargetObject(targetObject, true);
            }

            Tween notificationFadeTween = notificationCanvasGroup.DOFade(targetAlpha, 0.5f);
            Tween iconFadeTween = iconCanvasGroup.DOFade(targetAlpha, 0.5f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(notificationFadeTween);
            sequence.Join(iconFadeTween);

            yield return sequence.WaitForCompletion();

            _isFading = false;

            if (!show)
            {
                HighlightTargetObject(targetObject, false);
            }

            onComplete?.Invoke();
        }

        private float EvaluateDisplayTime(string notificationText)
        {
            int textLenght = notificationText.Length;
            _displayTime = Mathf.Clamp(textLenght * 50, 2000, 7000) / 1000 + _fadingOffset;

            return _displayTime;
        }
    }
}
