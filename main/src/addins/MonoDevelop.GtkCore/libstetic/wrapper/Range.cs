using System;

namespace Stetic.Wrapper {

	public abstract class Range : Widget {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			NotifyWorkaround.AddNotification (((Gtk.Range)Wrapped).Adjustment, AdjustmentNotifyHandler);
		}
		
		public override void Dispose ()
		{
			NotifyWorkaround.RemoveNotification (((Gtk.Range)Wrapped).Adjustment, AdjustmentNotifyHandler);
			base.Dispose ();
		}

		void AdjustmentNotifyHandler (object obj, GLib.NotifyArgs args)
		{
			EmitNotify (args.Property);
		}
	}
}
