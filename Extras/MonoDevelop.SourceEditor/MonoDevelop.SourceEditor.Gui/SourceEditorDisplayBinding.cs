using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Utils;
using MonoDevelop.SourceEditor.Properties;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;
using MonoDevelop.SourceEditor.FormattingStrategy;

using Gtk;
using GtkSourceView;

namespace MonoDevelop.SourceEditor.Gui
{
	public class SourceEditorDisplayBinding : IDisplayBinding
	{
		StringParserService sps = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
		
		static SourceEditorDisplayBinding ()
		{
			GtkSourceViewManager.Init ();
		}

		public string DisplayName {
			get { return "Source Code Editor"; }
		}
		
		public virtual bool CanCreateContentForFile (string fileName)
		{
			return false;
		}

		public virtual bool CanCreateContentForMimeType (string mimetype)
		{
			if (mimetype == null)
				return false;
			if (mimetype.StartsWith ("text"))
				return true;
			if (mimetype == "application/x-python")
				return true;
			if (mimetype == "application/x-config")
				return true;
			if (mimetype == "application/x-aspx")
				return true;
			if (mimetype == "application/x-ascx")
				return true;

			// If gedit can open the file, this editor also can do it
			foreach (DesktopApplication app in DesktopApplication.GetApplications (mimetype))
				if (app.Command == "gedit")
					return true;
			
			return false;
		}
		
		public virtual IViewContent CreateContentForFile (string fileName)
		{
			SourceEditorDisplayBindingWrapper w = new SourceEditorDisplayBindingWrapper ();
			return w;
		}
		
		public virtual IViewContent CreateContentForMimeType (string mimeType, Stream content)
		{
			StreamReader sr = new StreamReader (content);
			string text = sr.ReadToEnd ();
			sr.Close ();
			
			SourceEditorDisplayBindingWrapper w = new SourceEditorDisplayBindingWrapper ();
			w.LoadString (mimeType, sps.Parse (text));
			return w;
		}	
	}
	
	public class SourceEditorDisplayBindingWrapper : AbstractViewContent,
		IExtensibleTextEditor, IPositionable, IBookmarkBuffer, IDebuggableEditor, ICodeStyleOperations,
		IDocumentInformation, IEncodedTextContent, IViewHistory
	{
		VBox mainBox;
		VBox editorBar;
		HBox reloadBar;
		HBox classBrowser;
		Gtk.ComboBox classCombo;
		Gtk.ComboBox membersCombo;
		ListStore classStore;
		ListStore memberStore;
		bool classBrowserVisible = true;
		internal FileSystemWatcher fsw;
		IProperties properties;
		IParseInformation memberParseInfo;
		bool handlingParseEvent = false;
		bool disposed;
		
		BreakpointEventHandler breakpointAddedHandler;
		BreakpointEventHandler breakpointRemovedHandler;
		EventHandler executionChangedHandler;
		int currentExecutionLine = -1;
	
		internal SourceEditor se;

		object fileSaveLock = new object ();
		DateTime lastSaveTime;
		bool warnOverwrite = false;
		
		PropertyEventHandler propertyHandler;
		
		void UpdateFSW (object o, EventArgs e)
		{
			if (ContentName == null || ContentName.Length == 0 || !File.Exists (ContentName))
				return;

			fsw.EnableRaisingEvents = false;
			lastSaveTime = File.GetLastWriteTime (ContentName);
			fsw.Path = Path.GetDirectoryName (ContentName);
			fsw.Filter = Path.GetFileName (ContentName);
			fsw.EnableRaisingEvents = true;
		}

		private void OnFileChanged (object o, FileSystemEventArgs e)
		{
			lock (fileSaveLock) {
				if (lastSaveTime == File.GetLastWriteTime (ContentName))
					return;
			}
			
			if (e.ChangeType == WatcherChangeTypes.Changed) {
				ShowFileChangedWarning ();
			}
		}

		public void ExecutingAt (int line)
		{
			se.ExecutingAt (line);
		}

		public void ClearExecutingAt (int line)
		{
			se.ClearExecutingAt (line);
		}
		
		public override Gtk.Widget Control {
			get {
				return mainBox;
			}
		}
		
		public SourceEditor Editor {
			get {
				return se;
			}
		}
		
		public override string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Source Editor");
			}
		}
		
		public bool ClassBrowserVisible {
			get {
				return classBrowserVisible;
			}
			set {
				if (value && !classBrowserVisible)
					editorBar.PackEnd (classBrowser, true, true, 0);
				if (!value && classBrowserVisible)
					editorBar.Remove(classBrowser);
				classBrowserVisible = value;
			}
		}
		
		public SourceEditorDisplayBindingWrapper ()
		{
			mainBox = new VBox ();
			mainBox.Spacing = 3;
			editorBar = new VBox ();
			mainBox.PackStart (editorBar, false, true, 0);
			
			classBrowser = new HBox(true, 2);
			classCombo = new Gtk.ComboBox(); 
			classCombo.WidthRequest = 1;
			membersCombo = new Gtk.ComboBox();
			membersCombo.WidthRequest = 1;
			
			// Setup the columns and column renders for the comboboxes
			CellRendererPixbuf pixr = new CellRendererPixbuf();
			classCombo.PackStart(pixr, false);
			classCombo.AddAttribute(pixr, "pixbuf", 0);
			CellRenderer colr = new CellRendererText();
			classCombo.PackStart(colr, true);
			classCombo.AddAttribute(colr, "text", 1);
			
			pixr = new CellRendererPixbuf();
			membersCombo.PackStart(pixr, false);
			membersCombo.AddAttribute(pixr, "pixbuf", 0);
			colr = new CellRendererText();
			membersCombo.PackStart(colr, true);
			membersCombo.AddAttribute(colr, "text", 1);
			
			// Pack the controls into the editorbar just below the file name tabs.
			classBrowser.PackStart(classCombo, true, true, 0);
			classBrowser.PackStart(membersCombo, true, true, 0);
			editorBar.PackEnd (classBrowser, false, true, 0);
			
			// Set up the data stores for the comboboxes
			classStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IClass));
			classCombo.Model = classStore;	
			memberStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			membersCombo.Model = memberStore;
			
			se = new SourceEditor (this);
			se.Buffer.ModifiedChanged += new EventHandler (OnModifiedChanged);
			se.Buffer.MarkSet += new MarkSetHandler (OnMarkSet);
			se.Buffer.Changed += new EventHandler (OnChanged);
			se.View.ToggleOverwrite += new EventHandler (CaretModeChanged);
			ContentNameChanged += new EventHandler (UpdateFSW);
			
			CaretModeChanged (null, null);
			SetInitialValues ();
			
			propertyHandler = (PropertyEventHandler) Services.DispatchService.GuiDispatch (new PropertyEventHandler (PropertiesChanged));
			PropertyService propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));
			properties = ((IProperties) propertyService.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()));
			properties.PropertyChanged += propertyHandler;
			fsw = new FileSystemWatcher ();
			fsw.Changed += (FileSystemEventHandler) Services.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			UpdateFSW (null, null);
			mainBox.PackStart (se, true, true, 0);
			
			if (Services.DebuggingService != null) {
				breakpointAddedHandler = (BreakpointEventHandler) Services.DispatchService.GuiDispatch (new BreakpointEventHandler (OnBreakpointAdded));
				breakpointRemovedHandler = (BreakpointEventHandler) Services.DispatchService.GuiDispatch (new BreakpointEventHandler (OnBreakpointRemoved));
				executionChangedHandler = (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (OnExecutionLocationChanged));
				
				Services.DebuggingService.BreakpointAdded += breakpointAddedHandler;
				Services.DebuggingService.BreakpointRemoved += breakpointRemovedHandler;
				Services.DebuggingService.ExecutionLocationChanged += executionChangedHandler;
			}
			mainBox.ShowAll ();
		}
		
		public override object GetContent (Type type)
		{
			if (type == typeof(ICompletionWidget)) {
				if (se.View.EnableCodeCompletion)
					return se.View;
				else
					return null;
			} else
				return base.GetContent (type);
		}
		
		public void JumpTo (int line, int column)
		{
			// NOTE: 1 based!			
			TextIter itr = se.Buffer.GetIterAtLine (line - 1);
			itr.LineOffset = column - 1;

			se.Buffer.PlaceCursor (itr);		
			se.Buffer.HighlightLine (line - 1);	
			se.View.ScrollToMark (se.Buffer.InsertMark, 0.3, false, 0, 0);
			GLib.Timeout.Add (20, new GLib.TimeoutHandler (changeFocus));
		}

		//This code exists to workaround a gtk+ 2.4 regression/bug
		//
		//The gtk+ 2.4 treeview steals focus with double clicked
		//row_activated.
		// http://bugzilla.gnome.org/show_bug.cgi?id=138458
		bool changeFocus ()
		{
			if (disposed)
				return false;
			se.View.GrabFocus ();
			se.View.ScrollToMark (se.Buffer.InsertMark, 0.3, false, 0, 0);
			return false;
		}
		
		public override void RedrawContent()
		{
		}
		
		public override void Dispose()
		{
			disposed = true;
			
			if (Services.DebuggingService != null) {
				Services.DebuggingService.BreakpointAdded -= breakpointAddedHandler;
				Services.DebuggingService.BreakpointRemoved -= breakpointRemovedHandler;
				Services.DebuggingService.ExecutionLocationChanged -= executionChangedHandler;
			}

			mainBox.Remove (se);
			properties.PropertyChanged -= propertyHandler;
			se.Buffer.ModifiedChanged -= new EventHandler (OnModifiedChanged);
			se.Buffer.MarkSet -= new MarkSetHandler (OnMarkSet);
			se.Buffer.Changed -= new EventHandler (OnChanged);
			se.View.ToggleOverwrite -= new EventHandler (CaretModeChanged);
			ContentNameChanged -= new EventHandler (UpdateFSW);
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged -= new ParseInformationEventHandler(UpdateClassBrowser);
			se.Dispose ();
			fsw.Dispose ();
			se = null;
			base.Dispose ();
		}
		
		void OnModifiedChanged (object o, EventArgs e)
		{
			base.IsDirty = se.Buffer.Modified;
		}
		
		public override bool IsDirty {
			get {
				return base.IsDirty;
			}
			set {
				se.Buffer.Modified = value;
			}
		}
		
		public override bool IsReadOnly
		{
			get {
				return !se.View.Editable;
			}
		}
		
		public override void Save (string fileName)
		{
			Save (fileName, null);
		}
		
		public void Save (string fileName, string encoding)
		{
			if (warnOverwrite) {
				if (fileName == ContentName) {
					if (!Services.MessageService.AskQuestion (string.Format (GettextCatalog.GetString ("This file {0} has been changed outside of MonoDevelop. Are you sure you want to overwrite the file?"), fileName),"MonoDevelop"))
						return;
				}
				warnOverwrite = false;
				editorBar.Remove (reloadBar);
				WorkbenchWindow.ShowNotification = false;
			}

			lock (fileSaveLock) {
				se.Buffer.Save (fileName, encoding);
				lastSaveTime = File.GetLastWriteTime (fileName);
			}
			if (encoding != null)
				se.Buffer.SourceEncoding = encoding;
			ContentName = fileName;
			InitializeFormatter ();
		}
		
		public override void Load (string fileName)
		{
			Load (fileName, null);
		}
		
		public void Load (string fileName, string encoding)
		{
			if (warnOverwrite) {
				warnOverwrite = false;
				editorBar.Remove (reloadBar);
				WorkbenchWindow.ShowNotification = false;
			}
			string vfsname = fileName;
			vfsname = vfsname.Replace ("%", "%25");
			vfsname = vfsname.Replace ("#", "%23");
			vfsname = vfsname.Replace ("?", "%3F");
			se.Buffer.LoadFile (fileName, Gnome.Vfs.MimeType.GetMimeTypeForUri (vfsname), encoding);
			ContentName = fileName;
			InitializeFormatter ();
			
			if (Services.DebuggingService != null) {
				foreach (IBreakpoint b in Services.DebuggingService.GetBreakpointsAtFile (fileName))
					se.View.ShowBreakpointAt (b.Line - 1);
					
				UpdateExecutionLocation ();
			}
			
			IFileParserContext context = IdeApp.ProjectOperations.ParserDatabase.GetFileParserContext(fileName);
			memberParseInfo = context.ParseFile(fileName);
			BindClassCombo();
			
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged += new ParseInformationEventHandler(UpdateClassBrowser);
		  	Editor.View.MoveCursor += new MoveCursorHandler(UpdateMethodBrowser);
		}
		
		public INavigationPoint BuildNavPoint ()
		{
			int line, column;
			string content;
			
			GetLineColumnFromPosition (CursorPosition, out line, out column);
			content = GetLineTextAtOffset (CursorPosition);
			
			return new TextNavigationPoint (ContentName, line, column, content);
		}
		
		private void UpdateClassBrowser(object sender, ParseInformationEventArgs args)
		{
			// This event handler can get called when files other than the current content are updated. eg.
			// when loading a new document. If we didn't do this check the member combo for this tab would have
			// methods for a different class in it!
			if (ContentName == args.FileName && !handlingParseEvent) {
				handlingParseEvent = true;
				memberParseInfo = args.ParseInformation;
				GLib.Timeout.Add (1000, new GLib.TimeoutHandler (BindClassCombo));
			}
		}
		
	
		private void UpdateMethodBrowser(object sender, Gtk.MoveCursorArgs args)
		{
			if (memberParseInfo == null) {
				ClassBrowserVisible = false;
				return;
			}
			
			membersCombo.Changed -= new EventHandler (MemberChanged);
			
			// find out where the current cursor position is and set the combos.
			int line;
			int column;
			this.GetLineColumnFromPosition (this.CursorPosition, out line, out column);
			
			int index = 0;
			Gtk.TreeIter iter;
			IClass currentClass = null;
			
			// check we are in the right class
			if (classCombo.GetActiveIter (out iter))
				currentClass = classCombo.Model.GetValue (iter, 2) as IClass;
			
			if (currentClass != null && currentClass.BodyRegion != null && currentClass.BodyRegion.BeginLine <= line && line <= currentClass.BodyRegion.EndLine) {
				// we are still in the active class, just need to check method now
				bool more = memberStore.GetIterFirst (out iter);
			    while (more)
				{
			    	object member = memberStore.GetValue (iter, 2);
			    	if (member is IField) {
			    		if (((IField) member).Region.BeginLine <= line && line <= ((IField) member).Region.EndLine) {
			    			membersCombo.Active = index;
			    			membersCombo.Changed += new EventHandler (MemberChanged);
			    			return;
			    		}
			    	}
			    	if (member is IProperty) {
			    		if (((IProperty) member).BodyRegion.BeginLine <= line && line <= ((IProperty) member).BodyRegion.EndLine) {
			    			membersCombo.Active = index;
			    			membersCombo.Changed += new EventHandler (MemberChanged);
			    			return;
			    		}
			    	}
			    	if (member is IMethod) {
			    		if (((IMethod) member).BodyRegion.BeginLine <= line && line <= ((IMethod) member).BodyRegion.EndLine) {
			    			membersCombo.Active = index;
			    			membersCombo.Changed += new EventHandler (MemberChanged);
			    			return;
			    		}
			    	}
			    	index++;
					more = memberStore.IterNext (ref iter);
				}
			} else {
				// Changed class, so rebind
				BindClassCombo();
			}
		}
		
		
		private bool BindClassCombo()
		{
			if (disposed)
				return false;
			
			classCombo.Changed -= new EventHandler (ClassChanged);
			// Clear down all our local stores.
			classStore.Clear();				
			
			// check the IParseInformation member variable to see if we could get ParseInformation for the 
			// current docuement. If not we can't display class and member info so hide the browser bar.
			if (memberParseInfo == null) {
				ClassBrowserVisible = false;
				return false;
			}
			
			if (!ClassBrowserVisible)
				ClassBrowserVisible = true;
				
			ClassCollection cls = ((ICompilationUnit)memberParseInfo.BestCompilationUnit).Classes;
			// if we've got this far then we have valid parse info - but if we have not classes the not much point
			// in displaying the browser bar
			if (cls.Count == 0) {
				ClassBrowserVisible = false;
				return false;
			}
				
			foreach (IClass c in cls) {
				// Get the appropriate icon from the Icon service for the current IClass.
				Gdk.Pixbuf pix = IdeApp.Services.Resources.GetIcon (IdeApp.Services.Icons.GetIcon (c), IconSize.Menu);
				classStore.AppendValues (pix, c.Name, c);
			}
			
			// find out where the current cursor position is and set the combos.
			int line;
			int column;
			this.GetLineColumnFromPosition(this.CursorPosition, out line, out column);
			for(int i = 0; i < cls.Count; i++) {
				IClass c = cls[i];
				if (c.BodyRegion != null && c.BodyRegion.BeginLine <= line && line <= c.BodyRegion.EndLine)	{
					// found the right class. Now need right method
					classCombo.Active = i;
					BindMemberCombo(c);
					
					handlingParseEvent = false;
					classCombo.Changed += new EventHandler (ClassChanged);
					
					// return false to stop the GLib.Timeout
					return false;
				}
			}
			// Sometimes there might be no classes e.g. AssemblyInfo.cs
			if (cls.Count > 0) {
				for (int i = 0; i < cls.Count; i++) {
					// If the first "class" is a delegate it will have no members 
					if (cls[i].ClassType != ClassType.Delegate) {
						BindMemberCombo(cls[i]);
						classCombo.Active = i;
						break;
					}
				}
			}
			classCombo.Changed += new EventHandler (ClassChanged);
			
			handlingParseEvent = false;

			// return false to stop the GLib.Timeout
			return false;
		}
		
		
		private void BindMemberCombo(IClass c)
		{
			int position = 0;
			int activeIndex = 0;
			// find out where the current cursor position is and set the combos.
			int line;
			int column;
			this.GetLineColumnFromPosition(this.CursorPosition, out line, out column);
			
			membersCombo.Changed -= new EventHandler (MemberChanged);
			// Clear down all our local stores.
			memberStore.Clear();
				
			HybridDictionary methodMap = new HybridDictionary();
			
			Gdk.Pixbuf pix;
			// Add items to the member drop down 
			MethodCollection sortedMethods = c.Methods;
			ArrayList.Adapter (sortedMethods).Sort (new CaseInsensitiveComparer ());
			
			foreach (IMethod method in sortedMethods) {
				pix = IdeApp.Services.Resources.GetIcon(IdeApp.Services.Icons.GetIcon(method), IconSize.Menu); 
				// For methods we append their parameter types too. This is a nice feature,
				// and it is also necessay to avoid problems with overloaded methods having
				// the same name
				StringBuilder methodName = new StringBuilder();
				methodName.Append(method.Name + " (");
				for (int i = 0; i < method.Parameters.Count; i++) {
					methodName.Append(method.Parameters [i].ReturnType.Name);
					if (i < method.Parameters.Count - 1) methodName.Append (", ");
				}
				methodName.Append(")"); 
				
				memberStore.AppendValues(pix, methodName.ToString (), method);
			
				// Check if the current cursor position in inside this method
				if (method.BodyRegion.BeginLine <= line && line <= method.BodyRegion.EndLine) {
					activeIndex = position;
				}
				++position;
			}
			foreach (IProperty property in c.Properties) {
				pix = IdeApp.Services.Resources.GetIcon (IdeApp.Services.Icons.GetIcon (property), IconSize.Menu);
				if (methodMap [property.Name] != null) {
					// The unlikely case that two properties have the same name has occured.
					// We use the fully qualified name for each subsequent property to avoid
					// problems.
					memberStore.AppendValues (pix, property.FullyQualifiedName, property);
				}
				else {
					memberStore.AppendValues (pix, property.Name, property);
				}
				
				// Check if the current cursor position in inside this property
				if (property.BodyRegion.BeginLine <= line && line <= property.BodyRegion.EndLine) {
					activeIndex = position;
				}
				++position;
			}
			foreach(IField member in c.Fields) {
				pix = IdeApp.Services.Resources.GetIcon (IdeApp.Services.Icons.GetIcon (member), IconSize.Menu);
				memberStore.AppendValues (pix, member.Name, member);
				
				// Check if the current cursor position in inside this field
				if (member.Region.BeginLine <= line && line <= member.Region.EndLine) {
					activeIndex = position;
				}
				++position;
			}
			// don't need method map anymore
			methodMap.Clear ();
			
			// set active the method the cursor is in
			membersCombo.Active = activeIndex;
			membersCombo.Changed += new EventHandler (MemberChanged);
		}
		
		private void MemberChanged(object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			if (membersCombo.GetActiveIter (out iter)) {	    
				// Find the IMember object in our list store by name from the member combo
				IMember member = (IMember) memberStore.GetValue (iter, 2);
				int line = member.Region.BeginLine;
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IViewContent content = (IViewContent) IdeApp.Workbench.ActiveDocument.GetContent(typeof(IViewContent));
				if (content is IPositionable) {
					((IPositionable)content).JumpTo (Math.Max (1, line), 1);
				}
			}
		}
		
		private void ClassChanged(object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			if (classCombo.GetActiveIter(out iter)) 	{
				IClass selectedClass = (IClass)classStore.GetValue(iter, 2);
				int line = selectedClass.Region.BeginLine;
				
				// Get a handle to the current document
				if (IdeApp.Workbench.ActiveDocument == null) {
					return;
				}
				
				// If we can we navigate to the line location of the IMember.
				IViewContent content = (IViewContent)IdeApp.Workbench.ActiveDocument.GetContent(typeof(IViewContent));
				if (content is IPositionable) {
					((IPositionable)content).JumpTo (Math.Max (1, line), 1);
				}
				
				// check that selected "class" isn't a delegate
				if (selectedClass.ClassType == ClassType.Delegate) {
					memberStore.Clear();
				} else {
					BindMemberCombo(selectedClass);
				}
			}
		}
		
		void OnBreakpointAdded (object sender, BreakpointEventArgs args)
		{
			if (args.Breakpoint.FileName == ContentName)
				se.View.ShowBreakpointAt (args.Breakpoint.Line - 1);
		}
		
		void OnBreakpointRemoved (object sender, BreakpointEventArgs args)
		{
			if (args.Breakpoint.FileName == ContentName)
				se.View.ClearBreakpointAt (args.Breakpoint.Line - 1);
		}
		
		void OnExecutionLocationChanged (object sender, EventArgs args)
		{
			UpdateExecutionLocation ();
		}
		
		void UpdateExecutionLocation ()
		{
			if (currentExecutionLine != -1)
				se.View.ClearExecutingAt (currentExecutionLine - 1);

			if (Services.DebuggingService.CurrentFilename == ContentName) {
				currentExecutionLine = Services.DebuggingService.CurrentLineNumber;
				se.View.ExecutingAt (currentExecutionLine - 1);
				
				TextIter itr = se.Buffer.GetIterAtLine (currentExecutionLine - 1);
				itr.LineOffset = 0;
				se.Buffer.PlaceCursor (itr);		
				se.View.ScrollToMark (se.Buffer.InsertMark, 0.3, false, 0, 0);
				GLib.Timeout.Add (200, new GLib.TimeoutHandler (changeFocus));
			}
			else
				currentExecutionLine = -1;
		}
		
		void ShowFileChangedWarning ()
		{
			if (reloadBar == null) {
				reloadBar = new HBox ();
				reloadBar.BorderWidth = 3;
				Gtk.Image img = Services.Resources.GetImage ("gtk-dialog-warning", IconSize.Menu);
				reloadBar.PackStart (img, false, false, 2);
				reloadBar.PackStart (new Gtk.Label (GettextCatalog.GetString ("This file has been changed outside of MonoDevelop")), false, false, 5);
				HBox box = new HBox ();
				reloadBar.PackStart (box, true, true, 10);
				
				Button b1 = new Button (GettextCatalog.GetString("Reload"));
				box.PackStart (b1, false, false, 5);
				b1.Clicked += new EventHandler (ClickedReload);
				
				Button b2 = new Button (GettextCatalog.GetString("Ignore"));
				box.PackStart (b2, false, false, 5);
				b2.Clicked += new EventHandler (ClickedIgnore);

				reloadBar.ShowAll ();
			}
			warnOverwrite = true;
			editorBar.PackStart (reloadBar, false, true, 0);
			reloadBar.ShowAll ();
			WorkbenchWindow.ShowNotification = true;
		}
		
		void ClickedReload (object sender, EventArgs args)
		{
			try {
				Load (ContentName);
				editorBar.Remove (reloadBar);
				WorkbenchWindow.ShowNotification = false;
			} catch (Exception ex) {
				Services.MessageService.ShowError (ex, "Could not reload the file.");
			}
		}
		
		void ClickedIgnore (object sender, EventArgs args)
		{
			editorBar.Remove (reloadBar);
			WorkbenchWindow.ShowNotification = false;
		}
		
		public void InitializeFormatter()
		{
			string ext = Path.GetExtension (ContentName).ToUpper ();

			if (ext.Length > 0) {
				string path;
				switch (ext) {
					case ".CS":
						path = "/AddIns/DefaultTextEditor/Formatter/C#";
						break;
					case ".VB":
						path = "/AddIns/DefaultTextEditor/Formatter/VBNET";
						break;
					default:
						// we fall back to the uppercase extension without the .
						// ex. BOO, XML
						path = String.Format ("/AddIns/DefaultTextEditor/Formatter/{0}", ext.Substring (1));
						break;
				}
				
				if (AddInTreeSingleton.AddInTree.TreeNodeExists (path)) {
					IFormattingStrategy[] formatter = (IFormattingStrategy[])(AddInTreeSingleton.AddInTree.GetTreeNode(path).BuildChildItems(this)).ToArray(typeof(IFormattingStrategy));
					if (formatter != null && formatter.Length > 0) {
						se.View.fmtr = formatter[0];
						return;
					}
				}
			}
			
			// if the above specific formatter is not found
			// we fall back to the default formatter
			se.View.fmtr = new DefaultFormattingStrategy ();
		}
		
		public void InsertAtCursor (string s)
		{
			se.Buffer.InsertAtCursor (s);
			se.View.ScrollMarkOnscreen (se.Buffer.InsertMark);		
		}
		
		public void LoadString (string mime, string val)
		{
			se.Buffer.LoadText (val, mime);
		}
		
#region IExtensibleTextEditor
		ITextEditorExtension IExtensibleTextEditor.AttachExtension (ITextEditorExtension extension)
		{
			return se.View.AttachExtension (extension);
		}
#endregion

#region IEditableTextBuffer
		public IClipboardHandler ClipboardHandler {
			get { return se.Buffer; }
		}
		
		public string Name {
			get { return ContentName; }
		}
		
		string cachedText;
		GLib.IdleHandler bouncingDelegate;
		
		public string Text {
			get {
				if (bouncingDelegate == null)
					bouncingDelegate = new GLib.IdleHandler (BounceAndGrab);
				if (needsUpdate) {
					cachedText = se.Buffer.Text;
/*					GLib.Idle.Add (bouncingDelegate);
					if (cachedText == null)
						return se.Buffer.Text;
*/				}
				return cachedText;
			}
			set { se.Buffer.Text = value; }
		}

		bool needsUpdate;
		bool BounceAndGrab ()
		{
			if (needsUpdate && se != null) {
				cachedText = se.Buffer.Text;
				needsUpdate = false;
			}
			return false;
		}
		
		public void Undo ()
		{
			if (((SourceBuffer)se.Buffer).CanUndo ()) {
				se.Buffer.Undo ();
				TextIter iter = se.Buffer.GetIterAtMark (se.Buffer.InsertMark);
				if (!se.View.VisibleRect.Contains (se.View.GetIterLocation (iter)))
					se.View.ScrollToMark (se.Buffer.InsertMark, 0.1, false, 0, 0);
			}
		}
		
		public void Redo ()
		{
			if (((SourceBuffer)se.Buffer).CanRedo ()) {
				se.Buffer.Redo ();
				TextIter iter = se.Buffer.GetIterAtMark (se.Buffer.InsertMark);
				if (!se.View.VisibleRect.Contains (se.View.GetIterLocation (iter)))
					se.View.ScrollToMark (se.Buffer.InsertMark, 0.1, false, 0, 0);
			}
		}
		
		public void BeginAtomicUndo ()
		{
			Editor.Buffer.BeginUserAction ();
		}
		
		public void EndAtomicUndo ()
		{
			Editor.Buffer.EndUserAction ();
		}
		
		public string SelectedText {
			get {
				return se.Buffer.GetSelectedText ();
			}
			set {
				int offset = se.Buffer.GetLowerSelectionBounds ();
				((IClipboardHandler)se.Buffer).Delete (null, null);
				se.Buffer.Insert (offset, value);
				se.Buffer.PlaceCursor (se.Buffer.GetIterAtOffset (offset + value.Length));
				se.View.ScrollMarkOnscreen (se.Buffer.InsertMark);
			}
		}
		
		public int GetLineLength (int line)
		{
			TextIter begin = Editor.Buffer.GetIterAtLine (line);
			return begin.CharsInLine;
		}

		public int GetPositionFromLineColumn (int line, int column)
		{
			if (line > Editor.Buffer.LineCount)
				return -1;

			TextIter itr = se.Buffer.GetIterAtLine (line - 1);
			if (column - 1 > itr.CharsInLine)
				itr.LineOffset = itr.CharsInLine > 0 ? itr.CharsInLine - 1 : 0;
			else
				itr.LineOffset = column - 1;
			return itr.Offset;
		}
		
		public void InsertText (int position, string text)
		{
			se.Buffer.Insert (position, text);
		}
		
		public void DeleteText (int pos, int length)
		{
			se.Buffer.Delete (pos, length);
		}
		
		public event EventHandler TextChanged {
			add { se.Buffer.Changed += value; }
			remove { se.Buffer.Changed -= value; }
		}
		
		public string SourceEncoding {
			get { return se.Buffer.SourceEncoding; }
		}
		
#endregion
		
#region Status Bar Handling
		IStatusBarService statusBarService = (IStatusBarService) ServiceManager.GetService (typeof (IStatusBarService));
		
		void OnMarkSet (object o, MarkSetArgs args)
		{
			// 99% of the time, this is the insertion point
			UpdateLineCol ();
		}
		
		void OnChanged (object o, EventArgs e)
		{
			// gedit also hooks this event, but do we need it?
			UpdateLineCol ();
			OnContentChanged (null);
			needsUpdate = true;
		}
		
		void UpdateLineCol ()
		{
			int col = 1; // first char == 1
			int chr = 1;
			bool found_non_ws = false;
			int tab_size = (int) se.View.TabsWidth;
			
			TextIter iter = se.Buffer.GetIterAtMark (se.Buffer.InsertMark);
			TextIter start = iter;
			
			iter.LineOffset = 0;
			
			while (!iter.Equal (start))
			{
				char c = iter.Char[0];
				
				if (c == '\t')
					col += (tab_size - (col % tab_size));
				else
					col ++;
				
				if (c != '\t' && c != ' ')
					found_non_ws = true;
				
				if (found_non_ws ) {
					if (c == '\t')
						chr += (tab_size - (col % tab_size));
					else
						chr ++;
				}
				
				iter.ForwardChar ();
			}
			
			statusBarService.SetCaretPosition (iter.Line + 1, col, chr);
		}
		
		// This is false because we at first `toggle' it to set it to true
		bool insert_mode = false; // TODO: is this always the default
		void CaretModeChanged (object sender, EventArgs e)
		{
			statusBarService.SetInsertMode (insert_mode = ! insert_mode);
		}
#endregion
#region ICodeStyleOperations
		void ICodeStyleOperations.CommentCode ()
		{
			se.Buffer.CommentCode ();
		}
		void ICodeStyleOperations.UncommentCode ()
		{
			se.Buffer.UncommentCode ();
		}
		
		void ICodeStyleOperations.IndentSelection ()
		{
			se.View.IndentSelection (false, false);
		}
		
		void ICodeStyleOperations.UnIndentSelection ()
		{
			se.View.IndentSelection (true, false);
		}
#endregion 

		public int CursorPosition {
			get { return se.Buffer.GetIterAtMark (se.Buffer.InsertMark).Offset; }
			set { se.Buffer.MoveMark (se.Buffer.InsertMark, se.Buffer.GetIterAtOffset (value)); }
		}

		public void Select (int startPosition, int endPosition)
		{
			se.Buffer.MoveMark (se.Buffer.InsertMark, se.Buffer.GetIterAtOffset (startPosition));
			se.Buffer.MoveMark (se.Buffer.SelectionBound, se.Buffer.GetIterAtOffset (endPosition));
		}
		
		public int SelectionStartPosition {
			get {
				TextIter p1 = se.Buffer.GetIterAtMark (se.Buffer.InsertMark);
				TextIter p2 = se.Buffer.GetIterAtMark (se.Buffer.SelectionBound);
				if (p1.Offset < p2.Offset) return p1.Offset;
				else return p2.Offset;
			}
		}
		
		public int SelectionEndPosition {
			get {
				TextIter p1 = se.Buffer.GetIterAtMark (se.Buffer.InsertMark);
				TextIter p2 = se.Buffer.GetIterAtMark (se.Buffer.SelectionBound);
				if (p1.Offset > p2.Offset) return p1.Offset;
				else return p2.Offset;
			}
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			TextIter it = se.Buffer.GetIterAtOffset (position);
			line = it.Line + 1;
			column = it.LineOffset + 1;
		}
		
		public void ShowPosition (int position)
		{
			se.View.ScrollToIter (se.Buffer.GetIterAtOffset (position), 0.3, false, 0, 0);
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return se.Buffer.GetText (se.Buffer.GetIterAtOffset (startPosition), se.Buffer.GetIterAtOffset (endPosition), true);
		}
		
		int ITextFile.Length {
			get { return se.Buffer.Length; }
		}
		
		char ITextFile.GetCharAt (int offset)
		{
			if (offset < (se.Buffer.Length - 1))
				return se.Buffer.GetIterAtOffset (offset).Char[0];
			else
				return (char) 0;
		}

		public void SetBookmarked (int position, bool mark)
		{
			int line = se.Buffer.GetIterAtOffset (position).Line;
			if (se.Buffer.IsBookmarked (line) != mark)
				se.Buffer.ToggleBookmark (line);
		}
		
		public bool IsBookmarked (int position)
		{
			int line = se.Buffer.GetIterAtOffset (position).Line;
			return se.Buffer.IsBookmarked (line);
		}
		
		public void PrevBookmark ()
		{
			se.PrevBookmark ();
		}
		
		public void NextBookmark ()
		{
			se.NextBookmark ();
		}
		
		public void ClearBookmarks ()
		{
			se.ClearBookmarks ();
		}
		
#region IDocumentInformation
		string IDocumentInformation.FileName {
			get { return ContentName != null ? ContentName : UntitledName; }
		}
		
		public ITextIterator GetTextIterator ()
		{
			int startOffset = Editor.Buffer.GetIterAtMark (Editor.Buffer.InsertMark).Offset;
			return new SourceViewTextIterator (this, se.View, startOffset);
		}
		
		public string GetLineTextAtOffset (int offset)
		{
			TextIter resultIter = se.Buffer.GetIterAtOffset (offset);
			TextIter start_line = resultIter, end_line = resultIter;
			start_line.LineOffset = 0;
			end_line.ForwardToLineEnd ();
			return se.Buffer.GetText (start_line.Offset, end_line.Offset - start_line.Offset);
		}		
#endregion

		void SetInitialValues ()
		{
			se.View.ModifyFont (TextEditorProperties.Font);
			se.View.ShowLineNumbers = TextEditorProperties.ShowLineNumbers;
			se.Buffer.CheckBrackets = TextEditorProperties.ShowMatchingBracket;
			se.View.ShowMargin = TextEditorProperties.ShowVerticalRuler;
			se.View.EnableCodeCompletion = TextEditorProperties.EnableCodeCompletion;
			se.View.InsertSpacesInsteadOfTabs = TextEditorProperties.ConvertTabsToSpaces;
			se.View.AutoIndent = (TextEditorProperties.IndentStyle == IndentStyle.Auto);
			se.View.AutoInsertTemplates = TextEditorProperties.AutoInsertTemplates;
			se.View.HighlightCurrentLine = TextEditorProperties.HighlightCurrentLine;
			se.Buffer.UnderlineErrors = TextEditorProperties.UnderlineErrors;
			se.Buffer.Highlight = TextEditorProperties.SyntaxHighlight;
			se.DisplayBinding.ClassBrowserVisible = TextEditorProperties.ShowClassBrowser;

			if (TextEditorProperties.VerticalRulerRow > -1)
				se.View.Margin = (uint) TextEditorProperties.VerticalRulerRow;
			else
				se.View.Margin = (uint) 80;

			if (TextEditorProperties.TabIndent > -1)
				se.View.TabsWidth = (uint) TextEditorProperties.TabIndent;
			else
				se.View.TabsWidth = (uint) 4;

			se.View.WrapMode = TextEditorProperties.WrapMode;
		}
		
		void PropertiesChanged (object sender, PropertyEventArgs e)
 		{
			switch (e.Key) {
				case "DefaultFont":
					se.View.ModifyFont (TextEditorProperties.Font);
					se.UpdateMarkerSize ();
					break;
				case "ShowLineNumbers":
					se.View.ShowLineNumbers = TextEditorProperties.ShowLineNumbers;
					break;
				case "ShowBracketHighlight":
					se.Buffer.CheckBrackets = TextEditorProperties.ShowMatchingBracket;
					break;
				case "ShowVRuler":
					se.View.ShowMargin = TextEditorProperties.ShowVerticalRuler;
					break;
				case "EnableCodeCompletion":
					se.View.EnableCodeCompletion = TextEditorProperties.EnableCodeCompletion;
					break;
				case "ConvertTabsToSpaces":
					se.View.InsertSpacesInsteadOfTabs = TextEditorProperties.ConvertTabsToSpaces;
					break;
				case "IndentStyle":
					se.View.AutoIndent = (TextEditorProperties.IndentStyle == IndentStyle.Auto);
					break;
				case "AutoInsertTemplates":
					se.View.AutoInsertTemplates = TextEditorProperties.AutoInsertTemplates;
					break;
				case "ShowErrors":
					se.Buffer.UnderlineErrors = TextEditorProperties.UnderlineErrors;
					break;
				case "SyntaxHighlight":
					se.Buffer.Highlight = TextEditorProperties.SyntaxHighlight;
					break;
				case "VRulerRow":
					if (TextEditorProperties.VerticalRulerRow > -1)
						se.View.Margin = (uint) TextEditorProperties.VerticalRulerRow;
					else
						se.View.Margin = (uint) 80;
					break;
				case "TabIndent":
					if (TextEditorProperties.TabIndent > -1)
						se.View.TabsWidth = (uint) TextEditorProperties.TabIndent;
					else
						se.View.TabsWidth = (uint) 4;
					break;
				case "EnableFolding":
					// TODO
					break;
				case "WrapMode":
					se.View.WrapMode = TextEditorProperties.WrapMode;
					break;
				case "ShowClassBrowser":
					se.DisplayBinding.ClassBrowserVisible = TextEditorProperties.ShowClassBrowser;
					break;
				case "HighlightCurrentLine":
					se.View.HighlightCurrentLine = TextEditorProperties.HighlightCurrentLine;
					break;
				case "ShowControlCharacters":
					SourceEditorView.DrawWhiteSpacesEnabled = TextEditorProperties.ShowControlCharacters;
				se.View.QueueDraw ();
					break;
				default:
					Console.WriteLine ("unhandled property change: {0}", e.Key);
					break;
			}
 		}
	}
	
	class SourceViewTextIterator: ForwardTextIterator
	{
		bool initialBackwardsPosition;
		bool hasWrapped;
		
		public SourceViewTextIterator (IDocumentInformation docInfo, Gtk.TextView document, int endOffset)
		: base (docInfo, document, endOffset)
		{
			// Make sure the iterator is ready for use
			this.MoveAhead(1);
			this.hasWrapped = false;
		}
		
		public override bool SupportsSearch (SearchOptions options, bool reverse)
		{
			return !options.SearchWholeWordOnly;
		}
		
		public override void MoveToEnd ()
		{
			initialBackwardsPosition = true;
			base.MoveToEnd ();
		}
		
		public override bool SearchNext (string text, SearchOptions options, bool reverse)
		{
			// Make sure the backward search finds the first match when that match is just
			// at the left of the cursor. Position needs to be incremented in this case because it will be
			// at the last char of the match, and BackwardSearch don't return results that include
			// the initial search position.
			if (reverse && Position < BufferLength && initialBackwardsPosition) {
				Position++;
				initialBackwardsPosition = false;
			}
							
			// Use special search flags that work for both the old and new API
			// of gtksourceview (the enum values where changed in the API).
			// See bug #75770
			SourceSearchFlags flags = options.IgnoreCase ? (SourceSearchFlags)7 : (SourceSearchFlags)1;
			
			Gtk.TextIter matchStart, matchEnd, limit;
								
			
			if (reverse) {
				if (!hasWrapped)
					limit = Buffer.StartIter;
				else
					limit = Buffer.GetIterAtOffset (EndOffset);
			} else {
				if (!hasWrapped)
					limit = Buffer.EndIter;
				else
					limit = Buffer.GetIterAtOffset (EndOffset + text.Length);
			}
			
			// machEnd is the position of the last matched char + 1
			// When searching forward, the limit check is: matchEnd < limit
			// When searching backwards, the limit check is: matchEnd > limit
			
			TextIter iterator = Buffer.GetIterAtOffset (DocumentOffset);
			bool res = Find (reverse, iterator, text, flags, out matchStart, out matchEnd, limit);
			
			if (!res && !hasWrapped) {
				
				hasWrapped = true;																
								
				// Not found in the first half of the document, try the other half
				if (reverse && DocumentOffset <= EndOffset) {					
					limit = Buffer.GetIterAtOffset (EndOffset);
					res = Find (true, Buffer.EndIter, text, flags, out matchStart, out matchEnd, limit);
				// Not found in the second half of the document, try the other half
				} else if (!reverse && DocumentOffset >= EndOffset) {										
					limit = Buffer.GetIterAtOffset (EndOffset + text.Length);									
					res = Find (false, Buffer.StartIter, text, flags, out matchStart, out matchEnd, limit);
				}
			}
			
			if (!res) return false;
			
			DocumentOffset = matchStart.Offset;
			return true;
		}
		
		
		bool Find (bool reverse, Gtk.TextIter iter, string str, GtkSourceView.SourceSearchFlags flags, out Gtk.TextIter match_start, out Gtk.TextIter match_end, Gtk.TextIter limit)
		{
			if (reverse)
				return ((SourceBuffer)Buffer).BackwardSearch (iter, str, flags, out match_start, out match_end, limit);
			else
				return ((SourceBuffer)Buffer).ForwardSearch (iter, str, flags, out match_start, out match_end, limit);
		}
	}
}


