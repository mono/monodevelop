// SourceEditorWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Gtk;
using Mono.TextEditor;
//using Mono.TextEditor.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Document = Mono.TextEditor.TextDocument;
using Services = MonoDevelop.Projects.Services;
using System.Threading;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using Mono.TextEditor.Theatrics;
using System.ComponentModel;
using MonoDevelop.Ide.TypeSystem;
using Mono.TextEditor.Highlighting;
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor
{
	class SourceEditorWidget : IServiceProvider, IDocumentReloadPresenter
	{
		SourceEditorView view;
		DecoratedScrolledWindow mainsw;

		// We need a reference to TextEditorData to be able to access the
		// editor document without getting the TextEditor property. This
		// property runs some gtk code, so it can only be used from the GUI thread.
		// Other threads can use textEditorData to get the document.
		TextEditorData textEditorData;
		
		const uint CHILD_PADDING = 0;

		// VV: I removed the animation since it was very slow especially on @2x
		// TODO: Maybe the AddAnimationWidget () shouldn't be used at all
		const uint ANIMATION_DURATION = 0; // 300
		
//		bool shouldShowclassBrowser;
//		bool canShowClassBrowser;
		Mono.TextEditor.ITextEditorOptions options {
			get {
				
				return textEditor.Options;
			}
		}


		internal QuickTaskStrip QuickTaskStrip { 
			get {
				return mainsw.Strip;
			}
		} 

		bool isDisposed;
		
		ParsedDocument parsedDocument;
		
		ExtensibleTextEditor textEditor;
		ExtensibleTextEditor splittedTextEditor;
		ExtensibleTextEditor lastActiveEditor;
		
		public MonoDevelop.SourceEditor.ExtensibleTextEditor TextEditor {
			get {
				return lastActiveEditor ?? textEditor;
			}
		}
		
		List<IQuickTaskProvider> quickTaskProvider = new List<IQuickTaskProvider> ();
		public void AddQuickTaskProvider (IQuickTaskProvider provider)
		{
			quickTaskProvider.Add (provider);
			mainsw.AddQuickTaskProvider (provider); 
			if (secondsw != null)
				secondsw.AddQuickTaskProvider (provider);
		}

		internal void ClearQuickTaskProvider ()
		{
			foreach (var provider in quickTaskProvider.ToArray ()) {
				RemoveQuickTaskProvider (provider);
			}
			quickTaskProvider = new List<IQuickTaskProvider> ();
		}

		public void RemoveQuickTaskProvider (IQuickTaskProvider provider)
		{
			quickTaskProvider.Remove (provider);
			mainsw.RemoveQuickTaskProvider (provider); 
			if (secondsw != null)
				secondsw.RemoveQuickTaskProvider (provider);
		}		

		
		List<UsageProviderEditorExtension> usageProvider = new List<UsageProviderEditorExtension> ();

		internal void ClearUsageTaskProvider()
		{
			foreach (var provider in usageProvider.ToArray ()) {
				RemoveUsageTaskProvider (provider);
			}
			usageProvider = new List<UsageProviderEditorExtension> ();

		}

		public void AddUsageTaskProvider (UsageProviderEditorExtension provider)
		{
			usageProvider.Add (provider);
			mainsw.AddUsageProvider (provider); 
			if (secondsw != null)
				secondsw.AddUsageProvider (provider);
		}

		void RemoveUsageTaskProvider (UsageProviderEditorExtension provider)
		{
			usageProvider.Remove (provider);
			mainsw.RemoveUsageProvider (provider); 
			if (secondsw != null)
				secondsw.RemoveUsageProvider (provider);
		}
		
		public bool HasMessageBar {
			get { return messageBar != null; }
		}
		
		Gtk.VBox vbox = new Gtk.VBox ();
		public Gtk.VBox Vbox {
			get { return this.vbox; }
		}

		public bool SearchWidgetHasFocus {
			get {
				if (HasAnyFocusedChild (searchAndReplaceWidget) || HasAnyFocusedChild (gotoLineNumberWidget))
					return true;
				return false;
			}
		}

		static bool HasAnyFocusedChild (Widget widget)
		{
			// Seems that this is the only reliable way doing it on os x and linux :/
			if (widget == null)
				return false;
			var stack = new Stack<Widget> ();
			stack.Push (widget);
			while (stack.Count > 0) {
				var cur = stack.Pop ();
				if (cur.HasFocus) {
					return true;
				}
				var c = cur as Gtk.Container;
				if (c!= null) {
					foreach (var child in c.Children) {
						stack.Push (child);
					}
				}
			}
			return false;
		}
		
		class Border : Gtk.DrawingArea
		{
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				evnt.Window.DrawRectangle (this.Style.DarkGC (State), true, evnt.Area);
				return true;
			}
		}
		
		
		class DecoratedScrolledWindow : HBox
		{
			SourceEditorWidget parent;
			ScrolledWindow scrolledWindow;
			EventBox scrolledBackground;
			
			QuickTaskStrip strip;
			
			public Adjustment Hadjustment {
				get {
					return scrolledWindow.Hadjustment;
				}
			}
			
			public Adjustment Vadjustment {
				get {
					return scrolledWindow.Vadjustment;
				}
			}

			public QuickTaskStrip Strip {
				get {
					return strip;
				}
			}

			public DecoratedScrolledWindow (SourceEditorWidget parent)
			{
				this.parent = parent;
				this.strip = new QuickTaskStrip (parent.view);

				scrolledBackground = new EventBox ();
				scrolledBackground.Accessible.SetShouldIgnore (true);
				scrolledWindow = new CompactScrolledWindow ();
				scrolledWindow.CanFocus = false;
				scrolledWindow.ButtonPressEvent += PrepareEvent;
				scrolledWindow.Accessible.SetShouldIgnore (true);
				scrolledBackground.Add (scrolledWindow);
				PackStart (scrolledBackground, true, true, 0);
				strip.VAdjustment = scrolledWindow.Vadjustment;
				PackEnd (strip, false, true, 0);

				parent.quickTaskProvider.ForEach (AddQuickTaskProvider);

				QuickTaskStrip.EnableFancyFeatures.Changed += FancyFeaturesChanged;
				FancyFeaturesChanged (null, null);
			}

			void FancyFeaturesChanged (object sender, EventArgs e)
			{
				if (!QuickTaskStrip.MergeScrollBarAndQuickTasks)
					return;
				if (QuickTaskStrip.EnableFancyFeatures) {
					GtkWorkarounds.SetOverlayScrollbarPolicy (scrolledWindow, PolicyType.Automatic, PolicyType.Never);
					SetSuppressScrollbar (true);
				} else {
					GtkWorkarounds.SetOverlayScrollbarPolicy (scrolledWindow, PolicyType.Automatic, PolicyType.Automatic);
					SetSuppressScrollbar (false);
				}
				QueueResize ();
			}

			bool suppressScrollbar;

			void SetSuppressScrollbar (bool value)
			{
				if (suppressScrollbar == value)
					return;
				suppressScrollbar = value;

				if (suppressScrollbar) {
					scrolledWindow.VScrollbar.SizeRequested += SuppressSize;
					scrolledWindow.VScrollbar.ExposeEvent += SuppressExpose;
				} else {
					scrolledWindow.VScrollbar.SizeRequested -= SuppressSize;
					scrolledWindow.VScrollbar.ExposeEvent -= SuppressExpose;
				}
			}

			[GLib.ConnectBefore]
			static void SuppressExpose (object o, ExposeEventArgs args)
			{
				args.RetVal = true;
			}

			[GLib.ConnectBefore]
			static void SuppressSize (object o, SizeRequestedArgs args)
			{
				args.Requisition = Requisition.Zero;
				args.RetVal = true;
			}
			
			public void AddQuickTaskProvider (IQuickTaskProvider p)
			{
				p.TasksUpdated += HandleTasksUpdated; 
			}

			void HandleTasksUpdated (object sender, EventArgs e)
			{
				strip.Update ((IQuickTaskProvider)sender);
			}

			public void RemoveQuickTaskProvider (IQuickTaskProvider provider)
			{
				if (provider != null)
					provider.TasksUpdated -= HandleTasksUpdated;
			}	

			public void AddUsageProvider (UsageProviderEditorExtension p)
			{
				p.UsagesUpdated += HandleUsagesUpdated;
			}

			public void RemoveUsageProvider (UsageProviderEditorExtension p)
			{
				p.UsagesUpdated -= HandleUsagesUpdated;
			}	

			void HandleUsagesUpdated (object sender, EventArgs e)
			{
				strip.Update ((UsageProviderEditorExtension)sender);
			}

			protected override void OnDestroyed ()
			{
				if (scrolledWindow == null)
					return;
				if (scrolledWindow.Child != null)
					RemoveEvents ();
				SetSuppressScrollbar (false);
				QuickTaskStrip.EnableFancyFeatures.Changed -= FancyFeaturesChanged;
				scrolledWindow.ButtonPressEvent -= PrepareEvent;
				scrolledWindow.Vadjustment.Destroy ();
				scrolledWindow.Hadjustment.Destroy ();
				scrolledWindow.Destroy ();
				scrolledWindow = null;
				strip.Destroy ();
				strip = null;
				base.OnDestroyed ();
			}
			
			void PrepareEvent (object sender, ButtonPressEventArgs args) 
			{
				args.RetVal = true;
			}
		
			public void SetTextEditor (Mono.TextEditor.MonoTextEditor container)
			{
				scrolledWindow.Child = container;
				this.strip.TextEditor = container;
				container.EditorOptionsChanged += OptionsChanged;
				container.Caret.ModeChanged += parent.UpdateLineColOnEventHandler;
				container.Caret.PositionChanged += parent.CaretPositionChanged;
				container.SelectionChanged += parent.UpdateLineColOnEventHandler;
			}
			
			void OptionsChanged (object sender, EventArgs e)
			{
				var editor = parent?.TextEditor;
				if (editor == null)
					return;
				scrolledBackground.ModifyBg (StateType.Normal, SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Background));
			}
			
			void RemoveEvents ()
			{
				var container = scrolledWindow.Child as Mono.TextEditor.MonoTextEditor;
				if (container == null) {

					LoggingService.LogError ("can't remove events from text editor container.");
					return;
				}
				container.EditorOptionsChanged -= OptionsChanged;
				container.Caret.ModeChanged -= parent.UpdateLineColOnEventHandler;
				container.Caret.PositionChanged -= parent.CaretPositionChanged;
				container.SelectionChanged -= parent.UpdateLineColOnEventHandler;
			}
			
			public Mono.TextEditor.MonoTextEditor RemoveTextEditor ()
			{
				var child = scrolledWindow.Child as Mono.TextEditor.MonoTextEditor;
				if (child == null)
					return null;
				RemoveEvents ();
				scrolledWindow.Remove (child);
				child.Unparent ();
				return child;
			}
		}
		
		public SourceEditorWidget (SourceEditorView view, Mono.TextEditor.TextDocument doc)
		{
			this.view = view;
			vbox.SetSizeRequest (32, 32);
			vbox.Accessible.SetShouldIgnore (true);

			this.lastActiveEditor = this.textEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view, new StyledSourceEditorOptions (DefaultSourceEditorOptions.Instance), doc);
			this.textEditor.TextArea.FocusInEvent += (o, s) => {
				lastActiveEditor = (ExtensibleTextEditor)((TextArea)o).GetTextEditorData ().Parent;
				view.FireCompletionContextChanged ();
			};
			this.textEditor.TextArea.FocusOutEvent += delegate {
				if (this.splittedTextEditor == null || !splittedTextEditor.TextArea.HasFocus)
					OnLostFocus ();
			};
			if (IdeApp.CommandService != null)
				IdeApp.FocusOut += IdeApp_FocusOut;
			mainsw = new DecoratedScrolledWindow (this);
			mainsw.Accessible.SetShouldIgnore (true);
			mainsw.SetTextEditor (textEditor);
			
			vbox.PackStart (mainsw, true, true, 0);
			
			textEditorData = textEditor.GetTextEditorData ();
			textEditorData.EditModeChanged += TextEditorData_EditModeChanged;
			
			UpdateLineCol ();
			//			this.IsClassBrowserVisible = this.widget.TextEditor.Options.EnableQuickFinder;
			vbox.BorderWidth = 0;
			vbox.Spacing = 0;
			vbox.Focused += delegate {
				UpdateLineCol ();
			};
			vbox.Destroyed += delegate {
				if (isDisposed)
					return;
				isDisposed = true;
				StopParseInfoThread ();
				KillWidgets ();

				ClearQuickTaskProvider ();
				ClearUsageTaskProvider ();

				if (textEditor != null && !textEditor.IsDestroyed)
					textEditor.Destroy ();

				if (splittedTextEditor != null && !splittedTextEditor.IsDestroyed)
					splittedTextEditor.Destroy ();
				
				this.lastActiveEditor = null;
				this.splittedTextEditor = null;
				this.textEditor = null;
				textEditorData.EditModeChanged -= TextEditorData_EditModeChanged;
				textEditorData = null;
				view = null;
				parsedDocument = null;

//				IdeApp.Workbench.StatusBar.ClearCaretState ();
			};
			vbox.ShowAll ();

		}

		void TextEditorData_EditModeChanged (object sender, EditModeChangedEventArgs e)
		{
			KillWidgets ();
		}

		void IdeApp_FocusOut (object sender, EventArgs e)
		{
			textEditor.TextArea.HideTooltip ();
		}

		void OnLostFocus ()
		{
		}

		void UpdateLineColOnEventHandler (object sender, EventArgs e)
		{
			this.UpdateLineCol ();
		}

		public void Dispose ()
		{
			if (IdeApp.CommandService != null)
				IdeApp.FocusOut -= IdeApp_FocusOut;

			if (!isDisposed) {
				vbox.Destroy ();
				isDisposed = true;
			}
		}
		
		Mono.TextEditor.FoldSegment AddMarker (List<Mono.TextEditor.FoldSegment> foldSegments, string text, Mono.TextEditor.DocumentRegion region, FoldingType type)
		{
			Document document = textEditorData.Document;
			if (document == null || region.BeginLine <= 0 || region.EndLine <= 0 || region.BeginLine > document.LineCount || region.EndLine > document.LineCount)
				return null;
			
			int startOffset = document.LocationToOffset (region.BeginLine, region.BeginColumn);
			int endOffset   = document.LocationToOffset (region.EndLine, region.EndColumn );
			
			var result = new Mono.TextEditor.FoldSegment (text, startOffset, endOffset - startOffset, type);
			
			foldSegments.Add (result);
			return result;
		}
		bool reloadSettings;
		
		void HandleParseInformationUpdaterWorkerThreadDoWork (bool firstTime, ParsedDocument parsedDocument, CancellationToken token = default(CancellationToken))
		{


			if (reloadSettings) {
				reloadSettings = false;
				Application.Invoke ((o, args) => {
					if (isDisposed)
						return;
					view.LoadSettings ();
					mainsw.QueueDraw ();
				});
			}

		}

		internal void UpdateParsedDocument (ParsedDocument document)
		{
			if (this.isDisposed || document == null || this.view == null)
				return;
			
			SetParsedDocument (document, parsedDocument != null);
		}
		
		public ParsedDocument ParsedDocument {
			get {
				return this.parsedDocument;
			}
			set {
				SetParsedDocument (value, true);
			}
		}
		CancellationTokenSource parserInformationUpdateSrc = new CancellationTokenSource ();
		internal void SetParsedDocument (ParsedDocument newDocument, bool runInThread)
		{
			this.parsedDocument = newDocument;
			if (parsedDocument == null)
				return;
			StopParseInfoThread ();
			if (runInThread) {
				var token = parserInformationUpdateSrc.Token;
				System.Threading.Tasks.Task.Run (delegate {
					HandleParseInformationUpdaterWorkerThreadDoWork (false, parsedDocument, token);
				}); 
			} else {
				HandleParseInformationUpdaterWorkerThreadDoWork (true, parsedDocument);
			}
		}
		
		void StopParseInfoThread ()
		{
			parserInformationUpdateSrc.Cancel ();
			parserInformationUpdateSrc = new CancellationTokenSource ();
		}

		internal void SetLastActiveEditor (ExtensibleTextEditor editor)
		{
			this.lastActiveEditor = editor;
		}

		Gtk.Paned splitContainer;

		public bool IsSplitted {
			get {
				return splitContainer != null;
			}
		}
		
		public bool EditorHasFocus {
			get {
				return TextEditor.TextArea.HasFocus;
			}
		}

		public SourceEditorView View {
			get {
				return view;
			}
			set {
				view = value;
			}
		}
		
		public void Unsplit ()
		{
			if (splitContainer == null)
				return;
			double vadjustment = mainsw.Vadjustment.Value;
			double hadjustment = mainsw.Hadjustment.Value;
			
			splitContainer.Remove (mainsw);
			secondsw.Destroy ();
			secondsw = null;
			splittedTextEditor = null;
			
			vbox.Remove (splitContainer);
			splitContainer.Destroy ();
			splitContainer = null;
			
			RecreateMainSw ();
			vbox.PackStart (mainsw, true, true, 0);
			vbox.ShowAll ();
			mainsw.Vadjustment.Value = vadjustment; 
			mainsw.Hadjustment.Value = hadjustment;
		}
		
		public void SwitchWindow ()
		{
			if (splittedTextEditor.HasFocus) {
				this.textEditor.GrabFocus ();
			} else {
				this.splittedTextEditor.GrabFocus ();
			}
		}
		DecoratedScrolledWindow secondsw;

		public void Split (bool vSplit)
		{
			double vadjustment = this.mainsw.Vadjustment.Value;
			double hadjustment = this.mainsw.Hadjustment.Value;
			
			if (splitContainer != null)
				Unsplit ();
			vbox.Remove (this.mainsw);
			
			RecreateMainSw ();

			this.splitContainer = vSplit ? (Gtk.Paned)new VPaned () : (Gtk.Paned)new HPaned ();

			splitContainer.Add1 (mainsw);

			this.splitContainer.ButtonPressEvent += delegate(object sender, ButtonPressEventArgs args) {
				if (args.Event.Type == Gdk.EventType.TwoButtonPress && args.RetVal == null) {
					Unsplit ();
				}
			};
			secondsw = new DecoratedScrolledWindow (this);
			splittedTextEditor = new MonoDevelop.SourceEditor.ExtensibleTextEditor (view, textEditor.Options, textEditor.Document);
			splittedTextEditor.TextArea.FocusInEvent += (o, s) => {
				lastActiveEditor = (ExtensibleTextEditor)((TextArea)o).GetTextEditorData ().Parent;
				view.FireCompletionContextChanged ();
			};
			splittedTextEditor.TextArea.FocusOutEvent += delegate {
				 if (!textEditor.TextArea.HasFocus)
					OnLostFocus ();
			};
			splittedTextEditor.EditorExtension = textEditor.EditorExtension;
			if (textEditor.GetTextEditorData ().HasIndentationTracker)
				splittedTextEditor.GetTextEditorData ().IndentationTracker = textEditor.GetTextEditorData ().IndentationTracker;

			secondsw.SetTextEditor (splittedTextEditor);
			splitContainer.Add2 (secondsw);
			
			vbox.PackStart (splitContainer, true, true, 0);
			splitContainer.Position = (vSplit ? vbox.Allocation.Height : vbox.Allocation.Width) / 2 - 1;
			
			vbox.ShowAll ();
			secondsw.Vadjustment.Value = mainsw.Vadjustment.Value = vadjustment; 
			secondsw.Hadjustment.Value = mainsw.Hadjustment.Value = hadjustment;
		}

		void RecreateMainSw ()
		{
			// destroy old scrolled window to work around Bug 526721 - When splitting window vertically, 
			// the slider under left split is not shown unitl window is resized
			double vadjustment = mainsw.Vadjustment.Value;
			double hadjustment = mainsw.Hadjustment.Value;
			
			var removedTextEditor = mainsw.RemoveTextEditor ();
			mainsw.Destroy ();
			
			mainsw = new DecoratedScrolledWindow (this);
			mainsw.SetTextEditor (removedTextEditor);
			mainsw.Vadjustment.Value = vadjustment; 
			mainsw.Hadjustment.Value = hadjustment;
			lastActiveEditor = textEditor;
		}

		
//		void SplitContainerSizeRequested (object sender, SizeRequestedArgs args)
//		{
//			this.splitContainer.SizeRequested -= SplitContainerSizeRequested;
//			this.splitContainer.Position = args.Requisition.Width / 2;
//			this.splitContainer.SizeRequested += SplitContainerSizeRequested;
//		}
//		
		MonoDevelop.Components.InfoBar messageBar = null;
		
		internal static string EllipsizeMiddle (string str, int truncLen)
		{
			if (str == null) 
				return "";
			if (str.Length <= truncLen) 
				return str;
			
			string delimiter = "...";
			int leftOffset = (truncLen - delimiter.Length) / 2;
			int rightOffset = str.Length - truncLen + leftOffset + delimiter.Length;
			return str.Substring (0, leftOffset) + delimiter + str.Substring (rightOffset);
		}
		
		public void ShowFileChangedWarning (bool multiple)
		{
			RemoveMessageBar ();
			
			if (messageBar == null) {
				messageBar = new MonoDevelop.Components.InfoBar (MessageType.Warning);
				messageBar.SetMessageLabel (GettextCatalog.GetString (
					"<b>The file \"{0}\" has been changed outside of {1}.</b>\n" +
					"Do you want to keep your changes, or reload the file from disk?",
					EllipsizeMiddle (Document.FileName, 50), BrandingService.ApplicationName));
				
				var b1 = new Button (GettextCatalog.GetString ("_Reload from disk"));
				b1.Image = new ImageView (Gtk.Stock.Refresh, IconSize.Button);
				b1.Clicked += delegate {
					Reload ();
					view.TextEditor.GrabFocus ();
				};
				messageBar.ActionArea.Add (b1);
				
				var b2 = new Button (GettextCatalog.GetString ("_Keep changes"));
				b2.Image = new ImageView (Gtk.Stock.Cancel, IconSize.Button);
				b2.Clicked += delegate {
					RemoveMessageBar ();
					view.WorkbenchWindow.ShowNotification = false;
				};
				messageBar.ActionArea.Add (b2);

				if (multiple) {
					var b3 = new Button (GettextCatalog.GetString ("_Reload all"));
					b3.Image = new ImageView (Gtk.Stock.Cancel, IconSize.Button);
					b3.Clicked += delegate {
						DocumentRegistry.ReloadAllChangedFiles ();
					};
					messageBar.ActionArea.Add (b3);
	
					var b4 = new Button (GettextCatalog.GetString ("_Ignore all"));
					b4.Image = new ImageView (Gtk.Stock.Cancel, IconSize.Button);
					b4.Clicked += delegate {
						DocumentRegistry.IgnoreAllChangedFiles ();
					};
					messageBar.ActionArea.Add (b4);
				}
			}
			
			view.IsDirty = true;
			view.WarnOverwrite = true;
			vbox.PackStart (messageBar, false, false, CHILD_PADDING);
			vbox.ReorderChild (messageBar, 0);
			messageBar.ShowAll ();

			messageBar.QueueDraw ();
			
			view.WorkbenchWindow.ShowNotification = true;
		}
		
		#region Eol marker check
		internal bool UseIncorrectMarkers { get; set; }
		internal bool HasIncorrectEolMarker {
			get {
				var document = Document;
				if (document == null)
					return false;
				if (document.HasLineEndingMismatchOnTextSet)
					return true;
				string eol = DetectedEolMarker;
				if (eol == null)
					return false;
				return eol != textEditor.Options.DefaultEolMarker;
			}
		}
		string DetectedEolMarker {
			get {
				if (Document.HasLineEndingMismatchOnTextSet)
					return "?";
				if (textEditor.IsDisposed) {
					LoggingService.LogWarning ("SourceEditorWidget.cs: HasIncorrectEolMarker was called on disposed source editor widget." + Environment.NewLine + Environment.StackTrace);
					return null;
				}
				var firstLine = Document.GetLine (1);
				if (firstLine != null && firstLine.DelimiterLength > 0) {
					string firstDelimiter = Document.GetTextAt (firstLine.Length, firstLine.DelimiterLength);
					return firstDelimiter;
				}
				return null;
			}
		}

		internal void UpdateEolMarkerMessage (bool multiple)
		{
			if (UseIncorrectMarkers || DefaultSourceEditorOptions.Instance.LineEndingConversion == LineEndingConversion.LeaveAsIs)
				return;
			ShowIncorrectEolMarkers (Document.FileName, multiple);
		}

		internal bool EnsureCorrectEolMarker (string fileName)
		{
			if (UseIncorrectMarkers || DefaultSourceEditorOptions.Instance.LineEndingConversion == LineEndingConversion.LeaveAsIs)
				return true;
			if (HasIncorrectEolMarker) {
				switch (DefaultSourceEditorOptions.Instance.LineEndingConversion) {
				case LineEndingConversion.Ask:
					var hasMultipleIncorrectEolMarkers = FileRegistry.HasMultipleIncorrectEolMarkers;
					ShowIncorrectEolMarkers (fileName, hasMultipleIncorrectEolMarkers);
					if (hasMultipleIncorrectEolMarkers) {
						FileRegistry.UpdateEolMessages ();
					}
					return false;
				case LineEndingConversion.ConvertAlways:
					ConvertLineEndings ();
					return true;
				default:
					return true;
				}
			}
			return true;
		}

		internal void ConvertLineEndings ()
		{
			string correctEol = TextEditor.Options.DefaultEolMarker;
			var newText = StringBuilderCache.Allocate ();
			int offset = 0;
			foreach (var line in Document.Lines) {
				newText.Append (TextEditor.GetTextAt (offset, line.Length));
				offset += line.LengthIncludingDelimiter;
				if (line.DelimiterLength > 0)
					newText.Append (correctEol);
			}
			view.StoreSettings ();
			view.ReplaceContent (Document.FileName, StringBuilderCache.ReturnAndFree (newText), view.SourceEncoding);
			Document.HasLineEndingMismatchOnTextSet = false;
			view.LoadSettings ();
		}

		static string GetEolString (string detectedEol)
		{
			switch (detectedEol) {
			case "\n":
				return "UNIX";
			case "\r\n":
				return "Windows";
			case "\r":
				return "Mac";
			case "?":
				return "mixed";
			}
			return "Unknown";
		}

		//TODO: Support multiple Overlays at once to display above each other
		internal void AddOverlay (Widget messageOverlayContent, Func<int> sizeFunc = null)
		{
			var messageOverlayWindow = new OverlayMessageWindow ();
			messageOverlayWindow.Child = messageOverlayContent;
			messageOverlayWindow.SizeFunc = sizeFunc;
			messageOverlayWindow.ShowOverlay (TextEditor);
			messageOverlayWindows.Add (messageOverlayWindow);
		}

		internal void RemoveOverlay (Widget messageOverlayContent)
		{
			var window = messageOverlayWindows.FirstOrDefault (w => w.Child == messageOverlayContent);
			if (window == null)
				return;
			messageOverlayWindows.Remove (window);
			window.Destroy ();
		}

		List<OverlayMessageWindow> messageOverlayWindows = new List<OverlayMessageWindow> ();
		HBox incorrectEolMessage;

		void ShowIncorrectEolMarkers (string fileName, bool multiple)
		{
			RemoveMessageBar ();
			var hbox = new HBox ();
			hbox.Spacing = 8;

			var image = new HoverCloseButton ();
			hbox.PackStart (image, false, false, 0);
			var label = new Label (GettextCatalog.GetString ("This file has line endings ({0}) which differ from the policy settings ({1}).", GetEolString (DetectedEolMarker), GetEolString (textEditor.Options.DefaultEolMarker)));
			var color = (HslColor)SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.NotificationText);
			label.ModifyFg (StateType.Normal, color);

			int w, h;
			label.Layout.GetPixelSize (out w, out h);
			label.Ellipsize = Pango.EllipsizeMode.End;

			hbox.PackStart (label, true, true, 0);
			var okButton = new Button (Gtk.Stock.Ok);
			okButton.WidthRequest = 60;

			// Small amount of vertical padding for the OK button.
			const int verticalPadding = 2;
			var vbox = new VBox ();
			vbox.PackEnd (okButton, true, true, verticalPadding);
			hbox.PackEnd (vbox, false, false, 0);

			var list = new string [] {
				GettextCatalog.GetString ("Convert to {0} line endings", GetEolString (textEditor.Options.DefaultEolMarker)),
				GettextCatalog.GetString ("Convert all files to {0} line endings", GetEolString (textEditor.Options.DefaultEolMarker)),
				GettextCatalog.GetString ("Keep {0} line endings", GetEolString (DetectedEolMarker)),
				GettextCatalog.GetString ("Keep {0} line endings in all files", GetEolString (DetectedEolMarker))
			};
			var combo = new ComboBox (list);
			combo.Active = 0;
			hbox.PackEnd (combo, false, false, 0);
			incorrectEolMessage = new HBox ();
			const int containerPadding = 8;
			incorrectEolMessage.PackStart (hbox, true, true, containerPadding); 

			// This is hacky, but it will ensure that our combo appears with with the correct size.
			GLib.Timeout.Add (100, delegate {
				combo.QueueResize ();
				return false;
			});

			AddOverlay (incorrectEolMessage, () => {
				return okButton.SizeRequest ().Width +
							   combo.SizeRequest ().Width +
							   image.SizeRequest ().Width +
							   w +
							   hbox.Spacing * 4 +
							   containerPadding * 2;
			});

			image.Clicked += delegate {
				UseIncorrectMarkers = true;
				view.WorkbenchWindow.ShowNotification = false;
				RemoveMessageBar ();
			};
			okButton.Clicked += async delegate {
				switch (combo.Active) {
				case 0:
					ConvertLineEndings ();
					view.WorkbenchWindow.ShowNotification = false;
					await view.Save (fileName, view.SourceEncoding);
					break;
				case 1:
					FileRegistry.ConvertLineEndingsInAllFiles ();
					break;
				case 2:
					UseIncorrectMarkers = true;
					view.WorkbenchWindow.ShowNotification = false;
					break;
				case 3:
					FileRegistry.IgnoreLineEndingsInAllFiles ();
					break;
				}
				RemoveMessageBar ();
			};
		}
		#endregion
		public void ShowAutoSaveWarning (string fileName)
		{
			RemoveMessageBar ();
			TextEditor.Visible = false;
			if (messageBar == null) {
				messageBar = new MonoDevelop.Components.InfoBar (MessageType.Warning);
				messageBar.SetMessageLabel (BrandingService.BrandApplicationName (GettextCatalog.GetString (
						"<b>An autosave file has been found for this file.</b>\n" +
						"This could mean that another instance of MonoDevelop is editing this " +
						"file, or that MonoDevelop crashed with unsaved changes.\n\n" +
					    "Do you want to use the original file, or load from the autosave file?")));
				
				Button b1 = new Button (GettextCatalog.GetString("_Use original file"));
				b1.Image = new ImageView (Gtk.Stock.Refresh, IconSize.Button);
				b1.Clicked += delegate {
					try {
						AutoSave.RemoveAutoSaveFile (fileName);
						TextEditor.GrabFocus ();
						view.Load (fileName);
						view.WorkbenchWindow.Document.ReparseDocument ();
					} catch (Exception ex) {
						LoggingService.LogError ("Could not remove the autosave file.", ex);
					} finally {
						RemoveMessageBar ();
					}
				};
				messageBar.ActionArea.Add (b1);
				
				Button b2 = new Button (GettextCatalog.GetString("_Load from autosave"));
				b2.Image = new ImageView (Gtk.Stock.RevertToSaved, IconSize.Button);
				b2.Clicked += delegate {
					try {
						var content = AutoSave.LoadAndRemoveAutoSave (fileName);
						TextEditor.GrabFocus ();
						view.Load (fileName);
						view.ReplaceContent (fileName, content.Text, view.SourceEncoding);
						view.WorkbenchWindow.Document.ReparseDocument ();
						view.IsDirty = true;
					} catch (Exception ex) {
						LoggingService.LogError ("Could not remove the autosave file.", ex);
					} finally {
						RemoveMessageBar ();
					}
					
				};
				messageBar.ActionArea.Add (b2);
			}
			
			view.IsDirty = true;
			view.WarnOverwrite = true;
			vbox.PackStart (messageBar, false, false, CHILD_PADDING);
			vbox.ReorderChild (messageBar, 0);
			messageBar.ShowAll ();

			messageBar.QueueDraw ();
			
//			view.WorkbenchWindow.ShowNotification = true;
		}
		
		
		public void RemoveMessageBar ()
		{
			if (messageBar != null) {
				if (messageBar.Parent == vbox)
					vbox.Remove (messageBar);
				messageBar.Destroy ();
				messageBar = null;
			}
			if (!TextEditor.Visible)
				TextEditor.Visible = true;
			if (incorrectEolMessage != null) {
				RemoveOverlay (incorrectEolMessage);
				incorrectEolMessage = null;
			}
		}

		public async void Reload ()
		{
			try {
				if (!System.IO.File.Exists (view.ContentName) || this.isDisposed)
					return;

				view.StoreSettings ();
				reloadSettings = true;
				await view.Load (view.ContentName, view.SourceEncoding, true);
				view.WorkbenchWindow.ShowNotification = false;
			} catch (Exception ex) {
				MessageService.ShowError ("Could not reload the file.", ex);
			} finally {
				RemoveMessageBar ();
			}
		}

		#region Status Bar Handling
		MonoDevelop.SourceEditor.MessageBubbleTextMarker oldExpandedMarker;
		void CaretPositionChanged (object o, DocumentLocationEventArgs args)
		{
			UpdateLineCol ();
			DocumentLine curLine = TextEditor.Document.GetLine (TextEditor.Caret.Line);
			MonoDevelop.SourceEditor.MessageBubbleTextMarker marker = null;

			if (curLine != null && TextEditor.Document.GetMarkers (curLine).Any (m => m is MonoDevelop.SourceEditor.MessageBubbleTextMarker)) {
				marker = (MonoDevelop.SourceEditor.MessageBubbleTextMarker)TextEditor.Document.GetMarkers (curLine).First (m => m is MonoDevelop.SourceEditor.MessageBubbleTextMarker);
//				marker.CollapseExtendedErrors = false;
			}
			
			if (oldExpandedMarker != null && oldExpandedMarker != marker) {
//				oldExpandedMarker.CollapseExtendedErrors = true;
			}
			oldExpandedMarker = marker;
		}
		
//		void OnChanged (object o, EventArgs e)
//		{
//			UpdateLineCol ();
//			OnContentChanged (null);
//			needsUpdate = true;
//		}
		
		internal void UpdateLineCol ()
		{
//			int offset = TextEditor.Caret.Offset;
//			if (offset < 0 || offset > TextEditor.Document.TextLength)
//				return;
//			DocumentLocation location = TextEditor.LogicalToVisualLocation (TextEditor.Caret.Location);
//			IdeApp.Workbench.StatusBar.ShowCaretState (TextEditor.Caret.Line,
//			                                           location.Column,
//			                                           TextEditor.IsSomethingSelected ? TextEditor.SelectionRange.Length : 0,
//			                                           TextEditor.Caret.IsInInsertMode);
		}
		
		#endregion
		
		#region Search and Replace
		Components.RoundedFrame searchAndReplaceWidgetFrame = null;
		SearchAndReplaceWidget searchAndReplaceWidget = null;
		Components.RoundedFrame gotoLineNumberWidgetFrame = null;
		GotoLineNumberWidget   gotoLineNumberWidget   = null;
		
		bool KillWidgets ()
		{
			bool result = false;
			if (searchAndReplaceWidgetFrame != null) {
				searchAndReplaceWidgetFrame.Destroy ();
				searchAndReplaceWidgetFrame = null;
				searchAndReplaceWidget = null;
				result = true;
				//clears any message it may have set
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
			
			if (gotoLineNumberWidgetFrame != null) {
				gotoLineNumberWidgetFrame.Destroy ();
				gotoLineNumberWidgetFrame = null;
				gotoLineNumberWidget = null;
				result = true;
			}
			
			if (this.textEditor != null) 
				this.textEditor.HighlightSearchPattern = false;
			if (this.splittedTextEditor != null) 
				this.splittedTextEditor.HighlightSearchPattern = false;
			
			return result;
		}
		
		internal bool RemoveSearchWidget ()
		{
			bool result = KillWidgets ();
			if (!isDisposed)
				TextEditor.GrabFocus ();
			return result;
		}
		
		public void EmacsFindNext ()
		{
			if (searchAndReplaceWidget == null) {
				ShowSearchWidget ();
			} else {
				this.FindNext ();
			}
		}
		
		public void EmacsFindPrevious ()
		{
			if (searchAndReplaceWidget == null) {
				ShowSearchWidget ();
			} else {
				this.FindPrevious ();
			}
		}
		
		public void ShowSearchWidget ()
		{
			ShowSearchReplaceWidget (false);
		}
		
		public void ShowReplaceWidget ()
		{
			ShowSearchReplaceWidget (true);
		}
		
		internal void OnUpdateUseSelectionForFind (CommandInfo info)
		{
			info.Enabled = searchAndReplaceWidget != null && TextEditor.IsSomethingSelected;
		}
		
		public void UseSelectionForFind ()
		{
			SetSearchPatternToSelection ();
		}
		
		internal void OnUpdateUseSelectionForReplace (CommandInfo info)
		{
			info.Enabled = searchAndReplaceWidget != null && TextEditor.IsSomethingSelected;
		}
		
		public void UseSelectionForReplace ()
		{
			SetReplacePatternToSelection ();
		}

		void ShowSearchReplaceWidget (bool replace, bool switchFocus = true)
		{
			if (searchAndReplaceWidget == null) {
				KillWidgets ();
				searchAndReplaceWidgetFrame = new RoundedFrame ();
				//searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (widget.TextEditor.ColorStyle.Default.BackgroundColor));
				searchAndReplaceWidgetFrame.SetFillColor (CairoExtensions.GdkColorToCairoColor (vbox.Style.Background (StateType.Normal)));
				
				searchAndReplaceWidgetFrame.Child = searchAndReplaceWidget = new SearchAndReplaceWidget (TextEditor, searchAndReplaceWidgetFrame);
				searchAndReplaceWidget.Destroyed += (sender, e) => RemoveSearchWidget ();
				searchAndReplaceWidgetFrame.ShowAll ();
				this.TextEditor.AddAnimatedWidget (searchAndReplaceWidgetFrame, ANIMATION_DURATION, Mono.TextEditor.Theatrics.Easing.ExponentialInOut, Blocking.Downstage, TextEditor.Allocation.Width - 400, -searchAndReplaceWidget.Allocation.Height);
//				this.PackEnd (searchAndReplaceWidget);
//				this.SetChildPacking (searchAndReplaceWidget, false, false, CHILD_PADDING, PackType.End);
				//		searchAndReplaceWidget.ShowAll ();
				if (this.splittedTextEditor != null) {
					this.splittedTextEditor.HighlightSearchPattern = true;
					this.splittedTextEditor.TextViewMargin.RefreshSearchMarker ();
				}
			} else {
				if (TextEditor.IsSomethingSelected) {
					searchAndReplaceWidget.SetSearchPattern ();
				}
			}
			searchAndReplaceWidget.UpdateSearchPattern ();
			searchAndReplaceWidget.IsReplaceMode = replace;
			if (searchAndReplaceWidget.SearchFocused) {
				if (replace) {
					searchAndReplaceWidget.Replace ();
				} else {
					this.FindNext ();
				}
			}
			if (switchFocus)
				searchAndReplaceWidget.Focus ();
		}
		
		public void ShowGotoLineNumberWidget ()
		{
			if (gotoLineNumberWidget == null) {
				KillWidgets ();
				
				
				gotoLineNumberWidgetFrame = new MonoDevelop.Components.RoundedFrame ();
				//searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (widget.TextEditor.ColorStyle.Default.BackgroundColor));
				gotoLineNumberWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (vbox.Style.Background (StateType.Normal)));
				
				gotoLineNumberWidgetFrame.Child = gotoLineNumberWidget = new GotoLineNumberWidget (textEditor, gotoLineNumberWidgetFrame);
				gotoLineNumberWidget.Destroyed += (sender, e) => RemoveSearchWidget ();
				gotoLineNumberWidgetFrame.ShowAll ();
				TextEditor.AddAnimatedWidget (gotoLineNumberWidgetFrame, ANIMATION_DURATION, Mono.TextEditor.Theatrics.Easing.ExponentialInOut, Mono.TextEditor.Theatrics.Blocking.Downstage, this.TextEditor.Allocation.Width - 400, -gotoLineNumberWidget.Allocation.Height);
			}
			
			gotoLineNumberWidget.Focus ();
		}
		

		public SearchResult FindNext ()
		{
			return FindNext (true);
		}
		
		public SearchResult FindNext (bool focus)
		{
			if (searchAndReplaceWidget == null)
				ShowSearchReplaceWidget (false, false);
			return SearchAndReplaceWidget.FindNext (TextEditor);
		}
		
		public SearchResult FindPrevious ()
		{
			return FindPrevious (true);
		}
		
		public SearchResult FindPrevious (bool focus)
		{
			if (searchAndReplaceWidget == null)
				ShowSearchReplaceWidget (false, false);
			return SearchAndReplaceWidget.FindPrevious (TextEditor);
		}

		internal static string FormatPatternToSelectionOption (string pattern)
		{
			return MonoDevelop.Ide.FindInFiles.FindInFilesDialog.FormatPatternToSelectionOption (pattern, SearchAndReplaceWidget.SearchEngine == SearchAndReplaceWidget.RegexSearchEngine);
		}
		
		void SetSearchPatternToSelection ()
		{
			if (!TextEditor.IsSomethingSelected) {
				int start = textEditor.Caret.Offset;
				int end = start;
				while (start - 1 >= 0 && DynamicAbbrevHandler.IsIdentifierPart (textEditor.GetCharAt (start - 1)))
					start--;

				while (end < textEditor.Length && DynamicAbbrevHandler.IsIdentifierPart (textEditor.GetCharAt (end)))
					end++;
				textEditor.Caret.Offset = end;
				TextEditor.SetSelection (start, end);
			}

			if (TextEditor.IsSomethingSelected) {
				var pattern = FormatPatternToSelectionOption (TextEditor.SelectedText);
				SearchAndReplaceOptions.SearchPattern = pattern;
				SearchAndReplaceWidget.UpdateSearchHistory (TextEditor.SearchPattern);
			}
			if (searchAndReplaceWidget != null)
				searchAndReplaceWidget.UpdateSearchPattern ();
		}
		
		void SetReplacePatternToSelection ()
		{
			if (searchAndReplaceWidget != null && TextEditor.IsSomethingSelected)
				searchAndReplaceWidget.ReplacePattern = TextEditor.SelectedText;
		}
		
		public SearchResult FindNextSelection ()
		{
			SetSearchPatternToSelection ();
			TextEditor.GrabFocus ();
			return FindNext ();
		}
	
		public SearchResult FindPreviousSelection ()
		{
			SetSearchPatternToSelection ();
			TextEditor.GrabFocus ();
			return FindPrevious ();
		}
		
		#endregion
	
		public Mono.TextEditor.TextDocument Document {
			get {
				var editor = TextEditor;
				if (editor == null)
					return null;
				return editor.Document;
			}
		}
		
		#region Help
		internal void MonodocResolver ()
		{
			MonoDevelop.Ide.Editor.DocumentRegion region;
			var res = TextEditor.GetLanguageItem (TextEditor.Caret.Offset, out region);
			string url = HelpService.GetMonoDocHelpUrl (res);
			if (url != null)
				IdeApp.HelpOperations.ShowHelp (url);
		}
		
		internal void MonodocResolverUpdate (CommandInfo cinfo)
		{
			MonoDevelop.Ide.Editor.DocumentRegion region;
			var res = TextEditor.GetLanguageItem (TextEditor.Caret.Offset, out region);
			if (HelpService.GetMonoDocHelpUrl (res) == null)
				cinfo.Bypass = true;
		}
		
		#endregion
		
		#region commenting and indentation
	
		public void OnUpdateToggleErrorTextMarker (CommandInfo info)
		{
			DocumentLine line = TextEditor.Document.GetLine (TextEditor.Caret.Line);
			if (line == null) {
				info.Visible = false;
				return;
			}
			var marker = (MessageBubbleTextMarker)TextEditor.Document.GetMarkers (line).FirstOrDefault (m => m is MessageBubbleTextMarker);
			info.Visible = marker != null;
		}
		
		public void OnToggleErrorTextMarker ()
		{
			DocumentLine line = TextEditor.Document.GetLine (TextEditor.Caret.Line);
			if (line == null)
				return;
			var marker = (MessageBubbleTextMarker)TextEditor.Document.GetMarkers (line).FirstOrDefault (m => m is MessageBubbleTextMarker);
			if (marker != null) {
				marker.IsVisible = !marker.IsVisible;
				TextEditor.QueueDraw ();
			}
		}
	
		#endregion

		internal void NextIssue ()
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return;
			mainsw.Strip.GotoTask (mainsw.Strip.SearchNextTask (QuickTaskStrip.HoverMode.NextMessage));
		}	

		internal void PrevIssue ()
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return;
			mainsw.Strip.GotoTask (mainsw.Strip.SearchPrevTask (QuickTaskStrip.HoverMode.NextMessage));
		}

		internal void NextIssueError ()
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return;
			mainsw.Strip.GotoTask (mainsw.Strip.SearchNextTask (QuickTaskStrip.HoverMode.NextError));
		}	

		internal void PrevIssueError ()
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return;
			mainsw.Strip.GotoTask (mainsw.Strip.SearchPrevTask (QuickTaskStrip.HoverMode.NextError));
		}


		#region IServiceProvider implementation
		object IServiceProvider.GetService (Type serviceType)
		{
			return view.GetContent (serviceType);
		}
		#endregion
	}

}
