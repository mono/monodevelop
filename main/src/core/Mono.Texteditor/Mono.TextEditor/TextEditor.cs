//
// TextEditor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//#define DEBUG_EXPOSE

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.PopupWindow;
using Mono.TextEditor.Theatrics;

using Gdk;
using Gtk;

namespace Mono.TextEditor
{
	[System.ComponentModel.Category("Mono.TextEditor")]
	[System.ComponentModel.ToolboxItem(true)]
	public class TextEditor : Gtk.Widget, ITextEditorDataProvider
	{
		TextEditorData textEditorData;
		
		protected IconMargin       iconMargin;
		protected GutterMargin     gutterMargin;
		protected DashedLineMargin dashedLineMargin;
		protected FoldMarkerMargin foldMarkerMargin;
		protected TextViewMargin   textViewMargin;
		
		LineSegment longestLine      = null;
		double      longestLineWidth = -1;
		
		List<Margin> margins = new List<Margin> ();
		int oldRequest = -1;
		
		bool isDisposed = false;
		IMMulticontext imContext;
		Gdk.EventKey lastIMEvent;
		Gdk.Key lastIMEventMappedKey;
		Gdk.ModifierType lastIMEventMappedModifier;
		bool imContextActive;
		
		string currentStyleName;
		
		double mx, my;
		
		public Document Document {
			get {
				return textEditorData.Document;
			}
			set {
				textEditorData.Document.TextReplaced -= OnDocumentStateChanged;
				textEditorData.Document.TextSet -= OnTextSet;
				textEditorData.Document = value;
				textEditorData.Document.TextReplaced += OnDocumentStateChanged;
				textEditorData.Document.TextSet += OnTextSet;
			}
		}
		
		public Mono.TextEditor.Caret Caret {
			get {
				return textEditorData.Caret;
			}
		}
		
		protected IMMulticontext IMContext {
			get { return imContext; }
		}
		
		public MenuItem CreateInputMethodMenuItem (string label)
		{
			MenuItem imContextMenuItem = new MenuItem (label);
			Menu imContextMenu = new Menu ();
			imContextMenuItem.Submenu = imContextMenu;
			IMContext.AppendMenuitems (imContextMenu);
			return imContextMenuItem;
		}
		
		public ITextEditorOptions Options {
			get {
				return textEditorData.Options;
			}
			set {
				if (textEditorData.Options != null)
					textEditorData.Options.Changed -= OptionsChanged;
				textEditorData.Options = value;
				textEditorData.Options.Changed += OptionsChanged;
				if (IsRealized)
					OptionsChanged (null, null);
			}
		}
		Dictionary<int, double> lineHeights = new Dictionary<int, double> ();
		
		public TextEditor () : this(new Document ())
		{
			textEditorData.Document.LineChanged += UpdateLinesOnTextMarkerHeightChange; 
		}
		
		void HAdjustmentValueChanged (object sender, EventArgs args)
		{
			HAdjustmentValueChanged ();
		}
		
		protected virtual void HAdjustmentValueChanged ()
		{
			double value = this.textEditorData.HAdjustment.Value;
			if (value != System.Math.Round (value)) {
				this.textEditorData.HAdjustment.Value = System.Math.Round (value);
				return;
			}
			HideTooltip ();
			textViewMargin.HideCodeSegmentPreviewWindow ();
			QueueDrawArea ((int)this.textViewMargin.XOffset, 0, this.Allocation.Width - (int)this.textViewMargin.XOffset, this.Allocation.Height);
			OnHScroll (EventArgs.Empty);
		}
		
		void VAdjustmentValueChanged (object sender, EventArgs args)
		{
			
			VAdjustmentValueChanged ();
		}
		
		protected virtual void VAdjustmentValueChanged ()
		{
			HideTooltip ();
			textViewMargin.HideCodeSegmentPreviewWindow ();
			double value = this.textEditorData.VAdjustment.Value;
			if (value != System.Math.Round (value)) {
				this.textEditorData.VAdjustment.Value = System.Math.Round (value);
				return;
			}
			if (isMouseTrapped)
				FireMotionEvent (mx + textViewMargin.XOffset, my, lastState);
			
			double delta = value - this.oldVadjustment;
			oldVadjustment = value;
			TextViewMargin.caretY -= delta;
			
			if (System.Math.Abs (delta) >= Allocation.Height - this.LineHeight * 2 || this.TextViewMargin.inSelectionDrag) {
				this.QueueDraw ();
				OnVScroll (EventArgs.Empty);
				return;
			}
			
			if (GdkWindow != null)
				GdkWindow.Scroll (0, (int)-delta);
			
			OnVScroll (EventArgs.Empty);
		}
		
		protected virtual void OnVScroll (EventArgs e)
		{
			EventHandler handler = this.VScroll;
			if (handler != null)
				handler (this, e);
		}

		protected virtual void OnHScroll (EventArgs e)
		{
			EventHandler handler = this.HScroll;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler VScroll;
		public event EventHandler HScroll;
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			if (textEditorData == null)
				return;
			if (textEditorData.HAdjustment != null)
				textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (textEditorData.VAdjustment != null)
				textEditorData.VAdjustment.ValueChanged -= VAdjustmentValueChanged;
			
			this.textEditorData.HAdjustment = hAdjustement;
			this.textEditorData.VAdjustment = vAdjustement;
			
			if (hAdjustement == null || vAdjustement == null)
				return;

			this.textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
			this.textEditorData.VAdjustment.ValueChanged += VAdjustmentValueChanged;
		}
		
		protected TextEditor (IntPtr raw) : base (raw)
		{
		}
		
		public TextEditor (Document doc)
			: this (doc, null)
		{
		}
		
		public TextEditor (Document doc, ITextEditorOptions options)
			: this (doc, options, new SimpleEditMode ())
		{
		}
		
		public TextEditor (Document doc, ITextEditorOptions options, EditMode initialMode)
		{
			textEditorData = new TextEditorData (doc);
			textEditorData.Parent = this;
			textEditorData.RecenterEditor += delegate {
				CenterToCaret ();
				StartCaretPulseAnimation ();
			};
			doc.TextReplaced += OnDocumentStateChanged;
			doc.TextSet += OnTextSet;

			textEditorData.CurrentMode = initialMode;
			
//			this.Events = EventMask.AllEventsMask;
			this.Events = EventMask.PointerMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask | EventMask.VisibilityNotifyMask | EventMask.FocusChangeMask | EventMask.ScrollMask | EventMask.KeyPressMask | EventMask.KeyReleaseMask;
			this.DoubleBuffered = true;
			base.CanFocus = true;
			WidgetFlags |= WidgetFlags.NoWindow;
			iconMargin = new IconMargin (this);
			gutterMargin = new GutterMargin (this);
			dashedLineMargin = new DashedLineMargin (this);
			foldMarkerMargin = new FoldMarkerMargin (this);
			textViewMargin = new TextViewMargin (this);

			margins.Add (iconMargin);
			margins.Add (gutterMargin);
			margins.Add (foldMarkerMargin);
			margins.Add (dashedLineMargin);
			
			margins.Add (textViewMargin);
			this.textEditorData.SelectionChanged += TextEditorDataSelectionChanged;
			this.textEditorData.UpdateAdjustmentsRequested += TextEditorDatahandleUpdateAdjustmentsRequested;
			Document.DocumentUpdated += DocumentUpdatedHandler;
			
			this.textEditorData.Options = options ?? TextEditorOptions.DefaultOptions;
			this.textEditorData.Options.Changed += OptionsChanged;
			
			
			Gtk.TargetList list = new Gtk.TargetList ();
			list.AddTextTargets (ClipboardActions.CopyOperation.TextType);
			Gtk.Drag.DestSet (this, DestDefaults.All, (TargetEntry[])list, DragAction.Move | DragAction.Copy);
			
			imContext = new IMMulticontext ();
			imContext.Commit += IMCommit;
			
			imContext.UsePreedit = true;
			imContext.PreeditStart += delegate {
				preeditOffset = Caret.Offset;
				this.textViewMargin.ForceInvalidateLine (Caret.Line);
				this.textEditorData.Document.CommitLineUpdate (Caret.Line);
			};
			imContext.PreeditEnd += delegate {
				preeditOffset = -1;
				this.textViewMargin.ForceInvalidateLine (Caret.Line);
				this.textEditorData.Document.CommitLineUpdate (Caret.Line);
			};
			imContext.PreeditChanged += delegate(object sender, EventArgs e) {
				if (preeditOffset >= 0) {
					imContext.GetPreeditString (out preeditString, out preeditAttrs, out preeditCursorPos);
					this.textViewMargin.ForceInvalidateLine (Caret.Line);
					this.textEditorData.Document.CommitLineUpdate (Caret.Line);
				}
			};
			
			using (Pixmap inv = new Pixmap (null, 1, 1, 1)) {
				invisibleCursor = new Cursor (inv, inv, Gdk.Color.Zero, Gdk.Color.Zero, 0, 0);
			}
			
			InitAnimations ();
			this.Document.EndUndo += HandleDocumenthandleEndUndo;
#if ATK
			TextEditorAccessible.Factory.Init (this);
#endif
		}

		void HandleDocumenthandleEndUndo (object sender, Document.UndoOperationEventArgs e)
		{
			if (this.Document.HeightChanged) {
				this.Document.HeightChanged = false;
				SetAdjustments ();
			}
		}

		void TextEditorDatahandleUpdateAdjustmentsRequested (object sender, EventArgs e)
		{
			SetAdjustments ();
		}
		
		
		public void ShowListWindow<T> (ListWindow<T> window, DocumentLocation loc)
		{
			var p = LocationToPoint (loc);
			int ox = 0, oy = 0;
			GdkWindow.GetOrigin (out ox, out oy);
	
			window.Move (ox + p.X - window.TextOffset , oy + p.Y + (int)LineHeight);
			window.ShowAll ();
		}
		
		internal int preeditCursorPos = -1, preeditOffset = -1;
		internal string preeditString;
		internal Pango.AttrList preeditAttrs;
		
		void CaretPositionChanged (object sender, DocumentLocationEventArgs args) 
		{
			HideTooltip ();
			ResetIMContext ();
			
			if (Caret.AutoScrollToCaret)
				ScrollToCaret ();
			
//			Rectangle rectangle = textViewMargin.GetCaretRectangle (Caret.Mode);
			RequestResetCaretBlink ();
			
			textEditorData.CurrentMode.InternalCaretPositionChanged (this, textEditorData);
			
			if (!IsSomethingSelected) {
				if (/*Options.HighlightCaretLine && */args.Location.Line != Caret.Line) 
					RedrawMarginLine (TextViewMargin, args.Location.Line);
				RedrawMarginLine (TextViewMargin, Caret.Line);
			}
		}
		
		Selection oldSelection = null;
		void TextEditorDataSelectionChanged (object sender, EventArgs args)
		{
			if (IsSomethingSelected) {
				ISegment selectionRange = MainSelection.GetSelectionRange (textEditorData);
				if (selectionRange.Offset >= 0 && selectionRange.EndOffset < Document.Length) {
					ClipboardActions.CopyToPrimary (this.textEditorData);
				} else {
					ClipboardActions.ClearPrimary ();
				}
			} else {
				ClipboardActions.ClearPrimary ();
			}
			// Handle redraw
			Selection selection = Selection.Clone (MainSelection);
			int startLine    = selection != null ? selection.Anchor.Line : -1;
			int endLine      = selection != null ? selection.Lead.Line : -1;
			int oldStartLine = oldSelection != null ? oldSelection.Anchor.Line : -1;
			int oldEndLine   = oldSelection != null ? oldSelection.Lead.Line : -1;
			if (SelectionMode == SelectionMode.Block) {
				this.RedrawMarginLines (this.textViewMargin, 
				                        System.Math.Min (System.Math.Min (oldStartLine, oldEndLine), System.Math.Min (startLine, endLine)),
				                        System.Math.Max (System.Math.Max (oldStartLine, oldEndLine), System.Math.Max (startLine, endLine)));
				oldSelection = selection;
			} else {
				if (endLine < 0 && startLine >=0)
					endLine = Document.LineCount;
				if (oldEndLine < 0 && oldStartLine >=0)
					oldEndLine = Document.LineCount;
				int from = oldEndLine, to = endLine;
				if (selection != null && oldSelection != null) {
					if (startLine != oldStartLine && endLine != oldEndLine) {
						from = System.Math.Min (startLine, oldStartLine);
						to   = System.Math.Max (endLine, oldEndLine);
					} else if (startLine != oldStartLine) {
						from = startLine;
						to   = oldStartLine;
					} else if (endLine != oldEndLine) {
						from = endLine;
						to   = oldEndLine;
					} else if (startLine == oldStartLine && endLine == oldEndLine)  {
						if (selection.Anchor == oldSelection.Anchor) {
							this.RedrawMarginLine (this.textViewMargin, endLine);
						} else if (selection.Lead == oldSelection.Lead) {
							this.RedrawMarginLine (this.textViewMargin, startLine);
						} else { // 3rd case - may happen when changed programmatically
							this.RedrawMarginLine (this.textViewMargin, endLine);
							this.RedrawMarginLine (this.textViewMargin, startLine);
						}
						from = to = -1;
					}
				} else {
					if (selection == null) {
						from = oldStartLine;
						to = oldEndLine;
					} else if (oldSelection == null) {
						from = startLine;
						to = endLine;
					} 
				}
				
				if (from >= 0 && to >= 0) {
					oldSelection = selection;
					this.RedrawMarginLines (this.textViewMargin, 
					                        System.Math.Max (0, System.Math.Min (from, to) - 1),
					                        System.Math.Max (from, to));
				}
			}
			OnSelectionChanged (EventArgs.Empty);
		}
		
		void ResetIMContext ()
		{
			if (imContextActive) {
				imContext.Reset ();
				imContextActive = false;
			}
		}
		
		void IMCommit (object sender, Gtk.CommitArgs ca)
		{
			try {
				if (IsRealized && IsFocus) {
					uint lastChar = Keyval.ToUnicode ((uint)lastIMEventMappedKey);
					
					//this, if anywhere, is where we should handle UCS4 conversions
					for (int i = 0; i < ca.Str.Length; i++) {
						int utf32Char;
						if (char.IsHighSurrogate (ca.Str, i)) {
							utf32Char = char.ConvertToUtf32 (ca.Str, i);
							i++;
						} else {
							utf32Char = (int) ca.Str[i];
						}
						
						//include the other pre-IM state *if* the post-IM char matches the pre-IM (key-mapped) one
						if (lastChar == utf32Char)
							OnIMProcessedKeyPressEvent (lastIMEventMappedKey, lastChar, lastIMEventMappedModifier);
						else
							OnIMProcessedKeyPressEvent ((Gdk.Key)0, (uint)utf32Char, Gdk.ModifierType.None);
					}
				}
			} finally {
				ResetIMContext ();
			}
		}
		
		protected override bool OnFocusInEvent (EventFocus evnt)
		{
			var result = base.OnFocusInEvent (evnt);
			IMContext.FocusIn ();
			RequestResetCaretBlink ();
			Document.CommitLineUpdate (Caret.Line);
			return result;
		}
		
		protected override bool OnFocusOutEvent (EventFocus evnt)
		{
			var result = base.OnFocusOutEvent (evnt);
			imContext.FocusOut ();
			GLib.Timeout.Add (10, delegate {
				// Don't immediately hide the tooltip. Wait a bit and check if the tooltip has the focus.
				if (tipWindow != null && !tipWindow.HasToplevelFocus)
					HideTooltip ();
				return false;
			});
			TextViewMargin.StopCaretThread ();
			Document.CommitLineUpdate (Caret.Line);
			return result;
		}
		
		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;
			WindowAttr attributes = new WindowAttr ();
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.X = Allocation.X;
			attributes.Y = Allocation.Y;
			attributes.Width = Allocation.Width;
			attributes.Height = Allocation.Height;
			attributes.Wclass = WindowClass.InputOutput;
			attributes.Visual = this.Visual;
			attributes.Colormap = this.Colormap;
			attributes.EventMask = (int)(this.Events | Gdk.EventMask.ExposureMask);
			attributes.Mask = this.Events | Gdk.EventMask.ExposureMask;
//			attributes.Mask = EventMask;
			
			WindowAttributesType mask = WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Colormap | WindowAttributesType.Visual;
			this.GdkWindow = new Gdk.Window (ParentWindow, attributes, mask);
			this.GdkWindow.UserData = this.Raw;
			this.Style = Style.Attach (this.GdkWindow);
			this.WidgetFlags &= ~WidgetFlags.NoWindow;
			
			//base.OnRealized ();
			imContext.ClientWindow = this.GdkWindow;
			OptionsChanged (this, EventArgs.Empty);
			Caret.PositionChanged += CaretPositionChanged;
		}
		
		protected override void OnUnrealized ()
		{
			imContext.ClientWindow = null;
			CancelScheduledHide ();
			if (this.GdkWindow != null) {
				this.GdkWindow.UserData = IntPtr.Zero;
				this.GdkWindow.Destroy ();
				this.WidgetFlags |= WidgetFlags.NoWindow;
			}
			base.OnUnrealized ();
		}
		
		void DocumentUpdatedHandler (object sender, EventArgs args)
		{
			foreach (DocumentUpdateRequest request in Document.UpdateRequests) {
				request.Update (this);
			}
		}
		
		public event EventHandler EditorOptionsChanged;
		
		protected virtual void OptionsChanged (object sender, EventArgs args)
		{
			if (!this.IsRealized)
				return;
			if (currentStyleName != Options.ColorScheme) {
				currentStyleName = Options.ColorScheme;
				this.textEditorData.ColorStyle = Options.GetColorStyle (this.Style);
				SetWidgetBgFromStyle ();
			}
			
			iconMargin.IsVisible   = Options.ShowIconMargin;
			gutterMargin.IsVisible     = Options.ShowLineNumberMargin;
			foldMarkerMargin.IsVisible = Options.ShowFoldMargin;
			dashedLineMargin.IsVisible = foldMarkerMargin.IsVisible || gutterMargin.IsVisible;
			
			if (EditorOptionsChanged != null)
				EditorOptionsChanged (this, args);
			
			textViewMargin.OptionsChanged ();
			foreach (Margin margin in this.margins) {
				if (margin == textViewMargin)
					continue;
				margin.OptionsChanged ();
			}
			SetAdjustments (Allocation);
			this.QueueResize ();
		}
		
		void SetWidgetBgFromStyle ()
		{
			// This is a hack around a problem with repainting the drag widget.
			// When this is not set a white square is drawn when the drag widget is moved
			// when the bg color is differs from the color style bg color (e.g. oblivion style)
			if (this.textEditorData.ColorStyle != null && GdkWindow != null) {
				settingWidgetBg = true; //prevent infinite recusion
				
				this.ModifyBg (StateType.Normal, this.textEditorData.ColorStyle.Default.BackgroundColor);
				settingWidgetBg = false;
			}
		}
		
		bool settingWidgetBg = false;
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			if (!settingWidgetBg && textEditorData.ColorStyle != null) {
				textEditorData.ColorStyle.UpdateFromGtkStyle (this.Style);
				SetWidgetBgFromStyle ();
			}
		}
 
		protected override void OnDestroyed ()
		{
			if (popupWindow != null)
				popupWindow.Destroy ();
			
			if (this.Document != null)
				this.Document.EndUndo -= HandleDocumenthandleEndUndo;
			
			DisposeAnimations ();
			
			RemoveScrollWindowTimer ();
			if (invisibleCursor != null)
				invisibleCursor.Dispose ();
			
			Caret.PositionChanged -= CaretPositionChanged;
			
			Document.DocumentUpdated -= DocumentUpdatedHandler;
			if (textEditorData.Options != null)
				textEditorData.Options.Changed -= OptionsChanged;

			imContext = imContext.Kill (x => x.Commit -= IMCommit);

			if (this.textEditorData.HAdjustment != null)
				this.textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			
			if (this.textEditorData.VAdjustment != null)
				this.textEditorData.VAdjustment.ValueChanged -= VAdjustmentValueChanged;
			
			foreach (Margin margin in this.margins) {
				if (margin is IDisposable)
					((IDisposable)margin).Dispose ();
			}
			
			this.textEditorData.SelectionChanged -= TextEditorDataSelectionChanged;
			this.textEditorData.Dispose (); 
			this.Realized -= OptionsChanged;
			
			base.OnDestroyed ();
		}
		
		internal void RedrawMargin (Margin margin)
		{
			if (isDisposed)
				return;
			QueueDrawArea ((int)margin.XOffset, 0, GetMarginWidth (margin),  this.Allocation.Height);
		}
		
		public void RedrawMarginLine (Margin margin, int logicalLine)
		{
			if (isDisposed)
				return;
			
			double y = LineToY (logicalLine) - this.textEditorData.VAdjustment.Value;
			double h = GetLineHeight (logicalLine);
			
			if (y + h > 0)
				QueueDrawArea ((int)margin.XOffset, (int)y, (int)GetMarginWidth (margin), (int)h);
		}

		int GetMarginWidth (Margin margin)
		{
			if (margin.Width < 0)
				return Allocation.Width - (int)margin.XOffset;
			return (int)margin.Width;
		}
		
		internal void RedrawLine (int logicalLine)
		{
			if (isDisposed)
				return;
			
			double y = LineToY (logicalLine) - this.textEditorData.VAdjustment.Value;
			double h = GetLineHeight (logicalLine);
			
			if (y + h > 0)
				QueueDrawArea (0, (int)y, this.Allocation.Width, (int)h);
		}
		
		public new void QueueDrawArea (int x, int y, int w, int h)
		{
			if (GdkWindow != null) {
				GdkWindow.InvalidateRect (new Rectangle (x, y, w, h), false);
#if DEBUG_EXPOSE
				Console.WriteLine ("invalidated {0},{1} {2}x{3}", x, y, w, h);
#endif
			}
		}
		
		public new void QueueDraw ()
		{
			base.QueueDraw ();
#if DEBUG_EXPOSE
				Console.WriteLine ("invalidated entire widget");
#endif
		}
		
		internal void RedrawPosition (int logicalLine, int logicalColumn)
		{
			if (isDisposed)
				return;
//				Console.WriteLine ("Redraw position: logicalLine={0}, logicalColumn={1}", logicalLine, logicalColumn);
			RedrawLine (logicalLine);
		}
		
		public void RedrawMarginLines (Margin margin, int start, int end)
		{
			if (isDisposed)
				return;
			if (start < 0)
				start = 0;
			double visualStart = -this.textEditorData.VAdjustment.Value + LineToY (start);
			if (end < 0)
				end = Document.LineCount;
			double visualEnd   = -this.textEditorData.VAdjustment.Value + LineToY (end) + GetLineHeight (end);
			QueueDrawArea ((int)margin.XOffset, (int)visualStart, GetMarginWidth (margin), (int)(visualEnd - visualStart));
		}
			
		internal void RedrawLines (int start, int end)
		{
//			Console.WriteLine ("redraw lines: start={0}, end={1}", start, end);
			if (isDisposed)
				return;
			if (start < 0)
				start = 0;
			double visualStart = -this.textEditorData.VAdjustment.Value +  LineToY (start);
			if (end < 0)
				end = Document.LineCount;
			double visualEnd   = -this.textEditorData.VAdjustment.Value + LineToY (end) + GetLineHeight (end);
			QueueDrawArea (0, (int)visualStart, this.Allocation.Width, (int)(visualEnd - visualStart));
		}
		
		public void RedrawFromLine (int logicalLine)
		{
//			Console.WriteLine ("Redraw from line: logicalLine={0}", logicalLine);
			if (isDisposed)
				return;
			int y = System.Math.Max (0, (int)(-this.textEditorData.VAdjustment.Value + LineToY (logicalLine)));
			QueueDrawArea (0, y,
			               this.Allocation.Width, this.Allocation.Height - y);
		}
		
		public void RunAction (Action<TextEditorData> action)
		{
			try {
				action (this.textEditorData);
			} catch (Exception e) {
				Console.WriteLine ("Error while executing " + action + " :" + e);
			}
		}
		
		/// <summary>Handles key input after key mapping and input methods.</summary>
		/// <param name="key">The mapped keycode.</param>
		/// <param name="unicodeChar">A UCS4 character. If this is nonzero, it overrides the keycode.</param>
		/// <param name="modifier">Keyboard modifier, excluding any consumed by key mapping or IM.</param>
		public void SimulateKeyPress (Gdk.Key key, uint unicodeChar, ModifierType modifier)
		{
			ModifierType filteredModifiers = modifier & (ModifierType.ShiftMask | ModifierType.Mod1Mask
				 | ModifierType.ControlMask | ModifierType.MetaMask | ModifierType.SuperMask);
			CurrentMode.InternalHandleKeypress (this, textEditorData, key, unicodeChar, filteredModifiers);
			RequestResetCaretBlink ();
		}
		
		bool IMFilterKeyPress (Gdk.EventKey evt, Gdk.Key mappedKey, Gdk.ModifierType mappedModifiers)
		{
			if (lastIMEvent == evt)
				return false;
			
			if (evt.Type == EventType.KeyPress) {
				lastIMEvent = evt;
				lastIMEventMappedKey = mappedKey;
				lastIMEventMappedModifier = mappedModifiers;
			}
			
			if (imContext.FilterKeypress (evt)) {
				imContextActive = true;
				return true;
			} else {
				return false;
			}
		}
		
		Gdk.Cursor invisibleCursor;
		
		internal void HideMouseCursor ()
		{
			if (GdkWindow != null)
				GdkWindow.Cursor = invisibleCursor;
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evt)
		{
			ModifierType mod;
			Gdk.Key key;
			Platform.MapRawKeys (evt, out key, out mod);
			
			if (key == Gdk.Key.F1 && (mod & (ModifierType.ControlMask | ModifierType.ShiftMask)) == ModifierType.ControlMask) {
				var p = LocationToPoint (Caret.Location);
				ShowTooltip (Gdk.ModifierType.None, Caret.Offset, p.X, p.Y);
				return true;
			}
			
			if (key == Gdk.Key.space && (mod & (ModifierType.ShiftMask)) == ModifierType.ShiftMask && textViewMargin.IsCodeSegmentPreviewWindowShown) {
				textViewMargin.OpenCodeSegmentEditor ();
				return true;
			}
			
			uint unicodeChar = Gdk.Keyval.ToUnicode (evt.KeyValue);
			if (CurrentMode.WantsToPreemptIM || CurrentMode.PreemptIM (key, unicodeChar, mod)) {
				ResetIMContext ();	
				SimulateKeyPress (key, unicodeChar, mod);
				return true;
			}
			bool filter = IMFilterKeyPress (evt, key, mod);
			if (!filter) {
				return OnIMProcessedKeyPressEvent (key, unicodeChar, mod);
			}
			return base.OnKeyPressEvent (evt);
		}
		
		/// <remarks>
		/// The Key may be null if it has been handled by the IMContext. In such cases, the char is the value.
		/// </remarks>
		protected virtual bool OnIMProcessedKeyPressEvent (Gdk.Key key, uint ch, Gdk.ModifierType state)
		{
			SimulateKeyPress (key, ch, state);
			return true;
		}
		
		protected override bool OnKeyReleaseEvent (EventKey evnt)
		{
			if (IMFilterKeyPress (evnt, 0, ModifierType.None))
				imContextActive = true;
			return true;
		}
		
		uint mouseButtonPressed = 0;
		uint lastTime;
		double pressPositionX, pressPositionY;
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			pressPositionX = e.X;
			pressPositionY = e.Y;
			base.IsFocus = true;
			if (lastTime != e.Time) {// filter double clicks
				if (e.Type == EventType.TwoButtonPress) {
				    lastTime = e.Time;
				} else {
					lastTime = 0;
				}
				mouseButtonPressed = e.Button;
				double startPos;
				Margin margin = GetMarginAtX (e.X, out startPos);
				if (margin != null) 
					margin.MousePressed (new MarginMouseEventArgs (this, e.Button, e.X - startPos, e.Y, e.Type, e.State));
			}
			return base.OnButtonPressEvent (e);
		}
	/*	protected override bool OnWidgetEvent (Event evnt)
		{
			Console.WriteLine (evnt.Type);
			return base.OnWidgetEvent (evnt);
		}*/
		
		public Margin LockedMargin {
			get;
			set;
		}
		
		Margin GetMarginAtX (double x, out double startingPos)
		{
			double curX = 0;
			foreach (Margin margin in this.margins) {
				if (!margin.IsVisible)
					continue;
				if (LockedMargin != null) {
					if (LockedMargin == margin) {
						startingPos = curX;
						return margin;
					}
				} else {
					if (curX <= x && (x <= curX + margin.Width || margin.Width < 0)) {
						startingPos = curX;
						return margin;
					}
				}
				curX += margin.Width;
			}
			startingPos = -1;
			return null;
		}
		
		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			RemoveScrollWindowTimer ();
			double startPos;
			Margin margin = GetMarginAtX (e.X, out startPos);
			if (margin != null)
				margin.MouseReleased (new MarginMouseEventArgs (this, e.Button, e.X - startPos, e.Y, EventType.ButtonRelease, e.State));
			ResetMouseState ();
			return base.OnButtonReleaseEvent (e);
		}
		
		protected void ResetMouseState ()
		{
			mouseButtonPressed = 0;
			textViewMargin.inDrag = false;
			textViewMargin.inSelectionDrag = false;
		}
		
		bool dragOver = false;
		ClipboardActions.CopyOperation dragContents = null;
		DocumentLocation defaultCaretPos, dragCaretPos;
		Selection selection = null;
		
		public bool IsInDrag {
			get {
				return dragOver;
			}
		}
		
		public void CaretToDragCaretPosition ()
		{
			Caret.Location = defaultCaretPos = dragCaretPos;
		}
		
		protected override void OnDragLeave (DragContext context, uint time_)
		{
			if (dragOver) {
				Caret.PreserveSelection = true;
				Caret.Location = defaultCaretPos;
				Caret.PreserveSelection = false;
				dragOver = false;
			}
			base.OnDragLeave (context, time_);
		}
		
		protected override void OnDragDataGet (DragContext context, SelectionData selection_data, uint info, uint time_)
		{
			if (this.dragContents != null) {
				this.dragContents.SetData (selection_data, info);
				this.dragContents = null;
			}
			base.OnDragDataGet (context, selection_data, info, time_);
		}
				
		protected override void OnDragDataReceived (DragContext context, int x, int y, SelectionData selection_data, uint info, uint time_)
		{
			textEditorData.Document.BeginAtomicUndo ();
			int dragOffset = Document.LocationToOffset (dragCaretPos);
			if (context.Action == DragAction.Move) {
				if (CanEdit (Caret.Line) && selection != null) {
					ISegment selectionRange = selection.GetSelectionRange (textEditorData);
					if (selectionRange.Offset < dragOffset)
						dragOffset -= selectionRange.Length;
					Caret.PreserveSelection = true;
					textEditorData.DeleteSelection (selection);
					Caret.PreserveSelection = false;

					selection = null;
				}
			}
			if (selection_data.Length > 0 && selection_data.Format == 8) {
				Caret.Offset = dragOffset;
				if (CanEdit (dragCaretPos.Line)) {
					int offset = Caret.Offset;
					if (selection != null && selection.GetSelectionRange (textEditorData).Offset >= offset) {
						var start = Document.OffsetToLocation (selection.GetSelectionRange (textEditorData).Offset + selection_data.Text.Length);
						var end = Document.OffsetToLocation (selection.GetSelectionRange (textEditorData).Offset + selection_data.Text.Length + selection.GetSelectionRange (textEditorData).Length);
						selection = new Selection (start, end);
					}
					textEditorData.Insert (offset, selection_data.Text);
					Caret.Offset = offset + selection_data.Text.Length;
					MainSelection = new Selection (Document.OffsetToLocation (offset), Document.OffsetToLocation (offset + selection_data.Text.Length));
					textEditorData.PasteText (offset, selection_data.Text);
				}
				dragOver = false;
				context = null;
			}
			mouseButtonPressed = 0;
			textEditorData.Document.EndAtomicUndo ();
			base.OnDragDataReceived (context, x, y, selection_data, info, time_);
		}
		
		protected override bool OnDragMotion (DragContext context, int x, int y, uint time)
		{
			if (!this.HasFocus)
				this.GrabFocus ();
			if (!dragOver) {
				defaultCaretPos = Caret.Location;
			}
			
			DocumentLocation oldLocation = Caret.Location;
			dragOver = true;
			Caret.PreserveSelection = true;
			dragCaretPos = PointToLocation (x - textViewMargin.XOffset, y);
			int offset = Document.LocationToOffset (dragCaretPos);
			if (selection != null && offset >= this.selection.GetSelectionRange (textEditorData).Offset && offset < this.selection.GetSelectionRange (textEditorData).EndOffset) {
				Gdk.Drag.Status (context, DragAction.Default, time);
				Caret.Location = defaultCaretPos;
			} else {
				Gdk.Drag.Status (context, (context.Actions & DragAction.Move) == DragAction.Move ? DragAction.Move : DragAction.Copy, time);
				Caret.Location = dragCaretPos; 
			}
			this.RedrawLine (oldLocation.Line);
			if (oldLocation.Line != Caret.Line)
				this.RedrawLine (Caret.Line);
			Caret.PreserveSelection = false;
			return base.OnDragMotion (context, x, y, time);
		}
		
		Margin oldMargin = null;
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			RemoveScrollWindowTimer ();
			double x = e.X;
			double y = e.Y;
			Gdk.ModifierType mod = e.State;
			double startPos;
			Margin margin = GetMarginAtX (x, out startPos);
			if (textViewMargin.inDrag && margin == this.textViewMargin && Gtk.Drag.CheckThreshold (this, (int)pressPositionX, (int)pressPositionY, (int)x, (int)y)) {
				dragContents = new ClipboardActions.CopyOperation ();
				dragContents.CopyData (textEditorData);
				DragContext context = Gtk.Drag.Begin (this, ClipboardActions.CopyOperation.targetList, DragAction.Move | DragAction.Copy, 1, e);
				if (!Platform.IsMac) {
					CodeSegmentPreviewWindow window = new CodeSegmentPreviewWindow (this, true, textEditorData.SelectionRange, 300, 300);
					Gtk.Drag.SetIconWidget (context, window, 0, 0);
				}
				selection = Selection.Clone (MainSelection);
				textViewMargin.inDrag = false;
			} else {
				FireMotionEvent (x, y, mod);
				if (mouseButtonPressed != 0) {
					UpdateScrollWindowTimer (x, y, mod);
				}
			}
			return base.OnMotionNotifyEvent (e);
		}
		
		uint   scrollWindowTimer = 0;
		double scrollWindowTimer_x;
		double scrollWindowTimer_y;
		Gdk.ModifierType scrollWindowTimer_mod;
		
		void UpdateScrollWindowTimer (double x, double y, Gdk.ModifierType mod)
		{
			scrollWindowTimer_x = x;
			scrollWindowTimer_y = y;
			scrollWindowTimer_mod = mod;
			if (scrollWindowTimer == 0) {
				scrollWindowTimer = GLib.Timeout.Add (50, delegate {
					FireMotionEvent (scrollWindowTimer_x, scrollWindowTimer_y, scrollWindowTimer_mod);
					return true;
				});
			}
		}
		
		void RemoveScrollWindowTimer ()
		{
			if (scrollWindowTimer != 0) {
				GLib.Source.Remove (scrollWindowTimer);
				
				scrollWindowTimer = 0;
			}
		}
		
		Gdk.ModifierType lastState = ModifierType.None;
		void FireMotionEvent (double x, double y, Gdk.ModifierType state)
		{
			lastState = state;
			mx = x - textViewMargin.XOffset;
			my = y;

			ShowTooltip (state);

			double startPos;
			Margin margin;
			if (textViewMargin.inSelectionDrag) {
				margin = textViewMargin;
				startPos = textViewMargin.XOffset;
			} else {
				margin = GetMarginAtX (x, out startPos);
				if (margin != null && GdkWindow != null)
					GdkWindow.Cursor = margin.MarginCursor;
			}

			if (oldMargin != margin && oldMargin != null)
				oldMargin.MouseLeft ();
			
			if (margin != null) 
				margin.MouseHover (new MarginMouseEventArgs (this, mouseButtonPressed, x - startPos, y, EventType.MotionNotify, state));
			oldMargin = margin;
		}

		#region CustomDrag (for getting dnd data from toolbox items for example)
		string     customText;
		Gtk.Widget customSource;
		public void BeginDrag (string text, Gtk.Widget source, DragContext context)
		{
			customText = text;
			customSource = source;
			source.DragDataGet += CustomDragDataGet;
			source.DragEnd     += CustomDragEnd;
		}
		void CustomDragDataGet (object sender, Gtk.DragDataGetArgs args) 
		{
			args.SelectionData.Text = customText;
		}
		void CustomDragEnd (object sender, Gtk.DragEndArgs args) 
		{
			customSource.DragDataGet -= CustomDragDataGet;
			customSource.DragEnd -= CustomDragEnd;
			customSource = null;
			customText = null;
		}
		#endregion
		bool isMouseTrapped = false;
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			isMouseTrapped = true;
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing e)
		{
			isMouseTrapped = false;
			if (tipWindow != null && currentTooltipProvider.IsInteractive (this, tipWindow))
				DelayedHideTooltip ();
			else
				HideTooltip ();
			
			textViewMargin.HideCodeSegmentPreviewWindow ();
			
			if (e.Mode == CrossingMode.Normal) {
				if (GdkWindow != null)
					GdkWindow.Cursor = null;
				if (oldMargin != null)
					oldMargin.MouseLeft ();
			}
			return base.OnLeaveNotifyEvent (e); 
		}

		public double LineHeight {
			get {
				if (this.textViewMargin == null)
					return 16;
				return this.textViewMargin.LineHeight;
			}
		}
		
		public TextViewMargin TextViewMargin {
			get {
				return textViewMargin;
			}
		}
		
		public Margin IconMargin {
			get { return iconMargin; }
		}
		
		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			return Document.LogicalToVisualLocation (this.textEditorData, location);
		}
		
		public void CenterToCaret ()
		{
			CenterTo (Caret.Location);
		}
		
		public void CenterTo (int offset)
		{
			CenterTo (Document.OffsetToLocation (offset));
		}
		
		public void CenterTo (int line, int column)
		{
			CenterTo (new DocumentLocation (line, column));
		}
		
		public void CenterTo (DocumentLocation p)
		{
			if (isDisposed || p.Line < 0 || p.Line > Document.LineCount)
				return;
			SetAdjustments (this.Allocation);
			//			Adjustment adj;
			//adj.Upper
			if (this.textEditorData.VAdjustment.Upper < Allocation.Height) {
				this.textEditorData.VAdjustment.Value = 0;
				return;
			}
			
			//	int yMargin = 1 * this.LineHeight;
			double caretPosition = LineToY (p.Line);
			this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize / 2;
			
			if (this.textEditorData.HAdjustment.Upper < Allocation.Width)  {
				this.textEditorData.HAdjustment.Value = 0;
			} else {
				double caretX = ColumnToX (Document.GetLine (p.Line), p.Column);
				double textWith = Allocation.Width - textViewMargin.XOffset;
				if (this.textEditorData.HAdjustment.Value > caretX) {
					this.textEditorData.HAdjustment.Value = caretX;
				} else if (this.textEditorData.HAdjustment.Value + textWith < caretX + TextViewMargin.CharWidth) {
					double adjustment = System.Math.Max (0, caretX - textWith + TextViewMargin.CharWidth);
					this.textEditorData.HAdjustment.Value = adjustment;
				}
			}
		}

		public void ScrollTo (int offset)
		{
			ScrollTo (Document.OffsetToLocation (offset));
		}
		
		public void ScrollTo (int line, int column)
		{
			ScrollTo (new DocumentLocation (line, column));
		}
		
		public void ScrollTo (DocumentLocation p)
		{
			if (isDisposed || p.Line < 0 || p.Line > Document.LineCount || inCaretScroll)
				return;
			inCaretScroll = true;
			try {
				if (this.textEditorData.VAdjustment.Upper < Allocation.Height) {
					this.textEditorData.VAdjustment.Value = 0;
				} else {
					double yMargin = 1 * this.LineHeight;
					double caretPosition = LineToY (p.Line);
					if (this.textEditorData.VAdjustment.Value > caretPosition) {
						this.textEditorData.VAdjustment.Value = caretPosition;
					} else if (this.textEditorData.VAdjustment.Value + this.textEditorData.VAdjustment.PageSize - this.LineHeight < caretPosition + yMargin) {
						this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize + this.LineHeight + yMargin;
					}
				}
				
				if (this.textEditorData.HAdjustment.Upper < Allocation.Width)  {
					this.textEditorData.HAdjustment.Value = 0;
				} else {
					double caretX = ColumnToX (Document.GetLine (p.Line), p.Column);
					double textWith = Allocation.Width - textViewMargin.XOffset;
					if (this.textEditorData.HAdjustment.Value > caretX) {
						this.textEditorData.HAdjustment.Value = caretX;
					} else if (this.textEditorData.HAdjustment.Value + textWith < caretX + TextViewMargin.CharWidth) {
						double adjustment = System.Math.Max (0, caretX - textWith + TextViewMargin.CharWidth);
						this.textEditorData.HAdjustment.Value = adjustment;
					}
				}
			} finally {
				inCaretScroll = false;
			}
		}
		
		bool inCaretScroll = false;
		public void ScrollToCaret ()
		{
			ScrollTo (Caret.Location);
		}
		
		public void TryToResetHorizontalScrollPosition ()
		{
			int caretX = (int)ColumnToX (Document.GetLine (Caret.Line), Caret.Column);
			int textWith = Allocation.Width - (int)textViewMargin.XOffset;
			if (caretX < textWith - TextViewMargin.CharWidth) 
				this.textEditorData.HAdjustment.Value = 0;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
/*			if (longestLine == null) {
				foreach (LineSegment line in Document.Lines) {
					if (longestLine == null || line.EditableLength > longestLine.EditableLength)
						longestLine = line;
				}
			}*/
			if (this.GdkWindow != null) 
				this.GdkWindow.MoveResize (allocation);
			SetAdjustments (Allocation);
			QueueDraw ();
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			var modifier = !Platform.IsMac? Gdk.ModifierType.ControlMask
				//Mac window manager already uses control-scroll, so use command
				//Command might be either meta or mod1, depending on GTK version
				: (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);
			
			if ((evnt.State & modifier) !=0) {
				if (evnt.Direction == ScrollDirection.Up)
					Options.ZoomIn ();
				else if (evnt.Direction == ScrollDirection.Down)
					Options.ZoomOut ();
				
				this.QueueDraw ();
				if (isMouseTrapped)
					FireMotionEvent (mx + textViewMargin.XOffset, my, lastState);
				return true;
			}
			return base.OnScrollEvent (evnt); 
		}
		
		void SetHAdjustment ()
		{
			if (textEditorData.HAdjustment == null)
				return;
			textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (longestLine != null && this.textEditorData.HAdjustment != null) {
				double maxX = longestLineWidth;
				if (maxX > Allocation.Width)
					maxX += 2 * this.textViewMargin.CharWidth;
				double width = Allocation.Width - this.TextViewMargin.XOffset;
				this.textEditorData.HAdjustment.SetBounds (0, maxX, this.textViewMargin.CharWidth, width, width);
				if (maxX < width)
					this.textEditorData.HAdjustment.Value = 0;
			}
			textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
		}
		
		internal void SetAdjustments ()
		{
			SetAdjustments (Allocation);
		}
		
		internal void SetAdjustments (Gdk.Rectangle allocation)
		{
			if (this.textEditorData.VAdjustment != null) {
				double maxY = Document.LineCount > 1 ? LineToY (Document.LineCount) : 0;
				
				maxY += LineHeight;
				
				if (maxY > allocation.Height)
					maxY += 5 * this.LineHeight;
				
				this.textEditorData.VAdjustment.SetBounds (0, 
				                                           maxY, 
				                                           LineHeight,
				                                           allocation.Height,
				                                           allocation.Height);
				if (maxY < allocation.Height)
					this.textEditorData.VAdjustment.Value = 0;
			}
			SetHAdjustment ();
		}
		
		public int GetWidth (string text)
		{
			return this.textViewMargin.GetWidth (text);
		}
		
		void UpdateMarginXOffsets ()
		{
			double curX = 0;
			foreach (Margin margin in this.margins) {
				if (!margin.IsVisible)
					continue;
				margin.XOffset = curX;
				curX += margin.Width;
			}
		}
		
		void RenderMargins (Cairo.Context cr, Cairo.Context textViewCr, Cairo.Rectangle cairoRectangle)
		{
			this.TextViewMargin.rulerX = Options.RulerColumn * this.TextViewMargin.CharWidth - this.textEditorData.HAdjustment.Value;
			int startLine = YToLine (cairoRectangle.Y + this.textEditorData.VAdjustment.Value);
			double startY = LineToY (startLine);
			double curY = startY - this.textEditorData.VAdjustment.Value;
			bool setLongestLine = false;
			for (int visualLineNumber = startLine; ; visualLineNumber++) {
				int logicalLineNumber = visualLineNumber;
				LineSegment line      = Document.GetLine (logicalLineNumber);
				double lineHeight     = GetLineHeight (line);
				int lastFold = 0;
				foreach (FoldSegment fs in Document.GetStartFoldings (line).Where (fs => fs.IsFolded)) {
					lastFold = System.Math.Max (fs.EndOffset, lastFold);
				}
				if (lastFold >= DocumentLocation.MinLine)
					visualLineNumber = Document.OffsetToLineNumber (lastFold);
				foreach (Margin margin in this.margins) {
					if (!margin.IsVisible)
						continue;
					try {
						margin.Draw (margin == textViewMargin ? textViewCr : cr, cairoRectangle, line, logicalLineNumber, margin.XOffset, curY, lineHeight);
					} catch (Exception e) {
						System.Console.WriteLine (e);
					}
				}
				// take the line real render width from the text view margin rendering (a line can consist of more than 
				// one line and be longer (foldings!) ex. : someLine1[...]someLine2[...]someLine3)
				double lineWidth = textViewMargin.lastLineRenderWidth + HAdjustment.Value;
				if (longestLine == null || lineWidth > longestLineWidth) {
					longestLine = line;
					longestLineWidth = lineWidth;
					setLongestLine = true;
				}
				curY += lineHeight;
				if (curY > cairoRectangle.Y + cairoRectangle.Height)
					break;
			}
			
			foreach (Margin margin in this.margins) {
				if (!margin.IsVisible)
					continue;
				foreach (var drawer in margin.MarginDrawer)
					drawer.Draw (cr, cairoRectangle);
			}
			
			if (setLongestLine) 
				SetHAdjustment ();
		}
		
		/*
		protected override bool OnWidgetEvent (Event evnt)
		{
			System.Console.WriteLine(evnt);
			return base.OnWidgetEvent (evnt);
		}*/
		
		double oldVadjustment = 0;
		
		void UpdateAdjustments ()
		{
			int lastVisibleLine = Document.LogicalToVisualLine (Document.LineCount);
			if (oldRequest != lastVisibleLine) {
				SetAdjustments (this.Allocation);
				oldRequest = lastVisibleLine;
			}
		}

#if DEBUG_EXPOSE
		DateTime started = DateTime.Now;
#endif
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			if (this.isDisposed)
				return true;
			UpdateAdjustments ();
			
			var area = e.Region.Clipbox;
			var cairoArea = new Cairo.Rectangle (area.X, area.Y, area.Width, area.Height);
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window))
			using (Cairo.Context textViewCr = Gdk.CairoHelper.Create (e.Window)) {
				UpdateMarginXOffsets ();
				
				cr.LineWidth = Options.Zoom;
				textViewCr.LineWidth = Options.Zoom;
				textViewCr.Rectangle (textViewMargin.XOffset, 0, Allocation.Width - textViewMargin.XOffset, Allocation.Height);
				textViewCr.Clip ();
				
				RenderMargins (cr, textViewCr, cairoArea);
			
#if DEBUG_EXPOSE
				Console.WriteLine ("{0} expose {1},{2} {3}x{4}", (long)(DateTime.Now - started).TotalMilliseconds,
					e.Area.X, e.Area.Y, e.Area.Width, e.Area.Height);
#endif
				if (requestResetCaretBlink && HasFocus) {
					textViewMargin.ResetCaretBlink ();
					requestResetCaretBlink = false;
				}
				
				foreach (Animation animation in actors) {
					animation.Drawer.Draw (cr);
				}
				
				if (HasFocus && e.Area.Contains ((int)TextViewMargin.caretX, (int)TextViewMargin.caretY))
					textViewMargin.DrawCaret (e.Window);
				
				OnPainted (new PaintEventArgs (cr, cairoArea));
			}
			
			return true;
		}
		
		protected virtual void OnPainted (PaintEventArgs e)
		{
			EventHandler<PaintEventArgs> handler = this.Painted;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<PaintEventArgs> Painted;

		#region TextEditorData delegation
		public string EolMarker {
			get {
				return textEditorData.EolMarker;
			}
		}
		
		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get {
				return this.textEditorData.ColorStyle;
			}
		}
		
		public EditMode CurrentMode {
			get {
				return this.textEditorData.CurrentMode;
			}
			set {
				this.textEditorData.CurrentMode = value;
			}
		}
		
		public bool IsSomethingSelected {
			get {
				return this.textEditorData.IsSomethingSelected;
			}
		}
		
		public Selection MainSelection {
			get {
				return textEditorData.MainSelection;
			}
			set {
				textEditorData.MainSelection = value;
			}
		}
		
		public SelectionMode SelectionMode {
			get {
				return textEditorData.SelectionMode;
			}
			set {
				textEditorData.SelectionMode = value;
			}
		}

		public ISegment SelectionRange {
			get {
				return this.textEditorData.SelectionRange;
			}
			set {
				this.textEditorData.SelectionRange = value;
			}
		}
				
		public string SelectedText {
			get {
				return this.textEditorData.SelectedText;
			}
			set {
				this.textEditorData.SelectedText = value;
			}
		}
		
		public int SelectionAnchor {
			get {
				return this.textEditorData.SelectionAnchor;
			}
			set {
				this.textEditorData.SelectionAnchor = value;
			}
		}
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				return this.textEditorData.SelectedLines;
			}
		}
		
		public Adjustment HAdjustment {
			get {
				return this.textEditorData.HAdjustment;
			}
		}
		
		public Adjustment VAdjustment {
			get {
				return this.textEditorData.VAdjustment;
			}
		}
		
		public int Insert (int offset, string value)
		{
			return textEditorData.Insert (offset, value);
		}
		
		public void Remove (ISegment removeSegment)
		{
			textEditorData.Remove (removeSegment);
		}
		
		public void Remove (int offset, int count)
		{
			textEditorData.Remove (offset, count);
		}
		
		public int Replace (int offset, int count, string value)
		{
			return textEditorData.Replace (offset, count, value);
		}
		
		public void ClearSelection ()
		{
			this.textEditorData.ClearSelection ();
		}
		
		public void DeleteSelectedText ()
		{
			this.textEditorData.DeleteSelectedText ();
		}
		
		public void DeleteSelectedText (bool clearSelection)
		{
			this.textEditorData.DeleteSelectedText (clearSelection);
		}
		
		public void RunEditAction (Action<TextEditorData> action)
		{
			action (this.textEditorData);
		}
		
		public void SetSelection (int anchorOffset, int leadOffset)
		{
			this.textEditorData.SetSelection (anchorOffset, leadOffset);
		}
		
		public void SetSelection (DocumentLocation anchor, DocumentLocation lead)
		{
			this.textEditorData.SetSelection (anchor, lead);
		}
		
		public void ExtendSelectionTo (DocumentLocation location)
		{
			this.textEditorData.ExtendSelectionTo (location);
		}
		public void ExtendSelectionTo (int offset)
		{
			this.textEditorData.ExtendSelectionTo (offset);
		}
		public void SetSelectLines (int from, int to)
		{
			this.textEditorData.SetSelectLines (from, to);
		}
		
		public void InsertAtCaret (string text)
		{
			textEditorData.InsertAtCaret (text);
		}
		
		public bool CanEdit (int line)
		{
			return textEditorData.CanEdit (line);
		}
		
		public string GetLineText (int line)
		{
			return textEditorData.GetLineText (line);
		}
		
		public string GetLineText (int line, bool includeDelimiter)
		{
			return textEditorData.GetLineText (line, includeDelimiter);
		}
		
		/// <summary>
		/// Use with care.
		/// </summary>
		/// <returns>
		/// A <see cref="TextEditorData"/>
		/// </returns>
		public TextEditorData GetTextEditorData ()
		{
			return this.textEditorData;
		}
		
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
			CurrentMode.InternalSelectionChanged (this, textEditorData);
			if (SelectionChanged != null) 
				SelectionChanged (this, args);
		}
		#endregion
		
		#region Document delegation
		public int Length {
			get {
				return Document.Length;
			}
		}

		public string Text {
			get {
				return Document.Text;
			}
			set {
				Document.Text = value;
			}
		}

		public string GetTextBetween (int startOffset, int endOffset)
		{
			return Document.GetTextBetween (startOffset, endOffset);
		}
		
		public string GetTextBetween (DocumentLocation start, DocumentLocation end)
		{
			return Document.GetTextBetween (start, end);
		}
		
		public string GetTextBetween (int startLine, int startColumn, int endLine, int endColumn)
		{
			return Document.GetTextBetween (startLine, startColumn, endLine, endColumn);
		}

		public string GetTextAt (int offset, int count)
		{
			return Document.GetTextAt (offset, count);
		}

		public string GetTextAt (ISegment segment)
		{
			return Document.GetTextAt (segment);
		}
		
		public char GetCharAt (int offset)
		{
			return Document.GetCharAt (offset);
		}
		
		public IEnumerable<LineSegment> Lines {
			get {
				return Document.Lines;
			}
		}
		
		public int LineCount {
			get {
				return Document.LineCount;
			}
		}
		
		public int LocationToOffset (int line, int column)
		{
			return Document.LocationToOffset (line, column);
		}
		
		public int LocationToOffset (DocumentLocation location)
		{
			return Document.LocationToOffset (location);
		}
		
		public DocumentLocation OffsetToLocation (int offset)
		{
			return Document.OffsetToLocation (offset);
		}

		public string GetLineIndent (int lineNumber)
		{
			return Document.GetLineIndent (lineNumber);
		}
		
		public string GetLineIndent (LineSegment segment)
		{
			return Document.GetLineIndent (segment);
		}
		
		public LineSegment GetLine (int lineNumber)
		{
			return Document.GetLine (lineNumber);
		}
		
		public LineSegment GetLineByOffset (int offset)
		{
			return Document.GetLineByOffset (offset);
		}
		
		public int OffsetToLineNumber (int offset)
		{
			return Document.OffsetToLineNumber (offset);
		}
		#endregion
		
		#region Search & Replace
		
		bool highlightSearchPattern = false;
		
		public string SearchPattern {
			get {
				return this.textEditorData.SearchRequest.SearchPattern;
			}
			set {
				if (this.textEditorData.SearchRequest.SearchPattern != value) {
					this.textEditorData.SearchRequest.SearchPattern = value;
				}
			}
		}
		
		public ISearchEngine SearchEngine {
			get {
				return this.textEditorData.SearchEngine;
			}
			set {
				Debug.Assert (value != null);
				this.textEditorData.SearchEngine = value;
			}
		}
		
		public event EventHandler HighlightSearchPatternChanged;
		public bool HighlightSearchPattern {
			get {
				return highlightSearchPattern;
			}
			set {
				if (highlightSearchPattern != value) {
					this.highlightSearchPattern = value;
					if (HighlightSearchPatternChanged != null)
						HighlightSearchPatternChanged (this, EventArgs.Empty);
					textViewMargin.DisposeLayoutDict ();
					this.QueueDraw ();
				}
			}
		}
		
		public bool IsCaseSensitive {
			get {
				return this.textEditorData.SearchRequest.CaseSensitive;
			}
			set {
				this.textEditorData.SearchRequest.CaseSensitive = value;
			}
		}
		
		public bool IsWholeWordOnly {
			get {
				return this.textEditorData.SearchRequest.WholeWordOnly;
			}
			
			set {
				this.textEditorData.SearchRequest.WholeWordOnly = value;
			}
		}
		
		public SearchResult SearchForward (int fromOffset)
		{
			return textEditorData.SearchForward (fromOffset);
		}
		
		public SearchResult SearchBackward (int fromOffset)
		{
			return textEditorData.SearchBackward (fromOffset);
		}
		
		class CaretPulseAnimation : IAnimationDrawer
		{
			TextEditor editor;
			
			public double Percent { get; set; }
			
			public Gdk.Rectangle AnimationBounds {
				get {
					double x = editor.TextViewMargin.caretX;
					double y = editor.TextViewMargin.caretY;
					double extend = 100 * 5;
					int width = (int)(editor.TextViewMargin.charWidth + 2 * extend * editor.Options.Zoom / 2);
					return new Gdk.Rectangle ((int)(x - extend * editor.Options.Zoom / 2), 
					                          (int)(y - extend * editor.Options.Zoom),
					                          width,
					                          (int)(editor.LineHeight + 2 * extend * editor.Options.Zoom));
				}
			}
			
			public CaretPulseAnimation (TextEditor editor)
			{
				this.editor = editor;
			}
			
			public void Draw (Cairo.Context cr)
			{
				double x = editor.TextViewMargin.caretX;
				double y = editor.TextViewMargin.caretY;
				if (editor.Caret.Mode != CaretMode.Block)
					x -= editor.TextViewMargin.charWidth / 2;
				cr.Rectangle (editor.TextViewMargin.XOffset, 0, editor.Allocation.Width - editor.TextViewMargin.XOffset, editor.Allocation.Height);
				cr.Clip ();

				double extend = Percent * 5;
				double width = editor.TextViewMargin.charWidth + 2 * extend * editor.Options.Zoom / 2;
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, 
				                                                    x - extend * editor.Options.Zoom / 2, 
				                                                    y - extend * editor.Options.Zoom, 
				                                                    System.Math.Min (editor.TextViewMargin.charWidth / 2, width), 
				                                                    width,
				                                                    editor.LineHeight + 2 * extend * editor.Options.Zoom);
				Cairo.Color color = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.Caret.Color);
				color.A = 0.8;
				cr.LineWidth = editor.Options.Zoom;
				cr.Color = color;
				cr.Stroke ();
				cr.ResetClip ();
			}
		}
		
		public enum PulseKind {
			In, Out, Bounce
		}
		
		public class RegionPulseAnimation : IAnimationDrawer
		{
			TextEditor editor;
			
			public PulseKind Kind { get; set; }
			public double Percent { get; set; }
			
			Gdk.Rectangle region;
			
			public Gdk.Rectangle AnimationBounds {
				get {
					int x = region.X;
					int y = region.Y;
					int animationPosition = (int)(100 * 100);
					int width = (int)(region.Width + 2 * animationPosition * editor.Options.Zoom / 2);
					
					return new Gdk.Rectangle ((int)(x - animationPosition * editor.Options.Zoom / 2), 
					                          (int)(y - animationPosition * editor.Options.Zoom),
					                          width,
					                          (int)(region.Height + 2 * animationPosition * editor.Options.Zoom));
				}
			}
			
			public RegionPulseAnimation (TextEditor editor, Gdk.Point position, Gdk.Size size)
				: this (editor, new Gdk.Rectangle (position, size)) {}
			
			public RegionPulseAnimation (TextEditor editor, Gdk.Rectangle region)
			{
				if (region.X < 0 || region.Y < 0 || region.Width < 0 || region.Height < 0)
					throw new ArgumentException ("region is invalid");
				
				this.editor = editor;
				this.region = region;
			}
			
			public void Draw (Cairo.Context cr)
			{
				int x = region.X;
				int y = region.Y;
				int animationPosition = (int)(Percent * 100);
				
				cr.Rectangle (editor.TextViewMargin.XOffset, 0, editor.Allocation.Width - editor.TextViewMargin.XOffset, editor.Allocation.Height);
				cr.Clip ();

				int width = (int)(region.Width + 2 * animationPosition * editor.Options.Zoom / 2);
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, 
				                                                    (int)(x - animationPosition * editor.Options.Zoom / 2), 
				                                                    (int)(y - animationPosition * editor.Options.Zoom), 
				                                                    System.Math.Min (editor.TextViewMargin.charWidth / 2, width), 
				                                                    width,
				                                                    (int)(region.Height + 2 * animationPosition * editor.Options.Zoom));
				Cairo.Color color = Mono.TextEditor.Highlighting.Style.ToCairoColor (editor.ColorStyle.Caret.Color);
				color.A = 0.8;
				cr.LineWidth = editor.Options.Zoom;
				cr.Color = color;
				cr.Stroke ();
				cr.ResetClip ();
			}
		}
		
		Gdk.Rectangle RangeToRectangle (DocumentLocation start, DocumentLocation end)
		{
			if (start.Column < 0 || start.Line < 0 || end.Column < 0 || end.Line < 0)
				return Gdk.Rectangle.Zero;
			
			var startPt = this.LocationToPoint (start);
			var endPt = this.LocationToPoint (end);
			int width = endPt.X - startPt.X;
			
			if (startPt.Y != endPt.Y || startPt.X < 0 || startPt.Y < 0 || width < 0)
				return Gdk.Rectangle.Zero;
			
			return new Gdk.Rectangle (startPt.X, startPt.Y, width, (int)this.textViewMargin.LineHeight);
		}
		
		/// <summary>
		/// Initiate a pulse at the specified document location
		/// </summary>
		/// <param name="pulseLocation">
		/// A <see cref="DocumentLocation"/>
		/// </param>
		public void PulseCharacter (DocumentLocation pulseStart)
		{
			if (pulseStart.Column < 0 || pulseStart.Line < 0)
				return;
			var rect = RangeToRectangle (pulseStart, new DocumentLocation (pulseStart.Line, pulseStart.Column + 1));
			if (rect.X < 0 || rect.Y < 0 || System.Math.Max (rect.Width, rect.Height) <= 0)
				return;
			StartAnimation (new RegionPulseAnimation (this, rect) {
				Kind = PulseKind.Bounce
			});
		}

		
		public SearchResult FindNext (bool setSelection)
		{
			SearchResult result = textEditorData.FindNext (setSelection);
			TryToResetHorizontalScrollPosition ();
			AnimateSearchResult (result);
			return result;
		}

		public void StartCaretPulseAnimation ()
		{
			StartAnimation (new TextEditor.CaretPulseAnimation (this));
		}

		SearchHighlightPopupWindow popupWindow = null;
		
		public void StopSearchResultAnimation ()
		{
			if (popupWindow == null)
				return;
			popupWindow.StopPlaying ();
		}
		
		public void AnimateSearchResult (SearchResult result)
		{
			if (!IsComposited || !Options.EnableAnimations)
				return;
			TextViewMargin.MainSearchResult = result;
			if (result != null) {
				if (popupWindow != null) {
					popupWindow.StopPlaying ();
					popupWindow.Destroy ();
				}
				popupWindow = new SearchHighlightPopupWindow (this);
				popupWindow.Result = result;
				popupWindow.Popup ();
				popupWindow.Destroyed += delegate {
					popupWindow = null;
				};
			}
		}
		
		class SearchHighlightPopupWindow : BounceFadePopupWindow
		{
			public SearchResult Result {
				get;
				set;
			}
			
			public SearchHighlightPopupWindow (TextEditor editor) : base (editor)
			{
			}
			
			public override void Popup ()
			{
				ExpandWidth = (uint)Editor.LineHeight;
				ExpandHeight = (uint)Editor.LineHeight / 2;
				BounceEasing = Easing.Sine;
				Duration = 150;
				base.Popup ();
			}
			
			protected override void OnAnimationCompleted ()
			{
				base.OnAnimationCompleted ();
				DetachEvents ();
				Destroy ();
			}
			
			protected override void OnDestroyed ()
			{
				base.OnDestroyed ();
				if (layout != null)
					layout.Dispose ();
			}
			
			protected override Rectangle CalculateInitialBounds ()
			{
				LineSegment line = Editor.Document.GetLineByOffset (Result.Offset);
				int lineNr = Editor.Document.OffsetToLineNumber (Result.Offset);
				SyntaxMode mode = Editor.Document.SyntaxMode != null && Editor.Options.EnableSyntaxHighlighting ? Editor.Document.SyntaxMode : SyntaxMode.Default;
				int logicalRulerColumn = line.GetLogicalColumn(Editor.GetTextEditorData(), Editor.Options.RulerColumn);
				var lineLayout = Editor.textViewMargin.CreateLinePartLayout(mode, line, logicalRulerColumn, line.Offset, line.EditableLength, -1, -1);
				if (lineLayout == null)
					return Gdk.Rectangle.Zero;
				
				int l, x1, x2;
				int index = Result.Offset - line.Offset - 1;
				if (index >= 0) {
					lineLayout.Layout.IndexToLineX (index, true, out l, out x1);
				} else {
					l = x1 = 0;
				}
				
				index = Result.Offset - line.Offset - 1 + Result.Length;
				if (index >= 0) {
					lineLayout.Layout.IndexToLineX (index, true, out l, out x2);
				} else {
					x2 = 0;
					Console.WriteLine ("Invalid end index :" + index);
				}
				
				double y = Editor.LineToY (lineNr) - Editor.VAdjustment.Value ;
				double w = (x2 - x1) / Pango.Scale.PangoScale;
				double spaceX = System.Math.Ceiling (w / 3);
				double spaceY = Editor.LineHeight;
				if (layout != null)
					layout.Dispose ();
				
				return new Gdk.Rectangle ((int)(x1 / Pango.Scale.PangoScale + Editor.TextViewMargin.XOffset + Editor.TextViewMargin.TextStartPosition - Editor.HAdjustment.Value - spaceX), 
					(int)(y - spaceY), 
					(int)(w + spaceX * 2), 
					(int)(Editor.LineHeight + spaceY * 2));
			}
			
			protected override Gdk.Pixbuf RenderInitialPixbuf (Gdk.Window parentwindow, Gdk.Rectangle bounds)
			{
				// deprecated.
				return null;
			}
			/*
			protected override bool OnAnimationActorStep (Actor<BounceFadePopupWindow> actor)
			{
				if (actor.Expired) {
					OnAnimationCompleted ();
					return false;
				}
				
				// for the first half, use an easing
				if (actor.Percent < 0.5) {
					scale = Choreographer.Compose (actor.Percent * 2, BounceEasing);
				} else {
					scale = Choreographer.Compose (1.0, BounceEasing);
					alpha = 2.0 - actor.Percent * 2;
				}
				return true;
			}*/
			
			Pango.Layout layout = null;
			int layoutWidth, layoutHeight;
			
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				try {
					using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
						cr.SetSourceRGBA (1, 1, 1, 0);
						cr.Operator = Cairo.Operator.Source; 
						cr.Paint ();
						
						cr.Translate (width / 2,  height / 2);
						cr.Scale (1 + scale / 4, 1 + scale / 4);
						if (layout == null) {
							layout = cr.CreateLayout ();
							layout.FontDescription = Editor.Options.Font;
							layout.SetMarkup (Editor.Document.SyntaxMode.GetMarkup (Editor.Document, Editor.Options, Editor.ColorStyle, Result.Offset, Result.Length, true));
							layout.GetPixelSize (out layoutWidth, out layoutHeight);
						}
						
						FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, -layoutWidth / 2 - 2 + 2, -Editor.LineHeight / 2 + 2, System.Math.Min (10, layoutWidth), layoutWidth + 4, Editor.LineHeight);
						var color = TextViewMargin.DimColor (Editor.ColorStyle.SearchTextMainBg, 0.3);
						color.A = 0.5 * opacity;
						cr.Color = color;
						cr.Fill (); 
						
						FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, -layoutWidth / 2 -2, -Editor.LineHeight / 2, System.Math.Min (10, layoutWidth), layoutWidth + 4, Editor.LineHeight);
						using (Cairo.LinearGradient gradient = new Cairo.LinearGradient (0, -Editor.LineHeight / 2, 0, Editor.LineHeight / 2)) {
							color = TextViewMargin.DimColor (Editor.ColorStyle.SearchTextMainBg, 1.1);
							color.A = opacity;
							gradient.AddColorStop (0, color);
							color = TextViewMargin.DimColor (Editor.ColorStyle.SearchTextMainBg, 0.9);
							color.A = opacity;
							gradient.AddColorStop (1, color);
							cr.Pattern = gradient;
							cr.Fill (); 
						}
						
						cr.Translate (-layoutWidth / 2, -layoutHeight / 2);
						cr.ShowLayout (layout);
					}
					
				} catch (Exception e) {
					Console.WriteLine ("Exception in animation:" + e);
				}
				return true;
			}
		}
		
		public SearchResult FindPrevious (bool setSelection)
		{
			SearchResult result = textEditorData.FindPrevious (setSelection);
			TryToResetHorizontalScrollPosition ();
			AnimateSearchResult (result);
			return result;
		}
		
		public bool Replace (string withPattern)
		{
			return textEditorData.SearchReplace (withPattern, true);
		}
		
		public int ReplaceAll (string withPattern)
		{
			return textEditorData.SearchReplaceAll (withPattern);
		}
		#endregion
	
		#region Tooltips
		
		// Tooltip fields
		const int TooltipTimeout = 650;
		TooltipItem tipItem;
		
		int tipX, tipY;
		uint tipHideTimeoutId = 0;
		uint tipShowTimeoutId = 0;
		Gtk.Window tipWindow;
		List<ITooltipProvider> tooltipProviders = new List<ITooltipProvider> ();
		ITooltipProvider currentTooltipProvider;
		
		// Data for the next tooltip to be shown
		int nextTipOffset = 0;
		int nextTipX=0; int nextTipY=0;
		Gdk.ModifierType nextTipModifierState = ModifierType.None;
		DateTime nextTipScheduledTime; // Time at which we want the tooltip to show
		
		public List<ITooltipProvider> TooltipProviders {
			get { return tooltipProviders; }
		}
		

		void ShowTooltip (Gdk.ModifierType modifierState)
		{
			var loc = PointToLocation (mx, my);
			if (loc.IsEmpty)
				return;
			ShowTooltip (modifierState, 
			             Document.LocationToOffset (loc),
			             (int)mx,
			             (int)my);
		}
		
		void ShowTooltip (Gdk.ModifierType modifierState, int offset, int xloc, int yloc)
		{
			CancelSheduledShow ();
			
			if (tipWindow != null && currentTooltipProvider.IsInteractive (this, tipWindow)) {
				int wx, ww, wh;
				tipWindow.GetSize (out ww, out wh);
				wx = tipX - ww/2;
				if (xloc >= wx && xloc < wx + ww && yloc >= tipY && yloc < tipY + 20 + wh)
					return;
			}
			if (tipItem != null && tipItem.ItemSegment != null && !tipItem.ItemSegment.Contains (offset)) 
				HideTooltip ();
			
			nextTipX = xloc;
			nextTipY = yloc;
			nextTipOffset = offset;
			nextTipModifierState = modifierState;
			nextTipScheduledTime = DateTime.Now + TimeSpan.FromMilliseconds (TooltipTimeout);

			// If a tooltip is already scheduled, there is no need to create a new timer.
			if (tipShowTimeoutId == 0)
				tipShowTimeoutId = GLib.Timeout.Add (TooltipTimeout, TooltipTimer);
		}
		
		bool TooltipTimer ()
		{
			// This timer can't be reused, so reset the var now
			tipShowTimeoutId = 0;
			
			// Cancelled?
			if (nextTipOffset == -1)
				return false;
			
			int remainingMs = (int) (nextTipScheduledTime - DateTime.Now).TotalMilliseconds;
			if (remainingMs > 50) {
				// Still some significant time left. Re-schedule the timer
				tipShowTimeoutId = GLib.Timeout.Add ((uint) remainingMs, TooltipTimer);
				return false;
			}
			
			// Find a provider
			ITooltipProvider provider = null;
			TooltipItem item = null;
			
			foreach (ITooltipProvider tp in tooltipProviders) {
				try {
					item = tp.GetItem (this, nextTipOffset);
				} catch (Exception e) {
					System.Console.WriteLine ("Exception in tooltip provider " + tp + " GetItem:");
					System.Console.WriteLine (e);
				}
				if (item != null) {
					provider = tp;
					break;
				}
			}
			
			if (item != null) {
				// Tip already being shown for this item?
				if (tipWindow != null && tipItem != null && tipItem.Equals (item)) {
					CancelScheduledHide ();
					return false;
				}
				
				tipX = nextTipX;
				tipY = nextTipY;
				tipItem = item;
				
				Gtk.Window tw = provider.CreateTooltipWindow (this, nextTipOffset, nextTipModifierState, item);
				if (tw == tipWindow)
					return false;
				HideTooltip ();
				if (tw == null)
					return false;
				
				CancelSheduledShow ();
				DoShowTooltip (provider, tw, tipX, tipY);
				tipShowTimeoutId = 0;
			} else
				HideTooltip ();
			return false;
		}
		
		void DoShowTooltip (ITooltipProvider provider, Gtk.Window liw, int xloc, int yloc)
		{
			CancelSheduledShow ();
			
			tipWindow = liw;
			currentTooltipProvider = provider;
			
			tipWindow.EnterNotifyEvent += delegate {
				CancelScheduledHide ();
			};
			
			int ox = 0, oy = 0;
			if (GdkWindow != null)
				this.GdkWindow.GetOrigin (out ox, out oy);
			
			int w;
			double xalign;
			provider.GetRequiredPosition (this, liw, out w, out xalign);
			w += 10;
			
			int x = xloc + ox + (int)textViewMargin.XOffset - (int) ((double)w * xalign);
			Gdk.Rectangle geometry = Screen.GetMonitorGeometry (Screen.GetMonitorAtPoint (ox + xloc, oy + yloc));
			
			if (x + w >= geometry.Right)
				x = geometry.Right - w;
			if (x < geometry.Left)
				x = geometry.Left;
			
			tipWindow.Move (x, yloc + oy + 10);
			tipWindow.ShowAll ();
		}
		

		public void HideTooltip ()
		{
			CancelScheduledHide ();
			CancelSheduledShow ();
			
			if (tipWindow != null) {
				tipWindow.Destroy ();
				tipWindow = null;
			}
		}
		
		void DelayedHideTooltip ()
		{
			CancelScheduledHide ();
			tipHideTimeoutId = GLib.Timeout.Add (300, delegate {
				HideTooltip ();
				return false;
			});
		}
		
		void CancelScheduledHide ()
		{
			CancelSheduledShow ();
			if (tipHideTimeoutId != 0) {
				GLib.Source.Remove (tipHideTimeoutId);
				tipHideTimeoutId = 0;
			}
		}
		
		void CancelSheduledShow ()
		{
			// Don't remove the timeout handler since it may be reused
			nextTipOffset = -1;
		}
		
		void OnDocumentStateChanged (object s, EventArgs a)
		{
			HideTooltip ();
		}
		
		void OnTextSet (object sender, EventArgs e)
		{
			LineSegment longest = longestLine;
			foreach (LineSegment line in Document.Lines) {
				if (longest == null || line.EditableLength > longest.EditableLength)
					longest = line;
			}
			if (longest != longestLine) {
				int width = (int)(textViewMargin.GetLayout (longest).PangoWidth / Pango.Scale.PangoScale);
				
				if (width > this.longestLineWidth) {
					this.longestLineWidth = width;
					this.longestLine = longest;
				}
			}
		}
		#endregion
		
		#region Coordinate transformation
		public DocumentLocation PointToLocation (double xp, double yp)
		{
			return TextViewMargin.PointToLocation (xp, yp);
		}

		public DocumentLocation PointToLocation (Cairo.Point p)
		{
			return TextViewMargin.PointToLocation (p);
		}
		
		public DocumentLocation PointToLocation (Cairo.PointD p)
		{
			return TextViewMargin.PointToLocation (p);
		}

		public Cairo.Point LocationToPoint (DocumentLocation loc)
		{
			return TextViewMargin.LocationToPoint (loc);
		}

		public Cairo.Point LocationToPoint (int line, int column)
		{
			return TextViewMargin.LocationToPoint (line, column);
		}
		
		public Cairo.Point LocationToPoint (int line, int column, bool useAbsoluteCoordinates)
		{
			return TextViewMargin.LocationToPoint (line, column, useAbsoluteCoordinates);
		}
		
		public Cairo.Point LocationToPoint (DocumentLocation loc, bool useAbsoluteCoordinates)
		{
			return TextViewMargin.LocationToPoint (loc, useAbsoluteCoordinates);
		}

		public double ColumnToX (LineSegment line, int column)
		{
			return TextViewMargin.ColumnToX (line, column);
		}
		
		/// <summary>
		/// Calculates the line number at line start (in one visual line could be several logical lines be displayed).
		/// </summary>
		public int YToLine (double yPos)
		{
			return TextViewMargin.YToLine (yPos);
		}
		
		public double LineToY (int logicalLine)
		{
			return TextViewMargin.LineToY (logicalLine);
		}
		
		public double GetLineHeight (LineSegment line)
		{
			return TextViewMargin.GetLineHeight (line);
		}
		
		public double GetLineHeight (int logicalLineNumber)
		{
			return TextViewMargin.GetLineHeight (logicalLineNumber);
		}
		#endregion
		
		#region Animation
		Stage<Animation> animationStage = new Stage<Animation> ();
		List<Animation> actors = new List<Animation> ();
		
		protected void InitAnimations ()
		{
			animationStage.ActorStep += OnAnimationActorStep;
			animationStage.Iteration += OnAnimationIteration;
		}
		
		void DisposeAnimations ()
		{
			if (animationStage != null) {
				animationStage.Playing = false;
				animationStage.ActorStep -= OnAnimationActorStep;
				animationStage.Iteration -= OnAnimationIteration;
				animationStage = null;
			}
			
			if (actors != null) {
				foreach (Animation actor in actors) {
					if (actor is IDisposable)
						((IDisposable)actor).Dispose ();
				}
				actors.Clear ();
				actors = null;
			}
		}
		
		Animation StartAnimation (IAnimationDrawer drawer)
		{
			return StartAnimation (drawer, 300);
		}
		
		Animation StartAnimation (IAnimationDrawer drawer, uint duration)
		{
			return StartAnimation (drawer, duration, Easing.Linear);
		}
		
		Animation StartAnimation (IAnimationDrawer drawer, uint duration, Easing easing)
		{
			if (!Options.EnableAnimations)
				return null;
			Animation animation = new Animation (drawer, duration, easing, Blocking.Upstage);
			animationStage.Add (animation, duration);
			actors.Add (animation);
			return animation;
		}
		
		bool OnAnimationActorStep (Actor<Animation> actor)
		{
			switch (actor.Target.AnimationState) {
			case AnimationState.Coming:
				actor.Target.Drawer.Percent = actor.Percent;
				if (actor.Expired) {
					actor.Target.AnimationState = AnimationState.Going;
					actor.Reset ();
					return true;
				}
				break;
			case AnimationState.Going:
				if (actor.Expired) {
					RemoveAnimation (actor.Target);
					return false;
				}
				actor.Target.Drawer.Percent = 1.0 - actor.Percent;
				break;
			}
			return true;
		}
		
		void RemoveAnimation (Animation animation)
		{
			if (animation == null)
				return;
			Rectangle bounds = animation.Drawer.AnimationBounds;
			actors.Remove (animation);
			if (animation is IDisposable)
				((IDisposable)animation).Dispose ();
			QueueDrawArea (bounds.X, bounds.Y, bounds.Width, bounds.Height);
		}
		
		void OnAnimationIteration (object sender, EventArgs args)
		{
			foreach (Animation actor in actors) {
				Rectangle bounds = actor.Drawer.AnimationBounds;
				QueueDrawArea (bounds.X, bounds.Y, bounds.Width, bounds.Height);
			}
		}
		#endregion
		
		internal void FireLinkEvent (string link, uint button, ModifierType modifierState)
		{
			if (LinkRequest != null)
				LinkRequest (this, new LinkEventArgs (link, button, modifierState));
		}
		
		public event EventHandler<LinkEventArgs> LinkRequest;
		
		/// <summary>
		/// Inserts a margin at the specified list position
		/// </summary>
		public void InsertMargin (int index, Margin margin)
		{
			margins.Insert (index, margin);
			RedrawFromLine (0);
		}
		
		/// <summary>
		/// Checks whether the editor has a margin of a given type
		/// </summary>
		public bool HasMargin (Type marginType)
		{
			return margins.Exists((margin) => { return marginType.IsAssignableFrom (margin.GetType ()); });
		}
		
		/// <summary>
		/// Gets the first margin of a given type
		/// </summary>
		public Margin GetMargin (Type marginType)
		{
			return margins.Find((margin) => { return marginType.IsAssignableFrom (margin.GetType ()); });
		}
		bool requestResetCaretBlink = false;
		public void RequestResetCaretBlink ()
		{
			if (this.IsFocus)
				requestResetCaretBlink = true;
		}

		void UpdateLinesOnTextMarkerHeightChange (object sender, LineEventArgs e)
		{
			if (!e.Line.Markers.Any (m => m is IExtendingTextMarker))
				return;
			double currentHeight = GetLineHeight (e.Line);
			double h;
			if (!lineHeights.TryGetValue (e.Line.Offset, out h))
				h = TextViewMargin.LineHeight;
			if (h != currentHeight)
				textEditorData.Document.CommitLineToEndUpdate (textEditorData.Document.OffsetToLineNumber (e.Line.Offset));
			lineHeights[e.Line.Offset] = currentHeight;
		}

		class SetCaret 
		{
			TextEditor view;
			int line, column;
			bool highlightCaretLine;
			
			public SetCaret (TextEditor view, int line, int column, bool highlightCaretLine)
			{
				this.view = view;
				this.line = line;
				this.column = column;
				this.highlightCaretLine = highlightCaretLine;
 			}
			
			public void Run (object sender, EventArgs e)
			{
				if (view.isDisposed)
					return;
				line = System.Math.Min (line, view.Document.LineCount);
				view.Caret.AutoScrollToCaret = false;
				try {
					view.Caret.Location = new DocumentLocation (line, column);
					view.GrabFocus ();
					view.CenterToCaret ();
					if (view.TextViewMargin.XOffset == 0)
						view.HAdjustment.Value = 0;
					view.SizeAllocated -= Run;
				} finally {
					view.Caret.AutoScrollToCaret = true;
					if (highlightCaretLine) {
						view.TextViewMargin.HighlightCaretLine = true;
						view.StartCaretPulseAnimation ();
					}
				}
			}
		}

		public void SetCaretTo (int line, int column)
		{
			SetCaretTo (line, column, true);
		}

		public void SetCaretTo (int line, int column, bool highlight)
		{
			if (line < DocumentLocation.MinLine)
				throw new ArgumentException ("line < MinLine");
			if (column < DocumentLocation.MinColumn)
				throw new ArgumentException ("column < MinColumn");
			
			if (!IsRealized) {
				SetCaret setCaret = new SetCaret (this, line, column, highlight);
				SizeAllocated += setCaret.Run;
			} else {
				new SetCaret (this, line, column, highlight).Run (null, null);
			}
		}
	}

	public interface ITextEditorDataProvider
	{
		TextEditorData GetTextEditorData ();
	}
	
	[Serializable]
	public sealed class PaintEventArgs : EventArgs
	{
		public Cairo.Context Context {
			get;
			set;
		}
		
		public Cairo.Rectangle Area {
			get;
			set;
		}
		
		public PaintEventArgs (Cairo.Context context, Cairo.Rectangle area)
		{
			this.Context = context;
			this.Area = area;
		}
	}
}


