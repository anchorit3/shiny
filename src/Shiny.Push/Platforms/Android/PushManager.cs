﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using AndroidX.Core.App;
using Firebase;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Push;


public class PushManager : IPushManager, IAndroidLifecycle.IOnActivityNewIntent
{
    readonly AndroidPlatform platform;
    //    readonly IKeyValueStore settings;
    //    readonly FirebaseConfig config;
    readonly ILogger logger;
    readonly IPushProvider provider;


    public PushManager(
        AndroidPlatform platform,
        ILogger<PushManager> logger,
        IPushProvider provider
    )
    {
        this.platform = platform;
        this.logger = logger;
        //        this.platform = platform;
        //        this.settings = settings;
        //        this.config = config;
        this.provider = provider;
    }

    //                ShinyFirebaseService.NewToken = token => this.onToken.Invoke(token);
    //                ShinyFirebaseService.MessageReceived = async msg =>
    //                {
    //                    Notification? notification = null;
    //                    var native = msg.GetNotification();

    //                    if (native != null)
    //                    {
    //                        //native.ChannelId
    //                        //native.ImageUrl
    //                        notification = new Notification(
    //                            native.Title,
    //                            native.Body
    //                        );
    //                        this.TryTriggerNotification(msg);
    //                    }
    //                    var push = new PushNotification(msg.Data, notification);
    //                    await this.onReceived.Invoke(push).ConfigureAwait(false);
    //                };


    public IPushTagSupport? Tags => this.provider.Tags;

    public string? RegistrationToken => throw new NotImplementedException();

    
    public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            //Manifest.Permission.PostNotifications
        }
        //        if (!this.initialized)
        //        {
        //            if (this.config.UseEmbeddedConfiguration)
        //            {
        //                FirebaseApp.InitializeApp(this.platform.AppContext);
        //                if (FirebaseApp.Instance == null)
        //                    throw new InvalidOperationException("Firebase did not initialize.  Ensure your google.services.json is property setup.  Install the nuget package `Xamarin.GooglePlayServices.Tasks` into your Android head project, restart visual studio, and then set your google-services.json to GoogleServicesJson");
        //            }
        //            else
        //            {
        //                var options = new FirebaseOptions.Builder()
        //                    .SetApplicationId(this.config.AppId)
        //                    .SetProjectId(this.config.ProjectId)
        //                    .SetApiKey(this.config.ApiKey)
        //                    .SetGcmSenderId(this.config.SenderId)
        //                    .Build();

        //                FirebaseApp.InitializeApp(this.platform.AppContext, options);
        //            }
        //            this.initialized = true;
        //        }
        //        var task = await FirebaseMessaging.Instance.GetToken();
        //        var token = task.JavaCast<Java.Lang.String>().ToString();
        //        return new PushAccessState(AccessState.Available, token);
        return null;
    }

    public Task UnRegister() => Task.Run(() => FirebaseMessaging.Instance.DeleteToken());

    public async void Handle(Activity activity, Intent intent)
    {
        var clickAction = intent?.Action?.Equals(ShinyIntents.NotificationClickAction, StringComparison.InvariantCultureIgnoreCase) ?? false;
        if (!clickAction)
            return;

        this.logger.LogDebug("Detected incoming remote notification intent");

        //if (this.OnEntry == null)
        //{
        //    this.logger.LogWarning("OnEntry is not hooked");
        //}
        //else
        //{
            try
            {
                var dict = new Dictionary<string, string>();
                if (intent.Extras != null)
                {
                    foreach (var key in intent.Extras!.KeySet()!)
                    {
                        var value = intent.Extras.Get(key)?.ToString();
                        if (value != null)
                            dict.Add(key, value);
                    }
                }
                // TODO: can I extract the notification here?
                var push = new PushNotification(dict, null);
                //await this.OnEntry.Invoke(push).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error processing onEntry", ex);
            }
        //}
    }

    //    protected virtual void TryTriggerNotification(RemoteMessage message)
    //    {
    //        try
    //        {
    //            var notification = message.GetNotification();
    //            var intent = new Intent(notification.ClickAction ?? ShinyIntents.NotificationClickAction);

    //            if (message.Data != null)
    //            {
    //                foreach (var data in message.Data)
    //                    intent.PutExtra(data.Key, data.Value);
    //            }

    //            var pendingIntent = PendingIntent.GetActivity(this.platform.AppContext, 99, intent, PendingIntentFlags.Mutable);

    //            var builder = new NotificationCompat
    //                .Builder(
    //                    this.platform.AppContext,
    //                    notification.ChannelId ?? Channel.Default.Identifier
    //                )
    //                .SetAutoCancel(true)
    //                .SetSilent(false)
    //                .SetSmallIcon(this.platform.GetSmallIconResource(notification.Icon))
    //                .SetContentIntent(pendingIntent)
    //                .SetContentTitle(notification.Title);

    //            if (!notification.Ticker.IsEmpty())
    //                builder.SetTicker(notification.Ticker);

    //            if (!notification.Body.IsEmpty())
    //                builder.SetContentText(notification.Body);

    //            ///this.platform.TrySetImage(notification.ImageUrl, builder);

    //            if (!notification.Color.IsEmpty())
    //            {
    //                var color = this.platform.GetColorResourceId(notification.Color);
    //                builder.SetColor(color);
    //            }

    //            var notificationId = this.settings.IncrementValue("NotificationId");
    //            this.platform
    //                .GetSystemService<NotificationManager>(Context.NotificationService)
    //                .Notify(notificationId, builder.Build());
    //        }
    //        catch (Exception ex)
    //        {
    //            this.logger.LogError("Error processing foreground remote notification", ex);
    //        }
    //    }
}