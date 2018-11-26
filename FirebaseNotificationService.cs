using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using Ecolab.CustomerAuditApplication;
using MG365Mobile.Interfaces.Services;
using Firebase.Messaging;
using Newtonsoft.Json;
using Ecolab.CustomerAuditApplication;
using Android.Support.V7.App;
using Android.Graphics;
using Android.Support.V4.App;

namespace MG365Mobile.Droid.Services
{
	[Service]
	[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
	public class FirebaseNotificationService : FirebaseMessagingService
	{
		const string TAG = "FirebaseNotificationService";

		public override async void OnMessageReceived(RemoteMessage message)
		{
			Log.Info(TAG, "FCM Message Received!");
			Log.Debug(TAG, $"From: {message.From}");

			var success = await ProcessMessage(message);

			if (!success)
				Log.Debug(TAG, $"FCM Message failed to process.");
		}

		private async Task<bool> ProcessMessage(RemoteMessage remoteMessage)
		{
			try
			{
                var remoteNotification = remoteMessage.GetNotification();

                if (remoteNotification == null)
                {
                    var remoteNotificationData = remoteMessage.Data;
                    DateTime messageDateTime = DateTime.Now;
                    bool messageDateTimeResult = DateTime.TryParse(remoteNotificationData.GetValue("MessageDateTime"), out messageDateTime);

                    //TODO: add localized text for New Message title.
                    var notification = new Models.Notification
                    {
                        MessageId = Convert.ToInt32(remoteNotificationData.GetValue("MessageId")),
                        MessageIdentifier = remoteNotificationData.GetValue("MessageIdentifier"),
                        MessageTitle = remoteNotificationData.GetValue("MessageTitle"),
                        MessagePriority = remoteNotificationData.GetValue("MessagePriority"),
                        MessageText = remoteNotificationData.GetValue("MessageText"),
                        MessageDateTime = (messageDateTimeResult != false) ? DateTime.Parse(remoteNotificationData.GetValue("MessageDateTime")) : messageDateTime,
                        IsRead = false,
                    };
                    await NewNotificationArrived(notification);
                    PublishLocalNotification(notification);
                }
                else
                {

                    //These template is how most messages will be received
                    Log.Debug(TAG, $"Notification Message Body: {remoteNotification.Body}");

                    //TODO: add localized text for New Message title.
                    var notification = new Models.Notification
                    {
                        MessageId = 0,
                        MessageIdentifier = remoteMessage.MessageId,    
                        MessageTitle = !String.IsNullOrEmpty(remoteNotification.Title) ? remoteNotification.Title : "New Message",
                        MessagePriority = remoteMessage.MessageType,
                        MessageText = remoteNotification.Body,
                        MessageDateTime = DateTime.UtcNow,
                        IsRead = false,
                    };
                    await NewNotificationArrived(notification);
                    PublishLocalNotification(notification);
                }
				
				return true;
			}
			catch (Exception ex)
			{
				ex.Log(this.GetType().FullName);
				return false;
			}
		}


		private async Task NewNotificationArrived(Models.Notification notification)
		{
			INotificationService notificationService = App.Locator.NotificationService;

			if (notificationService == null)
				return;

			await notificationService.NewNotificationArrived(notification).ConfigureAwait(false);
		}



		/// <summary>
		/// Sends the notification immediately. 
		/// </summary>
		/// <param name="notification">Notification.</param>
        private void PublishLocalNotification(Models.Notification notification)
		{
            //var intent = new Intent(this, typeof(MainActivity));

            //if (notification != null)
            //	intent.PutExtra("Notification", JsonConvert.SerializeObject(notification));

            //intent.AddFlags(ActivityFlags.ClearTop);

            //var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            //TODO: add localized text for New Message title.


            NotificationManager notificationManager = (NotificationManager)NotificationManager.FromContext(BaseContext);
            NotificationCompat.Builder builder = null;
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {

                NotificationChannel notificationChannel = new NotificationChannel("ID", "EcoLab", NotificationImportance.High);
                notificationManager.CreateNotificationChannel(notificationChannel);
                builder = new NotificationCompat.Builder(ApplicationContext, notificationChannel.Id);
            }
            else
            {
                builder = new NotificationCompat.Builder(this);
            }

            builder = builder
                    .SetSmallIcon(Resource.Drawable.AppIcon)
                    .SetContentTitle(notification.MessageTitle ?? "New Message")
                    .SetContentText(notification.MessageText)
                     //.SetContentIntent(pendingIntent)
                     .SetAutoCancel(true);
            notificationManager.Notify(0, builder.Build());

            /*

            var notificationBuilder = new Notification.Builder(this)
														.SetSmallIcon(Resource.Drawable.AppIcon)
                                                      .SetContentTitle(notification.MessageTitle ?? "New Message")
                                                      .SetContentText(notification.MessageText)
														//.SetContentIntent(pendingIntent)
														.SetAutoCancel(true);

			var notificationManager = NotificationManager.FromContext(this);
			notificationManager.Notify(0, notificationBuilder.Build());
			*/
		}
	}
}
