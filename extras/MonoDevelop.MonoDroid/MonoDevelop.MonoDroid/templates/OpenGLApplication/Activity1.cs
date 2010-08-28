using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace ${Namespace}
{
	public class Activity1 : Activity
	{
		public Activity1 (IntPtr handle) : base (handle)
		{
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create our OpenGL view, and display it
			GLView1 view = new GLView1 (this);
			SetContentView (view);
		}
	}
}

