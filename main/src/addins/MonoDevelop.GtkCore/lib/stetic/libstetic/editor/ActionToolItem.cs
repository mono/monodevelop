
using System;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic.Editor
{
	class ActionToolItem: ActionItem
	{
		ActionToolbar parentToolbar;
		
		bool motionDrag;
		bool showingText;
		Gtk.Widget dropButton;
		
		public event EventHandler EditingDone;
		
		internal ActionToolItem (Widget wrapper, ActionToolbar parent, ActionTreeNode node)
		: this (wrapper, parent, node, 0)
		{
		}
		
		internal ActionToolItem (Widget wrapper, ActionToolbar parent, ActionTreeNode node, uint itemSpacing): base (node, parent, itemSpacing)
		{
			this.parentToolbar = parent;
			this.wrapper = wrapper;
			CreateControls ();
		}
		
		public void StartEditing (bool doClick)
		{
			if (!editing && node.Action != null) {
				// Don't allow efiting global actions
				if (wrapper != null && wrapper.Project.ActionGroups.IndexOf (node.Action.ActionGroup) != -1)
					return;
				editing = true;
				Refresh ();
				
				if (doClick && dropButton != null) {
					// Make sure the dropButton is properly shown
					while (Gtk.Application.EventsPending ())
						Gtk.Application.RunIteration ();
					OnSelectIcon (null, null);
				}
			}
		}
		
		protected override void EndEditing (Gdk.Key exitKey)
		{
			if (editing) {
				editing = false;
				Refresh ();
				while (Gtk.Application.EventsPending ())
					Gtk.Application.RunIteration ();
				GrabFocus ();
				if (EditingDone != null)
					EditingDone (this, EventArgs.Empty);
			}
		}

		void CreateControls ()
		{
			Gtk.Widget icon = null;
			Gtk.Widget label = null;
			dropButton = null;
			
			if (Child != null) {
				Gtk.Widget w = Child;
				Remove (w);
				w.Destroy ();
			}
			
			if (node.Type == Gtk.UIManagerItemType.Separator) {
				Gtk.Widget sep;
				if (parentToolbar.Orientation == Gtk.Orientation.Horizontal) {
					sep = new Gtk.VSeparator ();
				} else {
					sep = new Gtk.HSeparator ();
				}
				Gtk.HBox box = new Gtk.HBox ();
				box.BorderWidth = 6;
				box.PackStart (sep, true, true, 0);
				Add (box);
				return;
			}
			
			if (node.Action == null)
				return;
				
			Gtk.Action gaction = node.Action.GtkAction;
			
			bool showText = parentToolbar.ToolbarStyle == Gtk.ToolbarStyle.Text;
			bool showIcon = parentToolbar.ToolbarStyle == Gtk.ToolbarStyle.Icons;
			if (parentToolbar.ToolbarStyle == Gtk.ToolbarStyle.Both) {
				showText = showIcon = true;
			}
			else if (parentToolbar.ToolbarStyle == Gtk.ToolbarStyle.BothHoriz) {
				showText = parentToolbar.Orientation == Gtk.Orientation.Vertical || gaction.IsImportant;
				showIcon = true;
			}
		
			string text = node.Action.ToolLabel;
			showingText = showText;

			if (showIcon)
			{
				if (gaction.StockId != null) {
					icon = node.Action.CreateIcon (parentToolbar.IconSize);
				} else if (!gaction.IsImportant) {
					icon = CreateFakeItem ();
				}
			}
			
			Gtk.Tooltips tooltips = null;
			if (editing)
				tooltips = new Gtk.Tooltips ();
			
			if (editing) {
				Gtk.HBox bbox = new Gtk.HBox ();
				bbox.Spacing = 3;
				if (icon != null) {
					bbox.PackStart (icon, false, false, 0);
				}
				bbox.PackStart (new Gtk.Arrow (Gtk.ArrowType.Down, Gtk.ShadowType.In), false, false, 0);
				Gtk.Button b = new Gtk.Button (bbox);
				tooltips.SetTip (b, "Select action type", "");
				b.Relief = Gtk.ReliefStyle.None;
				b.ButtonPressEvent += OnSelectIcon;
				dropButton = b;
				icon = b;
				
				if (showText) {
					Gtk.Entry entry = new Gtk.Entry ();
					entry.Text = text;
					entry.Changed += OnLabelChanged;
					entry.Activated += OnLabelActivated;
					entry.HasFrame = false;
					label = entry;
					tooltips.SetTip (entry, "Action label", "");
				}
			} else if (showText && text != null && text.Length > 0) {
				label = new Gtk.Label (text);
				label.Sensitive = editing || node.Action == null || node.Action.GtkAction.Sensitive;
			}
			
			if (icon != null && label != null) {
				if (parentToolbar.ToolbarStyle == Gtk.ToolbarStyle.BothHoriz) {
					Gtk.HBox box = new Gtk.HBox ();
					box.PackStart (icon, false, false, 0);
					box.PackStart (label, true, true, 0);
					icon = box;
				} else if (parentToolbar.ToolbarStyle == Gtk.ToolbarStyle.Both) {
					Gtk.VBox box = new Gtk.VBox ();
					Gtk.Alignment al = new Gtk.Alignment (0.5f, 0f, 0f, 0f);
					al.Add (icon);
					box.PackStart (al, false, false, 0);
					box.PackStart (label, true, true, 0);
					icon = box;
				}
			} else if (label != null) {
				icon = label;
			}
			
			if (icon == null) {
				icon = CreateFakeItem ();
			}
			
			icon.Sensitive = editing || node.Action == null || node.Action.GtkAction.Sensitive;
			
			if (!editing) {
				Gtk.Button but = new Gtk.Button (icon);
				but.Relief = Gtk.ReliefStyle.None;
				but.ButtonPressEvent += OnToolItemPress;
				but.ButtonReleaseEvent += OnMemuItemRelease;
				but.MotionNotifyEvent += OnMotionNotify;
				but.Events |= Gdk.EventMask.PointerMotionMask;
				icon = but;
			}
			
			Add (icon);

			ShowAll ();
		}
		
		Gtk.Widget CreateFakeItem ()
		{
			Gtk.Frame frm = new Gtk.Frame ();
			frm.ShadowType = Gtk.ShadowType.Out;
			int w, h;
			Gtk.Icon.SizeLookup (parentToolbar.IconSize, out w, out h);
			frm.WidthRequest = w;
			frm.HeightRequest = h;
			return frm;
		}
		
		void OnLabelChanged (object ob, EventArgs args)
		{
			localUpdate = true;
			
			Gtk.Entry entry = ob as Gtk.Entry;
			if (entry.Text.Length > 0 || node.Action.GtkAction.StockId != null) {
				using (node.Action.UndoManager.AtomicChange) {
					node.Action.Label = entry.Text;
					node.Action.NotifyChanged ();
				}
			}
			localUpdate = false;
		}
		
		void OnLabelActivated (object ob, EventArgs args)
		{
			EndEditing (Gdk.Key.Return);
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnSelectIcon (object s, Gtk.ButtonPressEventArgs args)
		{
			Gtk.Menu menu = new Gtk.Menu ();
			
			Gtk.CheckMenuItem item = new Gtk.CheckMenuItem (Catalog.GetString ("Action"));
			item.DrawAsRadio = true;
			item.Active = (node.Action.Type == Stetic.Wrapper.Action.ActionType.Action);
			item.Activated += OnSetActionType;
			menu.Insert (item, -1);
			
			item = new Gtk.CheckMenuItem (Catalog.GetString ("Radio Action"));
			item.DrawAsRadio = true;
			item.Active = (node.Action.Type == Stetic.Wrapper.Action.ActionType.Radio);
			item.Activated += OnSetRadioType;
			menu.Insert (item, -1);
			
			item = new Gtk.CheckMenuItem (Catalog.GetString ("Toggle Action"));
			item.DrawAsRadio = true;
			item.Active = (node.Action.Type == Stetic.Wrapper.Action.ActionType.Toggle);
			item.Activated += OnSetToggleType;
			menu.Insert (item, -1);
			
			menu.Insert (new Gtk.SeparatorMenuItem (), -1);
			
			Gtk.MenuItem itIcons = new Gtk.MenuItem (Catalog.GetString ("Select Icon"));
			menu.Insert (itIcons, -1);
			IconSelectorMenu menuIcons = new IconSelectorMenu (GetProject ());
			menuIcons.IconSelected += OnStockSelected;
			itIcons.Submenu = menuIcons;
			
			Gtk.MenuItem it = new Gtk.MenuItem (Catalog.GetString ("Clear Icon"));
			it.Sensitive = (node.Action.GtkAction.StockId != null);
			it.Activated += OnClearIcon;
			menu.Insert (it, -1);
			
			menu.ShowAll ();

			uint but = args != null ? args.Event.Button : 1;
			menu.Popup (null, null, new Gtk.MenuPositionFunc (OnDropMenuPosition), but, Gtk.Global.CurrentEventTime);
			
			// Make sure we get the focus after closing the menu, so we can keep browsing buttons
			// using the keyboard.
			menu.Hidden += delegate (object sender, EventArgs a) {
				GrabFocus ();
			};
			
			if (args != null)
				args.RetVal = false;
		}
		
		void OnDropMenuPosition (Gtk.Menu menu, out int x, out int y, out bool pushIn)
		{
			dropButton.ParentWindow.GetOrigin (out x, out y);
			x += dropButton.Allocation.X;
			y += dropButton.Allocation.Y + dropButton.Allocation.Height;
			pushIn = true;
		}
		
		void OnSetToggleType (object ob, EventArgs args)
		{
			using (node.Action.UndoManager.AtomicChange) {
				node.Action.Type = Stetic.Wrapper.Action.ActionType.Toggle;
				node.Action.NotifyChanged ();
			}
		}
		
		void OnSetRadioType (object ob, EventArgs args)
		{
			using (node.Action.UndoManager.AtomicChange) {
				node.Action.Type = Stetic.Wrapper.Action.ActionType.Radio;
				node.Action.NotifyChanged ();
			}
		}
		
		void OnSetActionType (object ob, EventArgs args)
		{
			using (node.Action.UndoManager.AtomicChange) {
				node.Action.Type = Stetic.Wrapper.Action.ActionType.Action;
				node.Action.NotifyChanged ();
			}
		}
		
		void OnStockSelected (object s, IconEventArgs args)
		{
			using (node.Action.UndoManager.AtomicChange) {
				node.Action.StockId = args.IconId;
				node.Action.NotifyChanged ();
			}
		}
		
		void OnClearIcon (object on, EventArgs args)
		{
			using (node.Action.UndoManager.AtomicChange) {
				node.Action.StockId = null;
				node.Action.NotifyChanged ();
			}
		}
		
		public override void Refresh ()
		{
			CreateControls ();
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnToolItemPress (object ob, Gtk.ButtonPressEventArgs args)
		{
			if (wrapper != null && wrapper.Project.Selection != wrapper.Wrapped) {
				wrapper.Select ();
				args.RetVal = true;
				return;
			}
			if (args.Event.Button == 1)
				motionDrag = true;
			args.RetVal = ProcessButtonPress (args.Event);
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnMemuItemRelease (object ob, Gtk.ButtonReleaseEventArgs args)
		{
			args.RetVal = ProcessButtonRelease (args.Event);
			motionDrag = false;
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnMotionNotify (object ob, Gtk.MotionNotifyEventArgs args)
		{
			if (motionDrag) {
				// Looks like drag begin can be intercepted, so the motion notify
				// has to be used.
				ProcessDragBegin (null, args.Event);
				motionDrag = false;
			}
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton ev)
		{
			return ProcessButtonRelease (ev);
		}
		
		public bool ProcessButtonRelease (Gdk.EventButton ev)
		{
			// Clicking a selected item starts the edit mode
			if (editOnRelease) {
				StartEditing (!showingText);
			}

			editOnRelease = false;
			return true;
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey e)
		{
			if (e.Key == Gdk.Key.Return)
				EndEditing (Gdk.Key.Return);
			else if (e.Key == Gdk.Key.Escape)
				EndEditing (Gdk.Key.Escape);
			return base.OnKeyPressEvent (e);
		}
	}
}
