using System;

namespace Stetic.Wrapper {

	public abstract class Scale : Widget {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			foreach (string property in Range.adjustmentProperties)
				((Gtk.Scale)Wrapped).Adjustment.AddNotification (property, AdjustmentNotifyHandler);
		}

		public override void Dispose ()
		{
			foreach (string property in Range.adjustmentProperties)
				((Gtk.Scale)Wrapped).Adjustment.RemoveNotification (property, AdjustmentNotifyHandler);
			base.Dispose ();
		}

		void AdjustmentNotifyHandler (object obj, GLib.NotifyArgs args)
		{
			EmitNotify (args.Property);
		}
	}
}
