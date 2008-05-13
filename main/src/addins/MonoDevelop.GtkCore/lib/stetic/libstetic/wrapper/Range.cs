using System;

namespace Stetic.Wrapper {

	public abstract class Range : Widget {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			((Gtk.Range)Wrapped).Adjustment.AddNotification (AdjustmentNotifyHandler);
		}
		
		public override void Dispose ()
		{
			((Gtk.Range)Wrapped).Adjustment.RemoveNotification (AdjustmentNotifyHandler);
			base.Dispose ();
		}

		void AdjustmentNotifyHandler (object obj, GLib.NotifyArgs args)
		{
			EmitNotify (args.Property);
		}
	}
}
