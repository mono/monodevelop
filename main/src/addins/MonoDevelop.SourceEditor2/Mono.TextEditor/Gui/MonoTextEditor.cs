//
// TextEditor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	[System.ComponentModel.Category("Mono.TextEditor")]
	[System.ComponentModel.ToolboxItem(true)]
	class MonoTextEditor : Container
	{
		readonly TextArea textArea;

		internal LayoutCache LayoutCache {
			get;
			private set;
		}

		internal bool IsInKeypress {
			get { return textArea.IsInKeypress; }
		}

		public TextArea TextArea {
			get {
				return textArea;
			}
		}

		public MonoTextEditor () : this(new TextDocument ())
		{
		}

		public MonoTextEditor (TextDocument doc)
			: this (doc, null)
		{
		}
		
		internal MonoTextEditor (TextDocument doc, ITextEditorOptions options)
			: this (doc, options, new SimpleEditMode ())
		{
		}
		Thread uiThread;

		internal MonoTextEditor (TextDocument doc, ITextEditorOptions options, EditMode initialMode) 
		{
			uiThread = Thread.CurrentThread;
			GtkWorkarounds.FixContainerLeak (this);
			WidgetFlags |= WidgetFlags.NoWindow;
			LayoutCache = new LayoutCache (this);
			this.textArea = new TextArea (doc, options, initialMode);
			this.textArea.Initialize (this, doc, options, initialMode);
			this.textArea.EditorOptionsChanged += (sender, e) => OptionsChanged (sender, e);
			AddTopLevelWidget (textArea, 0, 0);
			ShowAll ();

			stage.ActorStep += OnActorStep;
			if (Platform.IsMac) {
				VScroll += delegate {
					for (int i = 1; i < containerChildren.Count; i++) {
						containerChildren[i].Child.QueueDraw ();
					}
				};
				HScroll += delegate {
					for (int i = 1; i < containerChildren.Count; i++) {
						containerChildren[i].Child.QueueDraw ();
					}
				};
			}
		}

		internal bool IsUIThread { get { return Thread.CurrentThread != uiThread; } }

		internal void CheckUIThread ()
		{
			if (Thread.CurrentThread != uiThread)
				throw new InvalidOperationException ("Not executed on UI thread.");
		}

		public new void GrabFocus ()
		{
			TextArea.GrabFocus ();
		}

		public new bool HasFocus {
			get {
				return TextArea.HasFocus;
			}
		}

		protected override void OnDestroyed ()
		{
			UnregisterAdjustments ();
			LayoutCache.Dispose ();
			base.OnDestroyed ();
		}

		void UnregisterAdjustments ()
		{
			if (vAdjustement != null)
				vAdjustement.ValueChanged -= HandleAdjustmentValueChange;
			if (hAdjustement != null)
				hAdjustement.ValueChanged -= HandleAdjustmentValueChange;
			vAdjustement = null;
			hAdjustement = null;
		}

		Adjustment hAdjustement;
		Adjustment vAdjustement;
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			UnregisterAdjustments ();
			this.vAdjustement = vAdjustement;
			this.hAdjustement = hAdjustement;
			base.OnSetScrollAdjustments (hAdjustement, vAdjustement);
			textArea.SetTextEditorScrollAdjustments (hAdjustement, vAdjustement);
			if (hAdjustement != null) {
				hAdjustement.ValueChanged += HandleAdjustmentValueChange;
			}

			if (vAdjustement != null) {
				vAdjustement.ValueChanged += HandleAdjustmentValueChange;
			}
			OnScrollAdjustmentsSet ();
		}

		void HandleAdjustmentValueChange (object sender, EventArgs e)
		{
			SetChildrenPositions (Allocation);
		}

		protected virtual void OnScrollAdjustmentsSet ()
		{
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			CurrentMode.AllocateTextArea (this, textArea, allocation);
			SetChildrenPositions (allocation);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			containerChildren.ForEach (c => c.Child.SizeRequest ());
		}

		internal protected virtual string GetIdeColorStyleName ()
		{
			return TextEditorOptions.DefaultColorStyle;
		}

		#region Container
		public override ContainerChild this [Widget w] {
			get {
				return containerChildren.FirstOrDefault (info => info.Child == w || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == w));
			}
		}

		public class EditorContainerChild : Container.ContainerChild
		{
			public int X { get; set; }
			public int Y { get; set; }
			public bool FixedPosition { get; set; }
			public EditorContainerChild (Container parent, Widget child) : base (parent, child)
			{
			}
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		internal List<EditorContainerChild> containerChildren = new List<EditorContainerChild> ();
		
		public void AddTopLevelWidget (Gtk.Widget widget, int x, int y)
		{
			widget.Parent = this;
			EditorContainerChild info = new EditorContainerChild (this, widget);
			info.X = x;
			info.Y = y;
			containerChildren.Add (info);
			SetAdjustments ();
		}
		
		public void MoveTopLevelWidget (Gtk.Widget widget, int x, int y)
		{
			foreach (EditorContainerChild info in containerChildren.ToArray ()) {
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
		}

		/// <summary>
		/// Returns the position of an embedded widget
		/// </summary>
		public void GetTopLevelWidgetPosition (Gtk.Widget widget, out int x, out int y)
		{
			foreach (EditorContainerChild info in containerChildren.ToArray ()) {
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
			EditorContainerChild editorContainerChild = containerChildren.FirstOrDefault (c => c.Child == widget);
			if (editorContainerChild == null)
				throw new Exception ("child " + widget + " not found.");
			List<EditorContainerChild> newChilds = new List<EditorContainerChild> (containerChildren.Where (child => child != editorContainerChild));
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
			foreach (EditorContainerChild info in containerChildren.ToArray ()) {
				if (info.Child == widget) {
					widget.Unparent ();
					containerChildren.Remove (info);
					break;
				}
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			foreach (var child in containerChildren.ToArray ()) {
				callback (child.Child);
			}
		}

		void ResizeChild (Rectangle allocation, EditorContainerChild child)
		{
			Requisition req = child.Child.SizeRequest ();
			var childRectangle = new Gdk.Rectangle (Allocation.X + child.X, Allocation.Y + child.Y, System.Math.Max (1, req.Width), System.Math.Max (1, req.Height));
			if (!child.FixedPosition) {
				double zoom = Options.Zoom;
				childRectangle.X = Allocation.X + (int)(child.X * zoom - HAdjustment.Value);
				childRectangle.Y = Allocation.Y + (int)(child.Y * zoom - VAdjustment.Value);
			}
			//			childRectangle.X += allocation.X;
			//			childRectangle.Y += allocation.Y;
			child.Child.SizeAllocate (childRectangle);
		}
		
		void SetChildrenPositions (Rectangle allocation)
		{
			foreach (EditorContainerChild child in containerChildren.ToArray ()) {
				if (child.Child == textArea)
					continue;
				ResizeChild (allocation, child);
			}
		}
		#endregion

		#region Animated Widgets
		Stage<AnimatedWidget> stage = new Stage<AnimatedWidget> ();
		
		bool OnActorStep (Actor<AnimatedWidget> actor)
		{
			switch (actor.Target.AnimationState) {
			case AnimationState.Coming:
				actor.Target.QueueDraw ();
				actor.Target.Percent = actor.Percent;
				if (actor.Expired) {
					actor.Target.AnimationState = AnimationState.Idle;
					return false;
				}
				break;
			case AnimationState.IntendingToGo:
				actor.Target.AnimationState = AnimationState.Going;
				actor.Target.Bias = actor.Percent;
				actor.Reset ((uint)(actor.Target.Duration * actor.Percent));
				break;
			case AnimationState.Going:
				if (actor.Expired) {
					this.Remove (actor.Target);
					return false;
				}
				actor.Target.Percent = 1.0 - actor.Percent;
				break;
			}
			return true;
		}
		
		void OnWidgetDestroyed (object sender, EventArgs args)
		{
			RemoveCore ((AnimatedWidget)sender);
		}
		
		void RemoveCore (AnimatedWidget widget)
		{
			RemoveCore (widget, widget.Duration, 0, 0, false, false);
		}
		
		void RemoveCore (AnimatedWidget widget, uint duration, Easing easing, Blocking blocking, bool use_easing, bool use_blocking)
		{
			if (duration > 0)
				widget.Duration = duration;
			
			if (use_easing)
				widget.Easing = easing;
			
			if (use_blocking)
				widget.Blocking = blocking;
			
			if (widget.AnimationState == AnimationState.Coming) {
				widget.AnimationState = AnimationState.IntendingToGo;
			} else {
				if (widget.Easing == Easing.QuadraticIn) {
					widget.Easing = Easing.QuadraticOut;
				} else if (widget.Easing == Easing.QuadraticOut) {
					widget.Easing = Easing.QuadraticIn;
				} else if (widget.Easing == Easing.ExponentialIn) {
					widget.Easing = Easing.ExponentialOut;
				} else if (widget.Easing == Easing.ExponentialOut) {
					widget.Easing = Easing.ExponentialIn;
				}
				widget.AnimationState = AnimationState.Going;
				stage.Add (widget, widget.Duration);
			}
		}
		
		internal void AddAnimatedWidget (Widget widget, uint duration, Easing easing, Blocking blocking, int x, int y)
		{
			AnimatedWidget animated_widget = new AnimatedWidget (widget, duration, easing, blocking, false);
			animated_widget.Parent = this;
			animated_widget.WidgetDestroyed += OnWidgetDestroyed;
			stage.Add (animated_widget, duration);
			animated_widget.StartPadding = 0;
			animated_widget.EndPadding = widget.Allocation.Height;
			//			animated_widget.Node = animated_widget;
			
			EditorContainerChild info = new EditorContainerChild (this, animated_widget);
			info.X = x;
			info.Y = y;
			info.FixedPosition = true;
			containerChildren.Add (info);
		}
		#endregion
		
		#region TextArea delegation
		public TextDocument Document {
			get {
				return textArea.Document;
			}
			set {
				textArea.Document = value;
			}
		}
		
		public bool IsDisposed {
			get {
				return textArea.IsDisposed;
			}
		}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoTextEditor"/> converts tabs to spaces.
		/// It is possible to overwrite the default options value for certain languages (like F#).
		/// </summary>
		/// <value>
		/// <c>true</c> if tabs to spaces should be converted; otherwise, <c>false</c>.
		/// </value>
		public bool TabsToSpaces {
			get {
				return textArea.TabsToSpaces;
			}
			set {
				textArea.TabsToSpaces = value;
			}
		}

		public Mono.TextEditor.CaretImpl Caret {
			get {
				return textArea.Caret;
			}
		}

		protected internal IMMulticontext IMContext {
			get { return textArea.IMContext; }
		}

		public string IMModule {
			get {
				return textArea.IMModule;
			}
			set {
				textArea.IMModule = value;
			}
		}

		internal ITextEditorOptions Options {
			get {
				return textArea.Options;
			}
			set {
				textArea.Options = value;
			}
		}

		public string FileName {
			get {
				return textArea.FileName;
			}
		}

		public string MimeType {
			get {
				return textArea.MimeType;
			}
		}

		public double LineHeight {
			get {
				return textArea.LineHeight;
			}
			internal set {
				textArea.LineHeight = value;
			}
		}

		public TextViewMargin TextViewMargin {
			get {
				return textArea.TextViewMargin;
			}
		}

		public ActionMargin ActionMargin {
			get {
				return textArea.ActionMargin;
			}
		}

		public Margin IconMargin {
			get { return textArea.IconMargin; }
		}

		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			return textArea.LogicalToVisualLocation (location);
		}
		
		public DocumentLocation LogicalToVisualLocation (int line, int column)
		{
			return textArea.LogicalToVisualLocation (line, column);
		}
		
		public void CenterToCaret ()
		{
			textArea.CenterToCaret ();
		}
		
		public void CenterTo (int offset)
		{
			textArea.CenterTo (offset);
		}
		
		public void CenterTo (int line, int column)
		{
			textArea.CenterTo (line, column);
		}
		
		public void CenterTo (DocumentLocation p)
		{
			textArea.CenterTo (p);
		}

		internal void SmoothScrollTo (double value)
		{
			textArea.SmoothScrollTo (value);
		}

		public void ScrollTo (int offset)
		{
			textArea.ScrollTo (offset);
		}
		
		public void ScrollTo (int line, int column)
		{
			textArea.ScrollTo (line, column);
		}

		public void ScrollTo (DocumentLocation p)
		{
			textArea.ScrollTo (p);
		}

		public void ScrollTo (Gdk.Rectangle rect)
		{
			textArea.ScrollTo (rect);
		}

		public void ScrollToCaret ()
		{
			textArea.ScrollToCaret ();
		}

		public void TryToResetHorizontalScrollPosition ()
		{
			textArea.TryToResetHorizontalScrollPosition ();
		}

		public int GetWidth (string text)
		{
			return textArea.GetWidth (text);
		}

		internal void HideMouseCursor ()
		{
			textArea.HideMouseCursor ();
		}

		public void ClearTooltipProviders ()
		{
			GetTextEditorData ().ClearTooltipProviders ();
		}
		
		public void AddTooltipProvider (TooltipProvider provider)
		{
			GetTextEditorData ().AddTooltipProvider (provider);
		}
		
		public void RemoveTooltipProvider (TooltipProvider provider)
		{
			GetTextEditorData ().RemoveTooltipProvider (provider);
		}

		internal void RedrawMargin (Margin margin)
		{
			textArea.RedrawMargin (margin);
		}
		
		public void RedrawMarginLine (Margin margin, int logicalLine)
		{
			textArea.RedrawMarginLine (margin, logicalLine);
		}
		internal void RedrawPosition (int logicalLine, int logicalColumn)
		{
			textArea.RedrawPosition (logicalLine, logicalColumn);
		}

		internal void RedrawLine (int line)
		{
			textArea.RedrawLine (line);
		}

		internal void RedrawLines (int start, int end)
		{
			textArea.RedrawLines (start, end);
		}

		internal string preeditString {
			get {
				return textArea.preeditString;
			}
		}

		internal int preeditOffset {
			get {
				return textArea.preeditOffset;
			}
		}
		
		internal int preeditCursorCharIndex {
			get {
				return textArea.preeditCursorCharIndex;
			}
		}
		
		internal Pango.AttrList preeditAttrs {
			get {
				return textArea.preeditAttrs;
			}
		}

		internal bool UpdatePreeditLineHeight ()
		{
			return textArea.UpdatePreeditLineHeight ();
		}

		internal void ResetIMContext ()
		{
			textArea.ResetIMContext ();
		}

		internal bool ContainsPreedit (int offset, int length)
		{
			return textArea.ContainsPreedit (offset, length);
		}

		internal void FireLinkEvent (string link, uint button, ModifierType modifierState)
		{
			textArea.FireLinkEvent (link, button, modifierState);
		}

		internal void RequestResetCaretBlink ()
		{
			textArea.RequestResetCaretBlink ();
		}
		
		internal void SetAdjustments  ()
		{
			textArea.SetAdjustments ();
		}
		
		public bool IsInDrag {
			get {
				return textArea.IsInDrag;
			}
		}

		public event EventHandler VScroll {
			add { textArea.VScroll += value; }
			remove { textArea.VScroll -= value; }
		}

		public event EventHandler HScroll {
			add { textArea.HScroll += value; }
			remove { textArea.HScroll -= value; }
		}

		#endregion

		#region TextEditorData delegation
		public string EolMarker {
			get {
				return textArea.EolMarker;
			}
		}

		internal EditorTheme EditorTheme {
			get {
				return textArea.EditorTheme;
			}
		}
		
		public EditMode CurrentMode {
			get {
				return textArea.CurrentMode;
			}
			set {
				textArea.CurrentMode = value;
			}
		}
		
		public bool IsSomethingSelected {
			get {
				return textArea.IsSomethingSelected;
			}
		}
		
		public MonoDevelop.Ide.Editor.Selection MainSelection {
			get {
				return textArea.MainSelection;
			}
			set {
				textArea.MainSelection = value;
			}
		}
		
		public MonoDevelop.Ide.Editor.SelectionMode SelectionMode {
			get {
				return textArea.SelectionMode;
			}
			set {
				textArea.SelectionMode = value;
			}
		}
		
		public ISegment SelectionRange {
			get {
				return textArea.SelectionRange;
			}
			set {
				textArea.SelectionRange = value;
			}
		}
		
		public string SelectedText {
			get {
				return textArea.SelectedText;
			}
			set {
				textArea.SelectedText = value;
			}
		}
		
		public int SelectionAnchor {
			get {
				return textArea.SelectionAnchor;
			}
			set {
				textArea.SelectionAnchor = value;
			}
		}

		public int SelectionLead {
			get {
				return textArea.SelectionLead;
			}
			set {
				textArea.SelectionLead = value;
			}
		}

		public IEnumerable<DocumentLine> SelectedLines {
			get {
				return textArea.SelectedLines;
			}
		}
		
		public Adjustment HAdjustment {
			get {
				return textArea.HAdjustment;
			}
		}
		
		public Adjustment VAdjustment {
			get {
				return textArea.VAdjustment;
			}
		}
		
		public int Insert (int offset, string value)
		{
			return textArea.Insert (offset, value);
		}
		
		public void Remove (DocumentRegion region)
		{
			textArea.Remove (region);
		}
		
		public void Remove (ISegment removeSegment)
		{
			textArea.Remove (removeSegment);
		}
		
		public void Remove (int offset, int count)
		{
			textArea.Remove (offset, count);
		}
		
		public int Replace (int offset, int count, string value)
		{
			return textArea.Replace (offset, count, value);
		}
		
		public void ClearSelection ()
		{
			textArea.ClearSelection ();
		}
		
		public void DeleteSelectedText ()
		{
			textArea.DeleteSelectedText ();
		}
		
		public void DeleteSelectedText (bool clearSelection)
		{
			textArea.DeleteSelectedText (clearSelection);
		}
		
		public void RunEditAction (Action<TextEditorData> action)
		{
			action (GetTextEditorData ());
		}
		
		public void SetSelection (int anchorOffset, int leadOffset)
		{
			textArea.SetSelection (anchorOffset, leadOffset);
		}
		
		public void SetSelection (DocumentLocation anchor, DocumentLocation lead)
		{
			textArea.SetSelection (anchor, lead);
		}
		
		public void SetSelection (int anchorLine, int anchorColumn, int leadLine, int leadColumn)
		{
			textArea.SetSelection (anchorLine, anchorColumn, leadLine, leadColumn);
		}
		
		public void ExtendSelectionTo (DocumentLocation location)
		{
			textArea.ExtendSelectionTo (location);
		}
		public void ExtendSelectionTo (int offset)
		{
			textArea.ExtendSelectionTo (offset);
		}
		public void SetSelectLines (int from, int to)
		{
			textArea.SetSelectLines (from, to);
		}
		
		public void InsertAtCaret (string text)
		{
			textArea.InsertAtCaret (text);
		}
		
		public bool CanEdit (int line)
		{
			return textArea.CanEdit (line);
		}
		
		public string GetLineText (int line)
		{
			return textArea.GetLineText (line);
		}
		
		public string GetLineText (int line, bool includeDelimiter)
		{
			return textArea.GetLineText (line, includeDelimiter);
		}
		
		/// <summary>
		/// Use with care.
		/// </summary>
		/// <returns>
		/// A <see cref="TextEditorData"/>
		/// </returns>
		public TextEditorData GetTextEditorData ()
		{
			return textArea.GetTextEditorData ();
		}

		/// <remarks>
		/// The Key may be null if it has been handled by the IMContext. In such cases, the char is the value.
		/// </remarks>
		protected internal virtual bool OnIMProcessedKeyPressEvent (Gdk.Key key, uint ch, Gdk.ModifierType state)
		{
			SimulateKeyPress (key, ch, state);
			return true;
		}

		public void SimulateKeyPress (Gdk.Key key, uint unicodeChar, ModifierType modifier)
		{
			textArea.SimulateKeyPress (key, unicodeChar, modifier);
		}

		
		public void RunAction (Action<TextEditorData> action)
		{
			try {
				action (GetTextEditorData ());
			} catch (Exception e) {
				if (Debugger.IsAttached)
					Debugger.Break ();
				//TODO: we should really find a way to log this properly
				Console.WriteLine ("Error while executing " + action + " :" + e);
			}
		}

		public void HideTooltip (bool checkMouseOver = true)
		{
			textArea.HideTooltip (checkMouseOver);
		}
		public Action<Gdk.EventButton> DoPopupMenu {
			get {
				return textArea.DoPopupMenu;
			}
			set {
				textArea.DoPopupMenu = value;
			} 
		}

		public MenuItem CreateInputMethodMenuItem (string label)
		{
			return textArea.CreateInputMethodMenuItem (label);
		}

		public event EventHandler SelectionChanged {
			add { textArea.SelectionChanged += value; }
			remove { textArea.SelectionChanged -= value; }
		}

		public void CaretToDragCaretPosition ()
		{
			textArea.CaretToDragCaretPosition ();
		}

		public event EventHandler<PaintEventArgs> Painted {
			add { textArea.Painted += value; }
			remove { textArea.Painted -= value; }
		}

		public event EventHandler<LinkEventArgs> LinkRequest {
			add { textArea.LinkRequest += value; }
			remove { textArea.LinkRequest -= value; }
		}

		internal void ShowListWindow<T> (ListWindow<T> window, DocumentLocation loc)
		{
			textArea.ShowListWindow<T> (window, loc);
		}

		public Margin LockedMargin {
			get {
				return textArea.LockedMargin;
			}
			set {
				textArea.LockedMargin = value;
			}
		}
		#endregion
		
		#region Document delegation

		public event EventHandler EditorOptionsChanged {
			add { textArea.EditorOptionsChanged += value; }
			remove { textArea.EditorOptionsChanged -= value; }
		}

		protected virtual void OptionsChanged (object sender, EventArgs args)
		{
		}

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
		public string SearchPattern {
			get {
				return textArea.SearchPattern;
			}
			set {
				textArea.SearchPattern = value;
			}
		}
		
		public ISearchEngine SearchEngine {
			get {
				return textArea.SearchEngine;
			}
			set {
				textArea.SearchEngine = value;
			}
		}

		public event EventHandler HighlightSearchPatternChanged {
			add { textArea.HighlightSearchPatternChanged += value; }
			remove { textArea.HighlightSearchPatternChanged -= value; }
		}

		public bool HighlightSearchPattern {
			get {
				return textArea.HighlightSearchPattern;
			}
			set {
				textArea.HighlightSearchPattern = value;
			}
		}
		
		public bool IsCaseSensitive {
			get {
				return textArea.IsCaseSensitive;
			}
			set {
				textArea.IsCaseSensitive = value;
			}
		}
		
		public bool IsWholeWordOnly {
			get {
				return textArea.IsWholeWordOnly;
			}
			
			set {
				textArea.IsWholeWordOnly = value;
			}
		}
		
		public ISegment SearchRegion {
			get {
				return textArea.SearchRegion;
			}
			
			set {
				textArea.SearchRegion = value;
			}
		}
		
		public SearchResult SearchForward (int fromOffset)
		{
			return textArea.SearchForward (fromOffset);
		}
		
		public SearchResult SearchBackward (int fromOffset)
		{
			return textArea.SearchBackward (fromOffset);
		}
		
		/// <summary>
		/// Initiate a pulse at the specified document location
		/// </summary>
		/// <param name="pulseStart">
		/// A <see cref="DocumentLocation"/>
		/// </param>
		public void PulseCharacter (DocumentLocation pulseStart)
		{
			textArea.PulseCharacter (pulseStart);
		}
		
		
		public SearchResult FindNext (bool setSelection)
		{
			return textArea.FindNext (setSelection);
		}
		
		public void StartCaretPulseAnimation ()
		{
			textArea.StartCaretPulseAnimation ();
		}

		public void StopSearchResultAnimation ()
		{
			textArea.StopSearchResultAnimation ();
		}
		
		public void AnimateSearchResult (SearchResult result)
		{
			textArea.AnimateSearchResult (result);
		}

		public SearchResult FindPrevious (bool setSelection)
		{
			return textArea.FindPrevious (setSelection);
		}
		
		public bool Replace (string withPattern)
		{
			return textArea.Replace (withPattern);
		}
		
		public int ReplaceAll (string withPattern)
		{
			return textArea.ReplaceAll (withPattern);
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
		
		public double ColumnToX (DocumentLine line, int column)
		{
			if (line == null)
				throw new ArgumentNullException ("line");
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


		public void SetCaretTo (int line, int column)
		{
			textArea.SetCaretTo (line, column);
		}
		
		public void SetCaretTo (int line, int column, bool highlight)
		{
			textArea.SetCaretTo (line, column, highlight);
		}
		
		public void SetCaretTo (int line, int column, bool highlight, bool centerCaret)
		{
			textArea.SetCaretTo (line, column, highlight, centerCaret);
		}
		public event EventHandler<Xwt.MouseMovedEventArgs> BeginHover {
			add { textArea.BeginHover += value; }
			remove { textArea.BeginHover -= value; }
		}

	}
}

