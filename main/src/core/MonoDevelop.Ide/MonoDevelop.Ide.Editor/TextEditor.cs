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
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.Editor.Projection;

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

		public TextEditorType TextEditorType { get; internal set; }

		FileTypeCondition fileTypeCondition = new FileTypeCondition ();

		List<TooltipExtensionNode> allProviders = new List<TooltipExtensionNode> ();

		void OnTooltipProviderChanged (object s, ExtensionNodeEventArgs a)
		{
			TooltipProvider provider;
			try {
				var extensionNode = a.ExtensionNode as TooltipExtensionNode;
				allProviders.Add (extensionNode);
				if (extensionNode.IsValidFor (MimeType))
					return;
				provider = (TooltipProvider)extensionNode.CreateInstance ();
			} catch (Exception e) {
				LoggingService.LogError ("Can't create tooltip provider:" + a.ExtensionNode, e);
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

		public event EventHandler BeginAtomicUndoOperation {
			add { textEditorImpl.BeginAtomicUndoOperation += value; }
			remove { textEditorImpl.BeginAtomicUndoOperation -= value; }
		}

		public event EventHandler EndAtomicUndoOperation {
			add { textEditorImpl.EndAtomicUndoOperation += value; }
			remove { textEditorImpl.EndAtomicUndoOperation -= value; }
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

		public event EventHandler VAdjustmentChanged {
			add { textEditorImpl.VAdjustmentChanged += value; }
			remove { textEditorImpl.VAdjustmentChanged -= value; }
		}

		public event EventHandler HAdjustmentChanged {
			add { textEditorImpl.HAdjustmentChanged += value; }
			remove { textEditorImpl.HAdjustmentChanged -= value; }
		}
		public char this[int offset] {
			get {
				return ReadOnlyTextDocument [offset];
			}
			set {
				ReadWriteTextDocument [offset] = value;
			}
		}

//		public event EventHandler<LineEventArgs> LineChanged {
//			add { textEditorImpl.LineChanged += value; }
//			remove { textEditorImpl.LineChanged -= value; }
//		}
//
//		public event EventHandler<LineEventArgs> LineInserted {
//			add { textEditorImpl.LineInserted += value; }
//			remove { textEditorImpl.LineInserted -= value; }
//		}
//
//		public event EventHandler<LineEventArgs> LineRemoved {
//			add { textEditorImpl.LineRemoved += value; }
//			remove { textEditorImpl.LineRemoved -= value; }
//		}

		public ITextEditorOptions Options {
			get {
				return textEditorImpl.Options;
			}
			set {
				textEditorImpl.Options = value;
				OnOptionsChanged (EventArgs.Empty);
			}
		}

		public event EventHandler OptionsChanged;

		void OnOptionsChanged (EventArgs e)
		{
			var handler = OptionsChanged;
			if (handler != null)
				handler (this, e);
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

		public SemanticHighlighting SemanticHighlighting {
			get {
				return textEditorImpl.SemanticHighlighting;
			}
			set {
				textEditorImpl.SemanticHighlighting = value;
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
		
		public int SelectionAnchorOffset {
			get {
				return textEditorImpl.SelectionAnchorOffset;
			}
			set {
				textEditorImpl.SelectionAnchorOffset = value;
			}
		}

		public int SelectionLeadOffset {
			get {
				return textEditorImpl.SelectionLeadOffset;
			}
			set {
				textEditorImpl.SelectionLeadOffset = value;
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

		public event EventHandler MimeTypeChanged {
			add { ReadWriteTextDocument.MimeTypeChanged += value; }
			remove { ReadWriteTextDocument.MimeTypeChanged -= value; }
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

		public double ZoomLevel {
			get {
				return textEditorImpl.ZoomLevel;
			}
			set {
				textEditorImpl.ZoomLevel = value;
			}
		}

		public event EventHandler ZoomLevelChanged {
			add {
				textEditorImpl.ZoomLevelChanged += value;
			}
			remove {
				textEditorImpl.ZoomLevelChanged -= value;
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

		public void SetCaretLocation (DocumentLocation location, bool usePulseAnimation = false, bool centerCaret = true)
		{
			CaretLocation = location;
			if (centerCaret) {
				CenterTo (CaretLocation);
			} else {
				ScrollTo (CaretLocation);
			}
			if (usePulseAnimation)
				StartCaretPulseAnimation ();
		}

		public void SetCaretLocation (int line, int col, bool usePulseAnimation = false, bool centerCaret = true)
		{
			CaretLocation = new DocumentLocation (line, col);
			if (centerCaret) {
				CenterTo (CaretLocation);
			} else {
				ScrollTo (CaretLocation);
			}
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
				throw new ArgumentNullException (nameof (action));
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
				throw new ArgumentNullException (nameof (insertionModeOptions));
			textEditorImpl.StartInsertionMode (insertionModeOptions);
		}

		public void StartTextLinkMode (TextLinkModeOptions textLinkModeOptions)
		{
			if (textLinkModeOptions == null)
				throw new ArgumentNullException (nameof (textLinkModeOptions));
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
				throw new ArgumentNullException (nameof (segment));
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
				throw new ArgumentNullException (nameof (segment));
			ReadWriteTextDocument.ReplaceText (segment.Offset, segment.Length, value);
		}

		public void ReplaceText (ISegment segment, ITextSource value)
		{
			if (segment == null)
				throw new ArgumentNullException (nameof (segment));
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
				throw new ArgumentNullException (nameof (line));
			if (lineMarker == null)
				throw new ArgumentNullException (nameof (lineMarker));
			textEditorImpl.AddMarker (line, lineMarker);
		}

		public void AddMarker (int lineNumber, ITextLineMarker lineMarker)
		{
			if (lineMarker == null)
				throw new ArgumentNullException (nameof (lineMarker));
			AddMarker (GetLine (lineNumber), lineMarker);
		}

		public void RemoveMarker (ITextLineMarker lineMarker)
		{
			if (lineMarker == null)
				throw new ArgumentNullException (nameof (lineMarker));
			textEditorImpl.RemoveMarker (lineMarker);
		}

		public IEnumerable<ITextLineMarker> GetLineMarkers (IDocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException (nameof (line));
			return textEditorImpl.GetLineMarkers (line);
		}

		public IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException (nameof (segment));
			return textEditorImpl.GetTextSegmentMarkersAt (segment);
		}

		public IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (int offset)
		{
			return textEditorImpl.GetTextSegmentMarkersAt (offset);
		}

		public void AddMarker (ITextSegmentMarker marker)
		{
			if (marker == null)
				throw new ArgumentNullException (nameof (marker));
			textEditorImpl.AddMarker (marker);
		}

		public bool RemoveMarker (ITextSegmentMarker marker)
		{
			if (marker == null)
				throw new ArgumentNullException (nameof (marker));
			return textEditorImpl.RemoveMarker (marker);
		}

		public void SetFoldings (IEnumerable<IFoldSegment> foldings)
		{
			if (foldings == null)
				throw new ArgumentNullException (nameof (foldings));
			textEditorImpl.SetFoldings (foldings);
		}


		public IEnumerable<IFoldSegment> GetFoldingsContaining (int offset)
		{
			return textEditorImpl.GetFoldingsContaining (offset);
		}

		public IEnumerable<IFoldSegment> GetFoldingsIn (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException (nameof (segment));
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
				throw new ArgumentNullException (nameof (segment));
			return ReadOnlyTextDocument.GetTextAt (segment);
		}

		public IReadonlyTextDocument CreateDocumentSnapshot ()
		{
			return ReadWriteTextDocument.CreateDocumentSnapshot ();
		}

		public string GetVirtualIndentationString (int lineNumber)
		{
			if (lineNumber < 1 || lineNumber > LineCount)
				throw new ArgumentOutOfRangeException (nameof (lineNumber));
			return textEditorImpl.GetVirtualIndentationString (lineNumber);
		}

		public string GetVirtualIndentationString (IDocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException (nameof (line));
			return textEditorImpl.GetVirtualIndentationString (line.LineNumber);
		}

		public int GetVirtualIndentationColumn (int lineNumber)
		{
			if (lineNumber < 1 || lineNumber > LineCount)
				throw new ArgumentOutOfRangeException (nameof (lineNumber));
			return 1 + textEditorImpl.GetVirtualIndentationString (lineNumber).Length;
		}

		public int GetVirtualIndentationColumn (IDocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException (nameof (line));
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
				throw new ArgumentNullException (nameof (segment));
			return ReadOnlyTextDocument.CreateSnapshot (segment.Offset, segment.Length);
		}

		public void WriteTextTo (TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException (nameof (writer));
			ReadOnlyTextDocument.WriteTextTo (writer);
		}

		public void WriteTextTo (TextWriter writer, int offset, int length)
		{
			if (writer == null)
				throw new ArgumentNullException (nameof (writer));
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

		public IList<SkipChar> SkipChars
		{
			get
			{
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
					//					textEditorImpl.ClearTooltipProviders ();
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

		internal TextEditorExtension TextEditorExtensionChain {
			get {
				return textEditorImpl.EditorExtension;
			}
		} 

		internal ITextMarkerFactory TextMarkerFactory {
			get {
				return textEditorImpl.TextMarkerFactory;
			}
		}

		internal TextEditor (ITextEditorImpl textEditorImpl, TextEditorType textEditorType)
		{
			if (textEditorImpl == null)
				throw new ArgumentNullException (nameof (textEditorImpl));
			this.textEditorImpl = textEditorImpl;
			this.TextEditorType = textEditorType;
			commandRouter = new InternalCommandRouter (this);
			fileTypeCondition.SetFileName (FileName);
			ExtensionContext = AddinManager.CreateExtensionContext ();
			ExtensionContext.RegisterCondition ("FileType", fileTypeCondition);

			FileNameChanged += delegate {
				fileTypeCondition.SetFileName (FileName);
			};

			MimeTypeChanged += delegate {
				textEditorImpl.ClearTooltipProviders ();
				foreach (var extensionNode in allProviders) {
					if (extensionNode.IsValidFor (MimeType))
						textEditorImpl.AddTooltipProvider ((TooltipProvider)extensionNode.CreateInstance ());
				}
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

		DocumentContext documentContext;
		internal DocumentContext DocumentContext {
			get {
				return documentContext;
			}
			set {
				documentContext = value;
				OnDocumentContextChanged (EventArgs.Empty);
			}
		}

		public event EventHandler DocumentContextChanged;

		void OnDocumentContextChanged (EventArgs e)
		{
			if (DocumentContext != null) {
				textEditorImpl.SetQuickTaskProviders (DocumentContext.GetContents<IQuickTaskProvider> ());
				textEditorImpl.SetUsageTaskProviders (DocumentContext.GetContents<UsageProviderEditorExtension> ());
			} else {
				textEditorImpl.SetQuickTaskProviders (Enumerable.Empty<IQuickTaskProvider> ());
				textEditorImpl.SetUsageTaskProviders (Enumerable.Empty<UsageProviderEditorExtension> ());
			}
			var handler = DocumentContextChanged;
			if (handler != null)
				handler (this, e);
		}

		internal void InitializeExtensionChain (DocumentContext documentContext)
		{
			if (documentContext == null)
				throw new ArgumentNullException (nameof (documentContext));
			DetachExtensionChain ();
			var extensions = ExtensionContext.GetExtensionNodes ("/MonoDevelop/Ide/TextEditorExtensions", typeof(TextEditorExtensionNode));
			var mimetypeChain = DesktopService.GetMimeTypeInheritanceChainForFile (FileName).ToArray ();
			var newExtensions = new List<TextEditorExtension> ();

			foreach (TextEditorExtensionNode extNode in extensions) {
				if (!extNode.Supports (FileName, mimetypeChain))
					continue;
				TextEditorExtension ext;
				try {
					var instance = extNode.CreateInstance ();
					ext = instance as TextEditorExtension;
					if (ext != null)
						newExtensions.Add (ext);
				} catch (Exception e) {
					LoggingService.LogError ("Error while creating text editor extension :" + extNode.Id + "(" + extNode.Type + ")", e);
					continue;
				}
			}
			SetExtensionChain (documentContext, newExtensions);
		}

		internal void SetExtensionChain (DocumentContext documentContext, IEnumerable<TextEditorExtension> extensions)
		{
			if (documentContext == null)
				throw new ArgumentNullException (nameof (documentContext));
			if (extensions == null)
				throw new ArgumentNullException (nameof (extensions));
			
			TextEditorExtension last = null;
			foreach (var ext in extensions) {
				if (ext.IsValidInContext (documentContext)) {
					if (last != null) {
						last.Next = ext;
						last = ext;
					} else {
						textEditorImpl.EditorExtension = last = ext;
					}
					ext.Initialize (this, documentContext);
				}
			}
			DocumentContext = documentContext;
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

		public T GetContent<T>() where T : class
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

		public IEnumerable<T> GetContents<T>() where T : class
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
				throw new ArgumentNullException (nameof (segment));
			return textEditorImpl.GetPangoMarkup (segment.Offset, segment.Length);
		}

		public static implicit operator Microsoft.CodeAnalysis.Text.SourceText (TextEditor editor)
		{
			return new MonoDevelopSourceText (editor);
		}


		#region Annotations
		// Annotations: points either null (no annotations), to the single annotation,
		// or to an AnnotationList.
		// Once it is pointed at an AnnotationList, it will never change (this allows thread-safety support by locking the list)

		object annotations;
		sealed class AnnotationList : List<object>, ICloneable
		{
			// There are two uses for this custom list type:
			// 1) it's private, and thus (unlike List<object>) cannot be confused with real annotations
			// 2) It allows us to simplify the cloning logic by making the list behave the same as a clonable annotation.
			public AnnotationList (int initialCapacity) : base (initialCapacity)
			{
			}

			public object Clone ()
			{
				lock (this) {
					AnnotationList copy = new AnnotationList (Count);
					for (int i = 0; i < Count; i++) {
						object obj = this [i];
						ICloneable c = obj as ICloneable;
						copy.Add (c != null ? c.Clone () : obj);
					}
					return copy;
				}
			}
		}

		public void AddAnnotation (object annotation)
		{
			if (annotation == null)
				throw new ArgumentNullException (nameof (annotation));
			retry: // Retry until successful
			object oldAnnotation = Interlocked.CompareExchange (ref annotations, annotation, null);
			if (oldAnnotation == null) {
				return; // we successfully added a single annotation
			}
			AnnotationList list = oldAnnotation as AnnotationList;
			if (list == null) {
				// we need to transform the old annotation into a list
				list = new AnnotationList (4);
				list.Add (oldAnnotation);
				list.Add (annotation);
				if (Interlocked.CompareExchange (ref annotations, list, oldAnnotation) != oldAnnotation) {
					// the transformation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			} else {
				// once there's a list, use simple locking
				lock (list) {
					list.Add (annotation);
				}
			}
		}

		public void RemoveAnnotations<T>() where T : class
		{
		retry: // Retry until successful
			object oldAnnotations = annotations;
			var list = oldAnnotations as AnnotationList;
			if (list != null) {
				lock (list)
					list.RemoveAll (obj => obj is T);
			} else if (oldAnnotations is T) {
				if (Interlocked.CompareExchange (ref annotations, null, oldAnnotations) != oldAnnotations) {
					// Operation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			}
		}

		public T Annotation<T>() where T : class
		{
			object annotations = this.annotations;
			var list = annotations as AnnotationList;
			if (list != null) {
				lock (list) {
					foreach (object obj in list) {
						T t = obj as T;
						if (t != null)
							return t;
					}
					return null;
				}
			}
			return annotations as T;
		}

		/// <summary>
		/// Gets all annotations stored on this AstNode.
		/// </summary>
		public IEnumerable<object> Annotations
		{
			get
			{
				object annotations = this.annotations;
				AnnotationList list = annotations as AnnotationList;
				if (list != null) {
					lock (list) {
						return list.ToArray ();
					}
				}
				if (annotations != null)
					return new [] { annotations };
				return Enumerable.Empty<object> ();
			}
		}
		#endregion

		List<ProjectedTooltipProvider> projectedProviders = new List<ProjectedTooltipProvider> ();
		IReadOnlyList<Editor.Projection.Projection> projections = null;

		public void SetOrUpdateProjections (DocumentContext ctx, IReadOnlyList<Editor.Projection.Projection> projections, DisabledProjectionFeatures disabledFeatures = DisabledProjectionFeatures.None)
		{
			if (ctx == null)
				throw new ArgumentNullException (nameof (ctx));
			if (this.projections != null) {
				foreach (var projection in this.projections) {
					projection.Dettach ();
				}
			}
			this.projections = projections;
			if (projections != null) {
				foreach (var projection in projections) {
					projection.Attach (this);
				}
			}

			if ((disabledFeatures & DisabledProjectionFeatures.SemanticHighlighting) != DisabledProjectionFeatures.SemanticHighlighting) {
				if (SemanticHighlighting is ProjectedSemanticHighlighting) {
					((ProjectedSemanticHighlighting)SemanticHighlighting).UpdateProjection (projections);
				} else {
					SemanticHighlighting = new ProjectedSemanticHighlighting (this, ctx, projections);
				}
			}

			if ((disabledFeatures & DisabledProjectionFeatures.Tooltips) != DisabledProjectionFeatures.Tooltips) {
				projectedProviders.ForEach (textEditorImpl.RemoveTooltipProvider);
				projectedProviders = new List<ProjectedTooltipProvider> ();
				foreach (var projection in projections) {
					foreach (var tp in projection.ProjectedEditor.allProviders) {
						if (!tp.IsValidFor (projection.ProjectedEditor.MimeType))
							continue;
						var newProvider = new ProjectedTooltipProvider (this, ctx, projection, (TooltipProvider)tp.CreateInstance ());
						projectedProviders.Add (newProvider);
						textEditorImpl.AddTooltipProvider (newProvider);
					}
				}
			}
			InitializeProjectionExtensions (ctx, disabledFeatures);
		}

		bool projectionsAdded = false;
		void InitializeProjectionExtensions (DocumentContext ctx, DisabledProjectionFeatures disabledFeatures)
		{
			if (projectionsAdded) {
				TextEditorExtension ext = textEditorImpl.EditorExtension;
				while (ext != null && ext.Next != null) {
					var pext = ext as IProjectionExtension;
					if (pext != null) {
						pext.Projections = projections;
					}
					ext = ext.Next;
				}
				return;
			}

			if (projections.Count == 0)
				return;

			TextEditorExtension lastExtension = textEditorImpl.EditorExtension;
			while (lastExtension != null && lastExtension.Next != null) {
				var completionTextEditorExtension = lastExtension.Next as CompletionTextEditorExtension;
				if (completionTextEditorExtension != null) {
					var projectedFilterExtension = new ProjectedFilterCompletionTextEditorExtension (completionTextEditorExtension, projections) { Next = completionTextEditorExtension.Next };
					completionTextEditorExtension.Deinitialize ();
					lastExtension.Next = projectedFilterExtension;
					projectedFilterExtension.Initialize (this, DocumentContext);
				}
				lastExtension = lastExtension.Next;
			}

			// no extensions -> no projections needed
			if (textEditorImpl.EditorExtension == null)
				return;

			if ((disabledFeatures & DisabledProjectionFeatures.Completion) != DisabledProjectionFeatures.Completion) {
				var projectedCompletionExtension = new ProjectedCompletionExtension (ctx, projections);
				projectedCompletionExtension.Next = textEditorImpl.EditorExtension;

				textEditorImpl.EditorExtension = projectedCompletionExtension;
				projectedCompletionExtension.Initialize (this, DocumentContext);
			}
			projectionsAdded = true;
		}

		public void AddOverlay (Control messageOverlayContent, Func<int> sizeFunc)
		{
			textEditorImpl.AddOverlay (messageOverlayContent, sizeFunc);
		}

		public void RemoveOverlay (Control messageOverlayContent)
		{
			textEditorImpl.RemoveOverlay (messageOverlayContent);
		}

		internal void UpdateBraceMatchingResult (BraceMatchingResult? result)
		{
			textEditorImpl.UpdateBraceMatchingResult (result);
		}
	}
}