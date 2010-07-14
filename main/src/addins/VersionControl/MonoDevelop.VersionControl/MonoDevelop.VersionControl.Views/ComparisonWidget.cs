// 
// DiffWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Components.Diff;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Views
{
	public class ComparisonWidget : Bin
	{
		Adjustment vAdjustment, leftVAdjustment, rightVAdjustment;
		Gtk.VScrollbar vScrollBar;
		
		Adjustment hAdjustment, leftHAdjustment, rightHAdjustment;
		Gtk.HScrollbar leftHScrollBar, rightHScrollBar;
		Gtk.Button next, prev;
		
		OverviewRenderer overview;
		MiddleArea middleArea;
		
		DropDownBox originalComboBox, diffComboBox;
		TextEditor originalEditor, diffEditor;
		
		List<ContainerChild> children = new List<ContainerChild> ();
		
		public Adjustment Vadjustment {
			get { return this.vAdjustment; }
		}

		public Adjustment Hadjustment {
			get { return this.hAdjustment; }
		}
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (ContainerChild info in children.ToArray ()) {
					if (info.Child == w)
						return info;
				}
				return null;
			}
		}
		
		public TextEditor OriginalEditor {
			get {
				return this.originalEditor;
			}
		}

		public TextEditor DiffEditor {
			get {
				return this.diffEditor;
			}
		}
		
		Diff diff;
		public Diff Diff {
			get { return diff; }
			set { diff = value; llcsCache.Clear (); }
		}
		
		protected ComparisonWidget (IntPtr ptr) : base (ptr)
		{
		}
		
		void Connect (Adjustment fromAdj, Adjustment toAdj)
		{
			fromAdj.Changed += AdjustmentChanged;
			fromAdj.ValueChanged += delegate {
				if (toAdj.Value != fromAdj.Value)
					toAdj.Value = fromAdj.Value;
			};
			
			toAdj.ValueChanged += delegate {
				if (toAdj.Value != fromAdj.Value)
					fromAdj.Value = toAdj.Value;
			};
		}
		
		void AdjustmentChanged (object sender, EventArgs e)
		{
			vAdjustment.SetBounds (Math.Min (leftVAdjustment.Lower, rightVAdjustment.Lower), 
				Math.Max (leftVAdjustment.Upper, rightVAdjustment.Upper),
				leftVAdjustment.StepIncrement,
				leftVAdjustment.PageIncrement,
				leftVAdjustment.PageSize);
			hAdjustment.SetBounds (Math.Min (leftHAdjustment.Lower, rightHAdjustment.Lower), 
				Math.Max (leftHAdjustment.Upper, rightHAdjustment.Upper),
				leftHAdjustment.StepIncrement,
				leftHAdjustment.PageIncrement,
				leftHAdjustment.PageSize);
		}


		VersionControlDocumentInfo info;
		public ComparisonWidget (VersionControlDocumentInfo info)
		{
			this.info = info;
			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			vAdjustment.Changed += HandleAdjustmentChanged;
			leftVAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (leftVAdjustment, vAdjustment);
			
			rightVAdjustment =  new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (rightVAdjustment, vAdjustment);
			
			vScrollBar = new VScrollbar (vAdjustment);
			AddChild (vScrollBar);
			vScrollBar.Hide ();
			
			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			hAdjustment.Changed += HandleAdjustmentChanged;
			leftHAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (leftHAdjustment, hAdjustment);
			
			rightHAdjustment =  new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (rightHAdjustment, hAdjustment);
			
			leftHScrollBar = new HScrollbar (hAdjustment);
			AddChild (leftHScrollBar);
			
			rightHScrollBar = new HScrollbar (hAdjustment);
			AddChild (rightHScrollBar);
			
			originalEditor = new TextEditor ();
			originalEditor.Caret.PositionChanged += CaretPositionChanged;
			originalEditor.FocusInEvent += EditorFocusIn;
			AddChild (originalEditor);
			originalEditor.SetScrollAdjustments (leftHAdjustment, leftVAdjustment);
			
			originalComboBox = new DropDownBox ();
			originalComboBox.WindowRequestFunc = CreateComboBoxSelector;
			originalComboBox.Text = "Local";
			originalComboBox.Tag = originalEditor;
			AddChild (originalComboBox);
			
			diffEditor = new TextEditor ();
			diffEditor.Caret.PositionChanged += CaretPositionChanged;
			diffEditor.FocusInEvent += EditorFocusIn;
			
			AddChild (diffEditor);
			diffEditor.Document.ReadOnly = true;
			diffEditor.SetScrollAdjustments (leftHAdjustment, leftVAdjustment);
			this.vAdjustment.ValueChanged += delegate {
				middleArea.QueueDraw ();
			};
			
			diffComboBox = new DropDownBox ();
			diffComboBox.WindowRequestFunc = CreateComboBoxSelector;
			diffComboBox.Text = "Base";
			diffComboBox.Tag = diffEditor;
			AddChild (diffComboBox);
			
			
			overview = new OverviewRenderer (this);
			AddChild (overview);
			
			middleArea = new MiddleArea (this);
			AddChild (middleArea);
			
			prev = new Button ();
			prev.Add (new Arrow (ArrowType.Up, ShadowType.None));
			AddChild (prev);
			prev.ShowAll ();
			prev.Clicked += delegate {
				if (this.Diff == null)
					return;
				originalEditor.GrabFocus ();
				
				int line = originalEditor.Caret.Line;
				int max  = -1, searched = -1;
				foreach (Diff.Hunk hunk in this.Diff) {
					if (hunk.Same)
						continue;
					max = System.Math.Max (hunk.Right.Start, max);
					if (hunk.Right.Start < line)
						searched = System.Math.Max (hunk.Right.Start, searched);
				}
				if (max >= 0) {
					originalEditor.Caret.Line = searched < 0 ? max : searched;
					originalEditor.CenterToCaret ();
				}
			};
			
			next = new Button ();
			next.BorderWidth = 0;
			next.Add (new Arrow (ArrowType.Down, ShadowType.None));
			next.Clicked += delegate {
				if (this.Diff == null)
					return;
				originalEditor.GrabFocus ();
				
				int line = originalEditor.Caret.Line;
				int min  = Int32.MaxValue, searched = Int32.MaxValue;
				foreach (Diff.Hunk hunk in this.Diff) {
					if (hunk.Same)
						continue;
					min = System.Math.Min (hunk.Right.Start, min);
					if (hunk.Right.Start > line)
						searched = System.Math.Min (hunk.Right.Start, searched);
				}
				if (min < Int32.MaxValue) {
					originalEditor.Caret.Line = searched == Int32.MaxValue ? min : searched;
					originalEditor.CenterToCaret ();
				}
			};
			AddChild (next);
			next.ShowAll ();
			
			this.DoubleBuffered = true;
			originalEditor.ExposeEvent += HandleLeftEditorExposeEvent;
			diffEditor.ExposeEvent += HandleRightEditorExposeEvent;
			info.Document.TextEditorData.Document.TextReplaced += HandleInfoDocumentTextEditorDataDocumentTextReplaced;
		}


		internal static void EditorFocusIn (object sender, FocusInEventArgs args)
		{
			TextEditor editor = (TextEditor)sender;
			UpdateCaretPosition (editor.Caret);
		}

		internal static void CaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			Caret caret = (Caret)sender;
			UpdateCaretPosition (caret);
		}
		
		static void UpdateCaretPosition (Caret caret)
		{
			int offset = caret.Offset;
			if (offset < 0 || offset > caret.TextEditorData.Document.Length)
				return;
			DocumentLocation location = caret.TextEditorData.LogicalToVisualLocation (caret.Location);
			IdeApp.Workbench.StatusBar.ShowCaretState (caret.Line + 1,
			                                           location.Column + 1,
			                                           caret.TextEditorData.IsSomethingSelected ? caret.TextEditorData.SelectionRange.Length : 0,
			                                           caret.IsInInsertMode);
		}

		List<TextEditorData> localUpdate = new List<TextEditorData> ();

		void HandleInfoDocumentTextEditorDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			foreach (var data in localUpdate.ToArray ()) {
				data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
				data.Replace (e.Offset, e.Count, e.Value);
				data.Document.TextReplaced += HandleDataDocumentTextReplaced;
				data.Document.CommitUpdateAll ();
			}
		}

		public void CreateDiff ()
		{
			var leftLines = from l in OriginalEditor.Document.Lines select OriginalEditor.Document.GetTextAt (l.Offset, l.EditableLength);
			var rightLines = from l in DiffEditor.Document.Lines select DiffEditor.Document.GetTextAt (l.Offset, l.EditableLength);
			
			Diff = new Diff (rightLines.ToArray (), leftLines.ToArray (), true, true);
			QueueDraw ();
		}
		
		Dictionary<Mono.TextEditor.Document, TextEditorData> dict = new Dictionary<Mono.TextEditor.Document, TextEditorData> ();
		public void SetLocal (TextEditorData data)
		{
			dict[data.Document] = data;
			data.Document.Text = info.Document.TextEditorData.Document.Text;
			data.Document.ReadOnly = false;
			data.Document.TextReplaced += HandleDataDocumentTextReplaced;
			localUpdate.Add (data);
			CreateDiff ();
		}
		
		void HandleDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			var data = dict[(Document)sender];
			localUpdate.Remove (data);
			info.Document.TextEditorData.Replace (e.Offset, e.Count, e.Value);
			localUpdate.Add (data);
			CreateDiff ();
		}
		
		public void RemoveLocal (TextEditorData data)
		{
			localUpdate.Remove (data);
			data.Document.ReadOnly = true;
			data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
		}
		

		class ComboBoxSelector : DropDownBoxListWindow.IListDataProvider
		{
			ComparisonWidget widget;
			DropDownBox box;
			
			public ComboBoxSelector (ComparisonWidget widget, DropDownBox box)
			{
				this.widget = widget;
				this.box = box;
				
			}

			#region IListDataProvider implementation
			public void Reset ()
			{
			}
	
			public string GetText (int n)
			{
				if (n == 0)
					return "Local";
				if (n == 1)
					return "Base";
				Revision rev = widget.info.History[n - 2];
				return rev.ToString () + "\t" + rev.Time.ToString () + "\t" + rev.Author;
			}

			public Pixbuf GetIcon (int n)
			{
				return null;
			}

			public object GetTag (int n)
			{
				if (n < 2)
					return null;
				return widget.info.History[n - 2];
			}
			
			public void ActivateItem (int n)
			{
				if (n == 0) {
					box.SetItem ("Local", null, new object());
					widget.SetLocal (((TextEditor)box.Tag).GetTextEditorData ());
					return;
				}
				widget.RemoveLocal (((TextEditor)box.Tag).GetTextEditorData ());
				((TextEditor)box.Tag).Document.ReadOnly = true;
				if (n == 1) {
					box.SetItem ("Base", null, new object());
					((TextEditor)box.Tag).Document.Text = System.IO.File.ReadAllText (widget.info.Item.Repository.GetPathToBaseText (widget.info.Item.Path));
					widget.CreateDiff ();
					return;
				}
				
				BackgroundWorker worker = new BackgroundWorker ();
				worker.DoWork += HandleWorkerDoWork;
				worker.RunWorkerCompleted += HandleWorkerRunWorkerCompleted;
				Revision rev = widget.info.History[n - 2];
				worker.RunWorkerAsync (rev);
				IdeApp.Workbench.StatusBar.BeginProgress (string.Format (GettextCatalog.GetString ("Retrieving revision {0}..."), rev.ToString ()));
				IdeApp.Workbench.StatusBar.AutoPulse = true;
				box.Sensitive = false;
			}

			void HandleWorkerRunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e)
			{
				Application.Invoke (delegate {
					var result = (KeyValuePair<Revision, string>)e.Result;
					box.SetItem (string.Format (GettextCatalog.GetString ("Revision {0}\t{1}\t{2}"), result.Key, result.Key.Time, result.Key.Author), null, result.Key);
					((TextEditor)box.Tag).Document.Text = result.Value;
					widget.CreateDiff ();
					IdeApp.Workbench.StatusBar.AutoPulse = false;
					IdeApp.Workbench.StatusBar.EndProgress ();
					box.Sensitive = true;
				});
			}

			void HandleWorkerDoWork (object sender, DoWorkEventArgs e)
			{
				Revision rev = (Revision)e.Argument;
				string text = null;
				try {
					Console.WriteLine (widget.info.VersionInfo.RepositoryPath);
					text = widget.info.Item.Repository.GetTextAtRevision (widget.info.VersionInfo.RepositoryPath, rev);
				} catch (Exception ex) {
					text = "Error retrieving revision " + rev + Environment.NewLine + ex.ToString ();
				}
				e.Result = new KeyValuePair<Revision, string> (rev, text);
			}

			public int IconCount {
				get {
					return widget.info.History == null ? 2 : widget.info.History.Length + 2;
				}
			}
			#endregion
		}
		
		Gtk.Window CreateComboBoxSelector (DropDownBox box)
		{
			DropDownBoxListWindow window = new DropDownBoxListWindow (new ComboBoxSelector (this, box));
			return window;
		}
		
		void HandleAdjustmentChanged (object sender, EventArgs e)
		{
			Adjustment adjustment = (Adjustment)sender;
			Scrollbar scrollbar = adjustment == vAdjustment ? (Scrollbar)vScrollBar : leftHScrollBar;
			bool newVisible = adjustment.Upper - adjustment.Lower > adjustment.PageSize;
			if (scrollbar.Visible != newVisible) {
				scrollbar.Visible = newVisible;
				QueueResize ();
			}
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			
			if (include_internals)
				children.ForEach (child => callback (child.Child));
		}
		
		public void AddChild (Gtk.Widget child)
		{
			child.Parent = this;
			children.Add (new ContainerChild (this, child));
			child.Show ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			if (widget == Child)
				widget.SetScrollAdjustments (hAdjustment, vAdjustment);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			widget.Unparent ();
			foreach (var info in children.ToArray ()) {
				if (info.Child == widget) {
					children.Remove (info);
					break;
				}
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			children.ForEach (child => child.Child.Destroy ());
			children.Clear ();
			info.Document.TextEditorData.Document.TextReplaced -= HandleInfoDocumentTextEditorDataDocumentTextReplaced;
		}
		 
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			int overviewWidth = 16;
			int vwidth = 1; // vScrollBar.Visible ? vScrollBar.Requisition.Width : 1;
			int hheight = leftHScrollBar.Visible ? leftHScrollBar.Requisition.Height : 1; 
			Rectangle childRectangle = new Rectangle (allocation.X + 1, allocation.Y + 1, allocation.Width - vwidth - overviewWidth, allocation.Height - hheight);
			
//			if (vScrollBar.Visible) {
//				int right = childRectangle.Right;
//				int vChildTopHeight = -1;
//				int v = leftHScrollBar.Visible ? leftHScrollBar.Requisition.Height : 0;
//				vScrollBar.SizeAllocate (new Rectangle (right, childRectangle.Y + vChildTopHeight, vwidth, Allocation.Height - v - vChildTopHeight));
//				vScrollBar.Value = System.Math.Max (System.Math.Min (vAdjustment.Upper - vAdjustment.PageSize, vScrollBar.Value), vAdjustment.Lower);
//			}
			
			Requisition nextReq = next.SizeRequest ();
			Requisition prevReq = prev.SizeRequest ();
			
			Requisition comboReq =  originalComboBox.SizeRequest ();
			
			overview.SizeAllocate (new Rectangle (allocation.Right - overviewWidth + 1, childRectangle.Y, overviewWidth - 1, childRectangle.Height - nextReq.Height - prevReq.Height));
			
			prev.SizeAllocate (new Rectangle (overview.Allocation.X, overview.Allocation.Bottom + 4, overviewWidth - 1, prevReq.Height));
			next.SizeAllocate (new Rectangle (overview.Allocation.X, prev.Allocation.Bottom, overviewWidth - 1, nextReq.Height));
			
			int spacerWidth = 34;
			int editorWidth = (childRectangle.Width - spacerWidth) / 2;
			
			diffComboBox.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Top, editorWidth, comboReq.Height));
			originalComboBox.SizeAllocate (new Rectangle (childRectangle.Right - editorWidth, childRectangle.Top, editorWidth, comboReq.Height));
			
			diffEditor.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Top + comboReq.Height, editorWidth, Allocation.Height - hheight - comboReq.Height));
			originalEditor.SizeAllocate (new Rectangle (childRectangle.Right - editorWidth, childRectangle.Top + comboReq.Height, editorWidth, Allocation.Height - hheight - comboReq.Height));
			
			middleArea.SizeAllocate (new Rectangle (diffEditor.Allocation.Right, childRectangle.Top, spacerWidth + 1, childRectangle.Height));
			
			if (leftHScrollBar.Visible) {
				leftHScrollBar.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Bottom, editorWidth, hheight));
				rightHScrollBar.SizeAllocate (new Rectangle (childRectangle.Right - editorWidth, childRectangle.Bottom, editorWidth, hheight));
				leftHScrollBar.Value = rightHScrollBar.Value = System.Math.Max (System.Math.Min (hAdjustment.Upper - hAdjustment.PageSize, leftHScrollBar.Value), hAdjustment.Lower);
			}
		}
		
		static double GetWheelDelta (Scrollbar scrollbar, ScrollDirection direction)
		{
			double delta = System.Math.Pow (scrollbar.Adjustment.PageSize, 2.0 / 3.0);
			if (direction == ScrollDirection.Up || direction == ScrollDirection.Left)
				delta = -delta;
			if (scrollbar.Inverted)
				delta = -delta;
			return delta;
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			Scrollbar scrollWidget = (evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Down) ? (Scrollbar)vScrollBar : leftHScrollBar;
			
			if (scrollWidget.Visible) {
				double newValue = scrollWidget.Adjustment.Value + GetWheelDelta (scrollWidget, evnt.Direction);
				newValue = System.Math.Max (System.Math.Min (scrollWidget.Adjustment.Upper  - scrollWidget.Adjustment.PageSize, newValue), scrollWidget.Adjustment.Lower);
				scrollWidget.Adjustment.Value = newValue;
			}
			return base.OnScrollEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}
		
		public static Cairo.Color GetColor (Diff.Hunk hunk, double alpha)
		{
			if (hunk.Left.Count == 0)
				return new Cairo.Color (0.4, 0.8, 0.4, alpha);
			if (hunk.Right.Count == 0) 
				return new Cairo.Color (0.8, 0.4, 0.4, alpha);
			return new Cairo.Color (0.4, 0.8, 0.8, alpha);
		}
		const double fillAlpha = 0.1;
		const double lineAlpha = 0.6;
		Dictionary<Diff.Hunk, int[,]> llcsCache = new Dictionary<Diff.Hunk, int[,]> ();
		
		int[,] GetLCS (Diff.Hunk hunk)
		{
			int[,] result;
			if (llcsCache.TryGetValue (hunk, out result))
				return result;
			return null;
		}
		
		void HandleLeftEditorExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				if (Diff != null) {
					foreach (Diff.Hunk hunk in Diff) {
						if (!hunk.Same) {
							int y1 = originalEditor.LineToVisualY (hunk.Right.Start) - (int)originalEditor.VAdjustment.Value;
							int y2 = originalEditor.LineToVisualY (hunk.Right.Start + hunk.Right.Count) - (int)originalEditor.VAdjustment.Value;
							if (y1 == y2)
								y2 = y1 + 1;
							cr.Rectangle (0, y1, originalEditor.Allocation.Width, y2 - y1);
							cr.Color = GetColor (hunk, fillAlpha);
							cr.Fill ();
							if (hunk.Right.Count > 0 && hunk.Left.Count > 0) {
								int startOffset = originalEditor.Document.GetLine (hunk.Right.Start).Offset;
								int rStartOffset = diffEditor.Document.GetLine (hunk.Left.Start).Offset;
								var lcs = GetLCS (hunk);
								if (lcs == null) {
									string leftText = originalEditor.Document.GetTextBetween (startOffset, originalEditor.Document.GetLine (hunk.Right.Start + hunk.Right.Count - 1).EndOffset);
									string rightText = diffEditor.Document.GetTextBetween (rStartOffset, diffEditor.Document.GetLine (hunk.Left.Start + hunk.Left.Count - 1).EndOffset);
									llcsCache[hunk] = lcs = GetLCS (leftText, rightText);
								}
								
								int ll = lcs.GetLength (0), rl = lcs.GetLength (1);
								int blockStart = -1;
								Stack<KeyValuePair<int, int>> posStack = new Stack<KeyValuePair<int, int>> ();
								if (ll > 0 && rl > 0)
									posStack.Push (new KeyValuePair<int, int> (ll - 1, rl - 1));
								while (posStack.Count > 0) {
									var pos = posStack.Pop ();
									int i = pos.Key, j = pos.Value;
									if (i > 0 && j > 0 && originalEditor.Document.GetCharAt (startOffset + i) == diffEditor.Document.GetCharAt (rStartOffset + j)) {
										posStack.Push (new KeyValuePair<int, int> (i - 1, j - 1));
										PaintBlock (originalEditor, cr, startOffset, i, ref blockStart);
										continue;
									}
									
									if (j > 0 && (i == 0 || lcs[i, j - 1] >= lcs[i - 1, j])) {
										posStack.Push (new KeyValuePair<int, int> (i, j - 1));
										PaintBlock (originalEditor, cr, startOffset, i, ref blockStart);
									} else if (i > 0 && (j == 0 || lcs[i, j - 1] < lcs[i - 1, j])) {
										posStack.Push (new KeyValuePair<int, int> (i - 1, j));
										if (blockStart < 0)
											blockStart = i;
									}
								}
								PaintBlock (originalEditor, cr, startOffset, 0, ref blockStart);
							}
							
							cr.Color = GetColor (hunk, lineAlpha);
							cr.MoveTo (0, y1);
							cr.LineTo (originalEditor.Allocation.Width, y1);
							cr.Stroke ();
							
							cr.MoveTo (0, y2);
							cr.LineTo (originalEditor.Allocation.Width, y2);
							cr.Stroke ();
						}
					}
				}
			}
		}
		
		void PaintBlock (TextEditor editor, Cairo.Context cr, int startOffset, int i, ref int blockStart)
		{
			if (blockStart < 0)
				return;
			var point = editor.DocumentToVisualLocation (editor.Document.OffsetToLocation (startOffset + i + 1));
			point.X += editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition - (int)originalEditor.HAdjustment.Value;
			point.Y -= (int)editor.VAdjustment.Value;
			
			var point2 = editor.DocumentToVisualLocation (editor.Document.OffsetToLocation (startOffset + blockStart + 1));
			point2.X += editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition - (int)originalEditor.HAdjustment.Value;
			point2.Y -= (int)editor.VAdjustment.Value;
			
			cr.Rectangle (point.X, point.Y, point2.X - point.X, editor.LineHeight);
			cr.Color = editor == originalEditor ? new Cairo.Color (0, 1, 0, 0.2) : new Cairo.Color (1, 0, 0, 0.2);
			cr.Fill ();
			blockStart = -1;
		}

		void HandleRightEditorExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				if (Diff != null) {
					foreach (Diff.Hunk hunk in Diff) {
						if (!hunk.Same) {
							int y1 = diffEditor.LineToVisualY (hunk.Left.Start) - (int)diffEditor.VAdjustment.Value;
							int y2 = diffEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)diffEditor.VAdjustment.Value;
								
							if (y1 == y2)
								y2 = y1 + 1;
							
							cr.Rectangle (0, y1, diffEditor.Allocation.Width, y2 - y1);
							cr.Color = GetColor (hunk, fillAlpha);
							cr.Fill ();
							
							if (hunk.Right.Count > 0 && hunk.Left.Count > 0) {
								int startOffset = originalEditor.Document.GetLine (hunk.Right.Start).Offset;
								int rStartOffset = diffEditor.Document.GetLine (hunk.Left.Start).Offset;
								var lcs = GetLCS (hunk);
								if (lcs == null) {
									string leftText = originalEditor.Document.GetTextBetween (startOffset, originalEditor.Document.GetLine (hunk.Right.Start + hunk.Right.Count - 1).EndOffset);
									string rightText = diffEditor.Document.GetTextBetween (rStartOffset, diffEditor.Document.GetLine (hunk.Left.Start + hunk.Left.Count - 1).EndOffset);
									llcsCache[hunk] = lcs = GetLCS (leftText, rightText);
								}
								
								int ll = lcs.GetLength (0), rl = lcs.GetLength (1);
								int blockStart = -1;
								Stack<KeyValuePair<int, int>> posStack = new Stack<KeyValuePair<int, int>> ();
								if (ll > 0 && rl > 0)
									posStack.Push (new KeyValuePair<int, int> (ll - 1, rl - 1));
								while (posStack.Count > 0) {
									var pos = posStack.Pop ();
									int i = pos.Key, j = pos.Value;
									if (i > 0 && j > 0 && originalEditor.Document.GetCharAt (startOffset + i) == diffEditor.Document.GetCharAt (rStartOffset + j)) {
										posStack.Push (new KeyValuePair<int, int> (i - 1, j - 1));
										PaintBlock (diffEditor, cr, rStartOffset, j, ref blockStart);
										continue;
									}
									
									if (j > 0 && (i == 0 || lcs[i, j - 1] >= lcs[i - 1, j])) {
										posStack.Push (new KeyValuePair<int, int> (i, j - 1));
										if (blockStart < 0)
											blockStart = j;
									} else if (i > 0 && (j == 0 || lcs[i, j - 1] < lcs[i - 1, j])) {
										posStack.Push (new KeyValuePair<int, int> (i - 1, j));
										PaintBlock (diffEditor, cr, rStartOffset, j, ref blockStart);
									}
								}
								PaintBlock (diffEditor, cr, rStartOffset, 0, ref blockStart);
							}
							
							cr.Color = GetColor (hunk, lineAlpha);
							cr.MoveTo (0, y1);
							cr.LineTo (diffEditor.Allocation.Width, y1);
							cr.Stroke ();
							
							cr.MoveTo (0, y2);
							cr.LineTo (diffEditor.Allocation.Width, y2);
							cr.Stroke ();
						}
					}
				}
			}
		}
		
		public static int[,] GetLCS (string left, string right)
		{
			int[,] result = new int[left.Length, right.Length];
			for (int i = 0; i < left.Length; i++) {
				for (int j = 0; j < right.Length; j++) {
					if (left[i] == right[j]) {
						result[i, j] = (i == 0 || j == 0) ? 1 : 1 + result[i - 1, j - 1];
					} else {
						if (i == 0) {
							result[i, j] = j == 0 ? 0 : Math.Max(0, result[i, j - 1]);
						} else {
							result[i, j] = j == 0 ? Math.Max(result[i - 1, j], 0) : Math.Max(result[i - 1, j], result [i, j - 1]);
						}
					}
				}
			}
			return result;
		}
		
		class MiddleArea : DrawingArea 
		{
			ComparisonWidget widget;
			public MiddleArea (ComparisonWidget widget)
			{
				this.widget = widget;
				this.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask;
			}
			
			Diff.Hunk selectedHunk = null;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				Diff.Hunk selectedHunk = null;
				if (!hideButton) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					foreach (Diff.Hunk hunk in widget.Diff) {
						if (!hunk.Same) {
							int y1 = delta + widget.OriginalEditor.LineToVisualY (hunk.Right.Start) - (int)widget.OriginalEditor.VAdjustment.Value;
							int y2 = delta + widget.OriginalEditor.LineToVisualY (hunk.Right.Start + hunk.Right.Count) - (int)widget.OriginalEditor.VAdjustment.Value;
							if (y1 == y2)
								y2 = y1 + 1;
							
							int z1 = delta + widget.DiffEditor.LineToVisualY (hunk.Left.Start) - (int)widget.DiffEditor.VAdjustment.Value;
							int z2 = delta + widget.DiffEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)widget.DiffEditor.VAdjustment.Value;
							
							if (z1 == z2)
								z2 = z1 + 1;
							
							int x = (Allocation.Width) / 2;
							int y = ((y1 + y2) / 2 + (z1 + z2) / 2) / 2;
							if (Math.Sqrt (System.Math.Abs (x - evnt.X) * System.Math.Abs (x - evnt.X) +
							               System.Math.Abs (y - evnt.Y) * System.Math.Abs (y - evnt.Y)) < 10) {
								selectedHunk = hunk;
								break;
							}
						}
					}
				} else {
					selectedHunk = null;
				}
					
				if (this.selectedHunk != selectedHunk) {
					this.selectedHunk = selectedHunk;
					QueueDraw ();
				}
				
				return base.OnMotionNotifyEvent (evnt);
			}
			
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				if (!hideButton && selectedHunk != null) {
					widget.OriginalEditor.Document.BeginAtomicUndo ();
					LineSegment start = widget.OriginalEditor.Document.GetLine (selectedHunk.Right.Start);
					LineSegment end   = widget.OriginalEditor.Document.GetLine (selectedHunk.Right.Start + selectedHunk.Right.Count - 1);
					if (selectedHunk.Right.Count > 0)
						widget.OriginalEditor.Remove (start.Offset, end.EndOffset - start.Offset);
					int offset = start.Offset;
					
					if (selectedHunk.Left.Count > 0) {
						start = widget.DiffEditor.Document.GetLine (selectedHunk.Left.Start);
						end   = widget.DiffEditor.Document.GetLine (selectedHunk.Left.Start + selectedHunk.Left.Count - 1);
						widget.OriginalEditor.Insert (offset, widget.DiffEditor.Document.GetTextAt (start.Offset, end.EndOffset - start.Offset));
					}
					widget.OriginalEditor.Document.EndAtomicUndo ();
				}
				return base.OnButtonPressEvent (evnt);
			}
			
			protected override bool OnExposeEvent (EventExpose evnt)
			{
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					if (widget.Diff != null) {
						foreach (Diff.Hunk hunk in widget.Diff) {
							if (!hunk.Same) {
								int y1 = delta + widget.DiffEditor.LineToVisualY (hunk.Right.Start) - (int)widget.OriginalEditor.VAdjustment.Value;
								int y2 = delta + widget.DiffEditor.LineToVisualY (hunk.Right.Start + hunk.Right.Count) - (int)widget.OriginalEditor.VAdjustment.Value;
								if (y1 == y2)
									y2 = y1 + 1;
								
								int z1 = delta + widget.OriginalEditor.LineToVisualY (hunk.Left.Start) - (int)widget.DiffEditor.VAdjustment.Value;
								int z2 = delta + widget.OriginalEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)widget.DiffEditor.VAdjustment.Value;
								
								if (z1 == z2)
									z2 = z1 + 1;
								cr.MoveTo (Allocation.Width, y1);
								cr.LineTo (0, z1);
								cr.LineTo (0, z2);
								cr.LineTo (Allocation.Width, y2);
								cr.ClosePath ();
								cr.Color = GetColor (hunk, fillAlpha);
								cr.Fill ();
								
								cr.Color = GetColor (hunk, lineAlpha);
								cr.MoveTo (Allocation.Width, y1);
								cr.LineTo (0, z1);
								cr.Stroke ();
								
								cr.MoveTo (Allocation.Width, y2);
								cr.LineTo (0, z2);
								cr.Stroke ();
								
								if (!hideButton) {
									int x = Allocation.Width / 2;
									int y = ((y1 + y2) / 2 + (z1 + z2) / 2) / 2;
									cr.Save ();
									cr.Translate (x, y);
									cr.Scale (10, 10);
									cr.Arc (0, 0, 1, 0, 2 * Math.PI);
									
									cr.Color = GetColor (hunk, 1);
									cr.FillPreserve ();
									
									Cairo.RadialGradient shadowGradient = new Cairo.RadialGradient (0.0, 0.0, .6,  0.0, 0.0, 1.0);
									shadowGradient.AddColorStop (0, new Cairo.Color (0, 0, 0, 0));
									shadowGradient.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.5));
									cr.Source = shadowGradient;
									cr.FillPreserve ();
									
									if (selectedHunk != null && hunk.Left.Start == selectedHunk.Left.Start && hunk.Right.Start == selectedHunk.Right.Start ) {
										cr.Scale (12, 12);
										shadowGradient = new Cairo.RadialGradient (0.0, 0.0, .6,  0.0, 0.0, 1.0);
										shadowGradient.AddColorStop (0, new Cairo.Color (1, 1, 1, 0.5));
										shadowGradient.AddColorStop (1, new Cairo.Color (1, 1, 1, 0.2));
										cr.Source = shadowGradient;
										cr.Fill ();
									}
									cr.Restore ();
								}
							}
						}
					}
				}
				var result = base.OnExposeEvent (evnt);
				
				Gdk.GC gc = Style.DarkGC (State);
				evnt.Window.DrawLine (gc, Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
				evnt.Window.DrawLine (gc, Allocation.Right, Allocation.Top, Allocation.Right, Allocation.Bottom);
				
				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Y, Allocation.Right, Allocation.Y);
				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Bottom, Allocation.Right, Allocation.Bottom);
				
				return result;
			}
		}
		
		class OverviewRenderer : DrawingArea 
		{
			ComparisonWidget widget;
				
			public OverviewRenderer (ComparisonWidget widget)
			{
				this.widget = widget;
				widget.Vadjustment.ValueChanged += delegate {
					QueueDraw ();
				};
				WidthRequest = 50;
				
				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask;
				
				Show ();
			}
			
			public void MouseMove (double y)
			{
				var adj = widget.Vadjustment;
				double position = (y / Allocation.Height) * adj.Upper - (double)adj.PageSize / 2;
				position = Math.Max (0, Math.Min (position, adj.Upper - adj.PageSize));
				widget.vScrollBar.Adjustment.Value = position;
			}

			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				if (button != 0)
					MouseMove (evnt.Y);
				return base.OnMotionNotifyEvent (evnt);
			}
			
			uint button;
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				button |= evnt.Button;
				MouseMove (evnt.Y);
				return base.OnButtonPressEvent (evnt);
			}
			
			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				button &= ~evnt.Button;
				return base.OnButtonReleaseEvent (evnt);
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				var adj = widget.Vadjustment;
				
				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = 1;
					
					int count = 0;
					foreach (Hunk h in widget.Diff) {
						IncPos(h, ref count);
					}
					
					int start = 0;
					foreach (Hunk h in widget.Diff) {
						int size = 0;
						IncPos(h, ref size);
						
						cr.Rectangle (0.5, 0.5 + Allocation.Height * start / count, Allocation.Width, Math.Max (1, Allocation.Height * size / count));
						if (h.Same) {
							cr.Color = new Cairo.Color (1, 1, 1);
						} else if (h.Original ().Count == 0) {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.4);
						} else if (h.Changes (0).Count == 0) {
							cr.Color = new Cairo.Color (0.8, 0.4, 0.4);
						} else {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.8);
						}
						cr.Fill ();
						start += size;
					}
					
					cr.Rectangle (1,
					              (int)(Allocation.Height * adj.Value / adj.Upper),
					              Allocation.Width - 2,
					              (int)(Allocation.Height * ((double)adj.PageSize / adj.Upper)));
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();
					
					cr.Color = new Cairo.Color (0, 0, 0, 0.03);
					cr.Fill ();
					cr.Rectangle (0.5, 0.5, Allocation.Width - 1, Allocation.Height - 1);
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
				}
				
/*				gc.RgbFgColor = ColorGrey;
				e.Window.DrawRectangle(gc, false,
					1,
					(int)(Allocation.Height*scroller.Adjustment.Value/scroller.Adjustment.Upper) + 1,
					Allocation.Width-3,
					(int)(Allocation.Height*((double)scroller.Allocation.Height/scroller.Adjustment.Upper))-2);
*/
				return true;
			}
			
			void IncPos(Hunk h, ref int pos)
			{
				pos += h.MaxLines();
/*				if (sidebyside)
					pos += h.MaxLines();
				else if (h.Same)
					pos += h.Original().Count;
				else {
					pos += h.Original().Count;
					for (int i = 0; i < h.ChangedLists; i++)
						pos += h.Changes(i).Count;
				}*/
			}
		}	
	}
}

