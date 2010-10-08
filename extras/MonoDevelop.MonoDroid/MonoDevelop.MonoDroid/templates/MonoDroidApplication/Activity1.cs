using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace ${Namespace}
{
	[Activity (Label = "My Activity", MainLauncher = true)]
	public class Activity1 : Activity
	{
		int count = 1;

		public Activity1 (IntPtr handle) : base (handle)
		{
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (R.layout.main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (R.id.myButton);
			
			button.Click += delegate { button.Text = string.Format ("{0} clicks!", count++); };
		}
	}
}

