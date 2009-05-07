using System;
using System.CodeDom;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using Stetic.Undo;
using Stetic.Editor;

namespace Stetic.Wrapper
{
	public class Container : Widget
	{
		int designWidth;
		int designHeight;
		IDesignArea designer;
		CodeExpression generatedTooltips;
		bool internalAdd;
		
		static DiffGenerator containerDiffGenerator;
		static bool showNonContainerWarning = true;

		static Container ()
		{
			XmlDiffAdaptor adaptor = new XmlDiffAdaptor ();
			adaptor.ChildElementName = "child";
			adaptor.ChildAdaptor = new XmlDiffAdaptor ();
			adaptor.ChildAdaptor.PropsElementName = "packing";
			
			containerDiffGenerator = new DiffGenerator ();
			containerDiffGenerator.CurrentStatusAdaptor = adaptor;
			containerDiffGenerator.NewStatusAdaptor = adaptor;
		}
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);

			ClassDescriptor klass = this.ClassDescriptor;
			foreach (PropertyDescriptor prop in klass.InternalChildren) {
				Gtk.Widget child = prop.GetValue (container) as Gtk.Widget;
				if (child == null)
					continue;
				Widget wrapper = ObjectWrapper.Create (proj, child) as Stetic.Wrapper.Widget;
				wrapper.InternalChildProperty = prop;
				if (child.Name == ((GLib.GType)child.GetType ()).ToString ())
					child.Name = container.Name + "_" + prop.Name;
			}

			container.Removed += ChildRemoved;
			container.Added += OnChildAdded;
			
			if (!initialized && container.Children.Length == 0 && AllowPlaceholders)
				AddPlaceholder ();

			if (Wrapped.GetType ().ToString ()[0] == 'H')
				ContainerOrientation = Gtk.Orientation.Horizontal;
			else
				ContainerOrientation = Gtk.Orientation.Vertical;
			
			if (!Loading)
				ValidateChildNames (Wrapped);
		}

		public override void Dispose ()
		{
			container.Removed -= ChildRemoved;
			container.Added -= OnChildAdded;
			AutoSize.Clear ();
			base.Dispose ();
		}
		
		void OnChildAdded (object o, Gtk.AddedArgs args)
		{
			if (!internalAdd)
				HandleNewChild (args.Widget);
		}
		
		protected void NotifyChildAdded (Gtk.Widget child)
		{
			HandleNewChild (child);
			EmitContentsChanged ();
		}
		
		void HandleNewChild (Gtk.Widget child)
		{
			// Make sure children's IDs don't conflict with other widgets
			// in the parent container.
			if (!Loading)
				ValidateChildNames (Wrapped);

			Widget w = Widget.Lookup (child);
			if (w != null) {
				w.RequiresUndoStatusUpdate = true;
				if (designer != null)
					w.OnDesignerAttach (designer);
				
				// If the ShowScrollbars flag is set, make sure the scrolled window is created.
				if (w.ShowScrollbars)
					w.UpdateScrolledWindow ();
			}
			
			Placeholder ph = child as Placeholder;
			if (ph != null) {
				ph.DragDrop += PlaceholderDragDrop;
				ph.DragDataReceived += PlaceholderDragDataReceived;
				ph.ButtonPressEvent += PlaceholderButtonPress;
				AutoSize[ph] = true;
			}
		}
		
		Gtk.Container container {
			get {
				return (Gtk.Container)Wrapped;
			}
		}

		protected virtual bool AllowPlaceholders {
			get {
				return true && this.ClassDescriptor.AllowChildren;
			}
		}
		
		public int DesignWidth {
			get { return designWidth; }
			set { designWidth = value; NotifyChanged (); }
		}

		public int DesignHeight {
			get { return designHeight; }
			set { designHeight = value; NotifyChanged (); }
		}
		
		public void IncreaseBorderWidth () 
		{
			container.BorderWidth += 3;
		}

		public void DecreaseBorderWidth () 
		{
			if (container.BorderWidth >= 3)
				container.BorderWidth -= 3;
			else
				container.BorderWidth = 0;
		}
		
		internal bool ChildrenAllowed ()
		{
			return this.ClassDescriptor.AllowChildren;
		}
		
		int freeze;
		protected void Freeze ()
		{
			freeze++;
		}

		protected void Thaw ()
		{
			if (--freeze == 0)
				Sync ();
		}

		protected virtual void DoSync ()
		{
			;
		}

		protected void Sync ()
		{
			if (freeze > 0 || Loading)
				return;
			freeze = 1;
			DoSync ();
			freeze = 0;
		}
		
		public override object GetUndoDiff ()
		{
			XmlElement oldElem = UndoManager.GetObjectStatus (this);

//			Console.WriteLine ("UNDO status: ");
//			Console.WriteLine (oldElem.OuterXml);
			
			// Write the new status of the object. This is going to replace the old status in undoManager.
			// In the process, register new objects found.
			
			UndoWriter writer = new UndoWriter (oldElem.OwnerDocument, UndoManager);
			XmlElement newElem = Write (writer);
			
//			Console.WriteLine ("CURRENT status: ");
//			Console.WriteLine (newElem.OuterXml);
			
			// Get the changes since the last undo checkpoint
			
			ObjectDiff actionsDiff = null;
			ObjectDiff objectDiff = containerDiffGenerator.GetDiff (newElem, oldElem);
			
			// If there are child changes there is no need to look for changes in the
			// actions, since the whole widget will be read again
			
			if (IsTopLevel && (objectDiff == null || objectDiff.ChildChanges == null))
				actionsDiff = LocalActionGroups.GetDiff (Project, oldElem);
			
			// The undo writer skips children which are already registered in the undo manager
			// to avoid writing information we already have. Now it's the moment to fill the gaps
			
			foreach (XmlElement newChild in newElem.SelectNodes ("child[widget/@unchanged_marker='yes']")) {
				string cid = newChild.GetAttribute ("undoId");
				XmlElement oldChild = (XmlElement) oldElem.SelectSingleNode ("child[@undoId='" + cid + "']");
				if (oldChild == null)
					throw new InvalidOperationException ("Child not found when filling widget info gaps.");
					
				XmlElement oldWidgetChild = oldChild ["widget"];
				XmlElement newWidgetChild = newChild ["widget"];
				
				oldChild.RemoveChild (oldWidgetChild);
				if (newWidgetChild != null)
					newChild.ReplaceChild (oldWidgetChild, newWidgetChild);
			}

			// Update the status tree
			
			UndoManager.UpdateObjectStatus (this, newElem);
			
//			UndoManager.Dump ();
			
			if (objectDiff != null || actionsDiff != null)
				return new ObjectDiff[] { objectDiff, actionsDiff };
			else
				return null;
		}
		
		public override object ApplyUndoRedoDiff (object data)
		{
			ObjectDiff diff = ((ObjectDiff[]) data)[0];
			ObjectDiff actionsDiff = ((ObjectDiff[]) data)[1];
			
			ObjectDiff reverseDiff = null;
			ObjectDiff reverseActionsDiff = null;
			
			XmlElement status = UndoManager.GetObjectStatus (this);
			XmlElement oldStatus = (XmlElement) status.CloneNode (true);
			UndoReader reader = new UndoReader (Project, FileFormat.Native, UndoManager);
			
			// Only apply the actions diff if the widget has not been completely reloaded
			if (actionsDiff != null && !(diff != null && diff.ChildChanges != null)) {
				// Apply the patch
				LocalActionGroups.ApplyDiff (Project, actionsDiff);
				
				// Get the redo patch
				reverseActionsDiff = LocalActionGroups.GetDiff (Project, oldStatus);
				
				// Update the status of the action group list in the undo status tree.
				// It has to remove all action groups and then write them again 
				foreach (XmlElement group in status.SelectNodes ("action-group"))
					status.RemoveChild (group);

				UndoWriter writer = new UndoWriter (status.OwnerDocument, UndoManager);
				foreach (ActionGroup actionGroup in LocalActionGroups)
					status.InsertBefore (actionGroup.Write (writer), status.FirstChild);
			}
			
			if (diff != null) {
				containerDiffGenerator.ApplyDiff (status, diff);
				reverseDiff = containerDiffGenerator.GetDiff (status, oldStatus);
			
				// Avoid reading the whole widget tree if only the properties have changed.
				if (diff.ChildChanges == null) {
					ReadProperties (reader, status);
				} else {
//					Console.WriteLine ("BEFORE PATCH: " + status.OuterXml);
					Read (reader, status);
//					Console.WriteLine ("\nAFTER PATCH:");
//					UndoManager.Dump ();
					EmitContentsChanged ();
				}
			}
			
			if (reverseDiff != null || reverseActionsDiff != null)
				return new ObjectDiff[] { reverseDiff, reverseActionsDiff };
			else
				return null;
		}
		
		public override void Read (ObjectReader reader, XmlElement elem)
		{
			// Remove all existing children
			if (ClassDescriptor.AllowChildren && Wrapped != null) {
				foreach (Gtk.Widget child in GladeChildren) {
					Widget wrapper = Widget.Lookup (child);
					
					if (wrapper != null) {
						if (wrapper.InternalChildProperty != null)
							continue;
						container.Remove (child);
						child.Destroy ();
					} else if (child is Stetic.Placeholder) {
						container.Remove (child);
						child.Destroy ();
					}
				}
			}
			
			ReadActionGroups (reader, elem);
			ReadProperties (reader, elem);
			ReadChildren (reader, elem);
			DoSync ();
		}
		
		protected virtual void ReadChildren (ObjectReader reader, XmlElement elem)
		{
			int gladeChildStackPos = reader.GladeChildStack.Count;
			
			foreach (XmlElement child_elem in elem.SelectNodes ("./child")) {
				try {
					if (child_elem.HasAttribute ("internal-child"))
						ReadInternalChild (reader, child_elem);
					else if (child_elem["widget"] == null)
						ReadPlaceholder (reader, child_elem);
					else {
						ObjectWrapper cw = ReadChild (reader, child_elem);

						// Set a temporary id used for the undo/redo operations
						ObjectWrapper ccw = ChildWrapper ((Widget)cw);
						if (ccw != null) {
							string cid = child_elem.GetAttribute ("undoId");
							if (cid.Length > 0)
								ChildWrapper ((Widget)cw).UndoId = cid;
							else
								child_elem.SetAttribute ("undoId", ChildWrapper ((Widget)cw).UndoId);
						}
					}
				} catch (GladeException ge) {
					Console.Error.WriteLine (ge.Message);
				}
			}
			
			if (reader.Format == FileFormat.Glade) {
				for (int n = reader.GladeChildStack.Count - 1; n >= gladeChildStackPos; n--) {
					ObjectWrapper ob = ReadInternalChild (reader, (XmlElement) reader.GladeChildStack [n]);
					if (ob != null)
						reader.GladeChildStack.RemoveAt (n);
				}
			}

			string ds = elem.GetAttribute ("design-size");
			if (ds.Length > 0) {
				int i = ds.IndexOf (' ');
				DesignWidth = int.Parse (ds.Substring (0, i));
				DesignHeight = int.Parse (ds.Substring (i+1));
			}
			
			Sync ();
		}

		protected virtual ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			ObjectWrapper wrapper = reader.ReadObject (child_elem["widget"]);
			Container.ContainerChild childwrapper = null;
			
			try {
				wrapper.Loading = true;

				Gtk.Widget child = (Gtk.Widget)wrapper.Wrapped;

				AutoSize[child] = false;
				container.Add (child);
				
				childwrapper = ChildWrapper ((Widget)wrapper);
				if (childwrapper != null)
					childwrapper.Loading = true;
				
				if (reader.Format == FileFormat.Glade)
					GladeUtils.SetPacking (childwrapper, child_elem);
				else
					WidgetUtils.SetPacking (childwrapper, child_elem);
				return wrapper;
			} finally {
				wrapper.Loading = false;
				if (childwrapper != null)
					childwrapper.Loading = false;
			}
		}
		
		void ReadPlaceholder (ObjectReader reader, XmlElement child_elem)
		{
			Placeholder ph = AddPlaceholder ();
			if (ph != null) {
				string cid = child_elem.GetAttribute ("undoId");
				if (cid.Length > 0)
					ph.UndoId = cid;
				else
					child_elem.SetAttribute ("undoId", ph.UndoId);
			}
		}

		protected virtual ObjectWrapper ReadInternalChild (ObjectReader reader, XmlElement child_elem)
		{
			ClassDescriptor klass = base.ClassDescriptor;
			string childId = child_elem.GetAttribute ("internal-child");
			
			foreach (PropertyDescriptor prop in klass.InternalChildren) {
				if (reader.Format == FileFormat.Glade && ((TypedPropertyDescriptor)prop).GladeName != childId)
					continue;
				else if (reader.Format == FileFormat.Native && prop.Name != childId)
					continue;
				
				Gtk.Widget child = prop.GetValue (container) as Gtk.Widget;
				Widget wrapper = Widget.Lookup (child);
				if (wrapper != null) {
					reader.ReadObject (wrapper, child_elem["widget"]);
					if (reader.Format == FileFormat.Glade)
						GladeUtils.SetPacking (ChildWrapper (wrapper), child_elem);
					else
						WidgetUtils.SetPacking (ChildWrapper (wrapper), child_elem);
					return wrapper;
				}
			}

			// In Glade, internal children may not be direct children of the root container. This is handled in a special way.
			if (reader.Format == FileFormat.Glade) {
				if (!reader.GladeChildStack.Contains (child_elem))
					reader.GladeChildStack.Add (child_elem);
				return null;
			}
			else
				throw new GladeException ("Unrecognized internal child name", Wrapped.GetType ().FullName, false, "internal-child", childId);
		}

		public override XmlElement Write (ObjectWriter writer)
		{
			XmlElement elem = WriteProperties (writer);
			WriteActionGroups (writer, elem);
			XmlElement child_elem;
			
			if (ClassDescriptor.AllowChildren) {
				foreach (Gtk.Widget child in GladeChildren) {
					Widget wrapper = Widget.Lookup (child);
					
					if (wrapper != null) {
						// Iternal children are written later
						if (wrapper.InternalChildProperty != null)
							continue;
						child_elem = WriteChild (writer, wrapper);
						if (child_elem != null)
							elem.AppendChild (child_elem);
					} else if (child is Stetic.Placeholder) {
						child_elem = writer.XmlDocument.CreateElement ("child");
						if (writer.CreateUndoInfo)
							child_elem.SetAttribute ("undoId", ((Stetic.Placeholder)child).UndoId);
						child_elem.AppendChild (writer.XmlDocument.CreateElement ("placeholder"));
						elem.AppendChild (child_elem);
					}
				}
			}
			
			foreach (PropertyDescriptor prop in this.ClassDescriptor.InternalChildren) {
				Gtk.Widget child = prop.GetValue (Wrapped) as Gtk.Widget;
				if (child == null)
					continue;

				child_elem = writer.XmlDocument.CreateElement ("child");
				Widget wrapper = Widget.Lookup (child);
				if (wrapper == null) {
					child_elem.AppendChild (writer.XmlDocument.CreateElement ("placeholder"));
					elem.AppendChild (child_elem);
					continue;
				}
				
				string cid = writer.Format == FileFormat.Glade ? prop.InternalChildId : prop.Name;
				
				XmlElement widget_elem = writer.WriteObject (wrapper);
				child_elem.SetAttribute ("internal-child", cid);
				// Sets the child Id to be used in undo/redo operations
				if (writer.CreateUndoInfo)
					child_elem.SetAttribute ("undoId", cid);
				
				child_elem.AppendChild (widget_elem);
				elem.AppendChild (child_elem);
			}

			if (DesignWidth != 0 || DesignHeight != 0)
				elem.SetAttribute ("design-size", DesignWidth + " " + DesignHeight);
				
			return elem;
		}

		protected virtual XmlElement WriteChild (ObjectWriter writer, Widget wrapper)
		{
			XmlElement child_elem = writer.XmlDocument.CreateElement ("child");
			XmlElement widget_elem = writer.WriteObject (wrapper);
			child_elem.AppendChild (widget_elem);

			Container.ContainerChild childwrapper = ChildWrapper (wrapper);
			if (childwrapper != null) {
				XmlElement packing_elem;
				
				if (writer.Format == FileFormat.Glade)
					packing_elem = GladeUtils.CreatePacking (writer.XmlDocument, childwrapper);
				else
					packing_elem = WidgetUtils.CreatePacking (writer.XmlDocument, childwrapper);
				
				// Sets the child Id to be used in undo/redo operations
				if (writer.CreateUndoInfo)
					child_elem.SetAttribute ("undoId", childwrapper.UndoId);

				if (packing_elem.HasChildNodes)
					child_elem.AppendChild (packing_elem);
			} else {
				// There is no container child, so make up an id.
				if (writer.CreateUndoInfo)
					child_elem.SetAttribute ("undoId", "0");
			}

			return child_elem;
		}
		
		public XmlElement WriteContainerChild (ObjectWriter writer, Widget wrapper)
		{
			return WriteChild (writer, wrapper);
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			generatedTooltips = null;
			
			base.GenerateBuildCode (ctx, var);
			
			if (ClassDescriptor.AllowChildren) {
				foreach (Gtk.Widget child in GladeChildren) {
					Widget wrapper = Widget.Lookup (child);
					
					if (wrapper != null && wrapper.InternalChildProperty == null)
						// Iternal children are written later
						GenerateChildBuildCode (ctx, var, wrapper);
				}
			}
			
			foreach (TypedPropertyDescriptor prop in this.ClassDescriptor.InternalChildren) {
				GenerateSetInternalChild (ctx, var, prop);
			}
			
			
			if (IsTopLevel && Wrapped is Gtk.Bin) {
				CodeExpression childExp = new CodePropertyReferenceExpression (var, "Child");
				CodeConditionStatement cond = new CodeConditionStatement ();
				cond.Condition = 
					new CodeBinaryOperatorExpression (
						childExp,
						CodeBinaryOperatorType.IdentityInequality,
						new CodePrimitiveExpression (null)
					);
				cond.TrueStatements.Add (
					new CodeMethodInvokeExpression (
						childExp,
						"ShowAll"
					)
				);
				ctx.Statements.Add (cond);
			}
		}
		
		protected virtual void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentVar, Widget wrapper)
		{
			ObjectWrapper childwrapper = ChildWrapper (wrapper);
			if (childwrapper != null) {
				ctx.Statements.Add (new CodeCommentStatement ("Container child " + Wrapped.Name + "." + childwrapper.Wrapped.GetType ()));
				CodeExpression var = ctx.GenerateNewInstanceCode (wrapper);
				CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression (
					parentVar,
					"Add",
					var
				);
				ctx.Statements.Add (invoke);

				GenerateSetPacking (ctx, parentVar, var, childwrapper);
			}
		}
		
		void GenerateSetInternalChild (GeneratorContext ctx, CodeExpression parentVar, TypedPropertyDescriptor prop)
		{
			Gtk.Widget child = prop.GetValue (container) as Gtk.Widget;
			Widget cwrapper = Widget.Lookup (child);
			if (cwrapper != null) {
				ctx.Statements.Add (new CodeCommentStatement ("Internal child " + Wrapped.Name + "." + prop.Name));
				string childVar = ctx.NewId ();
				CodeVariableDeclarationStatement varDec = new CodeVariableDeclarationStatement (child.GetType(), childVar);
				ctx.Statements.Add (varDec);
				varDec.InitExpression = new CodePropertyReferenceExpression (parentVar, prop.Name);
			
				ctx.GenerateBuildCode (cwrapper, new CodeVariableReferenceExpression (childVar));
				return;
			}
		}
		
		protected void GenerateSetPacking (GeneratorContext ctx, CodeExpression parentVar, CodeExpression childVar, ObjectWrapper containerChildWrapper)
		{
			Gtk.Container.ContainerChild cc = containerChildWrapper.Wrapped as Gtk.Container.ContainerChild;
			ClassDescriptor klass = containerChildWrapper.ClassDescriptor;
			
			// Generate a variable that holds the container child
			
			string contChildVar = ctx.NewId ();
			CodeVariableDeclarationStatement varDec = new CodeVariableDeclarationStatement (cc.GetType(), contChildVar);
			varDec.InitExpression = new CodeCastExpression ( 
				cc.GetType (),
				new CodeIndexerExpression (parentVar, childVar)
			);
			
			CodeVariableReferenceExpression var = new CodeVariableReferenceExpression (contChildVar);
			
			// Set the container child properties

			ctx.Statements.Add (varDec);
			int count = ctx.Statements.Count;
			
			foreach (ItemGroup group in klass.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					PropertyDescriptor prop = item as PropertyDescriptor;
					if (prop == null || !prop.IsRuntimeProperty)
						continue;
					GenerateChildPropertySet (ctx, var, klass, prop, cc);
				}
			}
			
			if (ctx.Statements.Count == count) {
				ctx.Statements.Remove (varDec);
			}
		}
		
		protected virtual void GenerateChildPropertySet (GeneratorContext ctx, CodeVariableReferenceExpression var, ClassDescriptor containerChildClass, PropertyDescriptor prop, object child)
		{
			if (containerChildClass.InitializationProperties != null && Array.IndexOf (containerChildClass.InitializationProperties, prop) != -1)
				return;
			
			// Design time
			if (prop.Name == "AutoSize")
				return;
				
			object oval = prop.GetValue (child);
			if (oval == null || (prop.HasDefault && prop.IsDefaultValue (oval)))
				return;
				
			CodePropertyReferenceExpression cprop = new CodePropertyReferenceExpression (var, prop.Name);
			CodeExpression val = ctx.GenerateValue (oval, prop.RuntimePropertyType, prop.Translatable);
			ctx.Statements.Add (new CodeAssignStatement (cprop, val));
		}
		
		internal protected override void GeneratePostBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			base.GeneratePostBuildCode (ctx, var);
			
			if (IsTopLevel && (Wrapped is Gtk.Bin) && Visible) {
				ctx.Statements.Add (
					new CodeMethodInvokeExpression (
						var,
						"Show"
					)
				);
			}
		}
		
		internal void GenerateTooltip (GeneratorContext ctx, Widget widget)
		{
			if (WidgetUtils.CompareVersions (Project.TargetGtkVersion, "2.12") <= 0) {
				ctx.Statements.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (ctx.WidgetMap.GetWidgetExp (widget), "TooltipMarkup"),
						new CodePrimitiveExpression (widget.Tooltip)
					)
				);
				return;
			}
 
			if (generatedTooltips == null) {
				string tid = ctx.NewId ();
				Type t = typeof(Gtk.Widget).Assembly.GetType ("Gtk.Tooltips");
				CodeVariableDeclarationStatement vardec = new CodeVariableDeclarationStatement (
					t, tid, new CodeObjectCreateExpression (t)
				);
				ctx.Statements.Add (vardec);
				generatedTooltips = new CodeVariableReferenceExpression (tid);
			}
			ctx.Statements.Add (
				new CodeMethodInvokeExpression (
					generatedTooltips,
					"SetTip",
					ctx.WidgetMap.GetWidgetExp (widget),
					new CodePrimitiveExpression (widget.Tooltip),
					new CodePrimitiveExpression (widget.Tooltip)
				)
			);
		}
		
		internal protected override void OnDesignerAttach (IDesignArea designer)
		{
			base.OnDesignerAttach (designer);
			this.designer = designer;
			foreach (Gtk.Widget w in RealChildren) {
				ObjectWrapper wr = ObjectWrapper.Lookup (w);
				if (wr != null)
					wr.OnDesignerAttach (designer);
			}
		}
		
		internal protected override void OnDesignerDetach (IDesignArea designer)
		{
			base.OnDesignerDetach (designer);
			foreach (Gtk.Widget w in RealChildren) {
				ObjectWrapper wr = ObjectWrapper.Lookup (w);
				if (wr != null)
					wr.OnDesignerDetach (designer);
			}
			this.designer = null;
		}
		
		public virtual Placeholder AddPlaceholder ()
		{
			Placeholder ph = CreatePlaceholder ();
			container.Add (ph);
			return ph;
		}

		public virtual void Add (Gtk.Widget child)
		{
			container.Add (child);
		}

		public static new Container Lookup (GLib.Object obj)
		{
			return Stetic.ObjectWrapper.Lookup (obj) as Stetic.Wrapper.Container;
		}

		public static Container LookupParent (Gtk.Widget widget)
		{
			if (widget == null)
				return null;
			Gtk.Widget parent = widget.Parent;
			Container wrapper = null;
			while ((wrapper == null || wrapper.Unselectable) && parent != null) {
				wrapper = Lookup (parent);
				parent = parent.Parent;
			}
			return wrapper;
		}

		public static Stetic.Wrapper.Container.ContainerChild ChildWrapper (Stetic.Wrapper.Widget wrapper) {
			Stetic.Wrapper.Container parentWrapper = wrapper.ParentWrapper;
			if (parentWrapper == null)
				return null;

			Gtk.Container parent = parentWrapper.Wrapped as Gtk.Container;
			if (parent == null)
				return null;

			Gtk.Widget child = (Gtk.Widget)wrapper.Wrapped;
			while (child != null && child.Parent != parent)
				child = child.Parent;
			if (child == null)
				return null;

			Gtk.Container.ContainerChild cc = parent[child];
			Container.ContainerChild cwrap = ObjectWrapper.Lookup (cc) as Container.ContainerChild;
			if (cwrap != null)
				return cwrap;
			else
				return Stetic.ObjectWrapper.Create (parentWrapper.proj, cc) as ContainerChild;
		}

		protected Gtk.Container.ContainerChild ContextChildProps (Gtk.Widget context)
		{
			if (context == container)
				return null;

			do {
				if (context.Parent == container)
					return container[context];
				context = context.Parent;
			} while (context != null);

			return null;
		}

		public delegate void ContentsChangedHandler (Container container);
		public event ContentsChangedHandler ContentsChanged;

		protected void EmitContentsChanged ()
		{
			if (Loading)
				return;
			if (ContentsChanged != null)
				ContentsChanged (this);
			if (ParentWrapper != null)
				ParentWrapper.ChildContentsChanged (this);
			if (Project != null)
				Project.NotifyWidgetContentsChanged (this);
			NotifyChanged ();
		}

		protected Set AutoSize = new Set ();

		protected virtual Placeholder CreatePlaceholder ()
		{
			Placeholder ph = new Placeholder ();
			ph.Show ();
			return ph;
		}

		void PlaceholderButtonPress (object obj, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Type != Gdk.EventType.ButtonPress)
				return;

			Placeholder ph = obj as Placeholder;

			if (args.Event.Button == 1) {
				proj.Selection = ph;
				args.RetVal = true;
			} else if (args.Event.Button == 3) {
				proj.PopupContextMenu (ph);
				args.RetVal = true;
			}
		}
		
		public static bool ShowNonContainerWarning {
			get { return showNonContainerWarning; }
			set { showNonContainerWarning = value; }
		}
		
		static IList nonContainers = new string[] {
			"Gtk.Button", "Gtk.Entry", "Gtk.Label", "Gtk.Arrow", "Gtk.Calendar", "Gtk.CheckButton",
			"Gtk.ColorButton", "Gtk.ComboBox", "Gtk.ComboBoxEntry", "Gtk.Entry", "Gtk.FontButton",
			"Gtk.HScale", "Gtk.VScale", "Gtk.Image", "Gtk.MenuBar", "Gtk.Toolbar", "Gtk.RadioButton",
			"Gtk.ProgressBar", "Stetic.Editor.ActionToolbar", "Stetic.Editor.ActionMenuBar",
			"Gtk.ToggleButton", "Gtk.TextView", "Gtk.VScrollbar", "Gtk.HScrollbar", "Gtk.SpinButton",
			"Gtk.Statusbar", "Gtk.HSeparator", "Gtk.VSeparator"
		};

		void PlaceholderDrop (Placeholder ph, Stetic.Wrapper.Widget wrapper)
		{
			Gtk.Dialog parentDialog = Wrapped.Parent as Gtk.Dialog;
			if (showNonContainerWarning && (IsTopLevel || (parentDialog != null && parentDialog.VBox == Wrapped))) {
				if (nonContainers.Contains (wrapper.Wrapped.GetType ().ToString ())) {
					using (NonContainerWarningDialog dlg = new NonContainerWarningDialog ()) {
						int res = dlg.Run ();
						showNonContainerWarning = dlg.ShowAgain;
						if (res != (int) Gtk.ResponseType.Ok)
							return;
					}
				}
			}
			using (UndoManager.AtomicChange) {
				ReplaceChild (ph, wrapper.Wrapped, true);
				wrapper.Select ();
			}
		}

		void PlaceholderDragDrop (object obj, Gtk.DragDropArgs args)
		{
			Placeholder ph = (Placeholder)obj;
			// This Drop call will end calling DropObject()
			DND.Drop (args.Context, args.Time, this, ph.UndoId);
			args.RetVal = true;
		}
		
		internal protected override void DropObject (string data, Gtk.Widget w)
		{
			Placeholder ph = FindPlaceholder (container, data);
			if (ph != null) {
				Widget dropped = Stetic.Wrapper.Widget.Lookup (w);
				if (dropped != null)
					PlaceholderDrop (ph, dropped);
			}
		}
		
		Placeholder FindPlaceholder (Gtk.Container c, string pid)
		{
			foreach (Gtk.Widget cw in c.AllChildren) {
				Placeholder ph = cw as Placeholder;
				if (ph != null && ph.UndoId == pid)
					return ph;
				Gtk.Container cc = cw as Gtk.Container;
				if (cc != null) {
					ph = FindPlaceholder (cc, pid);
					if (ph != null)
						return ph;
				}
			}
			return null;
		}

		void PlaceholderDragDataReceived (object obj, Gtk.DragDataReceivedArgs args)
		{
			Widget dropped = WidgetUtils.Paste (proj, args.SelectionData);
			Gtk.Drag.Finish (args.Context, dropped != null,
					 dropped != null, args.Time);
			if (dropped != null) {
				dropped.RequiresUndoStatusUpdate = true;
				PlaceholderDrop ((Placeholder)obj, dropped);
			}
		}

		protected virtual void ChildContentsChanged (Container child)
		{
		}

		void ChildRemoved (object obj, Gtk.RemovedArgs args)
		{
			NotifyChildRemoved (args.Widget);
		}
		
		protected void NotifyChildRemoved (Gtk.Widget child)
		{
			if (Loading)
				return;
				
			ObjectWrapper w = ObjectWrapper.Lookup (child);
			if (w != null) {
				if (w.Loading)
					return;
				if (designer != null)
					w.OnDesignerDetach (designer);
			}
			ChildRemoved (child);
		}

		protected virtual void ChildRemoved (Gtk.Widget w)
		{
			AutoSize[w] = false;
			EmitContentsChanged ();
		}

		public virtual IEnumerable RealChildren {
			get {
				ArrayList children = new ArrayList ();
				foreach (Gtk.Widget widget in container.AllChildren) {
					if (!(widget is Placeholder))
						children.Add (widget);
				}
				return children;
			}
		}

		public virtual IEnumerable GladeChildren {
			get {
				return container.AllChildren;
			}
		}
		
		public void PasteChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			using (UndoManager.AtomicChange) {
				Widget w = Widget.Lookup (newChild);
				w.RequiresUndoStatusUpdate = true;
				ReplaceChild (oldChild, newChild, true);
			}
		}

		internal protected void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild, bool destroyOld)
		{
			ReplaceChild (oldChild, newChild);
			if (destroyOld)
				oldChild.Destroy ();
		}
		
		protected virtual void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			using (UndoManager.AtomicChange)
			{
				Gtk.Container.ContainerChild cc;
				Hashtable props = new Hashtable ();

				cc = container[oldChild];
				foreach (PropertyInfo pinfo in cc.GetType ().GetProperties ()) {
					if (!pinfo.IsDefined (typeof (Gtk.ChildPropertyAttribute), true))
						continue;
					props[pinfo] = pinfo.GetValue (cc, null);
				}

				container.Remove (oldChild);
				AutoSize[oldChild] = false;
				AutoSize[newChild] = true;
				
				try {
					// Don't fire the child added event until the packing info is set
					internalAdd = true;
					container.Add (newChild);
				} finally {
					internalAdd = false;
				}

				cc = container[newChild];
				foreach (PropertyInfo pinfo in props.Keys)
					pinfo.SetValue (cc, props[pinfo], null);

				Sync ();
				NotifyChildAdded (newChild);
				if (Project != null)
					Project.Selection = newChild;
			}
		}

		Gtk.Widget selection;

		public virtual void Select (Gtk.Widget widget)
		{
			if (widget == null) {
				Select (null, false);
			} else {
				Widget wrapper = Widget.Lookup (widget);
				bool allowDrag = wrapper != null && wrapper.InternalChildProperty == null && !wrapper.IsTopLevel;
				Select (widget, allowDrag);
			}
		}

		public virtual void UnSelect (Gtk.Widget widget)
		{
			if (selection == widget)
				Select (null, false);
		}

		void Select (Gtk.Widget widget, bool dragHandles)
		{
			if (widget == selection)
				return;

			Gtk.Window win = GetParentWindow ();
			
			if (selection != null) {
				selection.Destroyed -= SelectionDestroyed;
				HideSelectionBox (selection);
				Widget wr = Widget.Lookup (selection);
				if (wr != null)
					wr.OnUnselected ();
			}
			
			selection = widget;
			if (win != null) {
				if (widget != null) {
					if (widget.CanFocus)
						win.Focus = widget;
					else {
						// Look for a focusable parent container
						Widget wr = GetTopLevel ();
						Gtk.Widget w = wr.Wrapped;
						while (w != null && !w.CanFocus)
							w = w.Parent;

						// If the widget is not focusable,
						// remove the focus from the window. In this way we ensure
						// that the current selected widget will lose the focus,
						// even if the new selection is not focusable.
						win.Focus = w;
					}
				} else {
					if (designer != null)
						designer.ResetSelection (null);
				}
			}
				
			if (selection != null) {
				selection.Destroyed += SelectionDestroyed;

				// FIXME: if the selection isn't mapped, we should try to force it
				// to be. (Eg, if you select a widget in a hidden window, the window
				// should map. If you select a widget on a non-current notebook
				// page, the notebook should switch pages, etc.)
				if (selection.IsDrawable && selection.Visible) {
					ShowSelectionBox (selection, dragHandles);
				}
				
				Widget wr = Widget.Lookup (selection);
				if (wr != null)
					wr.OnSelected ();
			}
		}
		
		void ShowSelectionBox (Gtk.Widget widget, bool dragHandles)
		{
			HideSelectionBox (selection);

			IDesignArea designArea = GetDesignArea (widget);
			if (designArea != null) {
				IObjectSelection sel = designArea.SetSelection (widget, widget, dragHandles);
				sel.Drag += HandleWindowDrag;
				return;
			}
		}
		
		void HideSelectionBox (Gtk.Widget widget)
		{
			if (widget != null) {
				IDesignArea designArea = GetDesignArea (widget);
				if (designArea != null)
					designArea.ResetSelection (widget);
			}
		}
		
		Gtk.Window GetParentWindow ()
		{
			Gtk.Container cc = Wrapped as Gtk.Container;
			while (cc.Parent != null)
				cc = cc.Parent as Gtk.Container;
			return cc as Gtk.Window;
		}

		void SelectionDestroyed (object obj, EventArgs args)
		{
			if (!IsDisposed)
				UnSelect (selection);
		}

		Gtk.Widget dragSource;

		void HandleWindowDrag (Gdk.EventMotion evt, int dx, int dy)
		{
			Gtk.Widget dragWidget = selection;

			Project.Selection = null;

			using (UndoManager.AtomicChange) {
				dragSource = CreateDragSource (dragWidget);
			}
			
			DND.Drag (dragSource, evt, dragWidget);
		}

		protected virtual Gtk.Widget CreateDragSource (Gtk.Widget dragWidget)
		{
			Placeholder ph = CreatePlaceholder ();
			Gdk.Rectangle alloc = dragWidget.Allocation;
			ph.SetSizeRequest (alloc.Width, alloc.Height);
			ph.DragEnd += DragEnd;
			ReplaceChild (dragWidget, ph, false);
			return ph;
		}

		void DragEnd (object obj, Gtk.DragEndArgs args)
		{
			using (UndoManager.AtomicChange) {
				Placeholder ph = obj as Placeholder;
				ph.DragEnd -= DragEnd;

				dragSource = null;
				if (DND.DragWidget == null) {
					if (AllowPlaceholders)
						ph.SetSizeRequest (-1, -1);
					else
						container.Remove (ph);
					Sync ();
				} else
					ReplaceChild (ph, DND.Cancel (), true);
			}
		}

		public virtual void Delete (Stetic.Wrapper.Widget wrapper)
		{
			using (UndoManager.AtomicChange) {
				if (AllowPlaceholders)
					ReplaceChild (wrapper.Wrapped, CreatePlaceholder (), true);
				else {
					container.Remove (wrapper.Wrapped);
					wrapper.Wrapped.Destroy ();
				}
			}
		}

		public virtual void Delete (Stetic.Placeholder ph)
		{
			if (AllowPlaceholders) {
				// Don't allow deleting the only placeholder of a top level container
				if (IsTopLevel && container.Children.Length == 1)
					return;
				using (UndoManager.AtomicChange) {
					container.Remove (ph);
					ph.Destroy ();
					// If there aren't more placeholders in this container, just delete the container
					if (container.Children.Length == 0)
						Delete ();
				}
			}
		}

		protected bool ChildHExpandable (Gtk.Widget child)
		{
			if (child == dragSource)
				child = DND.DragWidget;
			else if (child is Placeholder)
				return true;

			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (child);
			if (wrapper != null)
				return wrapper.HExpandable;
			else
				return false;
		}

		protected bool ChildVExpandable (Gtk.Widget child)
		{
			if (child == dragSource)
				child = DND.DragWidget;
			else if (child is Placeholder)
				return true;

			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (child);
			if (wrapper != null)
				return wrapper.VExpandable;
			else
				return false;
		}

		// Note that this will be invalid/random for non-H/V-paired classes
		protected Gtk.Orientation ContainerOrientation;

		public override bool HExpandable {
			get {
				if (base.HExpandable)
					return true;

				// A horizontally-oriented container is HExpandable if any
				// child is. A vertically-oriented container is HExpandable
				// if *every* child is.

				foreach (Gtk.Widget w in container) {
					if (ChildHExpandable (w)) {
						if (ContainerOrientation == Gtk.Orientation.Horizontal)
							return true;
					} else if (ContainerOrientation == Gtk.Orientation.Vertical)
						return false;
				}
				return (ContainerOrientation == Gtk.Orientation.Vertical);
			}
		}

		public override bool VExpandable {
			get {
				if (base.VExpandable)
					return true;

				// Opposite of above

				foreach (Gtk.Widget w in container) {
					if (ChildVExpandable (w)) {
						if (ContainerOrientation == Gtk.Orientation.Vertical)
							return true;
					} else if (ContainerOrientation == Gtk.Orientation.Horizontal)
						return false;
				}
				return (ContainerOrientation == Gtk.Orientation.Horizontal);
			}
		}
		
		void ValidateChildNames (Gtk.Widget newWidget)
		{
			// newWidget is the widget which triggered the name check.
			// It will be the last widget to check, so if there are
			// name conflicts, the name to change to avoid the conflict
			// will be the name of that widget.
			
			if (!IsTopLevel) {
				ParentWrapper.ValidateChildNames (newWidget);
				return;
			}
				
			Hashtable names = new Hashtable ();
			
			// Validate all names excluding the new widget
			ValidateChildName (names, container, newWidget);
			
			if (newWidget != null) {
				// Now validate names in the new widget.
				ValidateChildName (names, newWidget, null);
			}
		}

		void ValidateChildName (Hashtable names, Gtk.Widget w, Gtk.Widget newWidget)
		{
			if (w == newWidget)
				return;

			if (names.Contains (w.Name)) {
				// There is a widget with the same name. If the widget
				// has a numeric suffix, just increase it.
				string name; int idx;
				WidgetUtils.ParseWidgetName (w.Name, out name, out idx);
				
				string compName = idx != 0 ? name + idx : name;
				while (names.Contains (compName)) {
					idx++;
					compName = name + idx;
				}
				w.Name = compName;
			}
			
			names [w.Name] = w;
			
			if (w is Gtk.Container) {
				foreach (Gtk.Widget cw in ((Gtk.Container)w).AllChildren)
					ValidateChildName (names, cw, newWidget);
			}
		}
		
		internal string GetValidWidgetName (Gtk.Widget widget)
		{
			// Get a valid name for a widget (a name that doesn't
			// exist in the parent container.

			if (!IsTopLevel)
				return ParentWrapper.GetValidWidgetName (widget);

			string name;
			int idx;

			WidgetUtils.ParseWidgetName (widget.Name, out name, out idx);
			
			string compName = idx != 0 ? name + idx : name;
			
			Gtk.Widget fw = FindWidget (compName, widget);
			while (fw != null) {
				idx++;
				compName = name + idx;
				fw = FindWidget (compName, widget);
			}
			
			return compName;
		}
		
		public Widget FindChild (string name)
		{
			Gtk.Widget w = FindWidget (name, null);
			return Widget.Lookup (w);
		}
		
		Gtk.Widget FindWidget (string name, Gtk.Widget skipwidget)
		{
			if (Wrapped != skipwidget && Wrapped.Name == name)
				return Wrapped;
			else
				return FindWidget ((Gtk.Container)Wrapped, name, skipwidget);
		}
		
		Gtk.Widget FindWidget (Gtk.Container parent, string name, Gtk.Widget skipwidget)
		{
			foreach (Gtk.Widget w in parent.AllChildren) {
				if (w.Name == name && w != skipwidget)
					return w;
				if (w is Gtk.Container) {
					Gtk.Widget res = FindWidget ((Gtk.Container)w, name, skipwidget);
					if (res != null)
						return res;
				}
			}
			return null;
		}
		
		public override ObjectWrapper FindObjectByUndoId (string id)
		{
			ObjectWrapper c = base.FindObjectByUndoId (id);
			if (c != null)
				return c;

			foreach (Gtk.Widget w in container.AllChildren) {
				Widget ww = Widget.Lookup (w);
				if (ww == null)
					continue;
				ObjectWrapper ow = ww.FindObjectByUndoId (id);
				if (ow != null)
					return ow;
			}
			return null;
		}
		
		
		
		public class ContainerChild : Stetic.ObjectWrapper
		{
			internal static void Register ()
			{
				// FIXME?
			}

			public override void Wrap (object obj, bool initialized)
			{
				base.Wrap (obj, initialized);
				cc.Child.ChildNotified += ChildNotifyHandler;
				cc.Child.ParentSet += OnParentSet;
			}
			
			[GLib.ConnectBefore]
			void OnParentSet (object ob, Gtk.ParentSetArgs args)
			{
				// Dispose the wrapper if the child is removed from the parent
				Gtk.Widget w = (Gtk.Widget)ob;
				if (w.Parent == null) {
					Dispose ();
					w.ParentSet -= OnParentSet;
				}
			}

			public override void Dispose ()
			{
				cc.Child.ChildNotified -= ChildNotifyHandler;
				base.Dispose ();
			}
			
			protected virtual void ChildNotifyHandler (object obj, Gtk.ChildNotifiedArgs args)
			{
				ParamSpec pspec = new ParamSpec (args.Pspec);
				EmitNotify (pspec.Name);
			}

			protected override void EmitNotify (string propertyName)
			{
				base.EmitNotify (propertyName);
				ParentWrapper.Sync ();
				ParentWrapper.NotifyChanged ();
			}

			Gtk.Container.ContainerChild cc {
				get {
					return (Gtk.Container.ContainerChild)Wrapped;
				}
			}

			protected Stetic.Wrapper.Container ParentWrapper {
				get {
					return Stetic.Wrapper.Container.Lookup (cc.Parent);
				}
			}

			public bool AutoSize {
				get {
					return ParentWrapper.AutoSize[cc.Child];
				}
				set {
					ParentWrapper.AutoSize[cc.Child] = value;
					EmitNotify ("AutoSize");
				}
			}
		}
	}
}
