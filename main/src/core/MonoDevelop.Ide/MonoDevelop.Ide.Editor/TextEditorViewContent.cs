//
// TextEditorViewContent.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using System.IO;
using MonoDevelop.Core.Text;
using System.Text;
using Gtk;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using MonoDevelop.Ide.Editor.Extension;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Components;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System.Threading;
using System.Threading;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// The TextEditor object needs to be available through IBaseViewContent.GetContent therefore we need to insert a 
	/// decorator in between.
	/// </summary>
	class TextEditorViewContent : IViewContent, ICommandRouter, IQuickTaskProvider
	{
		readonly TextEditor textEditor;
		readonly ITextEditorImpl textEditorImpl;

		DocumentContext currentContext;
		MonoDevelop.Projects.Policies.PolicyContainer policyContainer;

		public TextEditorViewContent (TextEditor textEditor, ITextEditorImpl textEditorImpl)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");
			if (textEditorImpl == null)
				throw new ArgumentNullException ("textEditorImpl");
			this.textEditor = textEditor;
			this.textEditorImpl = textEditorImpl;
			this.textEditor.MimeTypeChanged += UpdateTextEditorOptions;
			this.textEditor.TextChanged += HandleTextChanged;
			DefaultSourceEditorOptions.Instance.Changed += UpdateTextEditorOptions;
			this.textEditorImpl.DirtyChanged += HandleDirtyChanged;
			this.textEditor.DocumentContextChanged += delegate {
				if (currentContext != null)
					currentContext.DocumentParsed -= HandleDocumentParsed;
				currentContext = textEditor.DocumentContext;
				currentContext.DocumentParsed += HandleDocumentParsed;
			};
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			InformAutoSave ();
		}

		void HandleTextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			InformAutoSave ();
		}

		void UpdateTextEditorOptions (object sender, EventArgs e)
		{
			UpdateStyleParent (Project, textEditor.MimeType);
		}

		uint autoSaveTimer = 0;

		void InformAutoSave ()
		{
			RemoveAutoSaveTimer ();
			autoSaveTimer = GLib.Timeout.Add (500, delegate {
				AutoSave.InformAutoSaveThread (textEditor.CreateSnapshot (), textEditor.FileName, textEditorImpl.IsDirty);
				autoSaveTimer = 0;
				return false;
			});
		}


		void RemoveAutoSaveTimer ()
		{
			if (autoSaveTimer == 0)
				return;
			GLib.Source.Remove (autoSaveTimer);
			autoSaveTimer = 0;
		}

		void RemovePolicyChangeHandler ()
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
		}

		void UpdateStyleParent (Project styleParent, string mimeType)
		{
			RemovePolicyChangeHandler ();

			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";

			var mimeTypes = DesktopService.GetMimeTypeInheritanceChain (mimeType);

			if (styleParent != null)
				policyContainer = styleParent.Policies;
			else
				policyContainer = MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies;
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);

			policyContainer.PolicyChanged += HandlePolicyChanged;
			textEditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (currentPolicy);
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			var mimeTypes = DesktopService.GetMimeTypeInheritanceChain (textEditor.MimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			textEditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (currentPolicy);
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			UpdateErrorUndelines (currentContext.ParsedDocument);
			UpdateQuickTasks (currentContext.ParsedDocument);
			UpdateFoldings (currentContext.ParsedDocument);
		}

		#region Error handling
		List<IErrorMarker> errors = new List<IErrorMarker> ();
		uint resetTimerId;

		void RemoveErrorUndelinesResetTimerId ()
		{
			if (resetTimerId > 0) {
				GLib.Source.Remove (resetTimerId);
				resetTimerId = 0;
			}
		}

		void RemoveErrorUnderlines ()
		{
			errors.ForEach (err => textEditor.RemoveMarker (err));
			errors.Clear ();
		}

		void UnderLineError (Error info)
		{
			var error = TextMarkerFactory.CreateErrorMarker (textEditor, info);
			textEditor.AddMarker (error); 
			errors.Add (error);
		}

		void UpdateErrorUndelines (ParsedDocument parsedDocument)
		{
			if (!DefaultSourceEditorOptions.Instance.UnderlineErrors || parsedDocument == null)
				return;

			Application.Invoke (delegate {
				RemoveErrorUndelinesResetTimerId ();
				const uint timeout = 500;
				resetTimerId = GLib.Timeout.Add (timeout, delegate {
					RemoveErrorUnderlines ();

					// Else we underline the error
					if (parsedDocument.Errors != null) {
						foreach (var error in parsedDocument.Errors) {
							UnderLineError (error);
						}
					}
					resetTimerId = 0;
					return false;
				});
			});
		}
		#endregion
		HashSet<string> symbols = new HashSet<string> ();
		CancellationTokenSource src = new CancellationTokenSource ();
		void UpdateFoldings (ParsedDocument parsedDocument, bool firstTime = false)
		{
			if (parsedDocument == null || !textEditor.Options.ShowFoldMargin)
				return;
			// don't update parsed documents that contain errors - the foldings from there may be invalid.
			if (parsedDocument.HasErrors)
				return;
			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			System.Action action = delegate {
				try {
					var foldSegments = new List<IFoldSegment> ();
					bool updateSymbols = parsedDocument.Defines.Count != symbols.Count;
					if (!updateSymbols) {
						foreach (PreProcessorDefine define in parsedDocument.Defines) {
							if (token.IsCancellationRequested)
								return;
							if (!symbols.Contains (define.Define)) {
								updateSymbols = true;
								break;
							}
						}
					}
					if (updateSymbols) {
						symbols.Clear ();
						foreach (PreProcessorDefine define in parsedDocument.Defines) {
							symbols.Add (define.Define);
						}
					}
					foreach (FoldingRegion region in parsedDocument.Foldings) {
						if (token.IsCancellationRequested)
							return;
						var type = FoldingType.Unknown;
						bool setFolded = false;
						bool folded = false;
						//decide whether the regions should be folded by default
						switch (region.Type) {
						case FoldType.Member:
							type = FoldingType.TypeMember;
							break;
						case FoldType.Type:
							type = FoldingType.TypeDefinition;
							break;
						case FoldType.UserRegion:
							type = FoldingType.Region;
							setFolded = DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
							folded = true;
							break;
						case FoldType.Comment:
							type = FoldingType.Comment;
							setFolded = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
							folded = true;
							break;
						case FoldType.CommentInsideMember:
							type = FoldingType.Comment;
							setFolded = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
							folded = false;
							break;
						case FoldType.Undefined:
							setFolded = true;
							folded = region.IsFoldedByDefault;
							break;
						}
						var start = textEditor.LocationToOffset (region.Region.Begin);
						var end = textEditor.LocationToOffset (region.Region.End);
						var marker = textEditor.CreateFoldSegment (start, end - start);
						foldSegments.Add (marker);
						marker.CollapsedText = region.Name;
						marker.FoldingType = type;
						//and, if necessary, set its fold state
						if (marker != null && setFolded && firstTime) {
							// only fold on document open, later added folds are NOT folded by default.
							marker.IsCollapsed = folded;
							continue;
						}
						if (marker != null && region.Region.IsInside (textEditor.CaretLine, textEditor.CaretColumn))
							marker.IsCollapsed = false;
					}
					if (firstTime) {
						textEditor.SetFoldings (foldSegments);
					} else {
						Application.Invoke (delegate {
							if (!token.IsCancellationRequested)
								textEditor.SetFoldings (foldSegments);
						});
					}
				}
				catch (Exception ex) {
					LoggingService.LogError ("Unhandled exception in ParseInformationUpdaterWorkerThread", ex);
				}
			};
			if (firstTime) {
				action ();
				return;
			}
			Task.Factory.StartNew (action);
		}

		void RunFirstTimeFoldUpdate (string text)
		{
			if (string.IsNullOrEmpty (text)) 
				return;
			ParsedDocument parsedDocument = null;

			var foldingParser = TypeSystemService.GetFoldingParser (textEditor.MimeType);
			if (foldingParser != null) {
				parsedDocument = foldingParser.Parse (textEditor.FileName, text);
			} else {
				var normalParser = TypeSystemService.GetParser (textEditor.MimeType);
				if (normalParser != null) {
					using (var sr = new StringReader (text))
						parsedDocument = normalParser.Parse (true, textEditor.FileName, sr, null);
				}
			}
			if (parsedDocument != null) {
				UpdateFoldings (parsedDocument, true);
			}
		}


		#region IViewFContent implementation

		event EventHandler IViewContent.ContentNameChanged {
			add {
				textEditorImpl.ContentNameChanged += value;
			}
			remove {
				textEditorImpl.ContentNameChanged -= value;
			}
		}

		event EventHandler IViewContent.ContentChanged {
			add {
				textEditorImpl.ContentChanged += value;
			}
			remove {
				textEditorImpl.ContentChanged -= value;
			}
		}

		event EventHandler IViewContent.DirtyChanged {
			add {
				textEditorImpl.DirtyChanged += value;
			}
			remove {
				textEditorImpl.DirtyChanged -= value;
			}
		}

		event EventHandler IViewContent.BeforeSave {
			add {
				textEditorImpl.BeforeSave += value;
			}
			remove {
				textEditorImpl.BeforeSave -= value;
			}
		}

		void IViewContent.Load (FileOpenInformation fileOpenInformation)
		{
			textEditorImpl.Load (fileOpenInformation);
			RunFirstTimeFoldUpdate (textEditor.Text);
		}
		
		void IViewContent.Load (string fileName)
		{
			textEditorImpl.Load (new FileOpenInformation (fileName));
			RunFirstTimeFoldUpdate (textEditor.Text);
		}

		void IViewContent.LoadNew (System.IO.Stream content, string mimeType)
		{
			textEditor.MimeType = mimeType;
			string text = null;
			if (content != null) {
				Encoding encoding;
				bool hadBom;
				text = TextFileUtility.GetText (content, out encoding, out hadBom);
				textEditor.Text = text;
				textEditor.Encoding = encoding;
				textEditor.UseBOM = hadBom;
			}
			RunFirstTimeFoldUpdate (text);
			textEditorImpl.InformLoadComplete ();
		}

		void IViewContent.Save (FileSaveInformation fileSaveInformation)
		{
			if (!string.IsNullOrEmpty (fileSaveInformation.FileName))
				AutoSave.RemoveAutoSaveFile (fileSaveInformation.FileName);
			textEditorImpl.Save (fileSaveInformation);
		}

		void IViewContent.Save (string fileName)
		{
			if (!string.IsNullOrEmpty (fileName))
				AutoSave.RemoveAutoSaveFile (fileName);
			textEditorImpl.Save (new FileSaveInformation (fileName));
		}

		void IViewContent.Save ()
		{
			if (!string.IsNullOrEmpty (textEditorImpl.ContentName))
				AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
			textEditorImpl.Save ();
		}

		void IViewContent.DiscardChanges ()
		{
			textEditorImpl.DiscardChanges ();
		}

		public Project Project {
			get {
				return textEditorImpl.Project;
			}
			set {
				textEditorImpl.Project = value;
				UpdateTextEditorOptions (null, null);
			}
		}

		string IViewContent.PathRelativeToProject {
			get {
				return textEditorImpl.PathRelativeToProject;
			}
		}

		string IViewContent.ContentName {
			get {
				return textEditorImpl.ContentName;
			}
			set {
				textEditorImpl.ContentName = value;
			}
		}

		string IViewContent.UntitledName {
			get {
				return textEditorImpl.UntitledName;
			}
			set {
				textEditorImpl.UntitledName = value;
			}
		}

		string IViewContent.StockIconId {
			get {
				return textEditorImpl.StockIconId;
			}
		}

		bool IViewContent.IsUntitled {
			get {
				return textEditorImpl.IsUntitled;
			}
		}

		bool IViewContent.IsViewOnly {
			get {
				return textEditorImpl.IsViewOnly;
			}
		}

		bool IViewContent.IsFile {
			get {
				return textEditorImpl.IsFile;
			}
		}

		bool IViewContent.IsDirty {
			get {
				return textEditorImpl.IsDirty;
			}
			set {
				textEditorImpl.IsDirty = value;
			}
		}

		bool IViewContent.IsReadOnly {
			get {
				return textEditorImpl.IsReadOnly;
			}
		}

		#endregion

		#region IBaseViewContent implementation

		object IBaseViewContent.GetContent (Type type)
		{
			if (type.IsAssignableFrom (typeof(TextEditor)))
				return textEditor;
			var ext = textEditorImpl.EditorExtension;
			while (ext != null) {
				if (type.IsInstanceOfType (ext))
					return ext;
				ext = ext.Next;
			}
			return textEditorImpl.GetContent (type);
		}

		public virtual IEnumerable<T> GetContents<T> () where T : class
		{
			if (typeof(T) == typeof(TextEditor)) {
				yield return (T)(object)textEditor;
				yield break;
			}
			var result = this as T;
			if (result != null) {
				yield return result;
			}
			var ext = textEditorImpl.EditorExtension;
			while (ext != null) {
				result = ext as T;
				if (result != null) {
					yield return result;
				}
				ext = ext.Next;
			}
			foreach (var cnt in textEditorImpl.GetContents<T> ()) {
				yield return cnt;
			}
		}

		bool IBaseViewContent.CanReuseView (string fileName)
		{
			return textEditorImpl.CanReuseView (fileName);
		}

		void IBaseViewContent.RedrawContent ()
		{
			textEditorImpl.RedrawContent ();
		}

		IWorkbenchWindow IBaseViewContent.WorkbenchWindow {
			get {
				return textEditorImpl.WorkbenchWindow;
			}
			set {
				textEditorImpl.WorkbenchWindow = value;
			}
		}

		Gtk.Widget IBaseViewContent.Control {
			get {
				return textEditor;
			}
		}

		string IBaseViewContent.TabPageLabel {
			get {
				return textEditorImpl.TabPageLabel;
			}
		}

		#endregion

		#region IDisposable implementation

		void IDisposable.Dispose ()
		{
			DefaultSourceEditorOptions.Instance.Changed -= UpdateTextEditorOptions;
			RemovePolicyChangeHandler ();
			RemoveAutoSaveTimer ();
			RemoveErrorUndelinesResetTimerId ();
			textEditorImpl.Dispose ();
		}

		#endregion

		#region ICommandRouter implementation

		object ICommandRouter.GetNextCommandTarget ()
		{
			return textEditorImpl;
		}

		#endregion
	
		#region Commands
		void ToggleCodeCommentWithBlockComments ()
		{
			var blockStarts = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentStart");
			var blockEnds = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentEnd");
			if (blockStarts == null || blockEnds == null || blockStarts.Length == 0 || blockEnds.Length == 0)
				return;

			string blockStart = blockStarts[0];
			string blockEnd = blockEnds[0];

			using (var undo = textEditor.OpenUndoGroup ()) {
				IDocumentLine startLine;
				IDocumentLine endLine;

				if (textEditor.IsSomethingSelected) {
					startLine = textEditor.GetLineByOffset (textEditor.SelectionRange.Offset);
					endLine = textEditor.GetLineByOffset (textEditor.SelectionRange.EndOffset);
				} else {
					startLine = endLine = textEditor.GetLine (textEditor.CaretLine);
				}
				string startLineText = textEditor.GetTextAt (startLine.Offset, startLine.Length);
				string endLineText = textEditor.GetTextAt (endLine.Offset, endLine.Length);
				if (startLineText.StartsWith (blockStart, StringComparison.Ordinal) && endLineText.EndsWith (blockEnd, StringComparison.Ordinal)) {
					textEditor.RemoveText (endLine.Offset + endLine.Length - blockEnd.Length, blockEnd.Length);
					textEditor.RemoveText (startLine.Offset, blockStart.Length);
					if (textEditor.IsSomethingSelected) {
						textEditor.SelectionAnchorOffset -= blockEnd.Length;
					}
				} else {
					textEditor.InsertText (endLine.Offset + endLine.Length, blockEnd);
					textEditor.InsertText (startLine.Offset, blockStart);
					if (textEditor.IsSomethingSelected) {
						textEditor.SelectionAnchorOffset += blockEnd.Length;
					}
				}
			}
		}

		bool TryGetLineCommentTag (out string commentTag)
		{
			var lineComments = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "LineComment");
			if (lineComments == null || lineComments.Length == 0) {
				commentTag = null;
				return false;
			}
			commentTag = lineComments [0];
			return true;
		}

		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		void OnUpdateToggleComment (CommandInfo info)
		{
			var lineComments = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "LineComment");
			if (lineComments != null && lineComments.Length > 0) {
				info.Visible = true;
				return;
			}
			var blockStarts = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentStart");
			var blockEnds = TextEditorFactory.GetSyntaxProperties (textEditor.MimeType, "BlockCommentEnd");
			info.Visible = blockStarts != null && blockStarts.Length > 0 && blockEnds != null && blockEnds.Length > 0;
		}

		[CommandHandler (EditCommands.ToggleCodeComment)]
		void ToggleCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;
			bool comment = false;
			foreach (var line in GetSelectedLines (textEditor)) {
				if (line.GetIndentation (textEditor).Length == line.Length)
					continue;
				string text = textEditor.GetTextAt (line);
				string trimmedText = text.TrimStart ();
				if (!trimmedText.StartsWith (commentTag, StringComparison.Ordinal)) {
					comment = true;
					break;
				}
			}

			if (comment) {
				AddCodeComment ();
			} else {
				RemoveCodeComment ();
			}
		}

		static IEnumerable<IDocumentLine> GetSelectedLines (TextEditor textEditor)
		{
			var selection = textEditor.SelectionRange;
			var line = textEditor.GetLineByOffset (selection.EndOffset);
			do {
				yield return line;
				line = line.PreviousLine;
			} while (line.EndOffset > selection.Offset);
		}

		[CommandHandler (EditCommands.AddCodeComment)]
		void AddCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;

			using (var undo = textEditor.OpenUndoGroup ()) {
				var wasSelected = textEditor.IsSomethingSelected;
				var lead = textEditor.SelectionLeadOffset;
				var anchor = textEditor.SelectionAnchorOffset;
				int lines = 0;
				foreach (var line in GetSelectedLines (textEditor)) {
					lines++;
					//					if (line.GetIndentation (TextEditor.Document).Length == line.EditableLength)
					//						continue;
					textEditor.InsertText (line.Offset, commentTag);
				}
				if (wasSelected) {
					if (anchor < lead) {
						textEditor.SelectionAnchorOffset = anchor + commentTag.Length;
						textEditor.SelectionLeadOffset = lead + commentTag.Length * lines;
					} else {
						textEditor.SelectionAnchorOffset = anchor + commentTag.Length * lines;
						textEditor.SelectionLeadOffset = lead + commentTag.Length;
					}
				}
			}
		}

		[CommandHandler (EditCommands.RemoveCodeComment)]
		void RemoveCodeComment ()
		{
			string commentTag;
			if (!TryGetLineCommentTag (out commentTag))
				return;
			
			using (var undo = textEditor.OpenUndoGroup ()) {
				var wasSelected = textEditor.IsSomethingSelected;
				var lead = textEditor.SelectionLeadOffset;
				var anchor = textEditor.SelectionAnchorOffset;
				int lines = 0;
				
				int first = -1;
				int last;
				foreach (var line in GetSelectedLines (textEditor)) {
					string text = textEditor.GetTextAt (line);
					string trimmedText = text.TrimStart ();
					int length = 0;
					if (trimmedText.StartsWith (commentTag, StringComparison.Ordinal)) {
						textEditor.RemoveText (line.Offset + (text.Length - trimmedText.Length), commentTag.Length);
						length = commentTag.Length;
						lines++;
					}
					
					last = length;
					if (first < 0)
						first = last;
				}
				
				if (wasSelected) {
					if (anchor < lead) {
						textEditor.SelectionAnchorOffset = anchor - commentTag.Length;
						textEditor.SelectionLeadOffset = lead - commentTag.Length * lines;
					} else {
						textEditor.SelectionAnchorOffset = anchor - commentTag.Length * lines;
						textEditor.SelectionLeadOffset = lead - commentTag.Length;
					}
				}
			}
		}



		[CommandHandler (EditCommands.InsertGuid)]
		void InsertGuid ()
		{
			textEditor.InsertAtCaret (Guid.NewGuid ().ToString ());
		}
		#endregion
	
		#region IQuickTaskProvider implementation
		List<QuickTask> tasks = new List<QuickTask> ();

		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
		{
			EventHandler handler = this.TasksUpdated;
			if (handler != null)
				handler (this, e);
		}

		public IEnumerable<QuickTask> QuickTasks {
			get {
				return tasks;
			}
		}

		void UpdateQuickTasks (ParsedDocument doc)
		{
			tasks.Clear ();
			foreach (var cmt in doc.TagComments) {
				var newTask = new QuickTask (cmt.Text, cmt.Region.Begin, Severity.Hint);
				tasks.Add (newTask);
			}

			foreach (var error in doc.Errors) {
				var newTask = new QuickTask (error.Message, error.Region.Begin, error.ErrorType == ErrorType.Error ? Severity.Error : Severity.Warning);
				tasks.Add (newTask);
			}

			OnTasksUpdated (EventArgs.Empty);
		}
		#endregion

	}
}