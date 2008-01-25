
using System;
using System.CodeDom;
using System.Xml;
using System.Collections;
using Stetic.Editor;

namespace Stetic.Wrapper
{
	public class MenuBar: Container
	{
		ActionTree actionTree;
		XmlElement menuInfo;
		bool treeChanged;
		
		public MenuBar()
		{
		}
		
		public override void Dispose ()
		{
			DisposeTree ();
			base.Dispose ();
		}
		
		public static new Gtk.MenuBar CreateInstance ()
		{
			return new ActionMenuBar ();
		}
		
		protected override bool AllowPlaceholders {
			get { return false; }
		}
		
		ActionMenuBar menu {
			get { return (ActionMenuBar) Wrapped; }
		}
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			CreateTree ();
		}
		
		internal protected override void OnSelected ()
		{
			Loading = true;
			menu.ShowInsertPlaceholder = true;
			Loading = false;
		}
		
		internal protected override void OnUnselected ()
		{
			base.OnUnselected ();
			Loading = true;
			menu.ShowInsertPlaceholder = false;
			menu.Unselect ();
			Loading = false;
		}
		
		protected override XmlElement WriteProperties (ObjectWriter writer)
		{
			XmlElement elem = base.WriteProperties (writer);
			if (menuInfo != null)
				elem.AppendChild (writer.XmlDocument.ImportNode (menuInfo, true));
			else
				elem.AppendChild (actionTree.Write (writer.XmlDocument, writer.Format));
			return elem;
		}
		
		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			base.ReadProperties (reader, elem);
			menuInfo = elem ["node"];
			treeChanged = false;
		}
		
		public override object GetUndoDiff ()
		{
			XmlElement oldElem = treeChanged ? UndoManager.GetObjectStatus (this) ["node"] : null;
			if (oldElem != null)
				oldElem = (XmlElement) oldElem.CloneNode (true);

			treeChanged = false;
			object baseDiff = base.GetUndoDiff ();
			
			if (oldElem != null) {
				XmlElement newElem = UndoManager.GetObjectStatus (this) ["node"];
				if (newElem != null && oldElem.OuterXml == newElem.OuterXml)
					oldElem = null;
			}
			
			if (baseDiff == null && oldElem == null)
				return null;
			else {
				object stat = menu.SaveStatus ();
				return new object[] { baseDiff, oldElem, stat };
			}
		}
		
		public override object ApplyUndoRedoDiff (object diff)
		{
			object[] data = (object[]) diff;
			object retBaseDiff;
			XmlElement oldNode = null;
			
			if (actionTree != null) {
				XmlElement status = UndoManager.GetObjectStatus (this);
				oldNode = status ["node"];
				if (oldNode != null)
					oldNode = (XmlElement) oldNode.CloneNode (true);
			}
			object oldStat = menu.SaveStatus ();
			
			if (data [0] != null)
				retBaseDiff = base.ApplyUndoRedoDiff (data [0]);
			else
				retBaseDiff = null;
				
			XmlElement xdiff = (XmlElement) data [1];

			if (xdiff != null) {
				XmlElement status = UndoManager.GetObjectStatus (this);
				XmlElement prevNode = status ["node"];
				if (prevNode != null)
					status.RemoveChild (prevNode);
				status.AppendChild (xdiff);
				
				if (actionTree != null) {
					Loading = true;
					menu.OpenSubmenu = null;
					DisposeTree ();
					CreateTree ();
					actionTree.Read (this, xdiff);
					menu.FillMenu (actionTree);
					Loading = false;
				} else
					menuInfo = xdiff;
			}
			
			// Restore the status after all menu structure has been properly built
			GLib.Timeout.Add (50, delegate {
				menu.RestoreStatus (data[2]);
				return false;
			});
			
			return new object [] { retBaseDiff, oldNode, oldStat };
		}
		
		protected override void OnNameChanged (WidgetNameChangedArgs args)
		{
			base.OnNameChanged (args);
			if (actionTree != null)
				actionTree.Name = Wrapped.Name;
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			BuildTree ();
			CodeExpression exp = GenerateUiManagerElement (ctx, actionTree);
			if (exp != null)
				return new CodeCastExpression (typeof(Gtk.MenuBar),	exp);
			else
				return base.GenerateObjectCreation (ctx);
		}

		internal protected override void OnDesignerAttach (IDesignArea designer)
		{
			base.OnDesignerAttach (designer);
			BuildTree ();
			Loading = true;
			menu.FillMenu (actionTree);
			Loading = false;
			
			if (LocalActionGroups.Count == 0)
				LocalActionGroups.Add (new ActionGroup ("Default"));
		}
		
		void BuildTree ()
		{
			if (menuInfo != null) {
				DisposeTree ();
				CreateTree ();
				actionTree.Read (this, menuInfo);
				menuInfo = null;
			}
		}
		
		void CreateTree ()
		{
			actionTree = new ActionTree ();
			actionTree.Name = Wrapped.Name;
			actionTree.Type = Gtk.UIManagerItemType.Menubar;
			actionTree.Changed += OnTreeChanged;
		}
		
		void DisposeTree ()
		{
			if (actionTree != null) {
				actionTree.Dispose ();
				actionTree.Changed -= OnTreeChanged;
				actionTree = null;
			}
		}
		
		void OnTreeChanged (object s, EventArgs a)
		{
			treeChanged = true;
			NotifyChanged ();
		}
	}
	
	class CustomMenuBarItem: Gtk.MenuItem
	{
		public ActionMenuItem ActionMenuItem;
		public ActionTreeNode Node;
	}
		
	public class ActionPaletteItem: Gtk.HBox
	{
		ActionTreeNode node;
		bool disposeNode;
		
		public ActionPaletteItem (Gtk.UIManagerItemType type, string name, Action action) 
		: this (new ActionTreeNode (type, name, action))
		{
			disposeNode = true;
		}
		
		public ActionPaletteItem (ActionTreeNode node)
		{
			this.node = node;
			Spacing = 3;
			if (node.Type == Gtk.UIManagerItemType.Menu) {
				PackStart (new Gtk.Label ("Menu"), true, true, 0);
			} else if (node.Action != null && node.Action.GtkAction != null) {
				if (node.Action.GtkAction.StockId != null)
					PackStart (node.Action.CreateIcon (Gtk.IconSize.Menu), true, true, 0);
				PackStart (new Gtk.Label (node.Action.GtkAction.Label), true, true, 0);
			} else if (node.Type == Gtk.UIManagerItemType.Separator) {
				PackStart (new Gtk.Label ("Separator"), true, true, 0);
			} else {
				PackStart (new Gtk.Label ("Empty Action"), true, true, 0);
			}
			ShowAll ();
		}
		
		public ActionTreeNode Node {
			get { return node; }
		}
		
		public override void Dispose ()
		{
			if (disposeNode)
				node.Dispose ();
			base.Dispose ();
		}

	}
}
