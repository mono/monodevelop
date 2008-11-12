
using System;

namespace Stetic
{
	internal class WidgetPropertyTreeBackend: PropertyTree, IObjectViewer
	{
		ProjectBackend project;
		Stetic.ObjectWrapper selection;
		Stetic.ObjectWrapper newSelection;
		Stetic.Wrapper.Container.ContainerChild packingSelection;
		
		public WidgetPropertyTreeBackend ()
		{
			Stetic.Registry.RegistryChanging += new EventHandler (OnRegistryChanging);
		}
		
		public ProjectBackend ProjectBackend {
			get { return project; }
			set {
				if (project != null) {
					project.SelectionChanged -= Selected;
				}
					
				project = value;
				if (project != null) {
					TargetObject = project.Selection;
					project.SelectionChanged += Selected;
				} else {
					TargetObject = null;
				}
			}
		}
		
		public override void Clear ()
		{
			base.Clear ();
			Wrapper.Widget selWidget = selection as Wrapper.Widget;
			if (selWidget != null)
				selWidget.Notify -= Notified;
			if (packingSelection != null)
				packingSelection.Notify -= Notified;
		}
		
		protected override void OnObjectChanged ()
		{
			if (selection != null)
				selection.NotifyChanged ();
		}
		
		public object TargetObject {
			get { return selection.Wrapped; }
			set {
				ObjectWrapper wrapper = ObjectWrapper.Lookup (value);
				if (newSelection == wrapper)
					return;

				newSelection = wrapper;
				if (newSelection != null)
					GLib.Timeout.Add (50, new GLib.TimeoutHandler (SelectedHandler));
				else
					SelectedHandler ();
			}
		}
		
		void Selected (object s, Wrapper.WidgetEventArgs args)
		{
			TargetObject = args != null && args.Widget != null? args.Widget : null;
		}
		
		bool SelectedHandler ()
		{
			SaveStatus ();
			
			Clear ();
			
			selection = newSelection;
			if (selection == null || selection.Wrapped is ErrorWidget || project == null) {
				return false;
			}

			Wrapper.Widget selWidget = selection as Wrapper.Widget;
			if (selWidget != null) {
				selWidget.Notify += Notified;
			
				PropertyDescriptor name = (PropertyDescriptor)Registry.LookupClassByName ("Gtk.Widget") ["Name"];
				AppendProperty (name, selection.Wrapped);
			}

			AddProperties (selection.ClassDescriptor.ItemGroups, selection.Wrapped, project.TargetGtkVersion);
			
			if (selWidget != null) {
				packingSelection = Stetic.Wrapper.Container.ChildWrapper (selWidget);
				if (packingSelection != null) {
					ClassDescriptor childklass = packingSelection.ClassDescriptor;
					if (childklass.ItemGroups.Count > 0) {
						AddProperties (childklass.ItemGroups, packingSelection.Wrapped, project.TargetGtkVersion);
						packingSelection.Notify += Notified;
					}
				}
			}
			
			RestoreStatus ();
			return false;
		}
		
		void Notified (object wrapper, string propertyName)
		{
			Update ();
		}
		
		void OnRegistryChanging (object o, EventArgs args)
		{
			Clear ();
		}
	}
	
}
