using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public class PriorityQueue
    {
        private NotificationManager.Notification _notification;
        private List<NotificationManager.Notification> _queue;

        public PriorityQueue()
        {
            _queue = new List<NotificationManager.Notification>();
        }

        public void Enqueue(NotificationManager.Notification notification)
        {
            bool inserted = false;
            for (int i = 0; i < _queue.Count; i++)
            {
                if (notification.Type < _queue[i].Type)
                {
                    _queue.Insert(i, notification);
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
            {
                _queue.Add(notification);
            }
        }

        public List<String> GetAllItems()
        {
            List<string> allItems = new List<string>();
            foreach (var notification in _queue)
            {
                allItems.Add(notification.Text);
            }
            return allItems;
        }
        public NotificationManager.Notification Dequeue()
        {
            if (_queue.Count == 0)
            {
                return null;
            }

            NotificationManager.Notification message = _queue[0];
            _queue.RemoveAt(0);
            return message;
        }

        public int Count => _queue.Count;
    }


     public class NotificationManager : Singleton<NotificationManager>
    {
        public class Notification
        {
            public NotificationType Type { get; set; }
            public String Text { get; set; }
            public String TitleText { get; set; }
            public String DetailText { get; set; }
            public float DisplayTime { get; set; }
            public Transform ObjectPosition { get; set; }
            public INotificationDisplay DisplayHandler { get; set; }

            public Notification(NotificationType type, string text, float displayTime,
                INotificationDisplay displayHandler)
            {
                Type = type;
                Text = text;
                DisplayTime = displayTime;
                DisplayHandler = displayHandler;
            }
            
            public Notification(NotificationType type, string text, string detailText, float displayTime,
                INotificationDisplay displayHandler, Transform objectPosition)
            {
                Type = type;
                Text = text;
                DetailText = detailText;
                DisplayTime = displayTime;
                DisplayHandler = displayHandler;
                ObjectPosition = objectPosition;
            }
            
            public Notification(NotificationType type, string titleText, string text, float displayTime,
                INotificationDisplay displayHandler)
            {
                Type = type;
                TitleText = titleText;
                Text = text;
                DisplayTime = displayTime;
                DisplayHandler = displayHandler;
                
            }
        }
        public enum NotificationType
        {
            CriticalError,
            Error,
            Warning,
            Success
        }

        private readonly PriorityQueue _notificationsQueue = new PriorityQueue();
        private bool _isDisplayingNotification = false;

        public void DisplayNotification(Notification notification)
        {
            _notificationsQueue.Enqueue(notification);
            Debug.Log("NotificationQueue:" + string.Join(", ", _notificationsQueue.GetAllItems()));
            if (!_isDisplayingNotification)
            {
                DisplayNextMessage();
            }
        }

        public void DisplayNextMessage()
        {
            //Debug.Log("NotificationQueueCount: " + _notificationsQueue.Count);
            if (_notificationsQueue.Count <= 0 || _isDisplayingNotification) return;
            _isDisplayingNotification = true;
            var notification = _notificationsQueue.Dequeue();
            notification.DisplayHandler.DisplayNotification(notification, NotificationDisplayed);
        }
        
        private void NotificationDisplayed()
        {
            _isDisplayingNotification = false;
            DisplayNextMessage();
        }
    }
}
    


