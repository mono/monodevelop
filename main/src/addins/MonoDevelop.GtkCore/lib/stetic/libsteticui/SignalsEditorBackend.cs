using System;
using System.Text;
using System.Collections;
using Stetic.Wrapper;
using Mono.Unix;

namespace Stetic
{
	internal class SignalsEditorBackend: Gtk.ScrolledWindow, IObjectViewer
	{
		Gtk.TreeView tree;
		Gtk.TreeStore store;
		SignalsEditorFrontend frontend;
		
		ProjectBackend project;
		ObjectWrapper selection;
		bool internalChange;
		
		const int ColSignal = 0;
		const int ColHandler = 1;
		const int ColAfter = 2;
		const int ColHasHandler = 3;
		const int ColIsSignal = 4;
		const int ColSignalTextWeight = 5;
		
		public SignalsEditorBackend (SignalsEditorFrontend frontend)
		{
			this.frontend = frontend;
			
			tree = new Gtk.TreeView ();
			store = new Gtk.TreeStore (typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(int));
			tree.Model = store;
			tree.RowActivated += new Gtk.RowActivatedHandler (OnRowActivated);
			
			Gtk.CellRendererText crtSignal = new Gtk.CellRendererText ();
			
			Gtk.CellRendererText crtHandler = new Gtk.CellRendererText ();
			crtHandler.Editable = true;
			crtHandler.Edited += new Gtk.EditedHandler (OnHandlerEdited);
			
			Gtk.CellRendererToggle crtogAfter = new Gtk.CellRendererToggle ();
			crtogAfter.Activatable = true;
			crtogAfter.Toggled += new Gtk.ToggledHandler (OnAfterToggled);
			
			tree.AppendColumn (Catalog.GetString ("Signal"), crtSignal, "text", ColSignal, "weight", ColSignalTextWeight);
			tree.AppendColumn (Catalog.GetString ("Handler"), crtHandler, "markup", ColHandler, "visible", ColIsSignal);
			tree.AppendColumn (Catalog.GetString ("After"), crtogAfter, "active", ColAfter, "visible", ColHasHandler);
			tree.Columns[0].Resizable = true;
			tree.Columns[1].Resizable = true;
			tree.Columns[2].Resizable = true;
			Add (tree);
			ShowAll ();
		}
		
		public SignalsEditorBackend (SignalsEditorFrontend frontend, ProjectBackend project): this (frontend)
		{
			ProjectBackend = project;
		}
		
		public ProjectBackend ProjectBackend {
			get { return project; }
			set {
				if (project != null) {
					project.SelectionChanged -= OnWidgetSelected;
					project.SignalAdded -= new SignalEventHandler (OnSignalAddedOrRemoved);
					project.SignalRemoved -= new SignalEventHandler (OnSignalAddedOrRemoved);
					project.SignalChanged -= new SignalChangedEventHandler (OnSignalChanged);
					project.ProjectReloaded -= new EventHandler (OnProjectReloaded);
				}
					
				project = value;
				if (project != null) {
					TargetObject = project.Selection;
					project.SelectionChanged += OnWidgetSelected;
					project.SignalAdded += new SignalEventHandler (OnSignalAddedOrRemoved);
					project.SignalRemoved += new SignalEventHandler (OnSignalAddedOrRemoved);
					project.SignalChanged += new SignalChangedEventHandler (OnSignalChanged);
					project.ProjectReloaded += new EventHandler (OnProjectReloaded);
				} else if (selection != null) {
					selection = null;
					RefreshTree ();
				}
			}
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			OnWidgetSelected (null, null);
		}
		
		public Signal SelectedSignal {
			get {
				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!tree.Selection.GetSelected (out foo, out iter))
					return null;
				return GetSignal (iter);
			}
		}
		
		public object TargetObject {
			get {
				return selection != null ? selection.Wrapped : null;
			}
			set {
				if (project != null) {
					ObjectWrapper wrapper = ObjectWrapper.Lookup (value);
					if (wrapper == selection)
						return;
					selection = wrapper;
					RefreshTree ();
				}
			}
		}
		
		void OnWidgetSelected (object s, Wrapper.WidgetEventArgs args)
		{
			ObjectWrapper wrapper = args != null ? args.WidgetWrapper : null;
			if (wrapper == selection)
				return;
			selection = wrapper;
			RefreshTree ();
		}
		
		void OnSignalAddedOrRemoved (object sender, SignalEventArgs args)
		{
			if (!internalChange && args.Wrapper == selection)
				RefreshTree ();
		}

		void OnSignalChanged (object sender, SignalChangedEventArgs args)
		{
			if (!internalChange && args.Wrapper == selection)
				RefreshTree ();
		}
		
		void RefreshTree ()
		{
			ArrayList status = SaveStatus ();
			store.Clear ();
			
			if (selection == null)
				return;

			ClassDescriptor klass = selection.ClassDescriptor;
			
			foreach (ItemGroup group in klass.SignalGroups) {
				Gtk.TreeIter iter = store.AppendValues (group.Label, null, false, false, false, (int) Pango.Weight.Normal);
				if (FillGroup (iter, group))
					store.SetValue (iter, ColSignalTextWeight, (int) Pango.Weight.Bold);
			}
			RestoreStatus (status);
		}
		
		bool FillGroup (Gtk.TreeIter groupIter, ItemGroup group)
		{
			bool hasSignals = false;
			foreach (SignalDescriptor sd in group) {
				if (!sd.SupportsGtkVersion (project.TargetGtkVersion))
					continue;
				
				bool foundSignal = false;
				Gtk.TreeIter parent = groupIter;
				
				foreach (Signal signal in selection.Signals) {
					if (signal.SignalDescriptor != sd) continue;

					Gtk.TreeIter iter = store.AppendValues (parent, null, signal.Handler, false, true, true, (int) Pango.Weight.Normal);
					if (!foundSignal) {
						parent = iter;
						store.SetValue (iter, ColSignal, sd.Name);
						store.SetValue (iter, ColSignalTextWeight, (int) Pango.Weight.Bold);
						hasSignals = foundSignal = true;
					}
				}
				
				InsertEmptySignalRow (parent, foundSignal ? null : sd.Name);
			}
			return hasSignals;
		}
		
		void SetSignalData (Gtk.TreeIter iter, Signal signal)
		{
			store.SetValue (iter, ColHandler, signal.Handler);
			store.SetValue (iter, ColAfter, false);
			store.SetValue (iter, ColHasHandler, true);
			store.SetValue (iter, ColIsSignal, true);
		}
		
		void InsertEmptySignalRow (Gtk.TreeIter parent, string name)
		{
			store.AppendValues (parent, name, EmptyHandlerMarkup, false, false, true, (int) Pango.Weight.Normal);
		}
		
		void OnRowActivated (object sender, Gtk.RowActivatedArgs args)
		{
			Gtk.TreeIter iter;
			if (!store.GetIter (out iter, args.Path))
				return;
			
			SignalDescriptor sd = GetSignalDescriptor (iter);
			if (sd != null) {
				if (GetSignal (iter) == null)
					AddHandler (iter, GetHandlerName (sd.Name));
				frontend.NotifySignalActivated ();
			}
		}
		
		string GetHandlerName (string signalName)
		{
			Wrapper.Widget selWidget = selection as Wrapper.Widget;
			if (selWidget != null) {
				if (selWidget.IsTopLevel)
					return "On" + signalName;
				else
					return "On" + GetIdentifier (selWidget.Wrapped.Name) + signalName;
			}
			
			Wrapper.Action action = selection as Wrapper.Action;
			if (action != null) {
				return "On" + GetIdentifier (action.Name) + signalName;
			}
			
			return "On" + signalName;
		}
		
		string GetIdentifier (string name)
		{
			StringBuilder sb = new StringBuilder ();
			
			bool wstart = true;
			foreach (char c in name) {
				if (c == '_' || c == '-' || c == ' ' || !char.IsLetterOrDigit (c)) {
					wstart = true;
					continue;
				}
				if (wstart) {
					sb.Append (char.ToUpper (c));
					wstart = false;
				} else
					sb.Append (c);
			}
			return sb.ToString ();
		}
		
		void OnHandlerEdited (object sender, Gtk.EditedArgs args)
		{
			if (args.NewText == EmptyHandlerText)
				return;

			Gtk.TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;
				
			AddHandler (iter, args.NewText);
		}
		
		void AddHandler (Gtk.TreeIter iter, string name)
		{
			internalChange = true;
			
			Gtk.TreeIter piter = iter;
			while (store.IterDepth (piter) != 0)
				store.IterParent (out piter, piter);
			
			Signal signal = GetSignal (iter);
			if (signal == null) {
				if (name != "") {
					SignalDescriptor sd = GetSignalDescriptor (iter);
					signal = new Signal (sd);
					signal.Handler = name;
					selection.Signals.Add (signal);
					SetSignalData (iter, signal);
					store.SetValue (iter, ColSignalTextWeight, (int) Pango.Weight.Bold);
					if (store.IterDepth (iter) != 1)
						store.IterParent (out iter, iter);
					InsertEmptySignalRow (iter, null);
				}
			} else {
				if (name != "") {
					signal.Handler = name;
					store.SetValue (iter, ColHandler, signal.Handler);
				} else {
					selection.Signals.Remove (signal);
					if (store.IterDepth (iter) == 1) {
						if (store.IterNChildren (iter) == 1) {
							Gtk.TreeIter parent;
							store.IterParent (out parent, iter);
							store.Remove (ref iter);
							InsertEmptySignalRow (parent, signal.SignalDescriptor.Name);
						} else {
							Gtk.TreeIter citer;
							store.IterChildren (out citer, iter);
							Signal csignal = GetSignal (citer);
							store.Remove (ref citer);
							SetSignalData (iter, csignal);
							if (store.IterNChildren (iter) == 1)
								tree.CollapseRow (store.GetPath (iter));
						}
					} else
						store.Remove (ref iter);
				}
			}
			UpdateGroupStatus (piter);
			internalChange = false;
		}
		
		void OnAfterToggled (object o, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter it;
			if (store.GetIterFromString (out it, args.Path)) {
				Signal signal = GetSignal (it);
				if (signal != null) {
					internalChange = true;
					signal.After = !signal.After;
					internalChange = false;
					store.SetValue (it, ColAfter, signal.After);
				}
			}
		}
		
		void UpdateGroupStatus (Gtk.TreeIter iter)
		{
			Gtk.TreeIter signalIter;
			if (store.IterChildren (out signalIter, iter)) {
				do {
					if (store.IterNChildren (signalIter) > 0) {
						store.SetValue (iter, ColSignalTextWeight, (int) Pango.Weight.Bold);
						return;
					}
				} while (store.IterNext (ref signalIter));
			}
			store.SetValue (iter, ColSignalTextWeight, (int) Pango.Weight.Normal);
		}
		
		Signal GetSignal (Gtk.TreeIter iter)
		{
			if (! (bool) store.GetValue (iter, ColHasHandler))
				return null;
			string handler = (string) store.GetValue (iter, ColHandler);
			foreach (Signal sig in selection.Signals)
				if (sig.Handler == handler)
					return sig;
			return null;
		}
		
		SignalDescriptor GetSignalDescriptor (Gtk.TreeIter iter)
		{
			Gtk.TreeIter group_iter;
			if (! (bool) store.GetValue (iter, ColIsSignal) || !store.IterParent (out group_iter, iter))
				return null;
			string name = (string) store.GetValue (iter, ColSignal);
			string group_name = (string) store.GetValue (group_iter, ColSignal);

			foreach (ItemGroup igroup in selection.ClassDescriptor.SignalGroups) {
				if (igroup.Label != group_name)
					continue;
				SignalDescriptor desc = (SignalDescriptor) igroup [name];
				if (desc != null)
					return desc;
			}

			return null;
		}
		
		ArrayList SaveStatus ()
		{
			ArrayList list = new ArrayList ();
			
			Gtk.TreeIter it; 
			if (!store.GetIterFirst (out it))
				return list;
			
			do {
				SaveStatus (list, "", it);
			} while (store.IterNext (ref it));
			
			return list;
		}
		
		void SaveStatus (ArrayList list, string path, Gtk.TreeIter iter)
		{
			string basePath = path + "/" + store.GetValue (iter, ColSignal);
				
			if (!tree.GetRowExpanded (store.GetPath (iter)))
				return;

			list.Add (basePath);
			
			if (store.IterChildren (out iter, iter)) {
				do {
					SaveStatus (list, basePath, iter);
				} while (store.IterNext (ref iter));
			}
		}
		
		void RestoreStatus (ArrayList list)
		{
			foreach (string namePath in list) {
				string[] names = namePath.Split ('/');
				
				Gtk.TreeIter iter = Gtk.TreeIter.Zero;

				bool found = true;
				foreach (string name in names) {
					if (name == "") continue;
					if (!FindChildByName (name, ref iter)) {
						found = false;
						break;
					}
				}
				
				if (found)
					tree.ExpandRow (store.GetPath (iter), false);
			}
		}
		
		bool FindChildByName (string name, ref Gtk.TreeIter iter)
		{
			if (iter.Equals (Gtk.TreeIter.Zero)) {
				if (!store.GetIterFirst (out iter))
					return false;
			} else if (!store.IterChildren (out iter, iter))
				return false;
			
			do {
				if (name == (string) store.GetValue (iter, ColSignal))
					return true;
			}
			while (store.IterNext (ref iter));
			
			return false;
		}
		
		string EmptyHandlerMarkup {
			get { return "<i><span foreground=\"grey\">" + EmptyHandlerText + "</span></i>"; }
		}

		string EmptyHandlerText {
			get { return Catalog.GetString ("Click here to add a new handler"); } 
		}
	}

	internal class SignalsEditorEditSession: MarshalByRefObject
	{
		SignalsEditorBackend backend;
		
		public SignalsEditorEditSession (SignalsEditorFrontend frontend)
		{
			backend = new SignalsEditorBackend (frontend);
		}
		
		public SignalsEditorBackend Editor {
			get { return backend; }
		}
		
		public ProjectBackend ProjectBackend {
			get { return backend.ProjectBackend; }
			set { backend.ProjectBackend = value; }
		}
		
		public Signal SelectedSignal {
			get {
				return backend.SelectedSignal;
			}
		}
		
		public void Dispose ()
		{
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
