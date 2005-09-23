using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Gtk;

using MonoDevelop.Gui;
using MonoDevelop.Services;
using Stock = MonoDevelop.Gui.Stock;

using Mono.Debugger;
using Mono.Debugger.Languages;
#if NET_2_0
using MonoDevelop.DebuggerVisualizers;
#endif

namespace MonoDevelop.Debugger
{
	public class LocalsPad : Gtk.ScrolledWindow, IPadContent
	{
		Mono.Debugger.StackFrame current_frame;

		Hashtable variable_rows;
		Hashtable iters;

		Hashtable visualizers_by_item;

		Gtk.TreeView tree;
		Gtk.TreeStore store;

		internal const int NAME_COL = 0;
		internal const int VALUE_COL = 1;
		internal const int TYPE_COL = 2;
		internal const int RAW_VIEW_COL = 3;
		internal const int PIXBUF_COL = 4;


		public LocalsPad ()
		{
			this.ShadowType = ShadowType.In;

			variable_rows = new Hashtable();
			iters = new Hashtable();

			store = new TreeStore (typeof (string),
						    typeof (string),
						    typeof (string),
						    typeof (bool),
						    typeof (Gdk.Pixbuf));

			tree = new TreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;

			TreeViewColumn NameCol = new TreeViewColumn ();
			CellRenderer NameRenderer = new CellRendererText ();
			CellRenderer IconRenderer = new CellRendererPixbuf ();
			NameCol.Title = "Name";
			NameCol.PackStart (IconRenderer, false);
			NameCol.PackStart (NameRenderer, true);
			NameCol.AddAttribute (IconRenderer, "pixbuf", PIXBUF_COL);
			NameCol.AddAttribute (NameRenderer, "text", NAME_COL);
			NameCol.Resizable = true;
			NameCol.Alignment = 0.0f;
			tree.AppendColumn (NameCol);

			TreeViewColumn ValueCol = new TreeViewColumn ();
			CellRenderer ValueRenderer = new CellRendererText ();
			ValueCol.Title = "Value";
			ValueCol.PackStart (ValueRenderer, true);
			ValueCol.AddAttribute (ValueRenderer, "text", VALUE_COL);
			ValueCol.Resizable = true;
			NameCol.Alignment = 0.0f;
			tree.AppendColumn (ValueCol);

			TreeViewColumn TypeCol = new TreeViewColumn ();
			CellRenderer TypeRenderer = new CellRendererText ();
			TypeCol.Title = "Type";
			TypeCol.PackStart (TypeRenderer, true);
			TypeCol.AddAttribute (TypeRenderer, "text", TYPE_COL);
			TypeCol.Resizable = true;
			NameCol.Alignment = 0.0f;
			tree.AppendColumn (TypeCol);

			tree.TestExpandRow += new TestExpandRowHandler (TestExpandRow);

#if NET_2_0
			tree.PopupMenu += new PopupMenuHandler (TreePopup);
#endif

			Add (tree);
			ShowAll ();

			Runtime.DebuggingService.PausedEvent += (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnPausedEvent));
			Runtime.DebuggingService.StoppedEvent += (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnStoppedEvent));
		}

		bool InsertArrayChildren (TreeIter parent, ITargetArrayObject array)
		{
			bool inserted = false;

			for (int i = array.LowerBound; i < array.UpperBound; i++) {

				inserted = true;

				ITargetObject elt = array [i];
				if (elt == null)
					continue;

				TreeIter iter = store.Append (parent);
				AddObject (i.ToString (), "" /* XXX */, elt, iter);
			}

			return inserted;
		}

		bool InsertStructMember (TreeIter parent, ITargetStructObject sobj, ITargetMemberInfo member, bool is_field)
		{
			bool inserted = false;

			string icon_name = GetIcon (member);

#if NET_2_0
			DebuggerBrowsableAttribute battr = GetDebuggerBrowsableAttribute (member);
			if (battr != null) {
				TreeIter iter;

				switch (battr.State) {
				case DebuggerBrowsableState.Never:
					// don't display it at all
					continue;
				case DebuggerBrowsableState.Collapsed:
					// the default behavior for the debugger (c&p from the battr == null branch at the bottom of this function)
					iter = store.Append (parent);
					AddObject (member.Name, icon_name, is_field ? sobj.GetField (member.Index) : sobj.GetProperty (member.Index),
						   iter);
					inserted = true;
					break;
				case DebuggerBrowsableState.Expanded:
					// add it as in the Collapsed case...
					iter = store.Append (parent);
					AddObject (member.Name, icon_name, is_field ? sobj.GetField (member.Index) : sobj.GetProperty (member.Index),
						   iter);
					inserted = true;
					// then expand the row
					tree.ExpandRow (store.GetPath (iter), false);
					break;
				case DebuggerBrowsableState.RootHidden:
					ITargetObject member_obj = is_field ? sobj.GetField (member.Index) : sobj.GetProperty (member.Index);

					if (member_obj != null) {
						switch (member_obj.TypeInfo.Type.Kind) {
						case TargetObjectKind.Array:
							iter = store.Append (parent);
							// handle arrays normally, should check how vs2005 does this.
							AddObject (member.Name, icon_name, member_obj, iter);
							inserted = true;
							break;
						case TargetObjectKind.Class:
							try {
								inserted = InsertClassChildren (parent, (ITargetClassObject)member_obj, false);
							}
							catch {
								// what about this case?  where the member is possibly
								// uninitialized, do we try to add it later?
							}
							break;
						case TargetObjectKind.Struct:
							try {
								inserted = InsertStructChildren (parent, (ITargetStructObject)member_obj, false);
							}
							catch {
								// what about this case?  where the member is possibly
								// uninitialized, do we try to add it later?
							}
							break;
						default:
							// nothing
							break;
						}
					}
					break;
				}
			}
			else {
#endif
				TreeIter iter = store.Append (parent);
				AddObject (member.Name, icon_name, is_field ? sobj.GetField (member.Index) : sobj.GetProperty (member.Index),
					   iter);
				inserted = true;
#if NET_2_0
			}
#endif

			return inserted;
		}

#if NET_2_0
		bool InsertProxyChildren (DebuggingService dbgr, DebuggerTypeProxyAttribute pattr, TreeIter parent, ITargetStructObject sobj)
		{
			Mono.Debugger.StackFrame frame = dbgr.MainThread.CurrentFrame;
	 		ITargetStructType proxy_type = frame.Language.LookupType (frame, pattr.ProxyTypeName) as ITargetStructType;
			if (proxy_type == null)
				proxy_type = frame.Language.LookupType (frame,
									sobj.Type.Name + "+" + pattr.ProxyTypeName) as ITargetStructType;
			if (proxy_type != null) {
				string name = String.Format (".ctor({0})", sobj.Type.Name);
				ITargetMethodInfo method = null;

				foreach (ITargetMethodInfo m in proxy_type.Constructors) {
					if (m.FullName == name)
						method = m;
				}

				if (method != null) {
					ITargetFunctionObject ctor = proxy_type.GetConstructor (frame, method.Index);
					ITargetObject[] args = new ITargetObject[1];
					args[0] = sobj;

					ITargetStructObject proxy_obj = ctor.Type.InvokeStatic (frame, args, false) as ITargetStructObject;

					if (proxy_obj != null) {
						foreach (ITargetPropertyInfo prop in proxy_obj.Type.Properties) {
							InsertStructMember (parent, proxy_obj, prop, false);
						}

						TreeIter iter = store.Append (parent);
						store.SetValue (iter, NAME_COL, "Raw View");
						store.SetValue (iter, RAW_VIEW_COL, true);

						Gdk.Pixbuf icon = Runtime.Gui.Resources.GetIcon (Stock.Class, Gtk.IconSize.Menu);
						if (icon != null)
							store.SetValue (iter, PIXBUF_COL, icon);

						iters.Remove (iter);
						AddPlaceholder (sobj, iter);

						return true;
					}
				}
			}

			return false;
		}
#endif

		bool InsertStructChildren (TreeIter parent, ITargetStructObject sobj, bool raw_view)
		{
			bool inserted = false;

#if NET_2_0
			if (!raw_view) {
				DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
				DebuggerTypeProxyAttribute pattr = GetDebuggerTypeProxyAttribute (dbgr, sobj);

				if (pattr != null) {
					if (InsertProxyChildren (dbgr, pattr, parent, sobj))
						inserted = true;
				}

				return inserted;
			}
#endif

			foreach (ITargetFieldInfo field in sobj.Type.Fields) {
				if (InsertStructMember (parent, sobj, field, true))
					inserted = true;
			}

			foreach (ITargetPropertyInfo prop in sobj.Type.Properties) {
				if (InsertStructMember (parent, sobj, prop, false))
					inserted = true;
			}

			return inserted;
		}

		bool InsertClassChildren (TreeIter parent, ITargetClassObject sobj, bool raw_view)
		{
			bool inserted = false;

			if (sobj.Type.HasParent) {
				TreeIter iter = store.Append (parent);
				AddObject ("<parent>", Stock.Class, sobj.Parent, iter);
				inserted = true;
			}

			if (InsertStructChildren (parent, sobj, raw_view))
				inserted = true;

			return inserted;
		}

		void InsertMessage (TreeIter parent, string message)
		{
			TreeIter child;
			if (store.IterChildren (out child, parent)) {
				while (!(child.Equals (Gtk.TreeIter.Zero)) && (child.Stamp != 0))
					store.Remove (ref child);
			}

			TreeIter iter = store.Append (parent);
			store.SetValue (iter, VALUE_COL, message);
		}

#if NET_2_0
		void VisualizerActivate (object sender, EventArgs args)
		{
			DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
	  		DebuggerVisualizerAttribute va_attr = (DebuggerVisualizerAttribute)visualizers_by_item [sender];
			TreeModel model;
			TreeIter selected_iter;

			Console.WriteLine ("Activating visualizer: {0}", va_attr.VisualizerTypeName);

			if (va_attr == null) {
				Console.WriteLine ("blarg");
				return;
			}

			if (!tree.Selection.GetSelected (out model, out selected_iter)) {
				Console.WriteLine ("blarg");
				return;
			}

			Type visualizerType = Type.GetType (va_attr.VisualizerTypeName);
			DialogDebuggerVisualizer visualizer = (DialogDebuggerVisualizer)Activator.CreateInstance (visualizerType);

			// make sure the assembly defining the
			// VisualizerObjectSource used by this
			// visualizer is loaded into the debuggee.
			Type sourceType = Type.GetType (va_attr.VisualizerObjectSourceTypeName);

			dbgr.LoadLibrary (dbgr.MainThread, sourceType.Assembly.Location);

			ITargetObject tobj = (ITargetObject)iters [selected_iter];
			visualizer.Show (null, new TargetObjectProvider (tobj, dbgr.MainThread, sourceType.FullName));
		}

		Gtk.Menu CreatePopup ()
		{
			DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
			TreeModel model;
			TreeIter selected_iter;
			ITargetObject obj;
			DebuggerVisualizerAttribute[] vas;
			Gtk.Menu popup_menu;

			if (!tree.Selection.GetSelected (out model, out selected_iter))
				return null;

			popup_menu = new Gtk.Menu ();

			obj = (ITargetObject)iters [selected_iter];
			vas = GetDebuggerVisualizerAttributes (dbgr, obj);
	    
			if (vas == null) {
				Gtk.MenuItem item = new Gtk.MenuItem ("No Visualizers Defined");
				item.Show();
				popup_menu.Append (item);
			}
			else {
				visualizers_by_item = new Hashtable ();

				Gtk.MenuItem item = new Gtk.MenuItem ("Visualizers");
				Gtk.Menu visualizer_submenu = new Gtk.Menu ();
				item.Submenu = visualizer_submenu;

				item.Show();

				foreach (DebuggerVisualizerAttribute va in vas) {
					Gtk.MenuItem va_item;

					va_item = new Gtk.MenuItem (va.Description != null ? va.Description : va.VisualizerTypeName);

					va_item.Activated += new EventHandler (VisualizerActivate);

					va_item.Show();

					popup_menu.Append(item);
					visualizer_submenu.Append (va_item);

					visualizers_by_item.Add (va_item, va);
				}
			}

			return popup_menu;
		}

		void TreePopup (object o, PopupMenuArgs args)
		{
			Gtk.Menu popup_menu = CreatePopup();

			if (popup_menu != null)
				popup_menu.Popup ();
		}
#endif

		void TestExpandRow (object o, TestExpandRowArgs args)
		{
			bool inserted = false;

			ITargetObject obj = (ITargetObject) iters [args.Iter];

			TreeIter child;
			if (store.IterChildren (out child, args.Iter)) {
				while (!(child.Equals (Gtk.TreeIter.Zero)) && (child.Stamp != 0))
					store.Remove (ref child);
			}

			if (obj == null) {
				child = store.Append (args.Iter);
				return;
			}

			switch (obj.TypeInfo.Type.Kind) {
			case TargetObjectKind.Array:
				ITargetArrayObject array = (ITargetArrayObject) obj;
				try {
					inserted = InsertArrayChildren (args.Iter, array);
				} catch {
					InsertMessage (args.Iter, "<can't display array>");
					inserted = true;
				}
				if (!inserted)
					InsertMessage (args.Iter, "<empty array>");
				break;

			case TargetObjectKind.Class:
				ITargetClassObject cobj = (ITargetClassObject) obj;
				try {
					bool raw_view = (bool)store.GetValue (args.Iter, RAW_VIEW_COL);
					inserted = InsertClassChildren (args.Iter, cobj, raw_view);
				} catch (Exception e) {
				  Console.WriteLine (e);
					InsertMessage (args.Iter, "<can't display class>");
					inserted = true;
				}
				if (!inserted)
					InsertMessage (args.Iter, "<empty class>");
				break;

			case TargetObjectKind.Struct:
				ITargetStructObject sobj = (ITargetStructObject) obj;
				try {
					bool raw_view = (bool)store.GetValue (args.Iter, RAW_VIEW_COL);
					inserted = InsertStructChildren (args.Iter, sobj, raw_view);
				} catch {
					InsertMessage (args.Iter, "<can't display struct>");
					inserted = true;
				}
				if (!inserted)
					InsertMessage (args.Iter, "<empty struct>");
				break;

			default:
				InsertMessage (args.Iter, "<unknown object>");
				break;
			}
		}

		void AddPlaceholder (ITargetObject obj, TreeIter parent)
		{
			if (obj.TypeInfo.Type.Kind == TargetObjectKind.Array) {
				ITargetArrayObject array = (ITargetArrayObject) obj;
				if (array.LowerBound == array.UpperBound)
					return;
			}

			store.Append (parent);
			iters.Add (parent, obj);
		}

		string GetObjectValueString (ITargetObject obj)
		{
			if (obj == null) {
				return "null";
			}

			switch (obj.TypeInfo.Type.Kind) {
			case TargetObjectKind.Fundamental:
				object contents = ((ITargetFundamentalObject) obj).Object;
				return contents.ToString ();

			case TargetObjectKind.Array:
				ITargetArrayObject array = (ITargetArrayObject) obj;
				if (array.LowerBound == array.UpperBound && array.LowerBound == 0)
					return "[]";
				else
					return "";

			case TargetObjectKind.Struct:
			case TargetObjectKind.Class:
				try {
#if NET_2_0
					DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
					DebuggerDisplayAttribute dattr = GetDebuggerDisplayAttribute (dbgr, obj);
					if (dattr != null) {
						return dbgr.AttributeHandler.EvaluateDebuggerDisplay (obj, dattr.Value);
					}
					else {
#endif
						// call the object's ToString() method.
						return ((ITargetStructObject)obj).PrintObject();
#if NET_2_0
					}
#endif
				}
				catch (Exception e) {
				  //Console.WriteLine ("getting object value failed: {0}", e);
					return "";
				}
			default:
				return "";
			}
		}

		void AddObject (string name, string icon_name, ITargetObject obj, TreeIter iter)
		{
			store.SetValue (iter, NAME_COL, name);
			store.SetValue (iter, VALUE_COL, GetObjectValueString (obj));
			store.SetValue (iter, TYPE_COL,
					obj == null ? "" : Runtime.Ambience.CurrentAmbience.GetIntrinsicTypeName (obj.TypeInfo.Type.Name));
			Gdk.Pixbuf icon = Runtime.Gui.Resources.GetIcon (icon_name, Gtk.IconSize.Menu);
			if (icon != null)
				store.SetValue (iter, PIXBUF_COL, icon);
			if (obj != null)
				AddPlaceholder (obj, iter);
		}

		string GetIcon (ITargetObject obj)
		{
			string icon = "";

			if (obj.TypeInfo.Type.TypeHandle is Type)
				icon = Runtime.Gui.Icons.GetIcon ((Type)obj.TypeInfo.Type.TypeHandle);

			return icon;
		}

		string GetIcon (ITargetMemberInfo member)
		{
			string icon = "";

#if mdb_api_brokenness
			if (member.Handle is PropertyInfo)
				icon = Runtime.Gui.Icons.GetIcon ((PropertyInfo)member.Handle);
			else if (member.Handle is FieldInfo)
				icon = Runtime.Gui.Icons.GetIcon ((FieldInfo)member.Handle);
#endif

			return icon;
		}

		void UpdateVariableChildren (IVariable variable, ITargetObject obj, TreePath path, TreeIter iter)
		{
			bool expanded = tree.GetRowExpanded (path);
			TreeIter citer;

			if (!expanded) {

				/* we aren't expanded, just remove all
				 * children and add the object back
				 * (since it might be a different
				 * object now) */

				if (store.IterChildren (out citer, iter))
					while (store.Remove (ref citer)) ;
				iters.Remove (iter);

				AddPlaceholder (obj, iter);
			}
			else {
				/* in a perfect world, we'd just iterate
				 * over the stuff we're showing and update
				 * it.  for now, just remove all rows and
				 * re-add them. */

				if (store.IterChildren (out citer, iter))
					while (store.Remove (ref citer)) ;

				iters.Remove (iter);

				AddObject (variable.Name, GetIcon (obj), obj, iter);

				tree.ExpandRow (path, false);
			}
		}

		void UpdateVariable (IVariable variable)
		{
			TreeRowReference row = (TreeRowReference)variable_rows[variable];

			if (row == null) {
				/* the variable isn't presently displayed */

				if (!variable.IsAlive (current_frame.TargetAddress))
					/* it's not displayed and not alive, just return */
					return;

				AddVariable (variable);
			}
			else {
				/* the variable is presently displayed */

				// XXX we need a obj.IsValid check in this branch

				if (!variable.IsAlive (current_frame.TargetAddress)) {
					/* it's in the display but no longer alive.  remove it */
					RemoveVariable (variable);
					return;
				}

				/* it's still alive - make sure the display is up to date */
				TreeIter iter;
				if (store.GetIter (out iter, row.Path)) {
					try {
						ITargetObject obj = variable.GetObject (current_frame);

						/* make sure the Value column is correct */
						string current_value = (string)store.GetValue (iter, VALUE_COL);
						string new_value = GetObjectValueString (obj);
						if (current_value != new_value)
							store.SetValue (iter, VALUE_COL, new_value);

						/* update the children */
						UpdateVariableChildren (variable, obj, row.Path, iter);

					} catch (Exception e) {
						Console.WriteLine ("can't update variable: {0} {1}", variable, e);
						store.SetValue (iter, VALUE_COL, "");
					}
				}
			}
		}

		void AddVariable (IVariable variable)
		{
			try {
				/* it's alive, add it to the display */

				ITargetObject obj = variable.GetObject (current_frame);
				TreeIter iter;

				if (!obj.IsValid)
					return;

				store.Append (out iter);

				variable_rows.Add (variable, new TreeRowReference (store, store.GetPath (iter)));

				AddObject (variable.Name, GetIcon (obj), obj, iter);
			} catch (LocationInvalidException) {
				// Do nothing
			} catch (Exception e) {
				Console.WriteLine ("can't add variable: {0} {1}", variable, e);
			}
		}

		void RemoveVariable (IVariable variable)
		{
			TreeRowReference row = (TreeRowReference)variable_rows[variable];
			TreeIter iter;

			if (row != null && store.GetIter (out iter, row.Path)) {
				iters.Remove (iter);
				store.Remove (ref iter);
			}

			variable_rows.Remove (variable);
		}

		public void UpdateDisplay ()
		{
			if ((current_frame == null) || (current_frame.Method == null))
				return;

			try {
				Hashtable vars_to_remove = new Hashtable();

				foreach (IVariable var in variable_rows.Keys) {
					vars_to_remove.Add (var, var);
				}

				// this
				if (current_frame.Method.HasThis) {
					UpdateVariable (current_frame.Method.This);
					vars_to_remove.Remove (current_frame.Method.This);
				}

				// locals
				IVariable[] local_vars = current_frame.Method.Locals;
				foreach (IVariable var in local_vars) {
					UpdateVariable (var);
					vars_to_remove.Remove (var);
				}

				// parameters
				IVariable[] param_vars = current_frame.Method.Parameters;
				foreach (IVariable var in param_vars) {
					UpdateVariable (var);
					vars_to_remove.Remove (var);
				}

				foreach (IVariable var in vars_to_remove.Keys) {
					RemoveVariable (var);
				}

			} catch (Exception e) {
				Console.WriteLine ("error getting variables for current stack frame: {0}", e);
				store.Clear ();
				iters = new Hashtable ();
			}
		}

		protected void OnStoppedEvent (object o, EventArgs args)
		{
			DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
			current_frame = dbgr.CurrentFrame;
			UpdateDisplay ();
		}

		protected void OnPausedEvent (object o, EventArgs args)
		{
			DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
			current_frame = dbgr.CurrentFrame;
			UpdateDisplay ();
		}

#if NET_2_0
		DebuggerBrowsableAttribute GetDebuggerBrowsableAttribute (ITargetMemberInfo info)
		{
	  		if (info.Handle != null && info.Handle is System.Reflection.MemberInfo) {
				System.Reflection.MemberInfo mi = (System.Reflection.MemberInfo)info.Handle;
				object[] attrs = mi.GetCustomAttributes (typeof (DebuggerBrowsableAttribute), false);

				if (attrs != null && attrs.Length > 0)
					return (DebuggerBrowsableAttribute)attrs[0];
			}

			return null;
		}

		DebuggerTypeProxyAttribute GetDebuggerTypeProxyAttribute (DebuggingService dbgr, ITargetObject obj)
		{
			if (obj.TypeInfo.Type.TypeHandle != null && obj.TypeInfo.Type.TypeHandle is Type)
				return dbgr.AttributeHandler.GetDebuggerTypeProxyAttribute ((Type)obj.TypeInfo.Type.TypeHandle);

			return null;
		}

		DebuggerDisplayAttribute GetDebuggerDisplayAttribute (DebuggingService dbgr, ITargetObject obj)
		{
			if (obj.TypeInfo.Type.TypeHandle != null && obj.TypeInfo.Type.TypeHandle is Type)
			  return dbgr.AttributeHandler.GetDebuggerDisplayAttribute ((Type)obj.TypeInfo.Type.TypeHandle);

			return null;
		}

		DebuggerVisualizerAttribute[] GetDebuggerVisualizerAttributes (DebuggingService dbgr, ITargetObject obj)
		{
			if (obj.TypeInfo.Type.TypeHandle != null && obj.TypeInfo.Type.TypeHandle is Type)
			  return dbgr.AttributeHandler.GetDebuggerVisualizerAttributes ((Type)obj.TypeInfo.Type.TypeHandle);

			return null;
		}
#endif

		public Gtk.Widget Control {
			get {
				return this;
			}
		}

		public string Id {
			get { return "MonoDevelop.Debugger.LocalsPad"; }
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public string Title {
			get {
				return "Locals";
			}
		}

		public string Icon {
			get {
				return Stock.OutputIcon;
			}
		}

		public void RedrawContent ()
		{
			UpdateDisplay ();
		}

		protected virtual void OnTitleChanged(EventArgs e)
		{
				if (TitleChanged != null) {
						TitleChanged(this, e);
				}
		}
		protected virtual void OnIconChanged(EventArgs e)
		{
				if (IconChanged != null) {
						IconChanged(this, e);
				}
		}
		public event EventHandler TitleChanged;
		public event EventHandler IconChanged;


	}
}
