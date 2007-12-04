
using System;
using System.Collections;
using Gtk;
using Mono.Unix;

namespace Stetic
{
	
	internal class WidgetActionBar: Gtk.Toolbar
	{
		ProjectBackend project;
		Stetic.Wrapper.Widget rootWidget;
		WidgetTreeCombo combo;
		ToolItem comboItem;
		Stetic.Wrapper.Widget selection;
		Stetic.Wrapper.Container.ContainerChild packingSelection;
		Hashtable editors, wrappers;
		Hashtable sensitives, invisibles;
		ArrayList toggles;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		bool updating;
		bool allowBinding;
		WidgetDesignerFrontend frontend;
		
		public WidgetActionBar (WidgetDesignerFrontend frontend, Stetic.Wrapper.Widget rootWidget)
		{
			this.frontend = frontend;
			
			editors = new Hashtable ();
			wrappers = new Hashtable ();
			sensitives = new Hashtable ();
			invisibles = new Hashtable ();
			toggles = new ArrayList ();

			IconSize = IconSize.Menu;
			Orientation = Orientation.Horizontal;
			ToolbarStyle = ToolbarStyle.BothHoriz;

			combo = new WidgetTreeCombo ();
			comboItem = new ToolItem ();
			comboItem.Add (combo);
			comboItem.ShowAll ();
			Insert (comboItem, -1);
			ShowAll ();
			RootWidget = rootWidget;
		}
		
		public override void Dispose ()
		{
			RootWidget = null;
			Clear ();
			base.Dispose ();
		}
		
		public bool AllowWidgetBinding {
			get { return allowBinding; }
			set { allowBinding = value; }
		}
		
		public Stetic.Wrapper.Widget RootWidget {
			get { return rootWidget; }
			set {
				if (project != null) {
					project.SelectionChanged -= OnSelectionChanged;
					project = null;
				}
				
				rootWidget = value;
				combo.RootWidget = rootWidget;
				
				if (rootWidget != null) {
					project = (Stetic.ProjectBackend) rootWidget.Project;
					UpdateSelection (Wrapper.Widget.Lookup (project.Selection));
					project.SelectionChanged += OnSelectionChanged;
				}
			}
		}
		
		void Clear ()
		{
			if (selection != null) {
				selection.Notify -= Notified;
				if (packingSelection != null)
					packingSelection.Notify -= Notified;
			}
			
			selection = null;
			packingSelection = null;
			
			editors.Clear ();
			wrappers.Clear ();
			sensitives.Clear ();
			invisibles.Clear ();
			toggles.Clear ();
				
			foreach (Gtk.Widget child in Children)
				if (child != comboItem) {
					Remove (child);
					child.Destroy ();
				}
		}
		
		void OnSelectionChanged (object s, Wrapper.WidgetEventArgs args)
		{
			UpdateSelection (args.WidgetWrapper);
		}
		
		void UpdateSelection (Wrapper.Widget w)
		{
			Clear ();
			selection = w;
			
			if (selection == null) {
				combo.SetSelection (null);
				return;
			}

			// Look for the root widget, and only update the bar if the selected
			// widget is a child of the root widget
			
			while (w != null && !w.IsTopLevel) {
				w = Stetic.Wrapper.Container.LookupParent ((Gtk.Widget) w.Wrapped);
			}
			if (w == null || w != rootWidget)
				return;

			combo.SetSelection (selection);
			
			selection.Notify += Notified;
			packingSelection = Stetic.Wrapper.Container.ChildWrapper (selection);
			if (packingSelection != null)
				packingSelection.Notify += Notified;
				
			AddWidgetCommands (selection);
			UpdateSensitivity ();
		}
		
		protected virtual void AddWidgetCommands (ObjectWrapper wrapper)
		{
			if (allowBinding && wrapper != RootWidget) {
				// Show the Bind to Field button only for children of the root container.
				ToolButton bindButton = new ToolButton (null, Catalog.GetString ("Bind to Field"));
				bindButton.IsImportant = true;
				bindButton.Clicked += delegate { frontend.NotifyBindField (); };
				bindButton.Show ();
				Insert (bindButton, -1);
			}
			AddCommands (wrapper);
		}
		
		void AddCommands (ObjectWrapper wrapper)
		{
			foreach (ItemGroup igroup in wrapper.ClassDescriptor.ItemGroups) {
				foreach (ItemDescriptor desc in igroup) {
					if ((desc is CommandDescriptor) && desc.SupportsGtkVersion (project.TargetGtkVersion))
						AppendCommand ((CommandDescriptor) desc, wrapper);
				}
			}
			
			Stetic.Wrapper.Widget widget = wrapper as Stetic.Wrapper.Widget;
			if (widget != null) {
				Stetic.Wrapper.Container.ContainerChild packingSelection = Stetic.Wrapper.Container.ChildWrapper (widget);
				if (packingSelection != null)
					AddCommands (packingSelection);
			}
		}
		
		void AppendCommand (CommandDescriptor cmd, ObjectWrapper widget)
		{
			Gtk.ToolButton button;
			
			if (cmd.IsToggleCommand (widget.Wrapped)) {
				button = new Gtk.ToggleToolButton ();
				((Gtk.ToggleToolButton)button).Active = cmd.IsToogled (widget.Wrapped);
				toggles.Add (cmd);
				editors[cmd.Name] = button;
			} else {
				button = new Gtk.ToolButton (null, null);
			}
				
			Gtk.Image img = cmd.GetImage ();
			if (img != null) {
				button.IconWidget = img;
				button.Label = cmd.Label;
				if (cmd.Label != null && cmd.Label.Length > 0)
					button.SetTooltip (tips, cmd.Label, "");
			}
			else {
				button.Label = cmd.Label;
				button.IsImportant = true;
			}
			button.Clicked += delegate (object o, EventArgs args) {
				if (!updating)
					cmd.Run (widget.Wrapped);
			};
			button.ShowAll ();
			Insert (button, -1);

			if (cmd.HasDependencies) {
				editors[cmd.Name] = button;
				sensitives[cmd] = cmd;
			}
			if (cmd.HasVisibility) {
				editors[cmd.Name] = button;
				invisibles[cmd] = cmd;
			}
			wrappers [cmd] = widget;
		}
		
		void Notified (object s, string propertyName)
		{
			UpdateSensitivity ();
		}
		
		void UpdateSensitivity ()
		{
			foreach (ItemDescriptor item in sensitives.Keys) {
				Widget w = editors[item.Name] as Widget;
				if (w != null) {
					ObjectWrapper wrapper = wrappers [item] as ObjectWrapper;
					object ob = sensitives.Contains (item) ? wrapper.Wrapped : null;
					w.Sensitive = item.EnabledFor (ob);
				}
			}
			foreach (ItemDescriptor item in invisibles.Keys) {
				Widget w = editors[item.Name] as Widget;
				if (w != null) {
					ObjectWrapper wrapper = wrappers [item] as ObjectWrapper;
					object ob = invisibles.Contains (item) ? wrapper.Wrapped : null;
					w.Visible = item.VisibleFor (ob);
				}
			}
			foreach (CommandDescriptor cmd in toggles) {
				ToggleToolButton w = editors[cmd.Name] as ToggleToolButton;
				if (w != null) {
					ObjectWrapper wrapper = wrappers [cmd] as ObjectWrapper;
					updating = true;
					w.Active = cmd.IsToogled (wrapper.Wrapped);
					updating = false;
				}
			}
		}
	}
}
