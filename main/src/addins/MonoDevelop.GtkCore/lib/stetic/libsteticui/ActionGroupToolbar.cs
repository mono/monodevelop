
using System;
using System.Collections;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic
{
	internal class ActionGroupToolbar: Gtk.Toolbar
	{
		Wrapper.ActionGroupCollection actionGroups;
		Gtk.ComboBox combo;
		bool updating;
		ActionGroup currentGroup;
		ArrayList internalButtons = new ArrayList ();
		bool singleGroupMode;
		bool allowBinding;
		ActionGroupDesignerFrontend frontend;
		Editor.ActionGroupEditor agroupEditor;
		Gtk.ToolButton addButton;
		Gtk.ToolButton removeButton;
		
		public event ActionGroupEventHandler ActiveGroupChanged;
		public event ActionGroupEventHandler ActiveGroupCreated;
		
		public ActionGroupToolbar (ActionGroupDesignerFrontend frontend, bool singleGroupMode)
		{
			Initialize (frontend, null, singleGroupMode);
		}
		
		public ActionGroupToolbar (ActionGroupDesignerFrontend frontend, Wrapper.ActionGroup actionGroup)
		{
			currentGroup = actionGroup;
			Initialize (frontend, null, true);
		}
		
		public ActionGroupToolbar (ActionGroupDesignerFrontend frontend, Wrapper.ActionGroupCollection actionGroups)
		{
			Initialize (frontend, actionGroups, false);
		}
		
		public bool AllowActionBinding {
			get { return allowBinding; }
			set { allowBinding = value; }
		}
		
		void Initialize (ActionGroupDesignerFrontend frontend, Wrapper.ActionGroupCollection actionGroups, bool singleGroupMode)
		{
			this.frontend = frontend;
			this.singleGroupMode = singleGroupMode;
			IconSize = Gtk.IconSize.SmallToolbar;
			Orientation = Gtk.Orientation.Horizontal;
			ToolbarStyle = Gtk.ToolbarStyle.BothHoriz;
			
			combo = Gtk.ComboBox.NewText ();
			
			if (!singleGroupMode) {
				combo.Changed += OnActiveChanged;

				Gtk.ToolItem comboItem = new Gtk.ToolItem ();
				Gtk.HBox cbox = new Gtk.HBox ();
				cbox.PackStart (new Gtk.Label (Catalog.GetString ("Action Group:") + " "), false, false, 3);
				cbox.PackStart (combo, true, true, 3);
				comboItem.Add (cbox);
				comboItem.ShowAll ();
				Insert (comboItem, -1);
				internalButtons.Add (comboItem);
				
				addButton = new Gtk.ToolButton (Gtk.Stock.Add);
				addButton.Clicked += OnAddGroup;
				Insert (addButton, -1);
				internalButtons.Add (addButton);
				
				removeButton = new Gtk.ToolButton (Gtk.Stock.Remove);
				removeButton.Clicked += OnRemoveGroup;
				Insert (removeButton, -1);
				internalButtons.Add (removeButton);
				
				ActionGroups = actionGroups;
				
				if (actionGroups != null && actionGroups.Count > 0)
					combo.Active = 0;
			} else {
				UpdateActionCommands (null);
			}

			ShowAll ();
		}
		
		public override void Dispose ()
		{
			combo.Changed -= OnActiveChanged;
			if (addButton != null) {
				addButton.Clicked -= OnAddGroup;
				removeButton.Clicked -= OnRemoveGroup;
			}
				
			if (agroupEditor != null) {
				agroupEditor.SelectionChanged -= OnEditorSelectionChanged;
				agroupEditor = null;
			}
			
			if (!singleGroupMode)
				ActionGroups = null;
			base.Dispose ();
		}
		
		public Wrapper.ActionGroupCollection ActionGroups {
			get { return actionGroups; }
			set {
				if (singleGroupMode)
					throw new InvalidOperationException ("ActionGroups can't be set in single group mode");

				if (actionGroups != null) {
					actionGroups.ActionGroupAdded -= OnCollectionChanged;
					actionGroups.ActionGroupRemoved -= OnCollectionChanged;
					actionGroups.ActionGroupChanged -= OnCollectionChanged;
				}
				
				this.actionGroups = value;
				
				if (actionGroups != null) {
					actionGroups.ActionGroupAdded += OnCollectionChanged;
					actionGroups.ActionGroupRemoved += OnCollectionChanged;
					actionGroups.ActionGroupChanged += OnCollectionChanged;
				}
				Refresh ();
			}
		}
		
		public void Bind (Editor.ActionGroupEditor agroupEditor)
		{
			this.agroupEditor = agroupEditor;
			agroupEditor.SelectionChanged += OnEditorSelectionChanged;
			agroupEditor.ActionGroup = ActiveGroup;
		}
		
		public void OnEditorSelectionChanged (object s, EventArgs a)
		{
			UpdateActionCommands (agroupEditor.SelectedAction);
		}
		
		public ActionGroup ActiveGroup {
			get {
				return currentGroup;
			}
			set {
				if (singleGroupMode) {
					currentGroup = value;
					UpdateActionCommands (null);
					NotifyActiveGroupChanged ();
				} else {
					int i = actionGroups.IndexOf (value);
					if (i != -1)
						combo.Active = i;
				}
			}
		}
		
		void Refresh ()
		{
			if (singleGroupMode)
				return;

			while (combo.Model.IterNChildren () > 0)
				combo.RemoveText (0);
			if (actionGroups != null) {
				foreach (ActionGroup group in actionGroups)
					combo.AppendText (group.Name);
			}
		}
		
		void OnCollectionChanged (object s, ActionGroupEventArgs args)
		{
			// Avoid firing the selection change event if the selected
			// group is the same after the refresh
			ActionGroup oldGroup = currentGroup;
			updating = true;
			
			int i = combo.Active;
			Refresh ();
			if (actionGroups.Count == 0) {
				combo.Sensitive = false;
				currentGroup = null;
			}
			else {
				combo.Sensitive = true;
				if (i == -1)
					i = 0;
				if (i < actionGroups.Count)
					combo.Active = i;
				else
					combo.Active = actionGroups.Count - 1;
				currentGroup = (ActionGroup) actionGroups [combo.Active];
			}
			updating = false;
			if (currentGroup != oldGroup)
				OnActiveChanged (null, null);
			frontend.NotifyModified ();
		}
		
		void OnAddGroup (object s, EventArgs args)
		{
			ActionGroup group = new ActionGroup ();
			group.Name = Catalog.GetString ("New Action Group");
			actionGroups.Add (group);
			combo.Active = actionGroups.Count - 1;
			if (ActiveGroupCreated != null)
				ActiveGroupCreated (this, new ActionGroupEventArgs (ActiveGroup));
			
			if (agroupEditor != null)
				agroupEditor.StartEditing ();
		}
		
		void OnRemoveGroup (object s, EventArgs args)
		{
			if (combo.Active != -1)
				actionGroups.RemoveAt (combo.Active);
		}
		
		void OnActiveChanged (object s, EventArgs args)
		{
			if (!updating) {
				UpdateActionCommands (null);
				if (combo.Active != -1)
					currentGroup = (ActionGroup) actionGroups [combo.Active];
				else
					currentGroup = null;
				NotifyActiveGroupChanged ();
			}
		}
		
		void NotifyActiveGroupChanged ()
		{
			if (agroupEditor != null)
				agroupEditor.ActionGroup = ActiveGroup;
			if (ActiveGroupChanged != null)
				ActiveGroupChanged (this, new ActionGroupEventArgs (ActiveGroup));
		}
		
		void UpdateActionCommands (Wrapper.Action action)
		{
			foreach (Gtk.Widget w in Children) {
				if (!internalButtons.Contains (w)) {
					Remove (w);
					w.Destroy ();
				}
			}
			AddActionCommands (action);
				
			if (internalButtons.Count > 0 && internalButtons.Count != Children.Length) {
				Insert (new Gtk.SeparatorToolItem (), internalButtons.Count);
			}
			ShowAll ();
		}
		
		protected virtual void AddActionCommands (Wrapper.Action action)
		{
			if (allowBinding) {
				Gtk.ToolButton bindButton = new Gtk.ToolButton (null, Catalog.GetString ("Bind to Field"));
				bindButton.IsImportant = true;
				bindButton.Show ();
				Insert (bindButton, -1);
				if (action == null)
					bindButton.Sensitive = false;
					
				bindButton.Clicked += delegate { frontend.NotifyBindField (); };
			}
		}
	}
}
