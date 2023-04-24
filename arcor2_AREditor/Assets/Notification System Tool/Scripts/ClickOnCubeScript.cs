#nullable enable
using System.Collections.Generic;
using Base;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARRaycastManager))]
public class ClickOnCubeScript : Singleton<ClickOnCubeScript>
{
    [SerializeField] 
    private PlacementObject[] placementObjects;

    private ARRaycastManager _arRaycastManager;

    [SerializeField]
    private Camera arCamera;

    private Vector2 touchPosition = default;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
    private void Awake()
    {
        _arRaycastManager = GetComponent<ARRaycastManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            touchPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                RaycastHit hitObject;

                if (Physics.Raycast(ray, out hitObject))
                {
                    PlacementObject placementObject = hitObject.transform.GetComponent<PlacementObject>();
                    if (placementObject != null)
                    {
                        ChangeSelectedObject(placementObject);
                    }
                }
            }
        } 
    }

    void ChangeSelectedObject(PlacementObject selected)
    {
        foreach (PlacementObject current in placementObjects)
        {
            if (selected == current)
            {
                current.IsSelected = true;
            
                // Get the object's tag
                string objectTag = selected.gameObject.tag;

                // Create a notification based on the object's tag

                NotificationManager.Notification? notification = objectTag switch
                {
                    "LeftCube" => new NotificationManager.Notification(NotificationManager.NotificationType.Error, "Failed to loading robot's end effectors",
                        "Something went wrong during loading scene objects. You would try restart connection by on/off switcher on left.", 4f, TopRightCornerNotificationIn3DSpace.Instance, selected.transform),
                    "MiddleCube" => new NotificationManager.Notification(NotificationManager.NotificationType.CriticalError, "ServerError",
                        "You clicked on the Middle Cube.", 4f, CriticalErrorWindow.Instance),
                    "RightCube" => new NotificationManager.Notification(NotificationManager.NotificationType.Success,
                        "You successful clicked on the Right Cube.", 4f, TopRightCornerNotificationDisplay.Instance),
                    _ => new NotificationManager.Notification(NotificationManager.NotificationType.Error,
                        "Wrong click! You clicked on the unknown Cube.", 4f, TopRightCornerNotificationDisplay.Instance)
                };
                Base.NotificationManager.Instance.DisplayNotification(notification);
            }
            else
            {
                current.IsSelected = false;
            }
        }
    }
}