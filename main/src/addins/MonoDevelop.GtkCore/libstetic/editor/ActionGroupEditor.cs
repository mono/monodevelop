
using System;
using System.Collections;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic.Editor
{
	public class ActionGroupEditor: Gtk.EventBox, IMenuItemContainer
	{
		ActionGroup actionGroup;
		Gtk.Table table;
		IProject project;
		ArrayList items = new ArrayList ();
		Gtk.EventBox emptyLabel;
		EditableLabel headerLabel;
		uint columns = 2;
		bool modified;
		bool disposed;
		ObjectWrapperEventHandler changedEvent;
		IDesignArea darea;
		
		public EventHandler GroupModified;
		public EventHandler SelectionChanged;
		
		public ActionGroupEditor ()
		{
			changedEvent = new ObjectWrapperEventHandler (OnActionChanged);
			
			Gtk.Fixed fx = new Gtk.Fixed ();
			table = new Gtk.Table (0, 0, false);
			table.RowSpacing = 8;
			table.ColumnSpacing = 8;
			table.BorderWidth = 12;
			
			Gtk.EventBox ebox = new Gtk.EventBox ();
			ebox.ModifyBg (Gtk.StateType.Normal, this.Style.Backgrounds [0]);
			headerLabel = new EditableLabel ();
			headerLabel.MarkupTemplate = "<b>$TEXT</b>";
			headerLabel.Changed += OnGroupNameChanged;
			Gtk.VBox vbox = new Gtk.VBox ();
			Gtk.Label grpLabel = new Gtk.Label ();
			grpLabel.Xalign = 0;
			grpLabel.Markup = "<span font='11'><i>Action Group</i><span>";
//			vbox.PackStart (grpLabel, false, false, 0);
			vbox.PackStart (headerLabel, false, false, 3);
			vbox.BorderWidth = 12;
			ebox.Add (vbox);
			
			Gtk.VBox box = new Gtk.VBox ();
			box.Spacing = 6;
			box.PackStart (ebox, false, false, 0);
			box.PackStart (table, false, false, 0);
			
			fx.Put (box, 0, 0);
			Add (fx);
			ShowAll ();
		}
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			headerLabel.Changed -= OnGroupNameChanged;
			if (emptyLabel != null)
				emptyLabel.ButtonPressEvent -= OnAddClicked;
			
			foreach (ActionMenuItem aitem in items) {
				aitem.KeyPressEvent -= OnItemKeyPress;
				aitem.Node.Dispose ();
				aitem.Detach ();
				aitem.Destroy ();
			}
			items.Clear ();
			ActionGroup = null;
			project = null;
			headerLabel = null;
			
			if (darea != null) {
				darea.SelectionChanged -= OnSelectionChanged;
				darea = null;
			}

			base.Dispose ();
		}
		
		public ActionGroup ActionGroup {
			get { return actionGroup; }
			set {
				if (actionGroup != null) {
					actionGroup.ObjectChanged -= OnGroupChanged;
					actionGroup.ActionAdded -= OnActionAdded;
					actionGroup.ActionRemoved -= OnActionRemoved;
					foreach (Wrapper.Action a in actionGroup.Actions)
						a.ObjectChanged -= changedEvent;
				}
				actionGroup = value;
				if (actionGroup != null) {
					headerLabel.Text = actionGroup.Name;
					actionGroup.ObjectChanged += OnGroupChanged;
					actionGroup.ActionAdded += OnActionAdded;
					actionGroup.ActionRemoved += OnActionRemoved;
					foreach (Wrapper.Action a in actionGroup.Actions)
						a.ObjectChanged += changedEvent;
				}
				if (!disposed)
					Fill ();
			}
		}
		
		public IProject Project {
			get { return project; }
			set { project = value; }
		}
		
		public bool Modified {
			get { return modified; }
			set { modified = value; }
		}
		
		public Wrapper.Action SelectedAction {
			get {
				IDesignArea designArea = GetDesignArea ();
				IObjectSelection sel = designArea.GetSelection ();
				if (sel != null)
					return ObjectWrapper.Lookup (sel.DataObject) as Wrapper.Action;
				else
					return null;
			}
			set {
				foreach (ActionMenuItem item in items) {
					if (item.Node.Action == value)
						item.Select ();
				}
			}
		}
		
		ActionMenuItem SelectedActionMenuItem {
			get {
				IDesignArea designArea = GetDesignArea ();
				IObjectSelection sel = designArea.GetSelection ();
				if (sel != null)
					return sel.Widget as ActionMenuItem;
				else
					return null;
			}
		}
		
		public void StartEditing ()
		{
			IDesignArea designArea = GetDesignArea ();
			designArea.SetSelection (headerLabel, null);
			headerLabel.StartEditing ();
		}
		
		void Fill ()
		{
			IDesignArea designArea = GetDesignArea ();
			if (designArea == null)
				return;

			Wrapper.Action selAction = null;
			
			foreach (ActionMenuItem item in items) {
				if (designArea.IsSelected (item))
					selAction = item.Node.Action;
				item.Node.Dispose ();
				item.Detach ();
				item.Destroy ();
			}
			items.Clear ();
			
			if (actionGroup != null) {
				Wrapper.Action[] sortedActions = new Wrapper.Action [actionGroup.Actions.Count];
				actionGroup.Actions.CopyTo (sortedActions, 0);
				Array.Sort (sortedActions, new ActionComparer ());
				for (int n = 0; n < sortedActions.Length; n++) {
					Wrapper.Action action = (Wrapper.Action) sortedActions [n];
					ActionMenuItem item = InsertAction (action, n);
					if (selAction == action)
						item.Select ();
				}
				
				if (selAction == null)
					designArea.SetSelection (null, null);
				
				headerLabel.Sensitive = true;
				PlaceAddLabel (actionGroup.Actions.Count);
			} else {
				HideAddLabel ();
				headerLabel.Text = Catalog.GetString ("No selection");
				headerLabel.Sensitive = false;
			}
			ShowAll ();
		}
		
		ActionMenuItem InsertAction (Wrapper.Action action, int n)
		{
			uint row = (uint) n / columns;
			uint col = (uint) (n % columns) * 3;
			
			IDesignArea designArea = GetDesignArea ();
			ActionTreeNode node = new ActionTreeNode (Gtk.UIManagerItemType.Menuitem, "", action);
			ActionMenuItem aitem = new ActionMenuItem (designArea, project, this, node);
			aitem.KeyPressEvent += OnItemKeyPress;
			aitem.MinWidth = 150;
			aitem.Attach (table, row, col);
			
			Gtk.Frame fr = new Gtk.Frame ();
			fr.Shadow = Gtk.ShadowType.Out;
			aitem.Add (fr);
			
			items.Add (aitem);
			return aitem;
		}
		
		void PlaceAddLabel (int n)
		{
			HideAddLabel ();

			uint r = (uint) n / columns;
			uint c = (uint) (n % columns) * 3;
			
			emptyLabel = new Gtk.EventBox ();
			emptyLabel.VisibleWindow = false;
			Gtk.Label label = new Gtk.Label ();
			label.Xalign = 0;
			label.Markup = "<i><span foreground='darkgrey'>" + Catalog.GetString ("Click to create action") + "</span></i>";
			emptyLabel.Add (label);
			emptyLabel.ButtonPressEvent += OnAddClicked;
			table.Attach (emptyLabel, c, c+3, r, r+1);
		}
		
		void HideAddLabel ()
		{
			if (emptyLabel != null) {
				table.Remove (emptyLabel);
				emptyLabel.ButtonPressEvent -= OnAddClicked;
			}
			emptyLabel = null;
		}
		
		void OnGroupChanged (object s, ObjectWrapperEventArgs args)
		{
			headerLabel.Text = actionGroup.Name;
			NotifyModified ();
		}
		
		void OnActionAdded (object s, ActionEventArgs args)
		{
			args.Action.ObjectChanged += changedEvent;
			Fill ();
			NotifyModified ();
		}
		
		void OnActionRemoved (object s, ActionEventArgs args)
		{
			args.Action.ObjectChanged -= changedEvent;
			Fill ();
			NotifyModified ();
		}
		
		void OnActionChanged (object s, ObjectWrapperEventArgs args)
		{
			NotifyModified ();
		}
		
		void NotifyModified ()
		{
			modified = true;
			if (GroupModified != null)
				GroupModified (this, EventArgs.Empty);
		}
		
		void OnAddClicked (object s, Gtk.ButtonPressEventArgs args)
		{
			Wrapper.Action ac = (Wrapper.Action) ObjectWrapper.Create (project, new Gtk.Action ("", "", null, null));
			ActionMenuItem item = InsertAction (ac, actionGroup.Actions.Count);
			item.EditingDone += OnEditDone;
			item.Select ();
			item.StartEditing ();
			HideAddLabel ();
			ShowAll ();
		}
		
		void OnEditDone (object sender, EventArgs args)
		{
			ActionMenuItem item = (ActionMenuItem) sender;
			item.EditingDone -= OnEditDone;
			if (item.Node.Action.GtkAction.Label.Length > 0 || item.Node.Action.GtkAction.StockId != null) {
				actionGroup.Actions.Add (item.Node.Action);
			} else {
				IDesignArea designArea = GetDesignArea ();
				designArea.ResetSelection (item);
				item.Detach ();
				item.Node.Dispose ();
				items.Remove (item);
				item.Destroy ();
				PlaceAddLabel (actionGroup.Actions.Count);
				ShowAll ();
			}
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			IDesignArea designArea = GetDesignArea ();
			designArea.SetSelection (null, null);
			return true;
		}
		
		void OnItemKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			int pos = items.IndexOf (s);
			
			switch (args.Event.Key) {
				case Gdk.Key.Up:
					pos -= (int) columns;
					break;
				case Gdk.Key.Down:
					pos += (int) columns;
					break;
				case Gdk.Key.Right:
					pos ++;
					break;
				case Gdk.Key.Left:
					pos --;
					break;
			}
			if (pos >= 0 && pos < items.Count) {
				((ActionMenuItem)items[pos]).Select ();
				args.RetVal = true;
			}
			else if (pos == items.Count) {
				OnAddClicked (null, null);
				args.RetVal = true;
			}
		}
		
		void OnGroupNameChanged (object s, EventArgs args)
		{
			if (actionGroup != null)
				actionGroup.Name = headerLabel.Text;
		}
		
		void OnSelectionChanged (object s, EventArgs args)
		{
			if (SelectionChanged != null)
				SelectionChanged (this, args);
		}
		
		public void Cut ()
		{
			ActionMenuItem menuItem = SelectedActionMenuItem;
			if (menuItem != null)
				Cut (SelectedActionMenuItem);
		}
		
		public void Copy ()
		{
			ActionMenuItem menuItem = SelectedActionMenuItem;
			if (menuItem != null)
				Copy (SelectedActionMenuItem);
		}
		
		public void Paste ()
		{
		}
		
		public void Delete ()
		{
			ActionMenuItem menuItem = SelectedActionMenuItem;
			if (menuItem != null)
				Delete (SelectedActionMenuItem);
		}
		
		void Cut (ActionMenuItem menuItem)
		{
		}
		
		void Copy (ActionMenuItem menuItem)
		{
		}
		
		void Paste (ActionMenuItem menuItem)
		{
		}
		
		void Delete (ActionMenuItem menuItem)
		{
			string msg = string.Format (Catalog.GetString ("Are you sure you want to delete the action '{0}'? It will be removed from all menus and toolbars."), menuItem.Node.Action.Name);
			Gtk.MessageDialog md = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, Gtk.ButtonsType.YesNo, msg);
			md.TransientFor = this.Toplevel as Gtk.Window;
			if (md.Run () == (int) Gtk.ResponseType.Yes) {
				menuItem.Node.Action.Delete ();
				darea.SetSelection (null, null);
			}
			md.Destroy ();
		}
		
		void IMenuItemContainer.ShowContextMenu (ActionItem aitem)
		{
			ActionMenuItem menuItem = aitem as ActionMenuItem;
			
			Gtk.Menu m = new Gtk.Menu ();
			Gtk.MenuItem item = new Gtk.ImageMenuItem (Gtk.Stock.Cut, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Cut (menuItem);
			};
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Copy, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Copy (menuItem);
			};
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Paste, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Paste (menuItem);
			};
			item.Visible = false;	// No copy & paste for now
			item = new Gtk.ImageMenuItem (Gtk.Stock.Delete, null);
			m.Add (item);
			item.Activated += delegate (object s, EventArgs a) {
				Delete (menuItem);
			};
			m.ShowAll ();
			m.Popup ();
		}		
		
		IDesignArea GetDesignArea ()
		{
			if (darea != null)
				return darea;
			
			darea = WidgetUtils.GetDesignArea (this);
			darea.SelectionChanged += OnSelectionChanged;
			return darea;
		}
		
		ActionMenu IMenuItemContainer.OpenSubmenu { 
			get { return null; } 
			set { }
		}
		
		bool IMenuItemContainer.IsTopMenu {
			get { return false; }
		}
		
		Gtk.Widget IMenuItemContainer.Widget {
			get { return this; }
		}
		
		class ActionComparer: IComparer
		{
			public int Compare (object x, object y)
			{
				return string.Compare (((Wrapper.Action)x).GtkAction.Label, ((Wrapper.Action)y).GtkAction.Label);
			}
		}
	}
	
	public class EditableLabel: Gtk.EventBox
	{
		string text;
		string markup;
		
		public EditableLabel (): this ("")
		{
		}
		
		public EditableLabel (string txt)
		{
			VisibleWindow = false;
			text = txt;
			Add (CreateLabel ());
		}
		
		public string Text {
			get { return text; }
			set {
				text = value;
				if (Child is Gtk.Entry)
					((Gtk.Entry)Child).Text = text;
				else
					((Gtk.Label)Child).Markup = Markup;
			}
		}
		
		public string MarkupTemplate {
			get { return markup; }
			set {
				markup = value;
				if (Child is Gtk.Label)
					((Gtk.Label)Child).Markup = Markup;
			}
		}
		
		public string Markup {
			get { return markup != null ? markup.Replace ("$TEXT",text) : text; }
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			IDesignArea d = WidgetUtils.GetDesignArea (this);
			if (d.IsSelected (this)) {
				if (Child is Gtk.Label) {
					StartEditing ();
					return true;
				}
			} else {
				d.SetSelection (this, null);
				return true;
			}
			return false;
		}
		
		void SelectionDisposed (object s, EventArgs args)
		{
			EndEditing ();
		}
		
		public void StartEditing ()
		{
			if (Child is Gtk.Label) {
				IDesignArea d = WidgetUtils.GetDesignArea (this);
				IObjectSelection sel = d.GetSelection (this);
				if (sel == null)
					sel = d.SetSelection (this, null);
				
				sel.Disposed += SelectionDisposed;
					
				Remove (Child);
				Add (CreateEntry ());
				ShowAll ();
				Child.GrabFocus ();
			}
		}
		
		public void EndEditing ()
		{
			if (Child is Gtk.Entry) {
				Remove (Child);
				Add (CreateLabel ());
				ShowAll ();
			}
		}
		
		Gtk.Label CreateLabel ()
		{
			Gtk.Label label = new Gtk.Label ();
			label.Markup = Markup;
			label.Xalign = 0;
			return label;
		}
		
		Gtk.Entry CreateEntry ()
		{
			Gtk.Entry e = new Gtk.Entry (text);
			e.Changed += delegate (object s, EventArgs a) {
				text = e.Text;
				if (Changed != null)
					Changed (this, a);
			};
			e.Activated += delegate (object s, EventArgs a) {
				EndEditing ();
			};
			return e;
		}
		
		public event EventHandler Changed;
	}
}
