using System;
using System.Collections;
using System.Xml;
using System.CodeDom;
using Stetic.Undo;

namespace Stetic.Wrapper {

	public class Widget : Object, IEditableObject
	{
		static DiffGenerator propDiffGenerator;
		
		string oldName;
		string oldMemberName;
		bool hexpandable, vexpandable;
		bool generatePublic = true;

		bool window_visible = true;
		bool hasDefault;
		bool canDefault;
		Gdk.EventMask events;
		bool canFocus;
		
		ActionGroupCollection actionGroups;
		string member;
		string tooltip;
		
		bool requiresUndoStatusUpdate;
		
		// Name of the generated UIManager
		string uiManagerName;
		// List of groups added to the UIManager
		ArrayList includedActionGroups;
		
		bool unselectable;
		bool boundToScrollWindow;
		
		public event EventHandler Destroyed;
		
		// Fired when the name of the widget changes.
		public event WidgetNameChangedHandler NameChanged;
		// Fired when the member name of the widget changes.
		public event WidgetNameChangedHandler MemberNameChanged;
		
		static Widget ()
		{
			propDiffGenerator = new DiffGenerator ();
			propDiffGenerator.CurrentStatusAdaptor = new XmlDiffAdaptor ();
			propDiffGenerator.NewStatusAdaptor = propDiffGenerator.CurrentStatusAdaptor;
		}
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			
			oldName = ((Gtk.Widget)obj).Name;
			
			if (!initialized) {
				events = Wrapped.Events;
				canFocus = Wrapped.CanFocus;
			}
		
			if (!(Wrapped is Gtk.Window))
				Wrapped.ShowAll ();
			
			Wrapped.PopupMenu += PopupMenu;
			Wrapped.FocusInEvent += OnFocusIn;
			InterceptClicks (Wrapped);

			hexpandable = this.ClassDescriptor.HExpandable;
			vexpandable = this.ClassDescriptor.VExpandable;
			
			if (ParentWrapper != null) {
				// Make sure the widget's name is not already being used.
				string nn = ParentWrapper.GetValidWidgetName (Wrapped);
				if (nn != Wrapped.Name)
					Wrapped.Name = nn;
			}
			
			Wrapped.Destroyed += OnDestroyed;
			
			if (Wrapped.Parent != null) {
				// The object was added to the parent before creating the wrapper.
				// Since it's now a wrapped object, the parent don't need to
				// intercept clicks for it anymore
				Widget w = GetInterceptorParent ();
				if (w != null)
					w.UninterceptClicks (Wrapped);
			}
		}
		
		void OnDestroyed (object on, EventArgs a)
		{
			if (Destroyed != null)
				Destroyed (this, a);
			Dispose ();
		}
		
		public override void Dispose ()
		{
			if (Wrapped == null)
				return;

			if (Project != null && Project.Selection == Wrapped)
				Project.Selection = null;

			Wrapped.Destroyed -= OnDestroyed;
			Wrapped.PopupMenu -= PopupMenu;
			Wrapped.FocusInEvent -= OnFocusIn;
			UninterceptClicks (Wrapped);
			
			if (actionGroups != null) {
				foreach (ActionGroup ag in actionGroups)
					ag.Dispose ();
				actionGroups = null;
			}
			base.Dispose ();
		}
		
		void OnFocusIn (object s, Gtk.FocusInEventArgs a)
		{
			if (!Unselectable)
				Select ();
			else if (ParentWrapper != null)
				ParentWrapper.Select ();
		}
		
		internal override UndoManager GetUndoManagerInternal ()
		{
			if (ParentWrapper != null)
				return ParentWrapper.UndoManager;
			else
				return base.GetUndoManagerInternal ();
		}
		
		public bool GeneratePublic {
			get { return generatePublic; }
			set { generatePublic = value; }
		}
		
		public bool Unselectable {
			get { 
				return unselectable; 
			}
			set {
				if (value == unselectable)
					return;
				unselectable = value;
				Widget w = GetInterceptorParent ();
				if (w != null) {
					// If a widget becomes unselectable, then the parent must intercept
					// their clicks.
					if (unselectable)
						w.InterceptClicks (Wrapped);
					else
						w.UninterceptClicks (Wrapped);
				}
			}
		}
		
		Widget GetInterceptorParent ()
		{
			Gtk.Widget wp = Wrapped.Parent;
			while (wp != null && Lookup (wp) == null)
				wp = wp.Parent;
			return Lookup (wp);
		}
		
		void InterceptClicks (Gtk.Widget widget)
		{
			if (widget is Stetic.Placeholder)
				return;

			if (!widget.IsRealized)
				widget.Events |= Gdk.EventMask.ButtonPressMask;
			widget.WidgetEvent += WidgetEvent;

			Gtk.Container container = widget as Gtk.Container;
			if (container != null) {
				container.Added += OnInterceptedChildAdded;
				container.Removed += OnInterceptedChildRemoved;
				foreach (Gtk.Widget child in container.AllChildren) {
					Widget w = Lookup (child);
					if (w == null || w.Unselectable)
						InterceptClicks (child);
				}
			}
		}
		
		[GLib.ConnectBefore]
		void OnInterceptedChildAdded (object o, Gtk.AddedArgs args)
		{
			Widget w = Lookup (args.Widget);
			if (w == null || w.Unselectable)
				InterceptClicks (args.Widget);
		}
		
		void OnInterceptedChildRemoved (object o, Gtk.RemovedArgs args)
		{
			UninterceptClicks (args.Widget);
		}
		
		void UninterceptClicks (Gtk.Widget widget)
		{
			widget.WidgetEvent -= WidgetEvent;
			
			Gtk.Container container = widget as Gtk.Container;
			if (container != null) {
				container.Added -= OnInterceptedChildAdded;
				container.Removed -= OnInterceptedChildRemoved;
				foreach (Gtk.Widget child in container.AllChildren) {
					if (Lookup (child) == null)
						UninterceptClicks (child);
				}
			}
		}
		
		public new Gtk.Widget Wrapped {
			get {
				return base.Wrapped as Gtk.Widget;
			}
		}

		public Stetic.Wrapper.Container ParentWrapper {
			get {
				return Container.LookupParent (Wrapped);
			}
		}
		
		public bool IsTopLevel {
			get { return Wrapped.Parent == null || Widget.Lookup (Wrapped.Parent) == null; }
		}
		
		public string UIManagerName {
			get { return uiManagerName; }
		}
		
		public string MemberName {
			get { return member != null ? member : ""; }
			set { member = value; EmitNotify ("MemberName"); }
		}

		public Container GetTopLevel ()
		{
			Widget c = this;
			while (!c.IsTopLevel)
				c = c.ParentWrapper;
			return c as Container;
		}
		
		public ActionGroupCollection LocalActionGroups {
			get {
				if (IsTopLevel) {
					if (actionGroups == null) {
						actionGroups = new ActionGroupCollection ();
						actionGroups.SetOwner (this);
						actionGroups.ActionGroupAdded += OnGroupAdded;
						actionGroups.ActionGroupRemoved += OnGroupRemoved;
						actionGroups.ActionGroupChanged += OnGroupChanged;
					}
					return actionGroups;
				} else {
					return ParentWrapper.LocalActionGroups;
				}
			}
		}
		
		void OnGroupAdded (object s, Stetic.Wrapper.ActionGroupEventArgs args)
		{
			args.ActionGroup.SignalAdded += OnSignalAdded;
			args.ActionGroup.SignalRemoved += OnSignalRemoved;
			args.ActionGroup.SignalChanged += OnSignalChanged;
			NotifyChanged ();
		}
		
		void OnGroupRemoved (object s, Stetic.Wrapper.ActionGroupEventArgs args)
		{
			args.ActionGroup.SignalAdded -= OnSignalAdded;
			args.ActionGroup.SignalRemoved -= OnSignalRemoved;
			args.ActionGroup.SignalChanged -= OnSignalChanged;
			NotifyChanged ();
		}
		
		void OnGroupChanged (object s, Stetic.Wrapper.ActionGroupEventArgs args)
		{
			NotifyChanged ();
		}
		
		void OnSignalAdded (object sender, SignalEventArgs args)
		{
			OnSignalAdded (args);
		}

		void OnSignalRemoved (object sender, SignalEventArgs args)
		{
			OnSignalRemoved (args);
		}

		void OnSignalChanged (object sender, SignalChangedEventArgs args)
		{
			OnSignalChanged (args);
		}

		[GLib.ConnectBefore]
		void WidgetEvent (object obj, Gtk.WidgetEventArgs args)
		{
			if (args.Event.Type == Gdk.EventType.ButtonPress)
				args.RetVal = HandleClick ((Gdk.EventButton)args.Event);
		}

		internal bool HandleClick (Gdk.EventButton evb)
		{
			int x = (int)evb.X, y = (int)evb.Y;
			int erx, ery, wrx, wry;

			// Translate from event window to widget window coords
			evb.Window.GetOrigin (out erx, out ery);
			Wrapped.GdkWindow.GetOrigin (out wrx, out wry);
			x += erx - wrx;
			y += ery - wry;

			Widget wrapper = FindWrapper (Wrapped, x, y);
			if (wrapper == null)
				return false;

			if (wrapper.Wrapped != proj.Selection) {
				wrapper.Select ();
				return true;
			} else if (evb.Button == 3) {
				proj.PopupContextMenu (wrapper);
				return true;
			} else
				return false;
		}

		Widget FindWrapper (Gtk.Widget top, int x, int y)
		{
			Widget wrapper;

			Gtk.Container container = top as Gtk.Container;
			if (container != null) {
				foreach (Gtk.Widget child in container.AllChildren) {
					if (!child.IsDrawable)
						continue;

					Gdk.Rectangle alloc = child.Allocation;
					if (alloc.Contains (x, y)) {
						if (child.GdkWindow == top.GdkWindow)
							wrapper = FindWrapper (child, x, y);
						else
							wrapper = FindWrapper (child, x - alloc.X, y - alloc.Y);
						if (wrapper != null)
							return wrapper;
					}
				}
			}

			wrapper = Lookup (top);
			if (wrapper == null || wrapper.Unselectable)
				return null;
			return wrapper;
		}

		void PopupMenu (object obj, EventArgs args)
		{
			proj.PopupContextMenu (this);
		}

		public void Select ()
		{
			proj.Selection = Wrapped;
		}
		
		public void Unselect ()
		{
			if (proj.Selection == Wrapped)
				proj.Selection = null;
		}
		
		internal protected virtual void OnSelected ()
		{
		}

		internal protected virtual void OnUnselected ()
		{
		}

		public void Delete ()
		{
			if (Project.Selection == Wrapped)
				Project.Selection = null;

			if (ParentWrapper != null)
				ParentWrapper.Delete (this);
			else
				Wrapped.Destroy ();
		}
		
		internal bool RequiresUndoStatusUpdate {
			get { return requiresUndoStatusUpdate; }
			set { requiresUndoStatusUpdate = value; }
		}
		
		public override ObjectWrapper FindObjectByUndoId (string id)
		{
			ObjectWrapper c = base.FindObjectByUndoId (id);
			if (c != null)
				return c;

			if (actionGroups != null)
				return actionGroups.FindObjectByUndoId (id);
			else
				return null;
		}
		
		public override object GetUndoDiff ()
		{
			XmlElement oldElem = UndoManager.GetObjectStatus (this);
			XmlElement newElem = WriteProperties (new ObjectWriter (oldElem.OwnerDocument, FileFormat.Native));
			
			ObjectDiff propsDiff = propDiffGenerator.GetDiff (newElem, oldElem);
			ObjectDiff actionsDiff = LocalActionGroups.GetDiff (Project, oldElem);
			
			UndoManager.UpdateObjectStatus (this, newElem);
			
			if (propsDiff == null && actionsDiff == null)
				return null;
			else
				return new ObjectDiff[] { propsDiff, actionsDiff };
		}
		
		public override object ApplyUndoRedoDiff (object diff)
		{
			ObjectDiff[] data = (ObjectDiff[]) diff;
			
			XmlElement status = UndoManager.GetObjectStatus (this);
			XmlElement oldElem = (XmlElement) status.CloneNode (true);
			
			ObjectDiff propsDiff = data [0];
			
			if (propsDiff != null) {
				propDiffGenerator.ApplyDiff (status, propsDiff);
				ReadProperties (new ObjectReader (Project, FileFormat.Native), status);
				data [0] = propDiffGenerator.GetDiff (status, oldElem);
			}
			
			ObjectDiff actionsDiff = data [1];
			if (actionsDiff != null) {
				LocalActionGroups.ApplyDiff (Project, actionsDiff);
				data [1] = LocalActionGroups.GetDiff (Project, oldElem);
			}

			return data;
		}

		public override void Read (ObjectReader reader, XmlElement elem)
		{
			ReadActionGroups (reader, elem);
			ReadProperties (reader, elem);
		}
		
		protected void ReadActionGroups (ObjectReader reader, XmlElement elem)
		{
			if (reader.Format == FileFormat.Native) {
				if (actionGroups == null) {
					actionGroups = new ActionGroupCollection ();
					actionGroups.SetOwner (this);
					actionGroups.ActionGroupAdded += OnGroupAdded;
					actionGroups.ActionGroupRemoved += OnGroupRemoved;
					actionGroups.ActionGroupChanged += OnGroupChanged;
				} else
					actionGroups.Clear ();
				foreach (XmlElement groupElem in elem.SelectNodes ("action-group")) {
					ActionGroup actionGroup = new ActionGroup ();
					actionGroup.Read (reader, groupElem);
					actionGroups.Add (actionGroup); 
				}
			}
		}

		protected virtual void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			if (Wrapped != null) {
				// There is already an instance. Load the default values.
				this.ClassDescriptor.ResetInstance (Wrapped);
				Signals.Clear ();
			}
			
			if (reader.Format == FileFormat.Native)
				WidgetUtils.Read (this, elem);
			else
				GladeUtils.ImportWidget (this, elem);
			
			string uid = elem.GetAttribute ("undoId");
			if (uid.Length > 0)
				UndoId = uid;
			oldName = Wrapped.Name;
		}
		
		public override XmlElement Write (ObjectWriter writer)
		{
			XmlElement elem = WriteProperties (writer);
			WriteActionGroups (writer, elem);
			return elem;
		}
		
		protected virtual XmlElement WriteProperties (ObjectWriter writer)
		{
			if (writer.Format == FileFormat.Native) {
				XmlElement elem = WidgetUtils.Write (this, writer.XmlDocument);
				if (writer.CreateUndoInfo)
					elem.SetAttribute ("undoId", UndoId);
				return elem;
			}
			else {
				XmlElement elem = GladeUtils.ExportWidget (this, writer.XmlDocument);
				GladeUtils.ExtractProperty (elem, "name", "");
				return elem;
			}
		}
		
		protected void WriteActionGroups (ObjectWriter writer, XmlElement elem)
		{
			if (writer.Format == FileFormat.Native) {
				if (actionGroups != null) {
					foreach (ActionGroup actionGroup in actionGroups)
						elem.InsertBefore (actionGroup.Write (writer), elem.FirstChild);
				}
			}
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (actionGroups != null && actionGroups.Count > 0) {
				// Create an UI manager
				uiManagerName = ctx.NewId ();
				CodeVariableDeclarationStatement uidec = new CodeVariableDeclarationStatement (
					typeof (Gtk.UIManager),
					uiManagerName,
					 new CodeObjectCreateExpression (typeof (Gtk.UIManager))
				);
				CodeVariableReferenceExpression uixp = new CodeVariableReferenceExpression (uiManagerName);
				ctx.Statements.Add (uidec);
				
				includedActionGroups = new ArrayList ();
				
				// Generate action group creation
				foreach (ActionGroup actionGroup in actionGroups) {
					
					// Create the action group
					string grpVar = ctx.NewId ();
					uidec = new CodeVariableDeclarationStatement (
						typeof (Gtk.ActionGroup),
						grpVar,
						actionGroup.GenerateObjectCreation (ctx)
					);
					ctx.Statements.Add (uidec);
					actionGroup.GenerateBuildCode (ctx, new CodeVariableReferenceExpression (grpVar));
					
					// Insert the action group in the UIManager
					CodeMethodInvokeExpression mi = new CodeMethodInvokeExpression (
						uixp,
						"InsertActionGroup",
						new CodeVariableReferenceExpression (grpVar),
						new CodePrimitiveExpression (includedActionGroups.Count)
					);
					ctx.Statements.Add (mi);
				
					includedActionGroups.Add (actionGroup);
				}
				
				// Adds the accel group to the window
				Window w = GetTopLevel () as Window;
				if (w != null) {
					CodeMethodInvokeExpression ami = new CodeMethodInvokeExpression (
						ctx.WidgetMap.GetWidgetExp (w),
						"AddAccelGroup",
						new CodePropertyReferenceExpression (
							uixp,
							"AccelGroup"
						)
					);
					ctx.Statements.Add (ami);
				} else {
					// There is no top level window, this must be a custom widget.
					// The only option is to register the accel group when
					// the widget is realized. This is done by the Bin wrapper.
				}
			}
			
			if (tooltip != null && tooltip.Length > 0)
				GetTopLevel().GenerateTooltip (ctx, this);
			
			base.GenerateBuildCode (ctx, var);
		}
		
		internal protected override void GeneratePostBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			base.GeneratePostBuildCode (ctx, var);
			
			// The visible property is generated here to ensure that widgets are made visible
			// after they have been fully built
			
			PropertyDescriptor prop = ClassDescriptor ["Visible"] as PropertyDescriptor;
			if (prop != null && prop.PropertyType == typeof(bool) && !(bool) prop.GetValue (Wrapped)) {
				ctx.Statements.Add (
					new CodeMethodInvokeExpression (
						var, 
						"Hide"
					)
				);
			}
			
			// The HasDefault property can only be assigned when the widget is added to the window
			prop = ClassDescriptor ["HasDefault"] as PropertyDescriptor;
			if (prop != null && (bool) prop.GetValue (Wrapped)) {
				ctx.Statements.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (
							var,
							"HasDefault"
						),
						new CodePrimitiveExpression (true)
					)
				);
			}
		}
		
		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			// Those properties are handled in GeneratePostBuildCode
			if (prop.Name == "Visible" || prop.Name == "HasDefault")
				return;
			
			// Don't generate a name for unselectable widgets
			if (prop.Name == "Name" && Unselectable)
				return;
			
			base.GeneratePropertySet (ctx, var, prop);
		}
		
		protected CodeExpression GenerateUiManagerElement (GeneratorContext ctx, ActionTree tree)
		{
			Widget topLevel = GetTopLevel ();
			string uiName = topLevel.UIManagerName;
			if (uiName != null) {
				CodeVariableReferenceExpression uiManager = new CodeVariableReferenceExpression (uiName);
				if (topLevel.includedActionGroups == null)
					topLevel.includedActionGroups = new ArrayList ();
				
				// Add to the uimanager all action groups required by the 
				// actions of the tree
				
				foreach (ActionGroup grp in tree.GetRequiredGroups ()) {
					if (!topLevel.includedActionGroups.Contains (grp)) {
						// Insert the action group in the UIManager
						CodeMethodInvokeExpression mi = new CodeMethodInvokeExpression (
							uiManager,
							"InsertActionGroup",
							ctx.GenerateValue (grp, typeof(ActionGroup)),
							new CodePrimitiveExpression (topLevel.includedActionGroups.Count)
						);
						ctx.Statements.Add (mi);
						topLevel.includedActionGroups.Add (grp);
					}
				}
				
				tree.GenerateBuildCode (ctx, uiManager);
				return new CodeMethodInvokeExpression (
					uiManager,
					"GetWidget",
					new CodePrimitiveExpression ("/" + Wrapped.Name)
				);
			}
			return null;
		}

		public static new Widget Lookup (GLib.Object obj)
		{
			return Stetic.ObjectWrapper.Lookup (obj) as Stetic.Wrapper.Widget;
		}

		PropertyDescriptor internalChildProperty;
		public PropertyDescriptor InternalChildProperty {
			get {
				return internalChildProperty;
			}
			set {
				internalChildProperty = value;
			}
		}

		public virtual void Drop (Gtk.Widget widget, object faultId)
		{
			widget.Destroy ();
		}

		public virtual bool HExpandable { get { return hexpandable; } }
		public virtual bool VExpandable { get { return vexpandable; } }

		public bool Visible {
			get {
				return window_visible;
			}
			set {
				window_visible = value;
				EmitNotify ("Visible");
			}
		}

		public bool HasDefault {
			get {
				return hasDefault;
			}
			set {
				hasDefault = value;
				EmitNotify ("HasDefault");
				if (hasDefault && !CanDefault)
					CanDefault = true;
			}
		}

		public bool CanDefault {
			get {
				return canDefault;
			}
			set {
				canDefault = value;
				EmitNotify ("CanDefault");
				if (!canDefault && HasDefault)
					HasDefault = false;
			}
		}

		public bool Sensitive {
			get {
				return Wrapped.Sensitive;
			}
			set {
				if (Wrapped.Sensitive == value)
					return;

				Wrapped.Sensitive = value;
				if (Wrapped.Sensitive)
					InsensitiveManager.Remove (this);
				else
					InsensitiveManager.Add (this);
				EmitNotify ("Sensitive");
			}
		}

		public Gdk.EventMask Events {
			get {
				return events;
			}
			set {
				events = value;
				EmitNotify ("Events");
			}
		}

		public bool CanFocus {
			get {
				return canFocus;
			}
			set {
				canFocus = value;
				EmitNotify ("CanFocus");
			}
		}

		public string Tooltip {
			get {
				return tooltip;
			}
			set {
				tooltip = value;
			}
		}
		
		public bool ShowScrollbars {
			get {
				return boundToScrollWindow;
			}
			set {
				if (boundToScrollWindow != value) {
					boundToScrollWindow = value;
					UpdateScrolledWindow ();
					EmitNotify ("ShowScrollbars");
				}
			}
		}
		
		internal void UpdateScrolledWindow ()
		{
			if (ParentWrapper == null)
				return;
			if (boundToScrollWindow) {
				if (!(Wrapped.Parent is Gtk.Viewport) && !(Wrapped.Parent is Gtk.ScrolledWindow)) {
					Gtk.ScrolledWindow scw = new Gtk.ScrolledWindow ();
					scw.HscrollbarPolicy = scw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
					scw.ShadowType = Gtk.ShadowType.In;
					ScrolledWindow wrapper = (ScrolledWindow) ObjectWrapper.Create (Project, scw);
					ParentWrapper.ReplaceChild (Wrapped, scw, false);
					if (Wrapped.SetScrollAdjustments (null, null))
						scw.Add (Wrapped);
					else
						wrapper.AddWithViewport (Wrapped);
					Select ();
				}
			}
			else if (((Wrapped.Parent is Gtk.Viewport) || (Wrapped.Parent is Gtk.ScrolledWindow)) && ParentWrapper.ParentWrapper != null) {
				Gtk.Container parent = (Gtk.Container) Wrapped.Parent;
				parent.Remove (Wrapped);
				Container grandParent;
				if (parent is Gtk.Viewport) {
					parent = (Gtk.Container) parent.Parent;
					grandParent = Container.LookupParent (parent);
				}
				else
					grandParent = Container.LookupParent (parent);
				grandParent.ReplaceChild (parent, Wrapped, true);
			}
		}
		
		public bool InWindow {
			get {
				return this.GetTopLevel ().Wrapped is Gtk.Window;
			}
		}
		
		public bool IsScrollable {
			get {
				return !IsTopLevel && !(Wrapped is Gtk.ScrolledWindow);
			}
		}

		public override string ToString ()
		{
			if (Wrapped.Name != null)
				return "[" + Wrapped.GetType ().Name + " '" + Wrapped.Name + "' " + Wrapped.GetHashCode ().ToString () + "]";
			else
				return "[" + Wrapped.GetType ().Name + " " + Wrapped.GetHashCode ().ToString () + "]";
		}
		
		public IDesignArea GetDesignArea ()
		{
			return GetDesignArea (Wrapped);
		}
		
		protected IDesignArea GetDesignArea (Gtk.Widget w)
		{
			while (w != null && !(w is IDesignArea))
				w = w.Parent;
			return w as IDesignArea;
		}
		
		protected override void EmitNotify (string propertyName)
		{
			// Don't notify parent change for top level widgets.
			if (propertyName == "parent" || propertyName == "has-focus" || 
				propertyName == "has-toplevel-focus" || propertyName == "is-active" ||
				propertyName == "is-focus" || propertyName == "style" || 
				propertyName == "Visible" || propertyName == "scroll-offset")
				return;
			
			if (propertyName == "Name") {
				if (Wrapped.Name != oldName) {
					if (ParentWrapper != null) {
						string nn = ParentWrapper.GetValidWidgetName (Wrapped);
						if (nn != Wrapped.Name) {
							// The name was not valid, so it has to be changed again.
							// Don't fire the changed event now, will be fired after the following change
							Wrapped.Name = nn;
							return;
						}
					}
					
					// This fires the changed event
					base.EmitNotify (propertyName);
						
					string on = oldName;
					oldName = Wrapped.Name;
					OnNameChanged (new WidgetNameChangedArgs (this, on, Wrapped.Name));
					
					// Keep the member name in sync with the widget name
					if (on == MemberName)
						MemberName = Wrapped.Name;
				}
			}
			else if (propertyName == "MemberName") {
				if (MemberName != oldMemberName) {
					base.EmitNotify (propertyName);
					string on = oldMemberName;
					oldMemberName = MemberName;
					OnMemberNameChanged (new WidgetNameChangedArgs (this, on, MemberName));
				}
			}
			else {
//				Console.WriteLine ("PROP: " + propertyName);
				base.EmitNotify (propertyName);
			}
		}
		
		protected virtual void OnNameChanged (WidgetNameChangedArgs args)
		{
			if (Project != null)
				Project.NotifyNameChanged (args);
			if (NameChanged != null)
				NameChanged (this, args);
		}
		
		protected virtual void OnMemberNameChanged (WidgetNameChangedArgs args)
		{
			if (MemberNameChanged != null)
				MemberNameChanged (this, args);
		}
		
		bool IEditableObject.CanCopy {
			get { return ClipboardCanCopy; }
		}
		
		bool IEditableObject.CanCut {
			get { return ClipboardCanCut; }
		}
		
		bool IEditableObject.CanPaste {
			get { return ClipboardCanPaste; }
		}
		
		bool IEditableObject.CanDelete {
			get { return CanDelete; }
		}
		
		void IEditableObject.Copy ()
		{
			ClipboardCopy ();
		}
		
		void IEditableObject.Cut ()
		{
			ClipboardCut ();
		}
		
		void IEditableObject.Paste ()
		{
			ClipboardPaste ();
		}
		
		void IEditableObject.Delete ()
		{
			Delete ();
		}
		
		protected virtual bool ClipboardCanCopy {
			get { return !IsTopLevel; }
		}
		
		protected virtual bool ClipboardCanCut {
			get { return InternalChildProperty == null && !IsTopLevel; }
		}
		
		protected virtual bool ClipboardCanPaste {
			get { return false; }
		}
		
		protected virtual bool CanDelete {
			get { return ClipboardCanCut; }
		}
		
		protected virtual void ClipboardCopy ()
		{
			Clipboard.Copy (Wrapped);
		}
		
		protected virtual void ClipboardCut ()
		{
			Clipboard.Cut (Wrapped);
		}
		
		protected virtual void ClipboardPaste ()
		{
		}
	}

	internal static class InsensitiveManager {

		static Gtk.Invisible invis;
		static Hashtable map;

		static InsensitiveManager ()
		{
			map = new Hashtable ();
			invis = new Gtk.Invisible ();
			invis.ButtonPressEvent += ButtonPress;
		}

		static void ButtonPress (object obj, Gtk.ButtonPressEventArgs args)
		{
			Gtk.Widget widget = (Gtk.Widget)map[args.Event.Window];
			if (widget == null)
				return;

			Widget wrapper = Widget.Lookup (widget);
			args.RetVal = wrapper.HandleClick (args.Event);
		}

		public static void Add (Widget wrapper)
		{
			Gtk.Widget widget = wrapper.Wrapped;

			widget.SizeAllocated += Insensitive_SizeAllocate;
			widget.Realized += Insensitive_Realized;
			widget.Unrealized += Insensitive_Unrealized;
			widget.Mapped += Insensitive_Mapped;
			widget.Unmapped += Insensitive_Unmapped;

			if (widget.IsRealized)
				Insensitive_Realized (widget, EventArgs.Empty);
			if (widget.IsMapped)
				Insensitive_Mapped (widget, EventArgs.Empty);
		}

		public static void Remove (Widget wrapper)
		{
			Gtk.Widget widget = wrapper.Wrapped;
			Gdk.Window win = (Gdk.Window)map[widget];
			if (win != null) {
				map.Remove (widget);
				map.Remove (win);
				win.Destroy ();
			}
			widget.SizeAllocated -= Insensitive_SizeAllocate;
			widget.Realized -= Insensitive_Realized;
			widget.Unrealized -= Insensitive_Unrealized;
			widget.Mapped -= Insensitive_Mapped;
			widget.Unmapped -= Insensitive_Unmapped;
		}

		static void Insensitive_SizeAllocate (object obj, Gtk.SizeAllocatedArgs args)
		{
			Gdk.Window win = (Gdk.Window)map[obj];
			if (win != null)
				win.MoveResize (args.Allocation);
		}

		static void Insensitive_Realized (object obj, EventArgs args)
		{
			Gtk.Widget widget = (Gtk.Widget)obj;

			Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.Wclass = Gdk.WindowClass.InputOnly;
			attributes.Mask = Gdk.EventMask.ButtonPressMask;

			Gdk.Window win = new Gdk.Window (widget.GdkWindow, attributes, 0);
			win.UserData = invis.Handle;
			win.MoveResize (widget.Allocation);

			map[widget] = win;
			map[win] = widget;
		}

		static void Insensitive_Mapped (object obj, EventArgs args)
		{
			Gdk.Window win = (Gdk.Window)map[obj];
			win.Show ();
		}

		static void Insensitive_Unmapped (object obj, EventArgs args)
		{
			Gdk.Window win = (Gdk.Window)map[obj];
			win.Hide ();
		}

		static void Insensitive_Unrealized (object obj, EventArgs args)
		{
			Gdk.Window win = (Gdk.Window)map[obj];
			win.Destroy ();
			map.Remove (obj);
			map.Remove (win);
		}
	}
}
