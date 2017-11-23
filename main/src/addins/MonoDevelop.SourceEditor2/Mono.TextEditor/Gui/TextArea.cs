//
// TextArea.cs
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

using MonoDevelop.Components.AtkCocoaHelper;

using Gdk;
using Gtk;
using GLib;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	class TextArea : Container, ITextEditorDataProvider
	{

		TextEditorData textEditorData;
		
		protected IconMargin       iconMargin;
		protected ActionMargin     actionMargin;
		protected GutterMargin     gutterMargin;
		protected FoldMarkerMargin foldMarkerMargin;
		protected TextViewMargin   textViewMargin;

		DocumentLine longestLine      = null;
		double      longestLineWidth = -1;
		
		List<Margin> margins = new List<Margin> ();
		int oldRequest = -1;
		
		bool isDisposed = false;
		IMMulticontext imContext;
		Gdk.EventKey lastIMEvent;
		Gdk.Key lastIMEventMappedKey;
		uint lastIMEventMappedChar;
		Gdk.ModifierType lastIMEventMappedModifier;
		bool sizeHasBeenAllocated;
		bool imContextNeedsReset;
		string currentStyleName;
		
		double mx, my;
		
		public TextDocument Document {
			get {
				return textEditorData.Document;
			}
		}

		public bool IsDisposed {
			get {
				return textEditorData.IsDisposed;
			}
		}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Mono.TextEditor.MonoTextEditor"/> converts tabs to spaces.
		/// It is possible to overwrite the default options value for certain languages (like F#).
		/// </summary>
		/// <value>
		/// <c>true</c> if tabs to spaces should be converted; otherwise, <c>false</c>.
		/// </value>
		public bool TabsToSpaces {
			get {
				return textEditorData.TabsToSpaces;
			}
			set {
				textEditorData.TabsToSpaces = value;
			}
		}
		
		public Mono.TextEditor.CaretImpl Caret {
			get {
				return textEditorData.Caret;
			}
		}
		
		protected internal IMMulticontext IMContext {
			get { return imContext; }
		}

		public MenuItem CreateInputMethodMenuItem (string label)
		{
			if (GtkWorkarounds.GtkMinorVersion >= 16) {
				bool showMenu = (bool) GtkWorkarounds.GetProperty (Settings, "gtk-show-input-method-menu").Val;
				if (!showMenu)
					return null;
			}
			MenuItem imContextMenuItem = new MenuItem (label);
			Menu imContextMenu = new Menu ();
			imContextMenuItem.Submenu = imContextMenu;
			IMContext.AppendMenuitems (imContextMenu);
			return imContextMenuItem;
		}

		[DllImport (PangoUtil.LIBGTK, CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_im_multicontext_set_context_id (IntPtr context, string context_id);

		[DllImport (PangoUtil.LIBGTK, CallingConvention = CallingConvention.Cdecl)]
		static extern string gtk_im_multicontext_get_context_id (IntPtr context);
		
		[GLib.Property ("im-module")]
		public string IMModule {
			get {
				if (GtkWorkarounds.GtkMinorVersion < 16 || imContext == null)
					return null;
				return gtk_im_multicontext_get_context_id (imContext.Handle);
			}
			set {
				if (GtkWorkarounds.GtkMinorVersion < 16 || imContext == null)
					return;
				gtk_im_multicontext_set_context_id (imContext.Handle, value);
			}
		}
		
		internal ITextEditorOptions Options {
			get {
				return textEditorData.Options;
			}
			set {
				if (textEditorData.Options != null)
					textEditorData.Options.Changed -= OptionsChanged;
				textEditorData.Options = value;
				if (textEditorData.Options != null) {
					textEditorData.Options.Changed += OptionsChanged;
					OptionsChanged (null, null);
				}
			}
		}
		
		
		public string FileName {
			get {
				return Document.FileName;
			}
		}
		
		public string MimeType {
			get {
				return Document.MimeType;
			}
		}

		void HandleTextEditorDataDocumentMarkerChange (object sender, TextMarkerEvent e)
		{
			if (e.TextMarker is IExtendingTextLineMarker) {
				int lineNumber = e.Line.LineNumber;
				if (lineNumber <= LineCount) {
					try {
						textEditorData.HeightTree.SetLineHeight (lineNumber, GetLineHeight (e.Line));
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				}
			}
		}
		
		void HAdjustmentValueChanged (object sender, EventArgs args)
		{
			var alloc = this.Allocation;
			alloc.X = alloc.Y = 0;

			HAdjustmentValueChanged ();
		}
		
		protected virtual void HAdjustmentValueChanged ()
		{
			HideTooltip (false);
			double value = this.textEditorData.HAdjustment.Value;
			if (value != System.Math.Round (value)) {
				value = System.Math.Round (value);
				this.textEditorData.HAdjustment.Value = value;
			}
			textViewMargin.HideCodeSegmentPreviewWindow ();
			QueueDrawArea ((int)this.textViewMargin.XOffset, 0, this.Allocation.Width - (int)this.textViewMargin.XOffset, this.Allocation.Height);
			OnHScroll (EventArgs.Empty);
			SetChildrenPositions (Allocation);
		}
		
		void VAdjustmentValueChanged (object sender, EventArgs args)
		{
			var alloc = this.Allocation;
			alloc.X = alloc.Y = 0;
			VAdjustmentValueChanged ();
			SetChildrenPositions (alloc);
		}
		
		protected virtual void VAdjustmentValueChanged ()
		{
			HideTooltip (false);
			textViewMargin.HideCodeSegmentPreviewWindow ();
			double value = this.textEditorData.VAdjustment.Value;
			if (value != System.Math.Round (value)) {
				value = System.Math.Round (value);
				this.textEditorData.VAdjustment.Value = value;
			}
			if (isMouseTrapped)
				FireMotionEvent (mx + textViewMargin.XOffset, my, lastState);
			
			double delta = value - this.oldVadjustment;
			oldVadjustment = value;
			TextViewMargin.caretY -= delta;
			
			if (System.Math.Abs (delta) >= Allocation.Height - this.LineHeight * 2 || this.TextViewMargin.InSelectionDrag) {
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

		void UnregisterAdjustments ()
		{
			if (textEditorData.HAdjustment != null)
				textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (textEditorData.VAdjustment != null)
				textEditorData.VAdjustment.ValueChanged -= VAdjustmentValueChanged;
		}

		internal void SetTextEditorScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			if (textEditorData == null)
				return;
			UnregisterAdjustments ();
			
			if (hAdjustement == null || vAdjustement == null)
				return;
			this.textEditorData.HAdjustment = hAdjustement;
			this.textEditorData.VAdjustment = vAdjustement;
			
			this.textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
			this.textEditorData.VAdjustment.ValueChanged += VAdjustmentValueChanged;
		}

		internal TextArea (TextDocument doc, ITextEditorOptions options, EditMode initialMode)
		{
			GtkWorkarounds.FixContainerLeak (this);
			this.Events = EventMask.PointerMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask | EventMask.VisibilityNotifyMask | EventMask.FocusChangeMask | EventMask.ScrollMask | EventMask.KeyPressMask | EventMask.KeyReleaseMask;
			base.CanFocus = true;

			// This is required to properly handle resizing and rendering of children
			ResizeMode = ResizeMode.Queue;
			snooperID = Gtk.Key.SnooperInstall (TooltipKeySnooper);
		}

		uint snooperID;

		int TooltipKeySnooper (Gtk.Widget widget, EventKey evnt)
		{
			if (evnt != null && (evnt.Key == Gdk.Key.Alt_L || evnt.Key == Gdk.Key.Alt_R)) {
				if (tipWindow != null && (nextTipModifierState & ModifierType.Mod1Mask) == 0) {
					nextTipModifierState |= ModifierType.Mod1Mask;
					nextTipX = tipX;
					nextTipY = tipY;
					nextTipOffset = tipOffset;
					nextTipScheduledTime = DateTime.FromBinary (0);
					tipItem = null;
					TooltipTimer ();
				}
			}
			return 0; //FALSE
		}

		MonoTextEditor editor;
		internal void Initialize (MonoTextEditor editor, TextDocument doc, ITextEditorOptions options, EditMode initialMode)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			this.editor = editor;
			textEditorData = new TextEditorData (doc);
			textEditorData.RecenterEditor += TextEditorData_RecenterEditor; 
			textEditorData.Document.TextChanged += OnDocumentStateChanged;
			textEditorData.Document.MarkerAdded += HandleTextEditorDataDocumentMarkerChange;
			textEditorData.Document.MarkerRemoved += HandleTextEditorDataDocumentMarkerChange;
			
			textEditorData.CurrentMode = initialMode;
			
			this.textEditorData.Options = options ?? TextEditorOptions.DefaultOptions;


			textEditorData.Parent = editor;

			iconMargin = new IconMargin (editor);
			iconMargin.Accessible.Label = GettextCatalog.GetString ("Icon Margin");
			iconMargin.Accessible.Help = GettextCatalog.GetString ("Icon margin contains breakpoints and bookmarks");
			iconMargin.Accessible.Identifier = "TextArea.IconMargin";
			iconMargin.Accessible.GtkParent = this;
			Accessible.AddAccessibleElement (iconMargin.Accessible);

			gutterMargin = new GutterMargin (editor);
			gutterMargin.Accessible.Label = GettextCatalog.GetString ("Line Numbers");
			gutterMargin.Accessible.Help = GettextCatalog.GetString ("Shows the line numbers for the current file");
			gutterMargin.Accessible.Identifier = "TextArea.GutterMargin";
			gutterMargin.Accessible.GtkParent = this;
			Accessible.AddAccessibleElement (gutterMargin.Accessible);

			actionMargin = new ActionMargin (editor);
			actionMargin.Accessible.Identifier = "TextArea.ActionMargin";
			actionMargin.Accessible.GtkParent = this;
			Accessible.AddAccessibleElement (actionMargin.Accessible);

			foldMarkerMargin = new FoldMarkerMargin (editor);
			foldMarkerMargin.Accessible.Label = GettextCatalog.GetString ("Fold Margin");
			foldMarkerMargin.Accessible.Help = GettextCatalog.GetString ("Shows method and class folds");
			foldMarkerMargin.Accessible.Identifier = "TextArea.FoldMarkerMargin";
			foldMarkerMargin.Accessible.GtkParent = this;
			Accessible.AddAccessibleElement (foldMarkerMargin.Accessible);

			textViewMargin = new TextViewMargin (editor);
			textViewMargin.Accessible.Label = GettextCatalog.GetString ("Text Editor");
			textViewMargin.Accessible.Help = GettextCatalog.GetString ("Edit the current file");
			textViewMargin.Accessible.Identifier = "TextArea.TextViewMargin";
			textViewMargin.Accessible.GtkParent = this;
			Accessible.AddAccessibleElement (textViewMargin.Accessible);

			margins.Add (iconMargin);
			margins.Add (gutterMargin);
			margins.Add (actionMargin);
			margins.Add (foldMarkerMargin);

			margins.Add (textViewMargin);
			this.textEditorData.SelectionChanged += TextEditorDataSelectionChanged;
			this.textEditorData.UpdateAdjustmentsRequested += TextEditorDatahandleUpdateAdjustmentsRequested;
			Document.DocumentUpdated += DocumentUpdatedHandler;
			
			this.textEditorData.Options.Changed += OptionsChanged;
			
			
			Gtk.TargetList list = new Gtk.TargetList ();
			list.AddTextTargets (ClipboardActions.CopyOperation.TextType);
			Gtk.Drag.DestSet (this, DestDefaults.All, (TargetEntry[])list, DragAction.Move | DragAction.Copy);
			
			imContext = new IMMulticontext ();
			imContext.Commit += IMCommit;
			
			imContext.UsePreedit = true;
			imContext.PreeditChanged += PreeditStringChanged;
			
			imContext.RetrieveSurrounding += delegate (object o, RetrieveSurroundingArgs args) {
				//use a single line of context, whole document would be very expensive
				//FIXME: UTF16 surrogates handling for caret offset? only matters for astral plane
				var text = Document.GetLineText (Caret.Line, false);
				// Gtk#, with some input methods, causes
				// "Gtk-Critical: IA__gtk_im_context_set_surrounding: assertion 'cursor_index >= 0 && cursor_index <= len' failed"
				// so, do not try to attempt erroneous imcontext call.
				if (Caret.Column < text.Length)
					imContext.SetSurrounding (text, Caret.Column);
				args.RetVal = true;
			};
			
			imContext.SurroundingDeleted += delegate (object o, SurroundingDeletedArgs args) {
				//FIXME: UTF16 surrogates handling for offset and NChars? only matters for astral plane
				var line = Document.GetLine (Caret.Line);
				Document.RemoveText (line.Offset + args.Offset, args.NChars);
				args.RetVal = true;
			};
			
			using (Pixmap inv = new Pixmap (null, 1, 1, 1)) {
				invisibleCursor = new Cursor (inv, inv, Gdk.Color.Zero, Gdk.Color.Zero, 0, 0);
			}
			
			InitAnimations ();
			this.Document.HeightChanged += HandleDocumentHeightChanged;
			this.textEditorData.HeightTree.LineUpdateFrom += HeightTree_LineUpdateFrom;
//#if ATK
//			TextEditorAccessible.Factory.Init (this);
//#endif

			OptionsChanged (this, EventArgs.Empty);
		}

		void TextEditorData_RecenterEditor (object sender, EventArgs e)
		{
			CenterToCaret ();
			StartCaretPulseAnimation ();
		}

		public void RunAction (Action<TextEditorData> action)
		{
			try {
				action (GetTextEditorData ());
			} catch (Exception e) {
				Console.WriteLine ("Error while executing " + action + " :" + e);
			}
		}

		void HandleDocumentHeightChanged (object sender, EventArgs e)
		{
			SetAdjustments ();
		}

		void TextEditorDatahandleUpdateAdjustmentsRequested (object sender, EventArgs e)
		{
			SetAdjustments ();
		}
		
		
		internal void ShowListWindow<T> (ListWindow<T> window, DocumentLocation loc)
		{
			var p = LocationToPoint (loc);
			int ox = 0, oy = 0;
			GdkWindow.GetOrigin (out ox, out oy);
	
			window.Move (ox + p.X - window.TextOffset , oy + p.Y + (int)LineHeight);
			window.ShowAll ();
		}
		
		internal int preeditOffset = -1, preeditLine, preeditCursorCharIndex;
		internal string preeditString;
		internal Pango.AttrList preeditAttrs;
		internal bool preeditHeightChange;
		
		internal bool ContainsPreedit (int offset, int length)
		{
			if (string.IsNullOrEmpty (preeditString))
				return false;
			
			return offset <= preeditOffset && preeditOffset <= offset + length;
		}

		void PreeditStringChanged (object sender, EventArgs e)
		{
			if (imContextNeedsReset)
				preeditString = null;
			else
				imContext.GetPreeditString (out preeditString, out preeditAttrs, out preeditCursorCharIndex);
			if (!string.IsNullOrEmpty (preeditString)) {
				if (preeditOffset < 0) {
					preeditOffset = Caret.Offset;
					preeditLine = Caret.Line;
				}
				if (UpdatePreeditLineHeight ())
					QueueDraw ();
			} else {
				preeditOffset = -1;
				preeditString = null;
				preeditAttrs = null;
				preeditCursorCharIndex = 0;
				if (UpdatePreeditLineHeight ())
					QueueDraw ();
			}
			this.textViewMargin.ForceInvalidateLine (preeditLine);
			this.textEditorData.Document.CommitLineUpdate (preeditLine);
		}

		internal bool UpdatePreeditLineHeight ()
		{
			if (!string.IsNullOrEmpty (preeditString)) {
				using (var preeditLayout = PangoUtil.CreateLayout (this)) {
					preeditLayout.SetText (preeditString);
					preeditLayout.Attributes = preeditAttrs;
					int w, h;
					preeditLayout.GetSize (out w, out h);
					var calcHeight = System.Math.Ceiling (h / Pango.Scale.PangoScale);
					if (LineHeight < calcHeight) {
						textEditorData.HeightTree.SetLineHeight (preeditLine, calcHeight);
						preeditHeightChange = true;
						return true;
					}
				}
			} else if (preeditHeightChange) {
				preeditHeightChange = false;
				textEditorData.HeightTree.Rebuild ();
				return true;
			}
			return false;
		}

		void CaretPositionChanged (object sender, DocumentLocationEventArgs args) 
		{
			HideTooltip ();
			ResetIMContext ();
			
			if (Caret.AutoScrollToCaret && HasFocus)
				ScrollToCaret ();

			if (textViewMargin.HighlightCaretLine == true)
				textViewMargin.HighlightCaretLine = false;

//			Rectangle rectangle = textViewMargin.GetCaretRectangle (Caret.Mode);
			RequestResetCaretBlink ();
			
			textEditorData.CurrentMode.InternalCaretPositionChanged (textEditorData.Parent, textEditorData);
			
			if (!IsSomethingSelected) {
				if (/*Options.HighlightCaretLine && */args.Location.Line != Caret.Line) 
					RedrawMarginLine (TextViewMargin, args.Location.Line);
				RedrawMarginLine (TextViewMargin, Caret.Line);
			}
		}
		
		MonoDevelop.Ide.Editor.Selection oldSelection = MonoDevelop.Ide.Editor.Selection.Empty;
		void TextEditorDataSelectionChanged (object sender, EventArgs args)
		{
			if (IsSomethingSelected) {
				var selectionRange = MainSelection.GetSelectionRange (textEditorData);
				if (selectionRange.Offset >= 0 && selectionRange.EndOffset < Document.Length) {
					ClipboardActions.CopyToPrimary (this.textEditorData);
				} else {
					ClipboardActions.ClearPrimary ();
				}
			} else {
				ClipboardActions.ClearPrimary ();
			}
			// Handle redraw
			var selection = MainSelection;
			int startLine    = !selection.IsEmpty ? selection.Anchor.Line : -1;
			int endLine      = !selection.IsEmpty ? selection.Lead.Line : -1;
			int oldStartLine = !oldSelection.IsEmpty ? oldSelection.Anchor.Line : -1;
			int oldEndLine   = !oldSelection.IsEmpty ? oldSelection.Lead.Line : -1;
			if (SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block) {
				this.RedrawMarginLines (this.textViewMargin, 
				                        System.Math.Min (System.Math.Min (oldStartLine, oldEndLine), System.Math.Min (startLine, endLine)),
				                        System.Math.Max (System.Math.Max (oldStartLine, oldEndLine), System.Math.Max (startLine, endLine)));
			} else {
				if (endLine < 0 && startLine >=0)
					endLine = Document.LineCount;
				if (oldEndLine < 0 && oldStartLine >=0)
					oldEndLine = Document.LineCount;
				int from = oldEndLine, to = endLine;
				if (!selection.IsEmpty && !oldSelection.IsEmpty) {
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
					if (selection.IsEmpty) {
						from = oldStartLine;
						to = oldEndLine;
					} else if (oldSelection.IsEmpty) {
						from = startLine;
						to = endLine;
					} 
				}
				
				if (from >= 0 && to >= 0) {
					this.RedrawMarginLines (this.textViewMargin, 
					                        System.Math.Max (0, System.Math.Min (from, to) - 1),
					                        System.Math.Max (from, to));
				}
			}
			oldSelection = selection;
			OnSelectionChanged (EventArgs.Empty);
		}
		
		internal void ResetIMContext ()
		{
			if (imContextNeedsReset) {
				imContext.Reset ();
				imContextNeedsReset = false;
			}
		}
		
		void IMCommit (object sender, Gtk.CommitArgs ca)
		{
			if (!IsRealized || !IsFocus)
				return;
			
			//this, if anywhere, is where we should handle UCS4 conversions
			for (int i = 0; i < ca.Str.Length; i++) {
				int utf32Char;
				if (char.IsHighSurrogate (ca.Str, i)) {
					utf32Char = char.ConvertToUtf32 (ca.Str, i);
					i++;
				} else {
					utf32Char = (int)ca.Str [i];
				}
				
				//include the other pre-IM state *if* the post-IM char matches the pre-IM (key-mapped) one
				 if (lastIMEventMappedChar == utf32Char && lastIMEventMappedChar == (uint)lastIMEventMappedKey) {
					editor.OnIMProcessedKeyPressEvent (lastIMEventMappedKey, lastIMEventMappedChar, lastIMEventMappedModifier);
				} else {
					editor.OnIMProcessedKeyPressEvent ((Gdk.Key)0, (uint)utf32Char, Gdk.ModifierType.None);
				}
			}
			
			//the IME can commit while there's still a pre-edit string
			//since we cached the pre-edit offset when it started, need to update it
			if (preeditOffset > -1) {
				preeditOffset = Caret.Offset;
			}
		}
		
		protected override bool OnFocusInEvent (EventFocus evnt)
		{
			var result = base.OnFocusInEvent (evnt);
			imContextNeedsReset = true;
			IMContext.FocusIn ();
			RequestResetCaretBlink ();
			Document.CommitLineUpdate (Caret.Line);
			return result;
		}
		
		protected override bool OnFocusOutEvent (EventFocus evnt)
		{
			var result = base.OnFocusOutEvent (evnt);
			imContextNeedsReset = true;
			mouseButtonPressed = 0;
			imContext.FocusOut ();

			if (tipWindow != null && currentTooltipProvider != null) {
				if (!currentTooltipProvider.IsInteractive (textEditorData.Parent, tipWindow))
					DelayedHideTooltip ();
			} else {
				HideTooltip ();
			}

			TextViewMargin.StopCaretThread ();
			Document.CommitLineUpdate (Caret.Line);
			return result;
		}

		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;
			WindowAttr attributes = new WindowAttr () {
				WindowType = Gdk.WindowType.Child,
				X = Allocation.X,
				Y = Allocation.Y,
				Width = Allocation.Width,
				Height = Allocation.Height,
				Wclass = WindowClass.InputOutput,
				Visual = this.Visual,
				Colormap = this.Colormap,
				EventMask = (int)(this.Events | Gdk.EventMask.ExposureMask),
				Mask = this.Events | Gdk.EventMask.ExposureMask,
			};
			
			WindowAttributesType mask = WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Colormap | WindowAttributesType.Visual;
			GdkWindow = new Gdk.Window (ParentWindow, attributes, mask);
			GdkWindow.UserData = Raw;
			GdkWindow.Background = Style.Background (StateType.Normal);
			Style = Style.Attach (GdkWindow);

			imContext.ClientWindow = this.GdkWindow;
			Caret.PositionChanged += CaretPositionChanged;

			SetWidgetBgFromStyle ();
		}	

		protected override void OnUnrealized ()
		{
			imContext.ClientWindow = null;
			CancelScheduledHide ();
			base.OnUnrealized ();
		}
		
		void DocumentUpdatedHandler (object sender, EventArgs args)
		{
			foreach (DocumentUpdateRequest request in Document.UpdateRequests) {
				request.Update (textEditorData.Parent);
			}
		}
		
		public event EventHandler EditorOptionsChanged;

		protected virtual void OptionsChanged (object sender, EventArgs args)
		{
			if (Options == null)
				return;
			if (currentStyleName != Options.EditorThemeName) {
				currentStyleName = Options.EditorThemeName;
				this.textEditorData.ColorStyle = Options.GetEditorTheme ();
				SetWidgetBgFromStyle ();
			}
			
			iconMargin.IsVisible   = Options.ShowIconMargin;
			gutterMargin.IsVisible     = Options.ShowLineNumberMargin;
			foldMarkerMargin.IsVisible = Options.ShowFoldMargin || Options.EnableQuickDiff;
//			dashedLineMargin.IsVisible = foldMarkerMargin.IsVisible || gutterMargin.IsVisible;
			if (!Options.ShowFoldMargin) {
				Document.UpdateFoldSegments (new List<FoldSegment> ()); 
			}
			if (EditorOptionsChanged != null)
				EditorOptionsChanged (this, args);
			
			textViewMargin.OptionsChanged ();
			foreach (Margin margin in this.margins) {
				if (margin == textViewMargin)
					continue;
				margin.OptionsChanged ();
			}
			SetAdjustments (Allocation);
			textEditorData.HeightTree.Rebuild ();
			this.QueueResize ();
		}

		void SetWidgetBgFromStyle ()
		{
			// This is a hack around a problem with repainting the drag widget.
			// When this is not set a white square is drawn when the drag widget is moved
			// when the bg color is differs from the color style bg color (e.g. oblivion style)
			if (this.textEditorData.ColorStyle != null && GdkWindow != null) {
				settingWidgetBg = true; //prevent infinite recusion

				Widget parent = this;
				while (parent != null && !(parent is ScrolledWindow)) {
					parent = parent.Parent;
				}

				if (parent != null) {
					
					parent.ModifyBg (StateType.Normal, SyntaxHighlightingService.GetColor (textEditorData.ColorStyle, EditorThemeColors.Background));
				}

				// set additionally the real parent background for gtk themes that use the content background
				// to draw the scrollbar slider trough.
				this.Parent.ModifyBg (StateType.Normal, SyntaxHighlightingService.GetColor (textEditorData.ColorStyle, EditorThemeColors.Background));

				this.ModifyBg (StateType.Normal, SyntaxHighlightingService.GetColor (textEditorData.ColorStyle, EditorThemeColors.Background));
				settingWidgetBg = false;
			}
		}
		
		bool settingWidgetBg = false;
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			if (!settingWidgetBg && textEditorData.ColorStyle != null) {
				SetWidgetBgFromStyle ();
			}
		}

		protected override bool OnVisibilityNotifyEvent (EventVisibility evnt)
		{
			if (evnt.State == VisibilityState.FullyObscured)
				HideTooltip ();
			return base.OnVisibilityNotifyEvent (evnt);
		}
		protected override void OnDestroyed ()
		{
			if (popupWindow != null)
				popupWindow.Destroy ();
			this.Options = null;
			Gtk.Key.SnooperRemove (snooperID);
			HideTooltip ();
			Document.HeightChanged -= TextEditorDatahandleUpdateAdjustmentsRequested;
			Document.TextChanged -= OnDocumentStateChanged;
			Document.MarkerAdded -= HandleTextEditorDataDocumentMarkerChange;
			Document.MarkerRemoved -= HandleTextEditorDataDocumentMarkerChange;

			DisposeAnimations ();

			RemoveScrollWindowTimer ();
			if (invisibleCursor != null)
				invisibleCursor.Dispose ();
			
			Caret.PositionChanged -= CaretPositionChanged;
			
			Document.DocumentUpdated -= DocumentUpdatedHandler;
			if (textEditorData.Options != null)
				textEditorData.Options.Changed -= OptionsChanged;
			
			if (imContext != null){
				ResetIMContext ();
				imContext = imContext.Kill (x => x.Commit -= IMCommit);
			}

			UnregisterAdjustments ();

			foreach (Margin margin in this.margins) {
				if (margin is IDisposable)
					((IDisposable)margin).Dispose ();
			}
			iconMargin = null;
			actionMargin = null;
			foldMarkerMargin = null;
			gutterMargin = null;
			textViewMargin = null;
			margins = null;
			oldMargin = null;
			textEditorData.ClearTooltipProviders ();

			textEditorData.RecenterEditor -= TextEditorData_RecenterEditor;
			textEditorData.Options = null;
			textEditorData.Parent = null;
			textEditorData.SelectionChanged -= TextEditorDataSelectionChanged;
			textEditorData.UpdateAdjustmentsRequested -= TextEditorDatahandleUpdateAdjustmentsRequested;
			textEditorData.HeightTree.LineUpdateFrom -= HeightTree_LineUpdateFrom;
			Gtk.Drag.DestUnset (this);

			textEditorData.Dispose ();
			textEditorData = null;
			longestLine = null;
			base.OnDestroyed ();
		}

		void HeightTree_LineUpdateFrom (object sender, TextEditor.HeightTree.HeightChangedEventArgs e)
		{
			//Console.WriteLine ("redraw from :" + e.Line);
			RedrawFromLine (e.Line);

		}

		public void RedrawMargin (Margin margin)
		{
			if (isDisposed)
				return;
			QueueDrawArea ((int)margin.XOffset, 0, GetMarginWidth (margin),  this.Allocation.Height);
		}
		
		public void RedrawMarginLine (Margin margin, int logicalLine)
		{
			if (isDisposed || !margin.IsVisible)
				return;
			
			double y = LineToY (logicalLine) - this.textEditorData.VAdjustment.Value;
			double h = GetLineHeight (logicalLine);

			if (y + h > 0) {
				var mw = (int)GetMarginWidth (margin);
				if (mw > 0 && h > 0) 
					QueueDrawArea ((int)margin.XOffset, (int)y, mw, (int)h);
			}
		}

		int GetMarginWidth (Margin margin)
		{
			if (margin.Width < 0)
				return Allocation.Width - (int)margin.XOffset;
			return (int)margin.Width;
		}
		
		internal void RedrawLine (int logicalLine)
		{
			if (isDisposed || logicalLine > LineCount || logicalLine < DocumentLocation.MinLine)
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

		internal bool IsInKeypress {
			get;
			set;
		}

		/// <summary>Handles key input after key mapping and input methods.</summary>
		/// <param name="key">The mapped keycode.</param>
		/// <param name="unicodeChar">A UCS4 character. If this is nonzero, it overrides the keycode.</param>
		/// <param name="modifier">Keyboard modifier, excluding any consumed by key mapping or IM.</param>
		public void SimulateKeyPress (Gdk.Key key, uint unicodeChar, ModifierType modifier)
		{
			IsInKeypress = true;
			try {
				ModifierType filteredModifiers = modifier & (ModifierType.ShiftMask | ModifierType.Mod1Mask
					 | ModifierType.ControlMask | ModifierType.MetaMask | ModifierType.SuperMask);
				CurrentMode.InternalHandleKeypress (textEditorData.Parent, textEditorData, key, unicodeChar, filteredModifiers);
			} finally {
				IsInKeypress = false;
			}
			RequestResetCaretBlink ();

		}
		
		bool IMFilterKeyPress (Gdk.EventKey evt, Gdk.Key mappedKey, uint mappedChar, Gdk.ModifierType mappedModifiers)
		{
			if (lastIMEvent == evt)
				return false;
			
			if (evt.Type == EventType.KeyPress) {
				lastIMEvent = evt;
				lastIMEventMappedChar = mappedChar;
				lastIMEventMappedKey = mappedKey;
				lastIMEventMappedModifier = mappedModifiers;
			}
			
			if (imContext.FilterKeypress (evt)) {
				imContextNeedsReset = true;
				return true;
			} else {
				return false;
			}
		}
		
		Gdk.Cursor invisibleCursor;
		
		internal void HideMouseCursor ()
		{
			SetCursor (invisibleCursor);
		}

		Gdk.Cursor currentCursor;

		/// <summary>
		/// Sets the mouse cursor of the gdk window and avoids unnecessary native calls.
		/// </summary>
		void SetCursor (Gdk.Cursor cursor)
		{
			if (GdkWindow == null || currentCursor == cursor)
				return;
			GdkWindow.Cursor = currentCursor = cursor;
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evt)
		{
			Gdk.Key key;
			Gdk.ModifierType mod;
			KeyboardShortcut[] accels;
			GtkWorkarounds.MapKeys (evt, out key, out mod, out accels);
			//HACK: we never call base.OnKeyPressEvent, so implement the popup key manually
			if (key == Gdk.Key.Menu || (key == Gdk.Key.F10 && mod.HasFlag (ModifierType.ShiftMask))) {
				OnPopupMenu ();
				return true;
			}
			uint keyVal = (uint)key;
			CurrentMode.SelectValidShortcut (accels, out key, out mod);
			if (key == Gdk.Key.F1 && (mod & (ModifierType.ControlMask | ModifierType.ShiftMask)) == ModifierType.ControlMask) {
				var p = LocationToPoint (Caret.Location);
				ShowTooltip (Gdk.ModifierType.None, Caret.Offset, p.X, p.Y);
				return true;
			}
			if (key == Gdk.Key.F2 && textViewMargin.IsCodeSegmentPreviewWindowShown) {
				textViewMargin.OpenCodeSegmentEditor ();
				return true;
			}
			
			//FIXME: why are we doing this?
			if ((key == Gdk.Key.space || key == Gdk.Key.parenleft || key == Gdk.Key.parenright) && (mod & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)
				mod = Gdk.ModifierType.None;
			
			uint unicodeChar = Gdk.Keyval.ToUnicode (keyVal);
			
			if (CurrentMode.WantsToPreemptIM || CurrentMode.PreemptIM (key, unicodeChar, mod)) {
				ResetIMContext ();
				//FIXME: should call base.OnKeyPressEvent when SimulateKeyPress didn't handle the event
				SimulateKeyPress (key, unicodeChar, mod);
				return true;
			}
			bool filter = IMFilterKeyPress (evt, key, unicodeChar, mod);
			if (filter)
				return true;
			
			//FIXME: OnIMProcessedKeyPressEvent should return false when it didn't handle the event
			if (editor.OnIMProcessedKeyPressEvent (key, unicodeChar, mod))
				return true;
			
			return base.OnKeyPressEvent (evt);
		}
		

		protected override bool OnKeyReleaseEvent (EventKey evnt)
		{
			if (IMFilterKeyPress (evnt, 0, 0, ModifierType.None)) {
				imContextNeedsReset = true;
			}
			return true;
		}
		
		uint mouseButtonPressed = 0;
		uint lastTime;
		double pressPositionX, pressPositionY;
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			if (overChildWidget)
				return true;

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
				if (margin == textViewMargin) {
					//main context menu
					if (DoPopupMenu != null && e.TriggersContextMenu ()) {
						DoClickedPopupMenu (e);
						return true;
					}
				}
				if (margin != null) 
					margin.MousePressed (new MarginMouseEventArgs (textEditorData.Parent, e, e.Button, e.X - startPos, e.Y, e.State));
			}
			return base.OnButtonPressEvent (e);
		}
		
		bool DoClickedPopupMenu (Gdk.EventButton e)
		{
			double tmOffset = e.X - textViewMargin.XOffset;
			if (tmOffset >= 0) {
				DocumentLocation loc;
				if (textViewMargin.CalculateClickLocation (tmOffset, e.Y, out loc)) {
					if (!this.IsSomethingSelected || !this.SelectionRange.Contains (Document.LocationToOffset (loc))) {
						Caret.Location = loc;
					}
				}
				DoPopupMenu (e);
				this.ResetMouseState ();
				return true;
			}
			return false;
		}
		
		public Action<Gdk.EventButton> DoPopupMenu { get; set; }

		protected override bool OnPopupMenu ()
		{
			if (DoPopupMenu != null) {
				DoPopupMenu (null);
				return true;
			}
			return base.OnPopupMenu ();
		}
		
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
			
			//main context menu
			if (DoPopupMenu != null && e.IsContextMenuButton ()) {
				return true;
			}
			
			double startPos;
			Margin margin = GetMarginAtX (e.X, out startPos);
			if (margin != null)
				margin.MouseReleased (new MarginMouseEventArgs (textEditorData.Parent, e, e.Button, e.X - startPos, e.Y, e.State));
			ResetMouseState ();
			return base.OnButtonReleaseEvent (e);
		}

		/// <summary>
		/// Use this method with care.
		/// </summary>
		public void ResetMouseState ()
		{
			mouseButtonPressed = 0;
			textViewMargin.inDrag = false;
			textViewMargin.InSelectionDrag = false;
		}
		
		bool dragOver = false;
		ClipboardActions.CopyOperation dragContents = null;
		DocumentLocation defaultCaretPos, dragCaretPos;
		MonoDevelop.Ide.Editor.Selection selection = MonoDevelop.Ide.Editor.Selection.Empty;
		
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
				ResetMouseState ();
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
			var undo = OpenUndoGroup ();
			int dragOffset = Document.LocationToOffset (dragCaretPos);
			if (context.Action == DragAction.Move) {
				if (CanEdit (Caret.Line) && !selection.IsEmpty) {
					var selectionRange = selection.GetSelectionRange (textEditorData);
					if (selectionRange.Offset < dragOffset)
						dragOffset -= selectionRange.Length;
					Caret.PreserveSelection = true;
					textEditorData.DeleteSelection (selection);
					Caret.PreserveSelection = false;

					selection = MonoDevelop.Ide.Editor.Selection.Empty;
				}
			}
			if (selection_data.Length > 0 && selection_data.Format == 8) {
				Caret.Offset = dragOffset;
				if (CanEdit (dragCaretPos.Line)) {
					int offset = Caret.Offset;
					if (!selection.IsEmpty && selection.GetSelectionRange (textEditorData).Offset >= offset) {
						var start = Document.OffsetToLocation (selection.GetSelectionRange (textEditorData).Offset + selection_data.Text.Length);
						var end = Document.OffsetToLocation (selection.GetSelectionRange (textEditorData).Offset + selection_data.Text.Length + selection.GetSelectionRange (textEditorData).Length);
						selection = new MonoDevelop.Ide.Editor.Selection (start, end);
					}
					textEditorData.PasteText (offset, selection_data.Text, null, ref undo);
					Caret.Offset = offset + selection_data.Text.Length;
					MainSelection = new MonoDevelop.Ide.Editor.Selection (Document.OffsetToLocation (offset), Caret.Location);
				}
				dragOver = false;
				context = null;
			}
			mouseButtonPressed = 0;
			undo.Dispose ();
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
			if (!selection.IsEmpty && offset >= this.selection.GetSelectionRange (textEditorData).Offset && offset < this.selection.GetSelectionRange (textEditorData).EndOffset) {
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
		bool overChildWidget;

		public event EventHandler<Xwt.MouseMovedEventArgs> BeginHover;

		protected virtual void OnBeginHover (Xwt.MouseMovedEventArgs e)
		{
			var handler = BeginHover;
			if (handler != null)
				handler (this, e);
		}

		bool dragging;
		protected override void OnDragBegin (DragContext context)
		{
			dragging = true;
			base.OnDragBegin (context);
		}

		protected override void OnDragEnd (DragContext context)
		{
			dragging = false;
			base.OnDragEnd (context);
		}
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			// This is workaround GTK behavior(bug?) that sometimes when dragging starts
			// still calls OnMotionNotifyEvent 1 or 2 times before mouse move events go to
			// dragging... What this results in... This method calls UpdateScrollWindowTimer
			// which calls FireMotionEvent every 50ms which causes flickering cursor while dragging
			// making dragging unusable.
			if (dragging) {
				return false;
			}
			OnBeginHover (new Xwt.MouseMovedEventArgs (e.Time, e.X, e.Y));
			try {
				// The coordinates have to be properly adjusted to the origin since
				// the event may come from a child widget
				int rx, ry;
				GdkWindow.GetOrigin (out rx, out ry);
				double x = (int) e.XRoot - rx;
				double y = (int) e.YRoot - ry;

				if (Platform.IsWindows) {
					// TODO: revisit this when we move to a newer Gtk.
					// https://bugzilla.xamarin.com/show_bug.cgi?id=57086
					// There seems to be a bug in Gtk+ where for a 4K multi-monitor configuration
					// on Windows the e.XRoot reported in EventMotion above is negative. Hence the
					// logic of computing x and y above based on XRoot results in incorrect (negative)
					// coordinates and hovering in the editor doesn't work (no tooltip showing, etc)
					// The good news is that the original reported TextArea-relative coordinates
					// are correct, so lets just use them. This fixes numerous bad symptoms:
					//  1. tooltips and hovering doesn't work
					//  2. selection with mouse doesn't properly work (selects from beginning of line)
					//  3. mouse caret doesn't change to beam when over the editor area
					//  4. possibly more
					x = e.X;
					y = e.Y;
				}

				overChildWidget = containerChildren.Any (w => w.Child.Allocation.Contains ((int)x, (int)y));

				RemoveScrollWindowTimer ();
				Gdk.ModifierType mod = e.State;
				double startPos;
				Margin margin = GetMarginAtX (x, out startPos);
				if (textViewMargin.inDrag && margin == this.textViewMargin && Gtk.Drag.CheckThreshold (this, (int)pressPositionX, (int)pressPositionY, (int)x, (int)y)) {
					dragContents = new ClipboardActions.CopyOperation ();
					dragContents.CopyData (textEditorData);
					DragContext context = Gtk.Drag.Begin (this, ClipboardActions.CopyOperation.TargetList, DragAction.Move | DragAction.Copy, 1, e);
					if (!Platform.IsMac) {
						CodeSegmentPreviewWindow window = new CodeSegmentPreviewWindow (textEditorData.Parent, true, textEditorData.SelectionRange, 300, 300);
						Gtk.Drag.SetIconWidget (context, window, 0, 0);
					}
					selection = MainSelection;
					textViewMargin.inDrag = false;
				} else {
					FireMotionEvent (x, y, mod);
					if (mouseButtonPressed != 0) {
						UpdateScrollWindowTimer (x, y, mod);
					}
				}
			} catch (Exception ex) {
				GLib.ExceptionManager.RaiseUnhandledException (ex, false);
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
					if (HasFocus) {
						FireMotionEvent (scrollWindowTimer_x, scrollWindowTimer_y, scrollWindowTimer_mod);
					}
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
			if (textViewMargin.InSelectionDrag) {
				margin = textViewMargin;
				startPos = textViewMargin.XOffset;
			} else {
				margin = GetMarginAtX (x, out startPos);
				if (margin != null && GdkWindow != null) {
					if (!overChildWidget) {
						if (!editor.IsInKeypress)
							SetCursor (margin.MarginCursor);
					} else {
						// Set the default cursor when the mouse is over an embedded widget
						SetCursor (null);
					}
				}
			}

			if (oldMargin != margin && oldMargin != null)
				oldMargin.MouseLeft ();
			
			if (margin != null) 
				margin.MouseHover (new MarginMouseEventArgs (textEditorData.Parent, EventType.MotionNotify,
					mouseButtonPressed, x - startPos, y, state));
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
			if (tipWindow != null && currentTooltipProvider != null) {
				if (!currentTooltipProvider.IsInteractive (textEditorData.Parent, tipWindow))
					DelayedHideTooltip ();
			} else {
				HideTooltip ();
			}
			textViewMargin.HideCodeSegmentPreviewWindow ();
			
			if (GdkWindow != null)
				SetCursor (null);
			if (oldMargin != null)
				oldMargin.MouseLeft ();
			
			return base.OnLeaveNotifyEvent (e); 
		}

		public double LineHeight {
			get {
				return this.textEditorData.LineHeight;
			}
			internal set {
				this.textEditorData.LineHeight = value;
			}
		}
		
		public TextViewMargin TextViewMargin {
			get {
				return textViewMargin;
			}
		}

		public GutterMargin GutterMargin {
			get {
				return gutterMargin;
			}
		}
		
		public Margin IconMargin {
			get { return iconMargin; }
		}

		public ActionMargin ActionMargin {
			get { return actionMargin; }
		}
		
		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			return textEditorData.LogicalToVisualLocation (location);
		}

		public DocumentLocation LogicalToVisualLocation (int line, int column)
		{
			return textEditorData.LogicalToVisualLocation (line, column);
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
			if (!sizeHasBeenAllocated) {
				var wrapper = new CenterToWrapper (editor, p);
				SizeAllocated += wrapper.Run;
			} else {
				new CenterToWrapper (editor, p).Run (null, null);
			}
		}

		class CenterToWrapper
		{
			MonoTextEditor editor;
			DocumentLocation p;

			public CenterToWrapper (MonoTextEditor editor, DocumentLocation p)
			{
				this.editor = editor;
				this.p = p;
			}

			public void Run (object sender, EventArgs e)
			{
				if (editor.IsDisposed)
					return;
				editor.TextArea.SizeAllocated -= Run;
				editor.TextArea.SetAdjustments (editor.Allocation);
				//			Adjustment adj;
				//adj.Upper
				if (editor.TextArea.textEditorData.VAdjustment.Upper < editor.TextArea.Allocation.Height) {
					editor.TextArea.textEditorData.VAdjustment.Value = 0;
					return;
				}

				//	int yMargin = 1 * this.LineHeight;
				double caretPosition = editor.TextArea.LineToY (p.Line);
				caretPosition -= editor.TextArea.textEditorData.VAdjustment.PageSize / 3;

				// Make sure the caret position is inside the bounds. This avoids an unnecessary bump of the scrollview.
				// The adjustment does this check, but does it after assigning the value, so the value may be out of bounds for a while.
				if (caretPosition + editor.TextArea.textEditorData.VAdjustment.PageSize > editor.TextArea.textEditorData.VAdjustment.Upper)
					caretPosition = editor.TextArea.textEditorData.VAdjustment.Upper - editor.TextArea.textEditorData.VAdjustment.PageSize;

				editor.TextArea.textEditorData.VAdjustment.Value = caretPosition;

				if (editor.TextArea.textEditorData.HAdjustment.Upper < editor.TextArea.Allocation.Width) {
					editor.TextArea.textEditorData.HAdjustment.Value = 0;
				} else {
					double caretX = editor.TextArea.ColumnToX (editor.TextArea.Document.GetLine (p.Line), p.Column);
					double textWith = editor.TextArea.Allocation.Width - editor.TextArea.textViewMargin.XOffset;
					if (caretX < editor.TextArea.textEditorData.HAdjustment.Upper) {
						editor.TextArea.textEditorData.HAdjustment.Value = 0;
					} else if (editor.TextArea.textEditorData.HAdjustment.Value > caretX) {
						editor.TextArea.textEditorData.HAdjustment.Value = System.Math.Max (0, caretX - editor.TextArea.textEditorData.HAdjustment.Upper / 2);
					} else if (editor.TextArea.textEditorData.HAdjustment.Value + textWith < caretX + editor.TextArea.TextViewMargin.CharWidth) {
						double adjustment = System.Math.Max (0, caretX - textWith + editor.TextArea.TextViewMargin.CharWidth);
						editor.TextArea.textEditorData.HAdjustment.Value = adjustment;
					}
				}
				editor.TextArea.QueueDraw ();
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

//		class ScrollingActor
//		{
//			readonly TextEditor editor;
//			readonly double targetValue;
//			readonly double initValue;
//			
//			public ScrollingActor (Mono.TextEditor.TextEditor editor, double targetValue)
//			{
//				this.editor = editor;
//				this.targetValue = targetValue;
//				this.initValue = editor.VAdjustment.Value;
//			}
//
//			public bool Step (Actor<ScrollingActor> actor)
//			{
//				if (actor.Expired) {
//					editor.VAdjustment.Value = targetValue;
//					return false;
//				}
//				var newValue = initValue + (targetValue - initValue) / 100   * actor.Percent;
//				editor.VAdjustment.Value = newValue;
//				return true;
//			}
//		}

		internal void SmoothScrollTo (double value)
		{
			this.textEditorData.VAdjustment.Value = value;
/*			Stage<ScrollingActor> scroll = new Stage<ScrollingActor> (50);
			scroll.UpdateFrequency = 10;
			var scrollingActor = new ScrollingActor (this, value);
			scroll.Add (scrollingActor, 50);

			scroll.ActorStep += scrollingActor.Step;
			scroll.Play ();*/
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
					double caretPosition = LineToY (p.Line);
					if (this.textEditorData.VAdjustment.Value > caretPosition) {
						this.textEditorData.VAdjustment.Value = caretPosition;
					} else if (this.textEditorData.VAdjustment.Value + this.textEditorData.VAdjustment.PageSize - this.LineHeight < caretPosition) {
						this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize + this.LineHeight;
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

		/// <summary>
		/// Scrolls the editor as required for making the specified area visible 
		/// </summary>
		public void ScrollTo (Gdk.Rectangle rect)
		{
			inCaretScroll = true;
			try {
				var vad = this.textEditorData.VAdjustment;
				if (vad.Upper < Allocation.Height) {
					vad.Value = 0;
				} else {
					if (vad.Value >= rect.Top) {
						vad.Value = rect.Top;
					} else if (vad.Value + vad.PageSize - rect.Height < rect.Top) {
						vad.Value = rect.Top - vad.PageSize + rect.Height;
					}
				}

				var had = this.textEditorData.HAdjustment;
				if (had.Upper < Allocation.Width)  {
					had.Value = 0;
				} else {
					if (had.Value >= rect.Left) {
						had.Value = rect.Left;
					} else if (had.Value + had.PageSize - rect.Width < rect.Left) {
						had.Value = rect.Left - had.PageSize + rect.Width;
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
			SetAdjustments (Allocation);
			sizeHasBeenAllocated = true;
			if (Options.WrapLines)
				textViewMargin.PurgeLayoutCache ();
			SetChildrenPositions (allocation);

			UpdateMarginRects (allocation);
		}

		void UpdateMarginRects (Gdk.Rectangle allocation)
		{
			double curX = 0;

			if (margins == null) {
				return;
			}

			foreach (var margin in margins) {
				Gdk.Rectangle marginRect;

				if (!margin.IsVisible)
					continue;

				marginRect.X = (int)curX;
				marginRect.Y = 0;
				if ((int)margin.Width == -1) {
					marginRect.Width = (int)(allocation.Width - curX);
				} else {
					marginRect.Width = (int)margin.Width;
				}
				marginRect.Height = allocation.Height;

				curX += margin.Width;

				margin.RectInParent = marginRect;
			}
		}

		uint lastScrollTime;
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			var modifier = !Platform.IsMac? Gdk.ModifierType.ControlMask
				//Mac window manager already uses control-scroll, so use command
				//Command might be either meta or mod1, depending on GTK version
				: (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);

			var hasZoomModifier = (evnt.State & modifier) != 0;
			if (hasZoomModifier && lastScrollTime != 0 && (evnt.Time - lastScrollTime) < 100)
				hasZoomModifier = false;
			
			if (hasZoomModifier) {
				if (evnt.Direction == ScrollDirection.Up)
					Options.ZoomIn ();
				else if (evnt.Direction == ScrollDirection.Down)
					Options.ZoomOut ();

				this.QueueDraw ();
				if (isMouseTrapped)
					FireMotionEvent (mx + textViewMargin.XOffset, my, lastState);
				return true;
			}

			if (!Platform.IsMac) {
				if ((evnt.State & ModifierType.ShiftMask) == ModifierType.ShiftMask) {
					if (evnt.Direction == ScrollDirection.Down)
						HAdjustment.Value = System.Math.Min (HAdjustment.Upper - HAdjustment.PageSize, HAdjustment.Value + HAdjustment.StepIncrement * 3);
					else if (evnt.Direction == ScrollDirection.Up)
						HAdjustment.Value -= HAdjustment.StepIncrement * 3;
					
					return true;
				}
			}
			lastScrollTime = evnt.Time;
			return base.OnScrollEvent (evnt); 
		}
		
		void SetHAdjustment ()
		{
			textEditorData.HeightTree.Rebuild ();
			
			if (textEditorData.HAdjustment == null || Options == null)
				return;
			textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (Options.WrapLines) {
				this.textEditorData.HAdjustment.SetBounds (0, 0, 0, 0, 0);
			} else {
				if (longestLine != null && this.textEditorData.HAdjustment != null) {
					double maxX = longestLineWidth;
					if (maxX > Allocation.Width)
						maxX += 2 * this.textViewMargin.CharWidth;
					double width = Allocation.Width - this.TextViewMargin.XOffset;
					var realMaxX = System.Math.Max (maxX, this.textEditorData.HAdjustment.Value + width);

					foreach (var containerChild in editor.containerChildren.Concat (containerChildren)) {
						if (containerChild.Child == this)
							continue;
						realMaxX = System.Math.Max (realMaxX, containerChild.X + containerChild.Child.SizeRequest ().Width);
					}

					this.textEditorData.HAdjustment.SetBounds (
						0,
						realMaxX,
						this.textViewMargin.CharWidth,
						width,
						width);
					if (realMaxX < width)
						this.textEditorData.HAdjustment.Value = 0;
				}
			}
			textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
		}
		
		internal void SetAdjustments ()
		{
			if (textEditorData == null)
				return;
			SetAdjustments (Allocation);
		}
		

		internal void SetAdjustments (Gdk.Rectangle allocation)
		{
			SetHAdjustment ();
			
			if (this.textEditorData.VAdjustment != null) {
				double maxY = textEditorData.HeightTree.TotalHeight;
				//				if (maxY > allocation.Height)
				maxY += allocation.Height / 2 - LineHeight;

				foreach (var containerChild in editor.containerChildren.Concat (containerChildren)) {
					maxY = System.Math.Max (maxY, containerChild.Y + containerChild.Child.SizeRequest().Height);
				}

				if (VAdjustment.Value > maxY - allocation.Height) {
					VAdjustment.Value = System.Math.Max (0, maxY - allocation.Height);
					QueueDraw ();
				}
				this.textEditorData.VAdjustment.SetBounds (0, 
				                                           System.Math.Max (allocation.Height, maxY), 
				                                           LineHeight,
				                                           allocation.Height,
				                                           allocation.Height);
				if (maxY < allocation.Height)
					this.textEditorData.VAdjustment.Value = 0;
			}
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
			foreach (var margin in this.margins) {
				if (margin.BackgroundRenderer != null) {
					var area = new Cairo.Rectangle(0, 0, Allocation.Width, Allocation.Height);
					margin.BackgroundRenderer.Draw (cr, area);
				}
			}

			for (int visualLineNumber = textEditorData.LogicalToVisualLine (startLine);; visualLineNumber++) {
				int logicalLineNumber = textEditorData.VisualToLogicalLine (visualLineNumber);
				var line = Document.GetLine (logicalLineNumber);
				// Ensure that the correct line height is set.
				if (line != null) {
					var wrapper = textViewMargin.GetLayout (line);
					if (wrapper.IsUncached)
						wrapper.Dispose ();
				}

				double lineHeight = GetLineHeight (line);
				foreach (var margin in this.margins) {
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
				if (curY >= cairoRectangle.Y + cairoRectangle.Height)
					break;
			}
			
			foreach (var margin in this.margins) {
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
			int lastVisibleLine = textEditorData.LogicalToVisualLine (Document.LineCount);
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
				return false;

			try {
				ExposeEventInternal (e);
			} catch (Exception ex) {
				GLib.ExceptionManager.RaiseUnhandledException (ex, false);
			}

			return base.OnExposeEvent (e);
		}

		void ExposeEventInternal (Gdk.EventExpose e)
		{
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
					textViewMargin.ResetCaretBlink (200);
					requestResetCaretBlink = false;
				}
				
				foreach (Animation animation in actors) {
					animation.Drawer.Draw (cr);
				}
				
				OnPainted (new PaintEventArgs (cr, cairoArea));
			}

			if (Caret.IsVisible)
				textViewMargin.DrawCaret (e.Window, Allocation);
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
		
		internal EditorTheme EditorTheme {
			get {
				return this.textEditorData?.ColorStyle;
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
		
		public MonoDevelop.Ide.Editor.Selection MainSelection {
			get {
				return textEditorData.MainSelection;
			}
			set {
				textEditorData.MainSelection = value;
			}
		}
		
		public MonoDevelop.Ide.Editor.SelectionMode SelectionMode {
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

		public int SelectionLead {
			get {
				return this.textEditorData.SelectionLead;
			}
			set {
				this.textEditorData.SelectionLead = value;
			}
		}

		public IEnumerable<DocumentLine> SelectedLines {
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
		
		public void Remove (DocumentRegion region)
		{
			textEditorData.Remove (region);
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
			
		public void SetSelection (int anchorLine, int anchorColumn, int leadLine, int leadColumn)
		{
			this.textEditorData.SetSelection (anchorLine, anchorColumn, leadLine, leadColumn);
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
			CurrentMode.InternalSelectionChanged (editor, textEditorData);
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
		
		public string GetTextAt (DocumentRegion region)
		{
			return Document.GetTextAt (region);
		}

		public char GetCharAt (int offset)
		{
			return Document.GetCharAt (offset);
		}
		
		public IEnumerable<DocumentLine> Lines {
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
		
		public string GetLineIndent (DocumentLine segment)
		{
			return Document.GetLineIndent (segment);
		}
		
		public DocumentLine GetLine (int lineNumber)
		{
			return Document.GetLine (lineNumber);
		}
		
		public DocumentLine GetLineByOffset (int offset)
		{
			return Document.GetLineByOffset (offset);
		}
		
		public int OffsetToLineNumber (int offset)
		{
			return Document.OffsetToLineNumber (offset);
		}
		
		public IDisposable OpenUndoGroup()
		{
			return Document.OpenUndoGroup ();
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
		
		public ISegment SearchRegion {
			get {
				return this.textEditorData.SearchRequest.SearchRegion;
			}
			
			set {
				this.textEditorData.SearchRequest.SearchRegion = value;
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
			MonoTextEditor editor;
			
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
			
			public CaretPulseAnimation (MonoTextEditor editor)
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
				Cairo.Color color = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Foreground);
				color.A = 0.8;
				cr.LineWidth = editor.Options.Zoom;
				cr.SetSourceColor (color);
				cr.Stroke ();
				cr.ResetClip ();
			}
		}
		
		public enum PulseKind {
			In, Out, Bounce
		}
		
		internal class RegionPulseAnimation : IAnimationDrawer
		{
			MonoTextEditor editor;
			
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
			
			public RegionPulseAnimation (MonoTextEditor editor, Gdk.Point position, Gdk.Size size)
				: this (editor, new Gdk.Rectangle (position, size)) {}
			
			public RegionPulseAnimation (MonoTextEditor editor, Gdk.Rectangle region)
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
				Cairo.Color color = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Foreground);
				color.A = 0.8;
				cr.LineWidth = editor.Options.Zoom;
				cr.SetSourceColor (color);
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
			
			return new Gdk.Rectangle (startPt.X, startPt.Y, width, (int)this.LineHeight);
		}
		
		/// <summary>
		/// Initiate a pulse at the specified document location
		/// </summary>
		/// <param name="pulseStart">
		/// A <see cref="DocumentLocation"/>
		/// </param>
		public void PulseCharacter (DocumentLocation pulseStart)
		{
			if (pulseStart.Column < 0 || pulseStart.Line < 0)
				return;
			var rect = RangeToRectangle (pulseStart, new DocumentLocation (pulseStart.Line, pulseStart.Column + 1));
			if (rect.X < 0 || rect.Y < 0 || System.Math.Max (rect.Width, rect.Height) <= 0)
				return;
			StartAnimation (new RegionPulseAnimation (editor, rect) {
				Kind = PulseKind.Bounce
			});
		}

		
		public SearchResult FindNext (bool setSelection)
		{
			var result = textEditorData.FindNext (setSelection);
			if (result == null)
				return result;
			TryToResetHorizontalScrollPosition ();
			AnimateSearchResult (result);
			return result;
		}

		public void StartCaretPulseAnimation ()
		{
			StartAnimation (new CaretPulseAnimation (editor));
			textViewMargin.HighlightCaretLine = true;
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
			if (!IsComposited || !Options.EnableAnimations || result == null)
				return;
			
			// Don't animate multi line search results
			if (OffsetToLineNumber (result.Segment.Offset) != OffsetToLineNumber (result.Segment.EndOffset))
				return;
			
			TextViewMargin.MainSearchResult = result.Segment;
			if (!TextViewMargin.MainSearchResult.IsInvalid ()) {
				if (popupWindow != null) {
					popupWindow.StopPlaying ();
					popupWindow.Destroy ();
				}
				popupWindow = new SearchHighlightPopupWindow (editor);
				popupWindow.Result = result;
				popupWindow.Popup ();
				popupWindow.Destroyed += delegate {
					popupWindow = null;
				};
			}
		}
		
		class SearchHighlightPopupWindow : BounceFadePopupWidget
		{
			public SearchResult Result {
				get;
				set;
			}
			
			public SearchHighlightPopupWindow (MonoTextEditor editor) : base (editor)
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
				Destroy ();
			}
			
			protected override void OnDestroyed ()
			{
				if (layout != null)
					layout.Dispose ();
				base.OnDestroyed ();
			}
			
			protected override Cairo.Rectangle CalculateInitialBounds ()
			{
				DocumentLine line = Editor.Document.GetLineByOffset (Result.Offset);
				int lineNr = Editor.Document.OffsetToLineNumber (Result.Offset);
				int logicalRulerColumn = line.GetLogicalColumn (Editor.GetTextEditorData (), Editor.Options.RulerColumn);
				var lineLayout = Editor.TextViewMargin.CreateLinePartLayout (line, logicalRulerColumn, line.Offset, line.Length, -1, -1);
				if (lineLayout == null)
					return new Cairo.Rectangle ();
				
				int l, x1, x2;
				int index = Result.Offset - line.Offset - 1;
				if (index >= 0) {
					lineLayout.IndexToLineX (index, true, out l, out x1);
				} else {
					l = x1 = 0;
				}
				
				index = Result.Offset - line.Offset - 1 + Result.Length;
				if (index >= 0) {
					lineLayout.IndexToLineX (index, true, out l, out x2);
				} else {
					x2 = 0;
					Console.WriteLine ("Invalid end index :" + index);
				}

				if (lineLayout.IsUncached) {
					lineLayout.Dispose ();
				}
				
				double y = Editor.LineToY (lineNr);
				double w = (x2 - x1) / Pango.Scale.PangoScale;
				double x = (x1 / Pango.Scale.PangoScale + Editor.TextViewMargin.XOffset + Editor.TextViewMargin.TextStartPosition);
				var h = Editor.LineHeight;

				//adjust the width to match TextViewMargin
				w = System.Math.Ceiling (w + 1);

				//add space for the shadow
				w += shadowOffset;
				h += shadowOffset;

				return new Cairo.Rectangle (x, y, w, h);
			}

			const int shadowOffset = 1;

			Pango.Layout layout = null;

			protected override void Draw (Cairo.Context cr, Cairo.Rectangle area)
			{
				cr.LineWidth = Editor.Options.Zoom;

				if (layout == null) {
					layout = cr.CreateLayout ();
					layout.FontDescription = Editor.Options.Font;
					string markup = Editor.GetTextEditorData ().GetMarkup (Result.Offset, Result.Length, true);
					layout.SetMarkup (markup);
				}

				// subtract off the shadow again
				var width = area.Width - shadowOffset;
				var height = area.Height - shadowOffset;

				//from TextViewMargin's actual highlighting
				double corner = System.Math.Min (4, width) * Editor.Options.Zoom;

				//fill in the highlight rect with solid white to prevent alpha blending artifacts on the corners
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, 0, 0, corner, width, height);
				cr.SetSourceRGB (1, 1, 1);
				cr.Fill ();

				//draw the shadow
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true,
					shadowOffset, shadowOffset, corner, width, height);
				// TODO: EditorTheme : searchResultMainColor? 
				var searchResultMainColor = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.FindHighlight);
				var color = TextViewMargin.DimColor (searchResultMainColor, 0.3);
				color.A = 0.5 * opacity * opacity;
				cr.SetSourceColor (color);
				cr.Fill ();

				//draw the highlight rectangle
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, 0, 0, corner, width, height);

				// FIXME: VV: Remove gradient features
				using (var gradient = new Cairo.LinearGradient (0, 0, 0, height)) {
					color = ColorLerp (
						TextViewMargin.DimColor (searchResultMainColor, 1.1),
						searchResultMainColor,
						1 - opacity);
					gradient.AddColorStop (0, color);
					color = ColorLerp (
						TextViewMargin.DimColor (searchResultMainColor, 0.9),
						searchResultMainColor,
						1 - opacity);
					gradient.AddColorStop (1, color);
					cr.SetSource (gradient);
					cr.Fill ();
				}

				//and finally the text
				cr.Translate (area.X, area.Y);
				cr.SetSourceRGB (0, 0, 0);
				cr.ShowLayout (layout);
			}

			static Cairo.Color ColorLerp (Cairo.Color from, Cairo.Color to, double scale)
			{
				return new Cairo.Color (
					Lerp (from.R, to.R, scale),
					Lerp (from.G, to.G, scale),
					Lerp (from.B, to.B, scale),
					Lerp (from.A, to.A, scale)
				);
			}

			static double Lerp (double from, double to, double scale)
			{
				return from + scale * (to - from);
			}
		}
		
		public SearchResult FindPrevious (bool setSelection)
		{
			var result = textEditorData.FindPrevious (setSelection);
			if (result == null)
				return result;
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
		
		int tipX, tipY, tipOffset;
		uint tipHideTimeoutId = 0;
		uint tipShowTimeoutId = 0;
		static Xwt.WindowFrame tipWindow;
		static TooltipProvider currentTooltipProvider;

		// Data for the next tooltip to be shown
		int nextTipOffset = 0;
		int nextTipX=0; int nextTipY=0;
		Gdk.ModifierType nextTipModifierState = ModifierType.None;
		DateTime nextTipScheduledTime; // Time at which we want the tooltip to show
		
		void ShowTooltip (Gdk.ModifierType modifierState)
		{
			if (mx < TextViewMargin.TextStartPosition) {
				HideTooltip ();
				return;
			}

			var loc = PointToLocation (mx, my, true);
			if (loc.IsEmpty) {
				HideTooltip ();
				return;
			}

			// Hide editor tooltips for text marker extended regions (message bubbles)
			double y = LineToY (loc.Line);
			if (y + LineHeight < my) {
				HideTooltip ();
				return;
			}
			
			ShowTooltip (modifierState, 
			             Document.LocationToOffset (loc),
			             (int)mx,
			             (int)my);
		}

		public void ShowQuickInfo ()
		{
			var p = LocationToPoint (Caret.Location);
			ShowTooltip (Gdk.ModifierType.None, Caret.Offset, p.X, p.Y, 0);
		}
		
		void ShowTooltip (Gdk.ModifierType modifierState, int offset, int xloc, int yloc, uint timeOut = TooltipTimeout)
		{
			CancelScheduledShow ();
			if (textEditorData.SuppressTooltips)
				return;
			if (tipWindow != null && currentTooltipProvider != null && currentTooltipProvider.IsInteractive (editor, tipWindow)) {
				int wx, ww, wh;
				var s = tipWindow.Size;
				ww = (int)s.Width;
				wh = (int)s.Height;
				wx = tipX - ww/2;
				if (xloc >= wx && xloc < tipX + ww && yloc >= tipY && yloc < tipY + 20 + wh)
					return;
			}
			if (tipItem != null && !tipItem.IsInvalid () && !tipItem.Contains (offset)) 
				HideTooltip ();
			nextTipX = xloc;
			nextTipY = yloc;
			nextTipOffset = offset;
			nextTipModifierState = modifierState;
			nextTipScheduledTime = DateTime.Now + TimeSpan.FromMilliseconds (TooltipTimeout);
			// If a tooltip is already scheduled, there is no need to create a new timer.
			if (tipShowTimeoutId == 0)
				tipShowTimeoutId = GLib.Timeout.Add (timeOut, () => { TooltipTimer (); return false; });
		}
		
		async void TooltipTimer ()
		{
			// This timer can't be reused, so reset the var now
			tipShowTimeoutId = 0;
			// Cancelled?
			if (nextTipOffset == -1)
				return;
			
			int remainingMs = (int) (nextTipScheduledTime - DateTime.Now).TotalMilliseconds;
			if (remainingMs > 50) {
				// Still some significant time left. Re-schedule the timer
				tipShowTimeoutId = GLib.Timeout.Add ((uint) remainingMs, () => { TooltipTimer (); return false; });
				return;
			}

			var token = tooltipCancellationSource.Token;
			// Find a provider
			TooltipProvider provider = null;
			TooltipItem item = null;
			
			foreach (TooltipProvider tp in textEditorData.tooltipProviders) {
				try {
					item = await tp.GetItem (editor, nextTipOffset, token);
				} catch (OperationCanceledException) {
				} catch (Exception e) {
					System.Console.WriteLine ("Exception in tooltip provider " + tp + " GetItem:");
					System.Console.WriteLine (e);
				}
				if (token.IsCancellationRequested) {
					return;
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
					return;
				}
				
				tipX = nextTipX;
				tipY = nextTipY;
				tipOffset = nextTipOffset;
				tipItem = item;
				Xwt.WindowFrame tw = null;
				try {
					tw = provider.CreateTooltipWindow (editor, nextTipOffset, nextTipModifierState, item);
					if (tw != null)
						provider.ShowTooltipWindow (editor, tw, nextTipOffset, nextTipModifierState, tipX + (int) TextViewMargin.XOffset, tipY, item);
				} catch (Exception e) {
					Console.WriteLine ("-------- Exception while creating tooltip: " + provider);
					Console.WriteLine (e);
				}
				if (tw == tipWindow)
					return;
				HideTooltip ();
				if (tw == null)
					return;
				
				CancelScheduledShow ();
				tipWindow = tw;
				currentTooltipProvider = provider;
				
				tipShowTimeoutId = 0;
			} else
				HideTooltip ();
			return;
		}

		internal void SetTooltip (Xwt.WindowFrame tooltipWindow)
		{
			HideTooltip ();
			tipWindow = tooltipWindow;
		}
		
		public void HideTooltip (bool checkMouseOver = true)
		{
			CancelScheduledHide ();
			CancelScheduledShow ();
			
			if (tipWindow != null) {
//				if (checkMouseOver && tipWindow.GdkWindow != null) {
//					// Don't hide the tooltip window if the mouse pointer is inside it.
//					int x, y, w, h;
//					Gdk.ModifierType m;
//					tipWindow.GdkWindow.GetPointer (out x, out y, out m);
//					tipWindow.GdkWindow.GetSize (out w, out h);
//					if (x >= 0 && y >= 0 && x < w && y < h)
//						return;
//				}
				tipWindow.Dispose ();
				tipWindow = null;
				tipItem = null;
			}
		}
		
		void DelayedHideTooltip ()
		{
			CancelScheduledHide ();
			tipHideTimeoutId = GLib.Timeout.Add (300, delegate {
				HideTooltip ();
				tipHideTimeoutId = 0;
				return false;
			});
		}
		
		void CancelScheduledHide ()
		{
			CancelScheduledShow ();
			if (tipHideTimeoutId != 0) {
				GLib.Source.Remove (tipHideTimeoutId);
				tipHideTimeoutId = 0;
			}
		}

		CancellationTokenSource tooltipCancellationSource = new CancellationTokenSource ();
		void CancelScheduledShow ()
		{
			tooltipCancellationSource.Cancel ();
			tooltipCancellationSource = new CancellationTokenSource ();
			// Don't remove the timeout handler since it may be reused
			nextTipOffset = -1;
		}
		
		void OnDocumentStateChanged (object s, TextChangeEventArgs args)
		{
			HideTooltip ();
			for (int i = 0; i < args.TextChanges.Count; ++i) {
				var change = args.TextChanges[i];
				var start = editor.Document.OffsetToLineNumber (change.NewOffset);
				var end = editor.Document.OffsetToLineNumber (change.NewOffset + change.InsertionLength);
				editor.Document.CommitMultipleLineUpdate (start, end);
			}
		}
		#endregion
		
		#region Coordinate transformation
		public DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false)
		{
			return TextViewMargin.PointToLocation (xp, yp, endAtEol);
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

		public double ColumnToX (DocumentLine line, int column)
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
		
		public double GetLineHeight (DocumentLine line)
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

		class SetCaret 
		{
			MonoTextEditor view;
			int line, column;
			bool highlightCaretLine;
			bool centerCaret;
			
			public SetCaret (MonoTextEditor view, int line, int column, bool highlightCaretLine, bool centerCaret)
			{
				this.view = view;
				this.line = line;
				this.column = column;
				this.highlightCaretLine = highlightCaretLine;
				this.centerCaret = centerCaret;
 			}
			
			public void Run (object sender, EventArgs e)
			{
				if (view.IsDisposed)
					return;
				line = System.Math.Min (line, view.Document.LineCount);
				view.Caret.AutoScrollToCaret = false;
				try {
					view.Caret.Location = new DocumentLocation (line, column);
					view.GrabFocus ();
					if (centerCaret)
						view.CenterToCaret ();
					if (view.TextViewMargin.XOffset == 0)
						view.HAdjustment.Value = 0;
					view.TextArea.SizeAllocated -= Run;
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
			SetCaretTo (line, column, highlight, true);
		}

		public void SetCaretTo (int line, int column, bool highlight, bool centerCaret)
		{
			if (line < DocumentLocation.MinLine)
				throw new ArgumentException ("line < MinLine");
			if (column < DocumentLocation.MinColumn)
				throw new ArgumentException ("column < MinColumn");
			
			if (!sizeHasBeenAllocated) {
				SetCaret setCaret = new SetCaret (editor, line, column, highlight, centerCaret);
				SizeAllocated += setCaret.Run;
			} else {
				new SetCaret (editor, line, column, highlight, centerCaret).Run (null, null);
			}
		}

		#region Container
		public override ContainerChild this [Widget w] {
			get {
				return containerChildren.FirstOrDefault (info => info.Child == w || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == w));
			}
		}

		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		internal List<MonoTextEditor.EditorContainerChild> containerChildren = new List<MonoTextEditor.EditorContainerChild> ();

		public void AddTopLevelWidget (Gtk.Widget widget, int x, int y)
		{
			widget.Parent = this;
			MonoTextEditor.EditorContainerChild info = new MonoTextEditor.EditorContainerChild (this, widget);
			info.X = x;
			info.Y = y;
			var newContainerChildren = new List<MonoTextEditor.EditorContainerChild> (containerChildren);
			newContainerChildren.Add (info);
			containerChildren = newContainerChildren;
			ResizeChild (Allocation, info);
			SetAdjustments ();
		}
		
		public void MoveTopLevelWidget (Gtk.Widget widget, int x, int y)
		{
			foreach (var info in containerChildren.ToArray ()) {
				if (info.Child == widget || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == widget)) {
					if (info.X == x && info.Y == y)
						break;
					info.X = x;
					info.Y = y;
					if (widget.Visible)
						ResizeChild (Allocation, info);
					break;
				}
			}
			SetAdjustments ();
		}

		/// <summary>
		/// Returns the position of an embedded widget
		/// </summary>
		public void GetTopLevelWidgetPosition (Gtk.Widget widget, out int x, out int y)
		{
			foreach (var info in containerChildren.ToArray ()) {
				if (info.Child == widget || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == widget)) {
					x = info.X;
					y = info.Y;
					return;
				}
			}
			x = y = 0;
		}
		
		public void MoveToTop (Gtk.Widget widget)
		{
			var editorContainerChild = containerChildren.FirstOrDefault (c => c.Child == widget);
			if (editorContainerChild == null)
				throw new Exception ("child " + widget + " not found.");
			var newChilds = containerChildren.Where (child => child != editorContainerChild).ToList ();
			newChilds.Add (editorContainerChild);
			this.containerChildren = newChilds;
			widget.GdkWindow.Raise ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			AddTopLevelWidget (widget, 0, 0);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			var newContainerChildren = new List<MonoTextEditor.EditorContainerChild> (containerChildren);
			foreach (var info in newContainerChildren.ToArray ()) {
				if (info.Child == widget) {
					widget.Unparent ();
					newContainerChildren.Remove (info);
					SetAdjustments ();
					break;
				}
			}
			containerChildren = newContainerChildren;
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			containerChildren.ForEach (child => callback (child.Child));
		}
		
		protected override void OnMapped ()
		{
			WidgetFlags |= WidgetFlags.Mapped;
			// Note: SourceEditorWidget.ShowAutoSaveWarning() might have set TextEditor.Visible to false,
			// in which case we want to not map it (would cause a gtk+ critical error).
			containerChildren.ForEach (child => { if (child.Child.Visible) child.Child.Map (); });
			GdkWindow.Show ();
		}
		
		protected override void OnUnmapped ()
		{
			WidgetFlags &= ~WidgetFlags.Mapped;
			
			// We hide the window first so that the user doesn't see widgets disappearing one by one.
			GdkWindow.Hide ();
			
			containerChildren.ForEach (child => child.Child.Unmap ());
		}

		void ResizeChild (Rectangle allocation, MonoTextEditor.EditorContainerChild child)
		{
			Requisition req = child.Child.SizeRequest ();
			var childRectangle = new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height);
			if (!child.FixedPosition) {
//				double zoom = Options.Zoom;
				childRectangle.X = (int)(child.X /* * zoom */- HAdjustment.Value);
				childRectangle.Y = (int)(child.Y /* * zoom */- VAdjustment.Value);
			}
			//			childRectangle.X += allocation.X;
			//			childRectangle.Y += allocation.Y;
			child.Child.SizeAllocate (childRectangle);
		}
		
		void SetChildrenPositions (Rectangle allocation)
		{
			foreach (var child in containerChildren.ToArray ()) {
				ResizeChild (allocation, child);
			}
		}
		#endregion

	}

	interface ITextEditorDataProvider
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


