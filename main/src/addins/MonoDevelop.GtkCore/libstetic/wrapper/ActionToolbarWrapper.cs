
using System;
using System.CodeDom;
using System.Xml;
using System.Collections;
using Stetic.Editor;

namespace Stetic.Wrapper
{
	public class ActionToolbarWrapper: Container
	{
		ActionTree actionTree;
		XmlElement toolbarInfo;
		ToolbarStyle toolbarStyle = ToolbarStyle.Default;
		ToolbarIconSize iconSize = ToolbarIconSize.Default;
		bool treeChanged;
		
		static Gtk.ToolbarStyle defaultStyle;
		static Gtk.IconSize defaultSize;
		static bool gotDefault;
		
		public enum ToolbarStyle {
			Icons,
			Text,
			Both,
			BothHoriz,
			Default
		}
	
		public enum ToolbarIconSize {
			Menu = Gtk.IconSize.Menu,
			SmallToolbar = Gtk.IconSize.SmallToolbar,
			LargeToolbar = Gtk.IconSize.LargeToolbar,
			Button = Gtk.IconSize.Button,
			Dnd = Gtk.IconSize.Dnd,
			Dialog = Gtk.IconSize.Dialog,
			Default = -1
		}
	
		public ActionToolbarWrapper()
		{
		}
		
		public override void Dispose ()
		{
			DisposeTree ();
			base.Dispose ();
		}

		public static Gtk.Toolbar CreateInstance ()
		{
			ActionToolbar t = new ActionToolbar ();
			// Looks like the default size and style are set when adding the toolbar to the window,
			// so we have to explicitly get the defaults to make sure the toolbar is properly initialized 
			GetDefaults ();
			t.IconSize = defaultSize;
			t.ToolbarStyle = defaultStyle;
			return t;
		}
		
		ActionToolbar toolbar {
			get { return (ActionToolbar) Wrapped; }
		}
		
		protected override bool AllowPlaceholders {
			get { return false; }
		}
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			CreateTree ();
			toolbar.FillMenu (actionTree);
		}
		
		public override bool HExpandable {
			get {
				return toolbar.Orientation == Gtk.Orientation.Horizontal;
			}
		}

		public override bool VExpandable {
			get {
				return toolbar.Orientation == Gtk.Orientation.Vertical;
			}
		}

		public Gtk.Orientation Orientation {
			get {
				return toolbar.Orientation;
			}
			set {
				toolbar.Orientation = value;
				EmitContentsChanged ();
			}
		}
		
		public ToolbarIconSize ButtonIconSize {
			get { return iconSize; }
			set {
				iconSize = value;
				if (value == ToolbarIconSize.Default) {
					GetDefaults ();
					toolbar.IconSize = defaultSize;
				} else {
					toolbar.IconSize = (Gtk.IconSize) ((int)value);
				}
				EmitNotify ("ButtonIconSize");
			}
		}
		
		public ToolbarStyle ButtonStyle {
			get { return toolbarStyle; }
			set {
				toolbarStyle = value;
				if (value == ToolbarStyle.Default) {
					GetDefaults ();
					toolbar.ToolbarStyle = defaultStyle;
				} else {
					toolbar.ToolbarStyle = (Gtk.ToolbarStyle) ((int)value);
				}
				EmitNotify ("ButtonStyle");
			}
		}
		
		static void GetDefaults ()
		{
			if (!gotDefault) {
				// Is there a better way of getting the default?
				Gtk.Window d = new Gtk.Window ("");
				Gtk.Toolbar t = new Gtk.Toolbar ();
				d.Add (t);
				defaultStyle = t.ToolbarStyle;
				defaultSize = t.IconSize;
				d.Destroy ();
				gotDefault = true;
			}
		}
		
		internal protected override void OnSelected ()
		{
			Loading = true;
			toolbar.ShowInsertPlaceholder = true;
			Loading = false;
		}
		
		internal protected override void OnUnselected ()
		{
			base.OnUnselected ();
			Loading = true;
			toolbar.ShowInsertPlaceholder = false;
			toolbar.Unselect ();
			Loading = false;
		}
		
		protected override XmlElement WriteProperties (ObjectWriter writer)
		{
			XmlElement elem = base.WriteProperties (writer);
			if (writer.Format == FileFormat.Native) {
				// The style and icon size is already stored in ButtonStyle and ButtonIconSize
				GladeUtils.ExtractProperty (elem, "ToolbarStyle", "");
				GladeUtils.ExtractProperty (elem, "IconSize", "");
				
				// Store ButtonIconSize as IconSize, for backwards compat
				GladeUtils.RenameProperty (elem, "ButtonIconSize", "IconSize");
				
				if (toolbarInfo != null)
					elem.AppendChild (writer.XmlDocument.ImportNode (toolbarInfo, true));
				else
					elem.AppendChild (actionTree.Write (writer.XmlDocument, writer.Format));
			}
			return elem;
		}
		
		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			// ButtonIconSize is stored as IconSize
			GladeUtils.RenameProperty (elem, "IconSize", "ButtonIconSize");
			
			base.ReadProperties (reader, elem);
			toolbarInfo = elem ["node"];
		}
		
		protected override void OnNameChanged (WidgetNameChangedArgs args)
		{
			base.OnNameChanged (args);
			if (actionTree != null)
				actionTree.Name = Name;
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			BuildTree ();
			actionTree.Type = Gtk.UIManagerItemType.Toolbar;
			actionTree.Name = Name;
			
			CodeExpression exp = GenerateUiManagerElement (ctx, actionTree);
			if (exp != null)
				return new CodeCastExpression (typeof(Gtk.Toolbar).ToGlobalTypeRef (),	exp);
			else
				return base.GenerateObjectCreation (ctx);
		}

		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			if (toolbarStyle == ToolbarStyle.Default && prop.Name == "ToolbarStyle")
				return;
			else if (iconSize == ToolbarIconSize.Default && prop.Name == "IconSize")
				return;
			else
				base.GeneratePropertySet (ctx, var, prop);
		}
		
		internal protected override void OnDesignerAttach (IDesignArea designer)
		{
			base.OnDesignerAttach (designer);
			BuildTree ();
			
			Loading = true;
			toolbar.FillMenu (actionTree);
			Loading = false;
			
			if (LocalActionGroups.Count == 0)
				LocalActionGroups.Add (new ActionGroup ("Default"));
		}
		
		protected override void EmitNotify (string propertyName)
		{
			base.EmitNotify (propertyName);
			toolbar.FillMenu (actionTree);
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
				object stat = toolbar.SaveStatus ();
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
			object oldStat = toolbar.SaveStatus ();
			
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
					DisposeTree ();
					CreateTree ();
					actionTree.Read (this, xdiff);
					toolbar.FillMenu (actionTree);
					Loading = false;
				}
			}
			
			// Restore the status after all menu structure has been properly built
			GLib.Timeout.Add (50, delegate {
				toolbar.RestoreStatus (data[2]);
				return false;
			});
			
			return new object [] { retBaseDiff, oldNode, oldStat };
		}
		
		
		void BuildTree ()
		{
			if (toolbarInfo != null) {
				DisposeTree ();
				CreateTree ();
				actionTree.Read (this, toolbarInfo);
				toolbarInfo = null;
			}
		}
		
		void CreateTree ()
		{
			actionTree = new ActionTree ();
			actionTree.Name = Name;
			actionTree.Type = Gtk.UIManagerItemType.Toolbar;
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
}
