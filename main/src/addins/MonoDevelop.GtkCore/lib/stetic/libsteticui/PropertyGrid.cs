using Gtk;
using GLib;
using System;
using System.Collections;
using System.Reflection;

namespace Stetic {

	internal class PropertyGrid : Gtk.VBox {

		Hashtable cachedGroups = new Hashtable ();

		Stetic.Wrapper.Widget selection;
		Stetic.Wrapper.Widget newSelection;
		Stetic.Wrapper.Container.ContainerChild packingSelection;
		
		PropertyGridHeader header;
		Gtk.Widget noSelection;
		
		ProjectBackend project;

		public PropertyGrid ()
		{
			header = new PropertyGridHeader ();
			header.Show ();
			PackStart (header, false, false, 0);
			
			Label lab = new Label ();
			lab.Markup = "<i>No selection</i>";
			PackStart (lab, false, false, 0);
			noSelection = lab;
			
			Stetic.Registry.RegistryChanging += new EventHandler (OnRegistryChanging);
		}
		
		public PropertyGrid (ProjectBackend project): this ()
		{
			this.ProjectBackend = project;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
		}
		
		void OnRegistryChanging (object o, EventArgs args)
		{
			cachedGroups.Clear ();
			Clear ();
			foreach (Gtk.Widget w in Children)
				if (! (w is PropertyGridHeader))
					Remove (w);
		}

		public ProjectBackend ProjectBackend {
			get { return project; }
			set {
				if (project != null)
					project.SelectionChanged -= Selected;
					
				project = value;
				project.SelectionChanged += Selected;
				Selected (null, null);
			}
		}
		
		void Clear ()
		{
			if (selection != null) {
				selection.Notify -= Notified;
				selection = null;
			}
			foreach (Gtk.Widget w in Children)
				w.Hide ();
		}

		void Notified (object wrapper, string propertyName)
		{
			foreach (object ob in Children) {
				PropertyGridGroup grid = ob as PropertyGridGroup;
				if (grid != null)
					grid.Notified (propertyName);
			}
		}

		void Selected (object s, Wrapper.WidgetEventArgs args)
		{
			newSelection = args != null ? args.WidgetWrapper : null;
			GLib.Timeout.Add (50, new GLib.TimeoutHandler (SelectedHandler));
		}
		
		bool SelectedHandler ()
		{
			ClassDescriptor klass;

			Clear ();
			
			selection = newSelection;

			if (selection == null || selection.Wrapped is ErrorWidget) {
				noSelection.Show ();
				return false;
			}
			
			header.Show ();

			selection.Notify += Notified;

			klass = selection.ClassDescriptor;

			header.AttachObject (selection.Wrapped);
			AppendItemGroups (klass, selection.Wrapped);

			packingSelection = Stetic.Wrapper.Container.ChildWrapper (selection);
			if (packingSelection != null) {
				klass = packingSelection.ClassDescriptor;
				if (klass.ItemGroups.Count > 0) {
					AppendItemGroups (klass, packingSelection.Wrapped);
					packingSelection.Notify += Notified;
				}
			}
			return false;
		}
		
		void AppendItemGroups (ClassDescriptor klass, object obj)
		{
			int n = 1;
			foreach (ItemGroup igroup in klass.ItemGroups) {
				PropertyGridGroup grid = (PropertyGridGroup) cachedGroups [igroup];
				if (grid == null) {
					grid = new PropertyGridGroup ();
					grid.AddGroup (igroup);
					cachedGroups [igroup] = grid;
					PackStart (grid, false, false, 0);
				}
				ReorderChild (grid, n++);
				grid.ShowAll ();
				grid.AttachObject (obj);
			}
		}
	}
	
	class PropertyGridGroup: Stetic.Grid
	{
		Hashtable editors;
		Hashtable sensitives, invisibles;
		object obj;
		
		public PropertyGridGroup ()
		{
			editors = new Hashtable ();
			sensitives = new Hashtable ();
			invisibles = new Hashtable ();
		}
		
		public void AddGroup (ItemGroup igroup)
		{
			AppendGroup (igroup.Label, true);
			foreach (ItemDescriptor item in igroup) {
				if (item.IsInternal)
					continue;
				if (item is PropertyDescriptor)
					AppendProperty ((PropertyDescriptor)item);
				else if (item is CommandDescriptor)
					AppendCommand ((CommandDescriptor)item);
			}
		}
		
		public virtual void AttachObject (object ob)
		{	
			this.obj = ob;
			
			foreach (object ed in editors.Values) {
				PropertyEditor pe = ed as PropertyEditor;
				if (pe != null)
					pe.AttachObject (ob);
			}
			UpdateSensitivity ();
		}

		protected void AppendProperty (PropertyDescriptor prop)
		{
			PropertyEditor rep = new PropertyEditor (prop);

			// FIXME: Make sure all notify events are based on prop names, not propspec
			editors[prop.Name] = rep;

			AppendPair (prop.Label, rep, prop.Description);
			rep.ShowAll ();

			if (prop.HasDependencies)
				sensitives[prop] = prop;
			if (prop.HasVisibility)
				invisibles[prop] = prop;
		}

		void AppendCommand (CommandDescriptor cmd)
		{
			Gtk.Button button = new Gtk.Button (cmd.Label);
			button.Clicked += delegate (object o, EventArgs args) {
				cmd.Run (this.obj);
			};
			button.Show ();
			Append (button, cmd.Description);

			if (cmd.HasDependencies) {
				editors[cmd.Name] = button;
				sensitives[cmd] = cmd;
			}
			if (cmd.HasVisibility) {
				editors[cmd.Name] = button;
				invisibles[cmd] = cmd;
			}
		}
		
		void UpdateSensitivity ()
		{
			foreach (ItemDescriptor item in sensitives.Keys) {
				Widget w = editors[item.Name] as Widget;
				if (w != null) {
					object ob = sensitives.Contains (item) ? obj : null;
					w.Sensitive = item.EnabledFor (ob);
				}
			}
			foreach (ItemDescriptor item in invisibles.Keys) {
				Widget w = editors[item.Name] as Widget;
				if (w != null) {
					object ob = invisibles.Contains (item) ? obj : null;
					if (!item.VisibleFor (ob))
						w.Hide ();
					else
						w.Show ();
				}
			}
		}
		
		public void Notified (string propertyName)
		{
			PropertyEditor ed = editors [propertyName] as PropertyEditor;
			if (ed != null)
				ed.Update ();
			UpdateSensitivity ();
		}
	}
	
	class PropertyGridHeader: PropertyGridGroup
	{
		PropertyDescriptor name;
		Gtk.Image image;
		Gtk.Label label;
		
		public PropertyGridHeader ()
		{
			name = (PropertyDescriptor)Registry.LookupClassByName ("Gtk.Widget") ["Name"];
			AppendProperty (name);

			Gtk.HBox box = new Gtk.HBox (false, 6);
			image = new Gtk.Image ();
			box.PackStart (image, false, false, 0);
			label = new Gtk.Label ();
			box.PackStart (label, false, false, 0);
			box.ShowAll ();
			AppendPair ("Widget Class", box, null);
		}
		
		public override void AttachObject (object ob)
		{
			base.AttachObject (ob);
			Gtk.Widget w = (Gtk.Widget) ob;
			ObjectWrapper wrapper = ObjectWrapper.Lookup (w);
			image.Pixbuf = wrapper.ClassDescriptor.Icon;
			label.Text = wrapper.WrappedTypeName;
		}
	}
}
