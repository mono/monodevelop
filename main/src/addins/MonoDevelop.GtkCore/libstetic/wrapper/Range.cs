using System;

namespace Stetic.Wrapper {

	public abstract class Range : Widget {
		internal static string[] adjustmentProperties = new string [] { "lower", "page-increment", "page-size", "step-increment", "upper", "value" };
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			foreach (string property in adjustmentProperties)
				((Gtk.Range)Wrapped).Adjustment.AddNotification (property, AdjustmentNotifyHandler);
		}
		
		public override void Dispose ()
		{
			foreach (string property in adjustmentProperties)
				((Gtk.Range)Wrapped).Adjustment.RemoveNotification (property, AdjustmentNotifyHandler);
			base.Dispose ();
		}

		void AdjustmentNotifyHandler (object obj, GLib.NotifyArgs args)
		{
			EmitNotify (args.Property);
		}
	}
}
