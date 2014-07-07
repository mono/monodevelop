//
// ITextEditor.cs
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
using MonoDevelop.Core.Text;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Editor.Extension;
using System.IO;
using MonoDevelop.Ide.Editor.Highlighting;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;
using System.Linq;
using MonoDevelop.Components;
using System.ComponentModel;

namespace MonoDevelop.Ide.Editor
{
	public sealed class TextEditor : Control, ITextDocument, IDisposable
	{
		readonly ITextEditorImpl textEditorImpl;
		IReadonlyTextDocument ReadOnlyTextDocument { get { return textEditorImpl.Document; } }
		ITextDocument ReadWriteTextDocument { get { return (ITextDocument)textEditorImpl.Document; } }

		public ITextSourceVersion Version {
			get {
				return ReadOnlyTextDocument.Version;
			}
		}

		FileTypeCondition fileTypeCondition = new FileTypeCondition ();

		void OnTooltipProviderChanged (object s, ExtensionNodeEventArgs a)
		{
			TooltipProvider provider;
			try {
				provider = (TooltipProvider) a.ExtensionObject;
			} catch (Exception e) {
				LoggingService.LogError ("Can't create tooltip provider:"+ a.ExtensionNode, e);
				return;
			}
			if (a.Change == ExtensionChange.Add) {
				textEditorImpl.AddTooltipProvider (provider);
			} else {
				textEditorImpl.RemoveTooltipProvider (provider);
			}
		}

		public event EventHandler SelectionChanged {
			add { textEditorImpl.SelectionChanged += value; }
			remove { textEditorImpl.SelectionChanged -= value; }
		}

		public event EventHandler CaretPositionChanged {
			add { textEditorImpl.CaretPositionChanged += value; }
			remove { textEditorImpl.CaretPositionChanged -= value; }
		}

		public event EventHandler BeginUndo {
			add { textEditorImpl.BeginUndo += value; }
			remove { textEditorImpl.BeginUndo -= value; }
		}

		public event EventHandler EndUndo {
			add { textEditorImpl.EndUndo += value; }
			remove { textEditorImpl.EndUndo -= value; }
		}

		public event EventHandler<TextChangeEventArgs> TextChanging {
			add { ReadWriteTextDocument.TextChanging += value; }
			remove { ReadWriteTextDocument.TextChanging -= value; }
		}

		public event EventHandler<TextChangeEventArgs> TextChanged {
			add { ReadWriteTextDocument.TextChanged += value; }
			remove { ReadWriteTextDocument.TextChanged -= value; }
		}

		public event EventHandler BeginMouseHover {
			add { textEditorImpl.BeginMouseHover += value; }
			remove { textEditorImpl.BeginMouseHover -= value; }
		}

		public ITextEditorOptions Options {
			get {
				return textEditorImpl.Options;
			}
		}

		public EditMode EditMode {
			get {
				return textEditorImpl.EditMode;
			}
		}

		public DocumentLocation CaretLocation {
			get {
				return textEditorImpl.CaretLocation;
			}
			set {
				textEditorImpl.CaretLocation = value;
			}
		}

		public int CaretLine {
			get {
				return CaretLocation.Line;
			}
			set {
				CaretLocation = new DocumentLocation (value, CaretColumn);
			}
		}

		public int CaretColumn {
			get {
				return CaretLocation.Column;
			}
			set {
				CaretLocation = new DocumentLocation (CaretLine, value);
			}
		}

		public int CaretOffset {
			get {
				return textEditorImpl.CaretOffset;
			}
			set {
				textEditorImpl.CaretOffset = value;
			}
		}

		public bool IsReadOnly {
			get {
				return ReadOnlyTextDocument.IsReadOnly;
			}
			set {
				ReadWriteTextDocument.IsReadOnly = value;
			}
		}

		public bool IsSomethingSelected {
			get {
				return textEditorImpl.IsSomethingSelected;
			}
		}

		public SelectionMode SelectionMode {
			get {
				return textEditorImpl.SelectionMode;
			}
		}

		public ISegment SelectionRange {
			get {
				return textEditorImpl.SelectionRange;
			}
			set {
				textEditorImpl.SelectionRange = value;
			}
		}

		public DocumentRegion SelectionRegion {
			get {
				return textEditorImpl.SelectionRegion;
			}
			set {
				textEditorImpl.SelectionRegion = value;
			}
		}

		public string SelectedText {
			get {
				return IsSomethingSelected ? ReadOnlyTextDocument.GetTextAt (SelectionRange) : null;
			}
			set {
				var selection = SelectionRange;
				ReplaceText (selection, value);
				SelectionRange = new TextSegment (selection.Offset, value.Length);
			}
		}

		public bool IsInAtomicUndo {
			get {
				return ReadWriteTextDocument.IsInAtomicUndo;
			}
		}

		public double LineHeight {
			get {
				return textEditorImpl.LineHeight;
			}
		}

		/// <summary>
		/// Gets or sets the type of the MIME.
		/// </summary>
		/// <value>The type of the MIME.</value>
		public string MimeType {
			get {
				return ReadOnlyTextDocument.MimeType;
			}
			set {
				ReadWriteTextDocument.MimeType = value;
			}
		}

		public string Text {
			get {
				return ReadOnlyTextDocument.Text;
			}
			set {
				ReadWriteTextDocument.Text = value;
			}
		}

		/// <summary>
		/// Gets the eol marker. On a text editor always use that and not GetEolMarker.
		/// The EOL marker of the document may get overwritten my the one from the options.
		/// </summary>
		public string EolMarker {
			get {
				if (Options.OverrideDocumentEolMarker)
					return Options.DefaultEolMarker;
				return ReadOnlyTextDocument.GetEolMarker ();
			}
		}

		public bool UseBOM {
			get {
				return ReadOnlyTextDocument.UseBOM;
			}
			set {
				ReadWriteTextDocument.UseBOM = value;
			}
		}

		public Encoding Encoding {
			get {
				return ReadOnlyTextDocument.Encoding;
			}
			set {
				ReadWriteTextDocument.Encoding = value;
			}
		}

		public int LineCount {
			get {
				return ReadOnlyTextDocument.LineCount;
			}
		}

		/// <summary>
		/// Gets the name of the file the document is stored in.
		/// Could also be a non-existent dummy file name or null if no name has been set.
		/// </summary>
		public FilePath FileName {
			get {
				return ReadOnlyTextDocument.FileName;
			}
			set {
				ReadWriteTextDocument.FileName = value;
			}
		}

		public event EventHandler FileNameChanged {
			add {
				ReadWriteTextDocument.FileNameChanged += value;
			}
			remove {
				ReadWriteTextDocument.FileNameChanged -= value;
			}
		}

		public int Length {
			get {
				return ReadOnlyTextDocument.Length;
			}
		}

		public IDisposable OpenUndoGroup ()
		{
			return ReadWriteTextDocument.OpenUndoGroup ();
		}

		public void SetSelection (int anchorOffset, int leadOffset)
		{
			textEditorImpl.SetSelection (anchorOffset, leadOffset);
		}

		public void SetSelection (DocumentLocation anchor, DocumentLocation lead)
		{
			SetSelection (LocationToOffset (anchor), LocationToOffset (lead));
		}

		public void SetCaretLocation (DocumentLocation location, bool usePulseAnimation = false)
		{
			CaretLocation = location;
			ScrollTo (CaretLocation);
			if (usePulseAnimation)
				StartCaretPulseAnimation ();
		}

		public void SetCaretLocation (int line, int col, bool usePulseAnimation = false)
		{
			CaretLocation = new DocumentLocation (line, col);
			ScrollTo (CaretLocation);
			if (usePulseAnimation)
				StartCaretPulseAnimation ();
		}

		public void ClearSelection ()
		{
			textEditorImpl.ClearSelection ();
		}

		public void CenterToCaret ()
		{
			textEditorImpl.CenterToCaret ();
		}

		public void StartCaretPulseAnimation ()
		{
			textEditorImpl.StartCaretPulseAnimation ();
		}

		public int EnsureCaretIsNotVirtual ()
		{
			return textEditorImpl.EnsureCaretIsNotVirtual ();
		}

		public void FixVirtualIndentation ()
		{
			textEditorImpl.FixVirtualIndentation ();
		}

		public void RunWhenLoaded (Action action)
		{
			if (action == null)
				throw new ArgumentNullException ("action");
			textEditorImpl.RunWhenLoaded (action);
		}

		public string FormatString (DocumentLocation insertPosition, string code)
		{
			return textEditorImpl.FormatString (LocationToOffset (insertPosition), code);
		}

		public string FormatString (int offset, string code)
		{
			return textEditorImpl.FormatString (offset, code);
		}

		public void StartInsertionMode (InsertionModeOptions insertionModeOptions)
		{
			if (insertionModeOptions == null)
				throw new ArgumentNullException ("insertionModeOptions");
			textEditorImpl.StartInsertionMode (insertionModeOptions);
		}

		public void StartTextLinkMode (TextLinkModeOptions textLinkModeOptions)
		{
			if (textLinkModeOptions == null)
				throw new ArgumentNullException ("textLinkModeOptions");
			textEditorImpl.StartTextLinkMode (textLinkModeOptions);
		}

		public void InsertAtCaret (string text)
		{
			InsertText (CaretOffset, text);
		}

		public DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false)
		{
			return textEditorImpl.PointToLocation (xp, yp, endAtEol);
		}

		public Xwt.Point LocationToPoint (DocumentLocation location)
		{
			return textEditorImpl.LocationToPoint (location.Line, location.Column);
		} 

		public Xwt.Point LocationToPoint (int line, int column)
		{
			return textEditorImpl.LocationToPoint (line, column);
		}

		public string GetLineText (int line, bool includeDelimiter = false)
		{
			var segment = GetLine (line);
			return GetTextAt (includeDelimiter ? segment.SegmentIncludingDelimiter : segment);
		}

		public int LocationToOffset (int line, int column)
		{
			return ReadOnlyTextDocument.LocationToOffset (new DocumentLocation (line, column));
		}

		public int LocationToOffset (DocumentLocation location)
		{
			return ReadOnlyTextDocument.LocationToOffset (location);
		}

		public DocumentLocation OffsetToLocation (int offset)
		{
			return ReadOnlyTextDocument.OffsetToLocation (offset);
		}

		public void InsertText (int offset, string text)
		{
			ReadWriteTextDocument.InsertText (offset, text);
		}

		public void InsertText (int offset, ITextSource text)
		{
			ReadWriteTextDocument.InsertText (offset, text);
		}

		public void RemoveText (int offset, int count)
		{
			RemoveText (new TextSegment (offset, count)); 
		}

		public void RemoveText (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			ReadWriteTextDocument.RemoveText (segment);
		}

		public void ReplaceText (int offset, int count, string value)
		{
			ReadWriteTextDocument.ReplaceText (offset, count, value);
		}

		public void ReplaceText (int offset, int count, ITextSource value)
		{
			ReadWriteTextDocument.ReplaceText (offset, count, value);
		}

		public void ReplaceText (ISegment segment, string value)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			ReadWriteTextDocument.ReplaceText (segment.Offset, segment.Length, value);
		}

		public IDocumentLine GetLine (int lineNumber)
		{
			return ReadOnlyTextDocument.GetLine (lineNumber);
		}

		public IDocumentLine GetLineByOffset (int offset)
		{
			return ReadOnlyTextDocument.GetLineByOffset (offset);
		}

		public int OffsetToLineNumber (int offset)
		{
			return ReadOnlyTextDocument.OffsetToLineNumber (offset);
		}

		public void AddMarker (IDocumentLine line, ITextLineMarker lineMarker)
		{
			if (line == null)
				throw new ArgumentNullException ("line");
			if (lineMarker == null)
				throw new ArgumentNullException ("lineMarker");
			textEditorImpl.AddMarker (line, lineMarker);
		}

		public void AddMarker (int lineNumber, ITextLineMarker lineMarker)
		{
			if (lineMarker == null)
				throw new ArgumentNullException ("lineMarker");
			AddMarker (GetLine (lineNumber), lineMarker);
		}

		public void RemoveMarker (ITextLineMarker lineMarker)
		{
			if (lineMarker == null)
				throw new ArgumentNullException ("lineMarker");
			textEditorImpl.RemoveMarker (lineMarker);
		}

		public IEnumerable<ITextLineMarker> GetLineMarkers (IDocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException ("line");
			return textEditorImpl.GetLineMarkers (line);
		}

		public IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return textEditorImpl.GetTextSegmentMarkersAt (segment);
		}

		public IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (int offset)
		{
			return textEditorImpl.GetTextSegmentMarkersAt (offset);
		}

		public void AddMarker (ITextSegmentMarker marker)
		{
			if (marker == null)
				throw new ArgumentNullException ("marker");
			textEditorImpl.AddMarker (marker);
		}

		public bool RemoveMarker (ITextSegmentMarker marker)
		{
			if (marker == null)
				throw new ArgumentNullException ("marker");
			return textEditorImpl.RemoveMarker (marker);
		}

		public void SetFoldings (IEnumerable<IFoldSegment> foldings)
		{
			if (foldings == null)
				throw new ArgumentNullException ("foldings");
			textEditorImpl.SetFoldings (foldings);
		}


		public IEnumerable<IFoldSegment> GetFoldingsContaining (int offset)
		{
			return textEditorImpl.GetFoldingsContaining (offset);
		}

		public IEnumerable<IFoldSegment> GetFoldingsIn (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return textEditorImpl.GetFoldingsIn (segment.Offset, segment.Length);
		}

		/// <summary>
		/// Gets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		public char GetCharAt (int offset)
		{
			return ReadOnlyTextDocument.GetCharAt (offset); 
		}

		public string GetTextAt (int offset, int length)
		{
			return ReadOnlyTextDocument.GetTextAt (offset, length);
		}

		public string GetTextAt (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return ReadOnlyTextDocument.GetTextAt (segment);
		}

		public IReadonlyTextDocument CreateDocumentSnapshot ()
		{
			return ReadWriteTextDocument.CreateDocumentSnapshot ();
		}

		public string GetVirtualIndentationString (int lineNumber)
		{
			if (lineNumber < 1 || lineNumber >= LineCount)
				throw new ArgumentOutOfRangeException ("lineNumber");
			return textEditorImpl.GetVirtualIndentationString (lineNumber);
		}

		public string GetVirtualIndentationString (IDocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException ("line");
			return textEditorImpl.GetVirtualIndentationString (line.LineNumber);
		}

		public int GetVirtualIndentationColumn (int lineNumber)
		{
			if (lineNumber < 1 || lineNumber >= LineCount)
				throw new ArgumentOutOfRangeException ("lineNumber");
			return 1 + textEditorImpl.GetVirtualIndentationString (lineNumber).Length;
		}

		public int GetVirtualIndentationColumn (IDocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException ("line");
			return 1 + textEditorImpl.GetVirtualIndentationString (line.LineNumber).Length;
		}

		public TextReader CreateReader ()
		{
			return ReadOnlyTextDocument.CreateReader ();
		}

		public TextReader CreateReader (int offset, int length)
		{
			return ReadOnlyTextDocument.CreateReader (offset, length);
		}

		public ITextSource CreateSnapshot ()
		{
			return ReadOnlyTextDocument.CreateSnapshot ();
		}

		public ITextSource CreateSnapshot (int offset, int length)
		{
			return ReadOnlyTextDocument.CreateSnapshot (offset, length);
		}

		public ITextSource CreateSnapshot (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return ReadOnlyTextDocument.CreateSnapshot (segment.Offset, segment.Length);
		}

		public void WriteTextTo (TextWriter writer)
		{
			ReadOnlyTextDocument.WriteTextTo (writer);
		}

		public void WriteTextTo (TextWriter writer, int offset, int length)
		{
			ReadOnlyTextDocument.WriteTextTo (writer, offset, length);
		}

		public void ScrollTo (int offset)
		{
			textEditorImpl.ScrollTo (offset);
		}

		public void ScrollTo (DocumentLocation loc)
		{
			ScrollTo (LocationToOffset (loc));
		}

		public void CenterTo (int offset)
		{
			textEditorImpl.CenterTo (offset);
		}

		public void CenterTo (DocumentLocation loc)
		{
			CenterTo (LocationToOffset (loc));
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public void SetIndentationTracker (IndentationTracker indentationTracker)
		{
			textEditorImpl.SetIndentationTracker (indentationTracker);
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public void SetSelectionSurroundingProvider (SelectionSurroundingProvider surroundingProvider)
		{
			textEditorImpl.SetSelectionSurroundingProvider (surroundingProvider);
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public void SetTextPasteHandler (TextPasteHandler textPasteHandler)
		{
			textEditorImpl.SetTextPasteHandler (textPasteHandler);
		}

		public IList<SkipChar> SkipChars {
			get {
				return textEditorImpl.SkipChars;
			}
		}

		/// <summary>
		/// Skip chars are 
		/// </summary>
		public void AddSkipChar (int offset, char ch)
		{
			textEditorImpl.AddSkipChar (offset, ch);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				DetachExtensionChain ();
				textEditorImpl.Dispose ();
			}
			base.Dispose (disposing);
		}

		protected override object CreateNativeWidget ()
		{
			return textEditorImpl.CreateNativeControl ();
		}

		#region Internal API
		ExtensionContext extensionContext;

		internal ExtensionContext ExtensionContext {
			get {
				return extensionContext;
			}
			set {
				if (extensionContext != null) {
					extensionContext.RemoveExtensionNodeHandler ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
					textEditorImpl.ClearTooltipProviders ();
				}
				extensionContext = value;
				if (extensionContext != null)
					extensionContext.AddExtensionNodeHandler ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
			}
		}

		internal IEditorActionHost EditorActionHost {
			get {
				return textEditorImpl.Actions;
			}
		}

		internal ITextMarkerFactory TextMarkerFactory {
			get {
				return textEditorImpl.TextMarkerFactory;
			}
		}

		internal TextEditor (ITextEditorImpl textEditorImpl)
		{
			if (textEditorImpl == null)
				throw new ArgumentNullException ("textEditorImpl");
			this.textEditorImpl = textEditorImpl;
			commandRouter = new InternalCommandRouter (this);
			fileTypeCondition.SetFileName (FileName);
			ExtensionContext = AddinManager.CreateExtensionContext ();
			ExtensionContext.RegisterCondition ("FileType", fileTypeCondition);

			FileNameChanged += delegate {
				fileTypeCondition.SetFileName (FileName);
			};
		}

		TextEditorViewContent viewContent;
		internal IViewContent GetViewContent ()
		{
			if (viewContent == null) {
				viewContent = new TextEditorViewContent (this, textEditorImpl);
			}

			return viewContent;
		}

		internal IFoldSegment CreateFoldSegment (int offset, int length, bool isFolded = false)
		{
			return textEditorImpl.CreateFoldSegment (offset, length, isFolded);
		}
		#endregion

		#region Editor extensions
		InternalCommandRouter commandRouter;
		class InternalCommandRouter : MonoDevelop.Components.Commands.IMultiCastCommandRouter
		{
			readonly TextEditor editor;

			public InternalCommandRouter (TextEditor editor)
			{
				this.editor = editor;
			}
			
			#region IMultiCastCommandRouter implementation

			System.Collections.IEnumerable MonoDevelop.Components.Commands.IMultiCastCommandRouter.GetCommandTargets ()
			{
				yield return editor.textEditorImpl;
				yield return editor.textEditorImpl.EditorExtension;
			}
			#endregion
		}

		internal object CommandRouter {
			get {
				return commandRouter;
			}
		}

		internal void InitializeExtensionChain (DocumentContext documentContext, TextEditor editor)
		{
			if (documentContext == null)
				throw new ArgumentNullException ("documentContext");
			if (editor == null)
				throw new ArgumentNullException ("editor");
			DetachExtensionChain ();
			var extensions = ExtensionContext.GetExtensionNodes ("/MonoDevelop/Ide/TextEditorExtensions", typeof(TextEditorExtensionNode));
			TextEditorExtension last = null;
			var mimetypeChain = DesktopService.GetMimeTypeInheritanceChainForFile (FileName).ToArray ();
			foreach (TextEditorExtensionNode extNode in extensions) {
				if (!extNode.Supports (FileName, mimetypeChain))
					continue;
				TextEditorExtension ext;
				try {
					var instance = extNode.CreateInstance ();
					ext = instance as TextEditorExtension;
					if (ext == null)
						continue;
				} catch (Exception e) {
					LoggingService.LogError ("Error while creating text editor extension :" + extNode.Id + "(" + extNode.Type +")", e); 
					continue;
				}
				if (ext.IsValidInContext (documentContext)) {
					if (last != null) {
						last.Next = ext;
						last = ext;
					} else {
						textEditorImpl.EditorExtension = last = ext;
					}
					ext.Initialize (editor, documentContext);
				}
			}
		}

		void DetachExtensionChain ()
		{
			var editorExtension = textEditorImpl.EditorExtension;
			while (editorExtension != null) {
				try {
					editorExtension.Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Exception while disposing extension:" + editorExtension, ex);
				}
				editorExtension = editorExtension.Next;
			}
			textEditorImpl.EditorExtension = null;
		}

		public T GetContent<T> () where T : class
		{
			T result = textEditorImpl as T;
			if (result != null)
				return result;
			var ext = textEditorImpl.EditorExtension;
			while (ext != null) {
				result = ext as T;
				if (result != null)
					return result;
				ext = ext.Next;
			}
			return null;
		}

		public IEnumerable<T> GetContents<T> () where T : class
		{
			T result = textEditorImpl as T;
			if (result != null)
				yield return result;
			var ext = textEditorImpl.EditorExtension;
			while (ext != null) {
				result = ext as T;
				if (result != null)
					yield return result;
				ext = ext.Next;
			}
		}
		#endregion

		public string GetPangoMarkup (int offset, int length)
		{
			return textEditorImpl.GetPangoMarkup (offset, length);
		}

		public string GetPangoMarkup (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return textEditorImpl.GetPangoMarkup (segment.Offset, segment.Length);
		}
	}
}