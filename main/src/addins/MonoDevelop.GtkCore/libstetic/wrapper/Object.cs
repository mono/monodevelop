using System;
using System.Collections;
using System.Collections.Generic;

namespace Stetic.Wrapper {
	public abstract class Object : Stetic.ObjectWrapper {

		public override void Dispose ()
		{
			if (Wrapped == null)
				return;
			((GLib.Object)Wrapped).RemoveNotification (NotifyHandler);
			base.Dispose ();
		}
		
		IEnumerable<string> GladePropertyNames {
			get {
				foreach (ItemGroup group in ClassDescriptor.ItemGroups) {
					foreach (ItemDescriptor item in group) {
						TypedPropertyDescriptor prop = item as TypedPropertyDescriptor;
						if (prop != null && !string.IsNullOrEmpty (prop.GladeName)) {
							yield return prop.GladeName;
						}
					}
				}
			}
		}
		
		internal protected override void OnDesignerAttach (IDesignArea designer)
		{
			base.OnDesignerAttach (designer);
			foreach (string property in GladePropertyNames)
				((GLib.Object)Wrapped).AddNotification (property, NotifyHandler);
		}
		
		internal protected override void OnDesignerDetach (IDesignArea designer)
		{
			base.OnDesignerDetach (designer);
			foreach (string property in GladePropertyNames)
				((GLib.Object)Wrapped).RemoveNotification (property, NotifyHandler);
		}
		
		public static Object Lookup (GLib.Object obj)
		{
			return Stetic.ObjectWrapper.Lookup (obj) as Stetic.Wrapper.Object;
		}

		void NotifyHandler (object obj, GLib.NotifyArgs args)
		{
			if (Loading)
				return;

			// Translate gtk names into descriptor names.
			foreach (ItemGroup group in ClassDescriptor.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					TypedPropertyDescriptor prop = item as TypedPropertyDescriptor;
					if (prop != null && prop.GladeName == args.Property) {
						EmitNotify (prop.Name);
						return;
					}
				}
			}
		}
	}
}
