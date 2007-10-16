using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

using Gtk;
using GtkSourceView;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Utils;
using MonoDevelop.Core.Execution;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Codons;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;

using MonoDevelop.SourceEditor.FormattingStrategy;

namespace MonoDevelop.SourceEditor.Gui
{
	public class SourceEditorDisplayBinding : IDisplayBinding
	{
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
			if (mimetype == "application/x-web-config")
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
			w.LoadString (mimeType, StringParserService.Parse (text));
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
		bool loadingMembers;
		ListStore classStore;
		ListStore memberStore;
		bool classBrowserVisible = true;
		internal FileSystemWatcher fsw;
		Properties properties;
		IParseInformation memberParseInfo;
		bool handlingParseEvent = false;
		bool disposed;
		Tooltips tips = new Tooltips ();
		
		BreakpointEventHandler breakpointAddedHandler;
		BreakpointEventHandler breakpointRemovedHandler;
		EventHandler executionChangedHandler;
		int currentExecutionLine = -1;
	
		internal SourceEditor se;

		object fileSaveLock = new object ();
		DateTime lastSaveTime;
		bool warnOverwrite = false;
		
		EventHandler<PropertyChangedEventArgs> propertyHandler;
		
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
			
			if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created) {
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
				classBrowser.Visible = value;
				classBrowserVisible = value;
				if (classBrowserVisible)
					BindClassCombo ();
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
			pixr.Ypad = 0;
			classCombo.PackStart(pixr, false);
			classCombo.AddAttribute(pixr, "pixbuf", 0);
			CellRenderer colr = new CellRendererText();
			colr.Ypad = 0;
			classCombo.PackStart(colr, true);
			classCombo.AddAttribute(colr, "text", 1);
			
			pixr = new CellRendererPixbuf();
			pixr.Ypad = 0;
			membersCombo.PackStart(pixr, false);
			membersCombo.AddAttribute(pixr, "pixbuf", 0);
			colr = new CellRendererText();
			colr.Ypad = 0;
			membersCombo.PackStart(colr, true);
			membersCombo.AddAttribute(colr, "text", 1);
			
			// Pack the controls into the editorbar just below the file name tabs.
			EventBox tbox = new EventBox ();
			tbox.Add (classCombo);
			classBrowser.PackStart(tbox, true, true, 0);
			tbox = new EventBox ();
			tbox.Add (membersCombo);
			classBrowser.PackStart (tbox, true, true, 0);
			
			editorBar.PackEnd (classBrowser, false, true, 0);
			
			
			// Set up the data stores for the comboboxes
			classStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IClass));
			classCombo.Model = classStore;	
			memberStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			membersCombo.Model = memberStore;
   			membersCombo.Changed += new EventHandler (MemberChanged);
			classCombo.Changed += new EventHandler (ClassChanged);
			
			se = new SourceEditor (this);
			se.Buffer.ModifiedChanged += new EventHandler (OnModifiedChanged);
			se.Buffer.MarkSet += new MarkSetHandler (OnMarkSet);
			se.Buffer.Changed += new EventHandler (OnChanged);
			se.View.ToggleOverwrite += new EventHandler (CaretModeChanged);
			se.Buffer.LineCountChanged += delegate (int line, int count, int column) {
				TextFileService.FireLineCountChanged (this, line, count, column);
			};
			ContentNameChanged += new EventHandler (UpdateFSW);
			
			// setup a focus chain so that the editor widget gets focus when
			// switching tabs rather than the classCombo, by default
			Widget [] chain = new Widget [3];
			chain[0] = se;
			chain[1] = classCombo;
			chain[2] = membersCombo;
			((Container) mainBox).FocusChain = chain;
			
			CaretModeChanged (null, null);
			
			propertyHandler = (EventHandler<PropertyChangedEventArgs>) DispatchService.GuiDispatch (new EventHandler<PropertyChangedEventArgs> (PropertiesChanged));
			properties = TextEditorProperties.Properties;
			properties.PropertyChanged += propertyHandler;
			fsw = new FileSystemWatcher ();
			fsw.Created += (FileSystemEventHandler) DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));	
			fsw.Changed += (FileSystemEventHandler) DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			UpdateFSW (null, null);
			mainBox.PackStart (se, true, true, 0);
			
			if (Services.DebuggingService != null) {
				breakpointAddedHandler = (BreakpointEventHandler) DispatchService.GuiDispatch (new BreakpointEventHandler (OnBreakpointAdded));
				breakpointRemovedHandler = (BreakpointEventHandler) DispatchService.GuiDispatch (new BreakpointEventHandler (OnBreakpointRemoved));
				executionChangedHandler = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnExecutionLocationChanged));
				
				Services.DebuggingService.BreakpointAdded += breakpointAddedHandler;
				Services.DebuggingService.BreakpointRemoved += breakpointRemovedHandler;
				Services.DebuggingService.ExecutionLocationChanged += executionChangedHandler;
			}
			
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged += new ParseInformationEventHandler(UpdateClassBrowser);
			
			mainBox.ShowAll ();
			
			SetInitialValues ();
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
			TextFileService.FireResetCountChanges (this);

			properties.PropertyChanged -= propertyHandler;
			se.Buffer.ModifiedChanged -= new EventHandler (OnModifiedChanged);
			se.Buffer.MarkSet -= new MarkSetHandler (OnMarkSet);
			se.Buffer.Changed -= new EventHandler (OnChanged);
			se.View.ToggleOverwrite -= new EventHandler (CaretModeChanged);
			ContentNameChanged -= new EventHandler (UpdateFSW);
   			membersCombo.Changed -= new EventHandler (MemberChanged);
			classCombo.Changed -= new EventHandler (ClassChanged);
				
			classStore.Dispose ();
			memberStore.Dispose ();
			
			membersCombo.Model = null;
			classCombo.Model = null;
			
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged -= new ParseInformationEventHandler(UpdateClassBrowser);
			mainBox.Destroy ();
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
			TextFileService.FireCommitCountChanges (this);
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
			lastSaveTime = File.GetLastWriteTime (ContentName);
			InitializeFormatter ();
			
			if (Services.DebuggingService != null) {
				foreach (IBreakpoint b in Services.DebuggingService.GetBreakpointsAtFile (fileName))
					se.View.ShowBreakpointAt (b.Line - 1);
					
				UpdateExecutionLocation ();
			}
			
			IFileParserContext context = IdeApp.ProjectOperations.ParserDatabase.GetFileParserContext(fileName);
			memberParseInfo = context.ParseFile(fileName);
			BindClassCombo();
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
		
		void UpdateMethodBrowser ()
		{
			if (!ClassBrowserVisible)
				return;

			if (memberParseInfo == null) {
				classBrowser.Visible = false;
				return;
			}
			
			int line;
			int column;
			GetLineColumnFromPosition (this.CursorPosition, out line, out column);

			// Find the selected class
			
			TreeIter iter;
			if (!classStore.GetIterFirst (out iter))
				return;
			
			IClass classFound = null;
			do {
				IClass c = (IClass) classStore.GetValue (iter, 2);
				if (c.BodyRegion != null && c.BodyRegion.BeginLine <= line && line <= c.BodyRegion.EndLine)
					classFound = c;
			} while (classFound == null && classStore.IterNext (ref iter));

			loadingMembers = true;
			
			try {
				if (classFound == null) {
					classCombo.Active = -1;
					membersCombo.Active = -1;
					memberStore.Clear ();
					UpdateComboTip (classCombo, null);
					UpdateComboTip (membersCombo, null);
					return;
				}
				
				TreeIter citer;
				if (!classCombo.GetActiveIter (out citer) || !citer.Equals (iter)) {
					classCombo.SetActiveIter (iter);
					BindMemberCombo (classFound);
					return;
				}
				
				// Find the member
				
				if (!memberStore.GetIterFirst (out iter))
					return;
				
				do {
					IMember mem = (IMember) memberStore.GetValue (iter, 2);
					if (IsMemberSelected (mem, line, column)) {
						membersCombo.SetActiveIter (iter);
						UpdateComboTip (membersCombo, mem);
						return;
					}
				}
				while (memberStore.IterNext (ref iter));
				
				membersCombo.Active = -1;
				UpdateComboTip (membersCombo, null);
			}
			finally {
				loadingMembers = false;
			}
		}
		
		private bool BindClassCombo ()
		{
			if (disposed)
				return false;
			
			if (!ClassBrowserVisible)
				return false;
			
			loadingMembers = true;
			
			try {
				// Clear down all our local stores.
				classStore.Clear();				
				
				// check the IParseInformation member variable to see if we could get ParseInformation for the 
				// current docuement. If not we can't display class and member info so hide the browser bar.
				if (memberParseInfo == null) {
					classBrowser.Visible = false;
					return false;
				}
				
				ClassCollection cls = ((ICompilationUnit)memberParseInfo.BestCompilationUnit).Classes;
				// if we've got this far then we have valid parse info - but if we have not classes the not much point
				// in displaying the browser bar
				if (cls.Count == 0) {
					classBrowser.Visible = false;
					return false;
				}
				
				classBrowser.Visible = true;
				ArrayList classes = new ArrayList ();
				classes.AddRange (cls);
				classes.Sort (new LanguageItemComparer ());

				MonoDevelop.Projects.Ambience.Ambience am = se.View.GetAmbience ();
				foreach (IClass c in classes) {
					// Get the appropriate icon from the Icon service for the current IClass.
					Gdk.Pixbuf pix = IdeApp.Services.Resources.GetIcon (IdeApp.Services.Icons.GetIcon (c), IconSize.Menu);
					classStore.AppendValues (pix, am.Convert (c, MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters), c);
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
						
						// return false to stop the GLib.Timeout
						return false;
					}
				}
				// Sometimes there might be no classes e.g. AssemblyInfo.cs
				classCombo.Active = -1;
				UpdateComboTip (classCombo, null);
				handlingParseEvent = false;
			}
			finally {
				loadingMembers = false;
			}
			
			// return false to stop the GLib.Timeout
			return false;
		}
		
		
		private void BindMemberCombo (IClass c)
		{
			if (!ClassBrowserVisible)
				return;

			int position = 0;
			int activeIndex = -1;
			
			// find out where the current cursor position is and set the combos.
			int line;
			int column;
			this.GetLineColumnFromPosition(this.CursorPosition, out line, out column);
			
			UpdateComboTip (classCombo, c);
			membersCombo.Changed -= new EventHandler (MemberChanged);
			// Clear down all our local stores.
			
			membersCombo.Model = null;
			memberStore.Clear();
			UpdateComboTip (membersCombo, null);
				
			HybridDictionary methodMap = new HybridDictionary();
			
			Gdk.Pixbuf pix;
			
			ArrayList members = new ArrayList ();
			members.AddRange (c.Methods);
			members.AddRange (c.Properties);
			members.AddRange (c.Fields);
			members.Sort (new LanguageItemComparer ());
			
			// Add items to the member drop down 
			
			foreach (IMember mem in members)
			{
				pix = IdeApp.Services.Resources.GetIcon(IdeApp.Services.Icons.GetIcon (mem), IconSize.Menu); 
				
				// Add the member to the list
				MonoDevelop.Projects.Ambience.Ambience am = se.View.GetAmbience ();
				string displayName = am.Convert (mem, MonoDevelop.Projects.Ambience.ConversionFlags.UseIntrinsicTypeNames |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameters |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameterNames |
				                                      MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters);
				memberStore.AppendValues (pix, displayName, mem);
				
				// Check if the current cursor position in inside this member
				if (IsMemberSelected (mem, line, column)) {
					UpdateComboTip (membersCombo, mem);
					activeIndex = position;
				}
				
				position++;
			}
			membersCombo.Model = memberStore;
			
			// don't need method map anymore
			methodMap.Clear ();
			
			// set active the method the cursor is in
			membersCombo.Active = activeIndex;
			membersCombo.Changed += new EventHandler (MemberChanged);
		}
		
		private void MemberChanged(object sender, EventArgs e)
		{
			if (loadingMembers)
				return;

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
			if (loadingMembers)
				return;
			
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
		
		void UpdateComboTip (ComboBox combo, ILanguageItem it)
		{
			MonoDevelop.Projects.Ambience.Ambience am = se.View.GetAmbience ();
			string txt;
			if (it != null)
				txt = am.Convert (it, MonoDevelop.Projects.Ambience.ConversionFlags.All);
			else
				txt = null;
			tips.SetTip (combo.Parent, txt, txt);
		}
		
		bool IsMemberSelected (IMember mem, int line, int column)
		{
			if (mem is IMethod) {
				IMethod method = (IMethod) mem;
				return (method.BodyRegion != null && method.BodyRegion.BeginLine <= line && line <= method.BodyRegion.EndLine);
			}
			else if (mem is IProperty) {
				IProperty property = (IProperty) mem;
				return (property.BodyRegion != null && property.BodyRegion.BeginLine <= line && line <= property.BodyRegion.EndLine);
			}
			else
				return (mem.Region != null && mem.Region.BeginLine <= line && line <= mem.Region.EndLine);
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
				double vscroll = se.View.VScroll;
				Load (ContentName);
				editorBar.Remove (reloadBar);
				se.View.VScroll = vscroll;
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
			string ext = Path.GetExtension (ContentName).ToLower ();

			if (ext.Length > 0) {
				string id = ext.Substring (1);
				TypeExtensionNode node = AddinManager.GetExtensionNode ("/MonoDevelop/SourceEditor/Formatters/" + id) as TypeExtensionNode;
				if (node != null) {
					se.View.fmtr = (IFormattingStrategy) node.CreateInstance (typeof(IFormattingStrategy));
					return;
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
		void OnMarkSet (object o, MarkSetArgs args)
		{
			if (args.Mark == se.Buffer.InsertMark) {
				UpdateLineCol ();
				UpdateMethodBrowser ();
			}
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
			
			IdeApp.Workbench.StatusBar.SetCaretPosition (iter.Line + 1, col, chr);
		}
		
		// This is false because we at first `toggle' it to set it to true
		bool insert_mode = false; // TODO: is this always the default
		void CaretModeChanged (object sender, EventArgs e)
		{
			IdeApp.Workbench.StatusBar.SetInsertMode (insert_mode = ! insert_mode);
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
			set {
				se.Buffer.MoveMark (se.Buffer.InsertMark, se.Buffer.GetIterAtOffset (value)); 
				se.Buffer.MoveMark (se.Buffer.SelectionBound, se.Buffer.GetIterAtOffset (value));
				se.View.ScrollMarkOnscreen (se.Buffer.InsertMark);
			}
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
			if (end_line.Char.Length == 0)
				return string.Empty;
			while (end_line.Char[0] != '\n' && end_line.ForwardChar ())
				;
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
		
		void PropertiesChanged (object sender, PropertyChangedEventArgs e)
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
				case "TabsToSpaces":
					se.View.InsertSpacesInsteadOfTabs = TextEditorProperties.ConvertTabsToSpaces;
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
				case "HighlightSpaces":
					SourceEditorView.HighlightSpacesEnabled = TextEditorProperties.HighlightSpaces;
					se.View.QueueDraw ();
					break;
				case "HighlightTabs":
					SourceEditorView.HighlightTabsEnabled = TextEditorProperties.HighlightTabs;
					se.View.QueueDraw ();
					break;
				case "HighlightNewlines":
					SourceEditorView.HighlightNewlinesEnabled = TextEditorProperties.HighlightNewlines;
					se.View.QueueDraw ();
					break;
				default:
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
			return true;
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
			bool res;
			do {
				res = Find (reverse, iterator, text, flags, out matchStart, out matchEnd, limit);
				
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
				iterator = matchEnd;
			} while (res && options.SearchWholeWordOnly && (!matchStart.StartsWord () || !matchEnd.EndsWord ()));
			
			if (!res) 
				return false;
			
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
	
	class LanguageItemComparer: IComparer
	{
		public int Compare (object x, object y)
		{
			return string.Compare (((ILanguageItem)x).Name, ((ILanguageItem)y).Name, true);
		}
	}
}


