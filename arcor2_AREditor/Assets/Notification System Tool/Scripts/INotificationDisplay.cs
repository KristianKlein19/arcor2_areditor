using Base;
using SystemAction = System.Action;

public interface INotificationDisplay
{
    void DisplayNotification(NotificationManager.Notification notification, SystemAction onHide);
}
