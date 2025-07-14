using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using Java.Lang;
using Shiny.Stores;
using TaskStackBuilder = AndroidX.Core.App.TaskStackBuilder;

namespace Shiny.Notifications;


public class AndroidNotificationManager(
    AndroidPlatform platform,
    IChannelManager channelManager,
    ISerializer serializer
)
{
    public NotificationManagerCompat NativeManager => NotificationManagerCompat.From(platform.AppContext);

    AlarmManager? alarms;
    public AlarmManager Alarms
    {
        get
        {
            if (this.alarms == null || this.alarms.Handle == IntPtr.Zero)
                this.alarms = platform.GetSystemService<AlarmManager>(Context.AlarmService);

            return this.alarms;
        }
    }

    
    public virtual void Send(AndroidNotification notification)
    {
        var channel = channelManager.Get(notification.Channel!);
        var builder = this.CreateNativeBuilder(notification, channel!);
        this.NativeManager.Notify(notification.Id, builder.Build());
    }


    public virtual NotificationCompat.Builder CreateNativeBuilder(AndroidNotification notification, Channel channel)
    {
        var builder = new NotificationCompat.Builder(platform.AppContext, channel.Identifier);
        this.ApplyChannel(builder, notification, channel);

        builder
            .SetContentTitle(notification.Title)
            .SetContentIntent(this.GetLaunchPendingIntent(notification))
            .SetSmallIcon(platform.GetSmallIconResource(notification.SmallIconResourceName))
            .SetAutoCancel(notification.AutoCancel)
            .SetOngoing(notification.OnGoing);

        if (!notification.Category.IsEmpty())
            builder.SetCategory(notification.Category);

        if (!notification.Thread.IsEmpty())
            builder.SetGroup(notification.Thread);

        //if (!notification.LocalAttachmentPath.IsEmpty())
        //    platform.TrySetImage(notification.LocalAttachmentPath, builder);

        //if (notification.BadgeCount != null)
        //{
        //    // channel needs badge too
        //    builder
        //        .SetBadgeIconType(NotificationCompat.BadgeIconSmall)
        //        .SetNumber(notification.BadgeCount.Value);
        //}

        if (!notification.Ticker.IsEmpty())
            builder.SetTicker(notification.Ticker);

        if (notification.UseBigTextStyle)
            builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
        else
            builder.SetContentText(notification.Message);

        if (!notification.LargeIconResourceName.IsEmpty())
        {
            var iconId = platform.GetResourceIdByName(notification.LargeIconResourceName!);
            if (iconId > 0)
                builder.SetLargeIcon(BitmapFactory.DecodeResource(platform.AppContext.Resources, iconId));
        }

        if (!notification.ColorResourceName.IsEmpty())
        {
            var color = platform.GetColorResourceId(notification.ColorResourceName!);
            builder.SetColor(color);
        }
        return builder;
    }


    public void SetAlarm(Notification notification)
    {
        var pendingIntent = this.GetAlarmPendingIntent(notification);
        var triggerTime = (notification.ScheduleDate!.Value.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds;
        var androidTriggerTime = JavaSystem.CurrentTimeMillis() + (long)triggerTime;
        this.Alarms.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, androidTriggerTime, pendingIntent);
    }


    public void CancelAlarm(Notification notification)
    {
        this.Alarms.Cancel(this.GetAlarmPendingIntent(notification));
    }


    protected virtual PendingIntent GetAlarmPendingIntent(Notification notification)
        => platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
            ShinyNotificationBroadcastReceiver.AlarmIntentAction,
            PendingIntentFlags.UpdateCurrent,
            notification.Id,
            intent => intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, notification.Id));
    

    public virtual PendingIntent GetLaunchPendingIntent(AndroidNotification notification)
    {
        Intent launchIntent;

        if (notification.LaunchActivityType == null)
        {
            launchIntent = platform
                .AppContext!
                .PackageManager!
                .GetLaunchIntentForPackage(platform!.AppContext.PackageName!)!
                .SetFlags(notification.LaunchActivityFlags);
        }
        else
        {
            launchIntent = new Intent(
                platform.AppContext,
                notification.LaunchActivityType
            );
        }

        this.PopulateIntent(launchIntent, notification);

        PendingIntent pendingIntent;
        if ((notification.LaunchActivityFlags & ActivityFlags.ClearTask) != 0)
        {
            pendingIntent = TaskStackBuilder
                .Create(platform.AppContext)
                .AddNextIntent(launchIntent)
                .GetPendingIntent(
                    notification.Id,
                    (int)platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
                )!;
        }
        else
        {
            pendingIntent = PendingIntent.GetActivity(
                platform.AppContext!,
                notification.Id,
                launchIntent!,
                platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
            )!;
        }
        return pendingIntent;
    }


    public virtual void ApplyChannel(NotificationCompat.Builder builder, Notification notification, Channel channel)
    {
        if (channel == null)
            return;

        builder.SetChannelId(channel.Identifier);
        if (channel.Actions != null)
        {
            foreach (var action in channel.Actions)
            {
                switch (action.ActionType)
                {
                    case ChannelActionType.OpenApp:
                        break;

                    case ChannelActionType.TextReply:
                        var textReplyAction = this.CreateTextReply(notification, action);
                        builder.AddAction(textReplyAction);
                        break;

                    case ChannelActionType.None:
                    case ChannelActionType.Destructive:
                        var destAction = this.CreateAction(notification, action);
                        builder.AddAction(destAction);
                        break;

                    default:
                        throw new ArgumentException("Invalid action type");
                }
            }
        }
    }


    protected virtual void PopulateIntent(Intent intent, Notification notification)
    {
        var content = serializer.Serialize(notification);
        intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content);
    }


    static int counter = 100;
    protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
    {
        counter++;
        return platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
            ShinyNotificationBroadcastReceiver.EntryIntentAction,
            PendingIntentFlags.UpdateCurrent,
            counter,
            intent =>
            {
                this.PopulateIntent(intent, notification);
                intent.PutExtra(AndroidNotificationProcessor.IntentActionKey, action.Identifier);
            }
        );
    }


    protected virtual NotificationCompat.Action CreateAction(Notification notification, ChannelAction action)
    {
        var pendingIntent = this.CreateActionIntent(notification, action);
        var iconId = platform.GetResourceIdByName(action.Identifier);
        var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

        return nativeAction;
    }


    protected virtual NotificationCompat.Action CreateTextReply(Notification notification, ChannelAction action)
    {
        var pendingIntent = this.CreateActionIntent(notification, action);
        var input = new AndroidX.Core.App.RemoteInput.Builder(AndroidNotificationProcessor.RemoteInputResultKey)
            .SetLabel(action.Title)
            .Build();

        var iconId = platform.GetResourceIdByName(action.Identifier);
        var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
            .SetAllowGeneratedReplies(true)
            .AddRemoteInput(input)
            .Build();

        return nativeAction;
    }
}
