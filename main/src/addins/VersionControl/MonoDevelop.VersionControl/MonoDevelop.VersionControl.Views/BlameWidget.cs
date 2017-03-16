//
// BlameWidget.cs
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
using MonoDevelop.Ide;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.VersionControl.Views
{
	public enum BlameCommands {
		CopyRevision,
		ShowDiff,
		ShowLog,
		ShowBlameBefore
	}
	
	class BlameWidget : Bin
	{
		Revision revision;
		Adjustment vAdjustment;
		Gtk.VScrollbar vScrollBar;
		
		Adjustment hAdjustment;
		Gtk.HScrollbar hScrollBar;
		
		BlameRenderer overview;
		
		MonoTextEditor editor;
		List<ContainerChild> children = new List<ContainerChild> ();
		
		public Adjustment Vadjustment {
			get { return this.vAdjustment; }
		}

		public Adjustment Hadjustment {
			get { return this.hAdjustment; }
		}
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (ContainerChild info in children) {
					if (info.Child == w)
						return info;
				}
				return null;
			}
		}
		
		public MonoTextEditor Editor {
			get {
				return this.editor;
			}
		}
		VersionControlDocumentInfo info;
		
		public Ide.Gui.Document Document {
			get {
				return info.Document.ParentDocument;
			}
		}
		public VersionControlItem VersionControlItem {
			get {
				return info.Item;
			}
		}

		public BlameWidget (VersionControlDocumentInfo info)
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			this.info = info;
			var sourceEditor = info.Document.GetContent<MonoDevelop.SourceEditor.SourceEditorView> ();
			
			vAdjustment = new Adjustment (
				sourceEditor.TextEditor.VAdjustment.Value, 
				sourceEditor.TextEditor.VAdjustment.Lower, 
				sourceEditor.TextEditor.VAdjustment.Upper, 
				sourceEditor.TextEditor.VAdjustment.StepIncrement, 
				sourceEditor.TextEditor.VAdjustment.PageIncrement, 
				sourceEditor.TextEditor.VAdjustment.PageSize);
			vAdjustment.Changed += HandleAdjustmentChanged;
			
			vScrollBar = new VScrollbar (vAdjustment);
			AddChild (vScrollBar);
			
			hAdjustment = new Adjustment (
				sourceEditor.TextEditor.HAdjustment.Value, 
				sourceEditor.TextEditor.HAdjustment.Lower, 
				sourceEditor.TextEditor.HAdjustment.Upper, 
				sourceEditor.TextEditor.HAdjustment.StepIncrement, 
				sourceEditor.TextEditor.HAdjustment.PageIncrement, 
				sourceEditor.TextEditor.HAdjustment.PageSize);
			hAdjustment.Changed += HandleAdjustmentChanged;

			hScrollBar = new HScrollbar (hAdjustment);
			AddChild (hScrollBar);

			var doc = new TextDocument (sourceEditor.TextEditor.Document.Text) {
				IsReadOnly = true,
				MimeType = sourceEditor.TextEditor.Document.MimeType,
			};
			editor = new MonoTextEditor (doc, sourceEditor.TextEditor.Options);
			AddChild (editor);
			editor.SetScrollAdjustments (hAdjustment, vAdjustment);
			
			overview = new BlameRenderer (this);
			AddChild (overview);
			
			this.DoubleBuffered = true;
			editor.Painted += HandleEditorExposeEvent;
			editor.EditorOptionsChanged += delegate {
				overview.OptionsChanged ();
			};
			editor.Caret.PositionChanged += ComparisonWidget.CaretPositionChanged;
			editor.FocusInEvent += ComparisonWidget.EditorFocusIn;
			editor.Document.Folded += delegate {
				QueueDraw ();
			};
			editor.Document.FoldTreeUpdated += delegate {
				QueueDraw ();
			};
			editor.DoPopupMenu = ShowPopup;
			Show ();
		}

		internal void Reset ()
		{
			revision = null;
			overview.UpdateAnnotations ();
		}
		
		void ShowPopup (EventButton evt)
		{
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/VersionControl/BlameView/ContextMenu");
			Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
			menu.Destroyed += delegate {
				this.QueueDraw ();
			};
			
			if (evt != null) {
				GtkWorkarounds.ShowContextMenu (menu, this, evt);
			} else {
				var pt = editor.LocationToPoint (editor.Caret.Location);
				GtkWorkarounds.ShowContextMenu (menu, editor, new Gdk.Rectangle (pt.X, pt.Y, 1, (int)editor.LineHeight));
			}
		}
		
		void HandleAdjustmentChanged (object sender, EventArgs e)
		{
			Adjustment adjustment = (Adjustment)sender;
			Scrollbar scrollbar = adjustment == vAdjustment ? (Scrollbar)vScrollBar : hScrollBar;
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
		
		void AddChild (Gtk.Widget child)
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
			hScrollBar.Destroy ();
			hAdjustment.Destroy ();

			vScrollBar.Destroy ();
			vAdjustment.Destroy ();

			editor.Destroy ();
			overview.Destroy ();
		}
		 
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			int vwidth = vScrollBar.Visible ? vScrollBar.Requisition.Width : 0;
			int hheight = hScrollBar.Visible ? hScrollBar.Requisition.Height : 0; 
			Rectangle childRectangle = new Rectangle (allocation.X + 1, allocation.Y + 1, allocation.Width - vwidth - 1, allocation.Height - hheight - 1);
			
			if (vScrollBar.Visible) {
				int right = childRectangle.Right;
				int vChildTopHeight = -1;
				int v = hScrollBar.Visible ? hScrollBar.Requisition.Height : 0;
				vScrollBar.SizeAllocate (new Rectangle (right, childRectangle.Y + vChildTopHeight, vwidth, Allocation.Height - v - vChildTopHeight - 1));
				vScrollBar.Value = System.Math.Max (System.Math.Min (vAdjustment.Upper - vAdjustment.PageSize, vScrollBar.Value), vAdjustment.Lower);
			}
			int overviewWidth = overview.WidthRequest;
			overview.SizeAllocate (new Rectangle (childRectangle.Right - overviewWidth, childRectangle.Top, overviewWidth, childRectangle.Height));
			editor.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Top, childRectangle.Width - overviewWidth, childRectangle.Height));
		
			if (hScrollBar.Visible) {
				hScrollBar.SizeAllocate (new Rectangle (childRectangle.X, childRectangle.Y + childRectangle.Height, childRectangle.Width, hheight));
				hScrollBar.Value = System.Math.Max (System.Math.Min (hAdjustment.Upper - hAdjustment.PageSize, hScrollBar.Value), hAdjustment.Lower);
			}
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			var alloc = Allocation;
			double dx, dy;
			evnt.GetPageScrollPixelDeltas (alloc.Width, alloc.Height, out dx, out dy);
			
			if (dx != 0.0 && hScrollBar.Visible)
				hAdjustment.AddValueClamped (dx);
			
			if (dy != 0.0 && vScrollBar.Visible)
				vAdjustment.AddValueClamped (dy);
			
			return (dx != 0.0 || dy != 0.0) || base.OnScrollEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}
		
		void HandleEditorExposeEvent (object o, PaintEventArgs args)
		{
			var cr = args.Context;
			int startLine = Editor.YToLine (Editor.VAdjustment.Value);
			double startY = Editor.LineToY (startLine);
			double curY = startY - Editor.VAdjustment.Value;
			int line = startLine;
			
			while (curY < editor.Allocation.Bottom && line <= editor.LineCount) {
				Annotation ann = line <= overview.annotations.Count ? overview.annotations[line - 1] : null;
				double curStart = curY;
				do {
					JumpOverFoldings (ref line);
					line++;
				} while (curY < editor.Allocation.Bottom && line <= overview.annotations.Count && ann != null && overview.annotations[line - 1] != null && overview.annotations[line - 1].Revision == ann.Revision);
				curY = Editor.LineToY (line) - Editor.VAdjustment.Value;
				
				if (overview.highlightAnnotation != null) {
					if (ann != null && overview.highlightAnnotation.Revision == ann.Revision && curStart <= overview.highlightPositon && overview.highlightPositon < curY) {
					} else {
						cr.Rectangle (Editor.TextViewMargin.XOffset, curStart + cr.LineWidth, Editor.Allocation.Width - Editor.TextViewMargin.XOffset, curY - curStart - cr.LineWidth);
						cr.SetSourceColor (Styles.BlameView.RangeHazeColor.ToCairoColor ());
						cr.Fill ();
						
					}
				}
				if (ann != null) {
					cr.MoveTo (Editor.TextViewMargin.XOffset, curY + 0.5);
					cr.LineTo (Editor.Allocation.Width, curY + 0.5);
					
					cr.SetSourceColor (Styles.BlameView.RangeSplitterColor.ToCairoColor ());
					cr.Stroke ();
				}
			}
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			Gdk.GC gc = Style.DarkGC (State);
			evnt.Window.DrawLine (gc, Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
			evnt.Window.DrawLine (gc, Allocation.Right, Allocation.Top, Allocation.Right, Allocation.Bottom);
			
			evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Y, Allocation.Right, Allocation.Y);
			evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Bottom, Allocation.Right, Allocation.Bottom);
			
			
			return base.OnExposeEvent (evnt);
		}
		
		void JumpOverFoldings (ref int line)
		{
			int lastFold = -1;
			foreach (FoldSegment fs in Editor.Document.GetStartFoldings (line).Where (fs => fs.IsCollapsed)) {
				lastFold = System.Math.Max (fs.EndOffset, lastFold);
			}
			if (lastFold > 0) 
				line = Editor.Document.OffsetToLineNumber (lastFold);
		}

		class BlameRenderer : DrawingArea 
		{
			BlameWidget widget;
			internal List<Annotation> annotations;
			Pango.Layout layout;

			double dragPosition = -1;

			TextDocument document;

			public BlameRenderer (BlameWidget widget)
			{
				this.widget = widget;
				widget.info.Updated += OnWidgetChanged;
				annotations = new List<Annotation> ();
				UpdateAnnotations ();
	//			widget.Document.Saved += UpdateAnnotations;
				document = widget.Editor.Document;
				widget.vScrollBar.ValueChanged += OnWidgetChanged;
				
				layout = new Pango.Layout (PangoContext);
				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
				OptionsChanged ();
				Show ();
			}

			void OnWidgetChanged (object sender, EventArgs e)
			{
				QueueDraw ();
			}
			
			public void OptionsChanged ()
			{
				layout.FontDescription = FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11);
				UpdateWidth ();
			}
			
			protected override void OnDestroyed ()
			{
				base.OnDestroyed ();
//				widget.Document.Saved -= UpdateAnnotations;
				if (document != null) { 
					document = null;
				}
				if (layout != null) {
					layout.Dispose ();
					layout = null;
				}
				if (widget != null && widget.info != null) {
					widget.info.Updated -= OnWidgetChanged;
					widget.vScrollBar.ValueChanged -= OnWidgetChanged;
					widget = null;
				}
			}
			
			internal double highlightPositon;
			internal Annotation highlightAnnotation, menuAnnotation;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				TooltipText = null;
				if (dragPosition >= 0) {
					int x, y;
					widget.GetPointer (out x, out y);
					int newWidthRequest = widget.Allocation.Width - x;
					newWidthRequest = Math.Min (widget.Allocation.Width - (int)widget.Editor.TextViewMargin.XOffset, Math.Max (leftSpacer, newWidthRequest));
					
					WidthRequest = newWidthRequest;
					QueueResize ();
				}
				int startLine = widget.Editor.YToLine (widget.Editor.VAdjustment.Value + evnt.Y);
				var ann = startLine > 0 && startLine <= annotations.Count ? annotations[startLine - 1] : null;
				if (ann != null)
					TooltipText = GetCommitMessage (startLine - 1, true);

				highlightPositon = evnt.Y;
				if (highlightAnnotation != ann) {
					highlightAnnotation = ann;
					widget.QueueDraw ();
				}
				
				return base.OnMotionNotifyEvent (evnt);
			}
			
			
			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				highlightAnnotation = null;
				widget.QueueDraw ();
				return base.OnLeaveNotifyEvent (evnt);
			}
			
			uint grabTime;
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				if (evnt.TriggersContextMenu ()) {
					int startLine = widget.Editor.YToLine (widget.Editor.VAdjustment.Value + evnt.Y);
					menuAnnotation = startLine > 0 && startLine <= annotations.Count ? annotations[startLine - 1] : null;

					CommandEntrySet opset = new CommandEntrySet ();
					opset.AddItem (BlameCommands.ShowDiff);
					opset.AddItem (BlameCommands.ShowLog);
					opset.AddItem (BlameCommands.ShowBlameBefore);
					opset.AddItem (Command.Separator);
					opset.AddItem (BlameCommands.CopyRevision);
					IdeApp.CommandService.ShowContextMenu (this, evnt, opset, this);
					return true;
				} else {
					if (evnt.X < leftSpacer) {
						grabTime = evnt.Time;
						var status = Gdk.Pointer.Grab (this.GdkWindow, false, EventMask.PointerMotionHintMask | EventMask.Button1MotionMask | EventMask.ButtonReleaseMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask, null, null, grabTime);
						if (status == GrabStatus.Success) {
							dragPosition = evnt.X;
						}
					}
				}
				return base.OnButtonPressEvent (evnt);
			}
			
			[CommandHandler (BlameCommands.CopyRevision)]
			protected void OnCopyRevision ()
			{
				if (menuAnnotation == null)
					return;
				var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				clipboard.Text = menuAnnotation.Revision.ToString ();
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
				clipboard.Text = menuAnnotation.Revision.ToString ();
			}
		
			[CommandHandler (BlameCommands.ShowDiff)]
			protected void OnShowDiff ()
			{
				if (menuAnnotation == null)
					return;
				foreach (var view in widget.info.Document.ParentDocument.Views) {
					DiffView diffView = view.GetContent<DiffView> ();
					if (diffView != null) {
						view.Select ();
						if (menuAnnotation.Revision == null)
							return;
						var rev = widget.info.History.FirstOrDefault (h => h == menuAnnotation.Revision);
						if (rev == null)
							return;
						diffView.ComparisonWidget.SetRevision (diffView.ComparisonWidget.DiffEditor, rev.GetPrevious ());
						diffView.ComparisonWidget.SetRevision (diffView.ComparisonWidget.OriginalEditor, rev);
						break;
					}
				}
			}
		
			[CommandHandler (BlameCommands.ShowLog)]
			protected void OnShowLog ()
			{
				if (menuAnnotation == null)
					return;
				foreach (var view in widget.info.Document.ParentDocument.Views) {
					LogView logView = view.GetContent<LogView> ();
					if (logView != null) {
						view.Select ();
						if (menuAnnotation.Revision == null)
							return;
						var rev = widget.info.History.FirstOrDefault (h => h == menuAnnotation.Revision);
						if (rev == null)
							return;
						logView.LogWidget.SelectedRevision = rev;
						break;
					}
				}
			}

			[CommandHandler (BlameCommands.ShowBlameBefore)]
			protected void OnShowBlameBefore ()
			{
				var current = menuAnnotation?.Revision;
				Revision rev;

				if (current == null) {
					rev = widget.info.History.FirstOrDefault ();
				} else {
					rev = current?.GetPrevious ();
				}

				if (rev == null)
					return;
				
				widget.revision = rev;
				UpdateAnnotations ();
			}

			[CommandUpdateHandler (BlameCommands.ShowBlameBefore)]
			protected void OnUpdateShowBlameBefore (CommandInfo cinfo)
			{
				var current = menuAnnotation?.Revision;
				// If we have a working copy segment or we have a parent commit.
				cinfo.Enabled = current == null || current.GetPrevious () != null;
			}
			
			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				if (dragPosition >= 0) {
					Gdk.Pointer.Ungrab (grabTime);
					dragPosition = -1;
				}
				return base.OnButtonReleaseEvent (evnt);
			}
			DateTime minDate, maxDate;
			/// <summary>
			/// Reloads annotations for the current document
			/// </summary>
			internal void UpdateAnnotations ()
			{
				StatusBarContext ctx = IdeApp.Workbench.StatusBar.CreateContext ();
				ctx.AutoPulse = true;
				ctx.ShowMessage ("md-version-control", GettextCatalog.GetString ("Retrieving history"));

				ThreadPool.QueueUserWorkItem (delegate {
				try {
						annotations = new List<Annotation> (widget.VersionControlItem.Repository.GetAnnotations (widget.Document.FileName, widget.revision));
						
//						for (int i = 0; i < annotations.Count; i++) {
//							Annotation varname = annotations[i];
//							System.Console.WriteLine (i + ":" + varname);
//						}
						minDate = annotations.Min (a => a.Date);
						maxDate = annotations.Max (a => a.Date);
					} catch (Exception ex) {
						LoggingService.LogError ("Error retrieving history", ex);
					}
					
					Runtime.RunInMainThread (delegate {
						var location = widget.Editor.Caret.Location;
						var adj = widget.editor.VAdjustment.Value;
						if (widget.revision != null) {
							document.Text = widget.VersionControlItem.Repository.GetTextAtRevision (widget.Document.FileName, widget.revision);
						} else {
							document.Text = widget.Document.Editor.Text;
						}
						widget.editor.Caret.Location = location;
						widget.editor.VAdjustment.Value = adj;

						ctx.AutoPulse = false;
						ctx.Dispose ();
						UpdateWidth ();
						QueueDraw ();
					});
				});
			}
	
			/// <summary>
			/// Gets the commit message matching a given annotation index.
			/// </summary>
			internal string GetCommitMessage (int index, bool tooltip)
			{
				Annotation annotation = (index < annotations.Count)? annotations[index]: null;
				var history = widget.info.History;
				if (null != history && annotation != null) {
					foreach (Revision rev in history) {
						if (rev == annotation.Revision) {
							if (tooltip && annotation.HasEmail)
								return GettextCatalog.GetString ("Email: {0}{1}{2}", annotation.Email, Environment.NewLine, rev.Message);
							return rev.Message;
						}
					}
				}
				return null;
			}
			
			string TruncRevision (string revision)
			{
				return TruncRevision (revision, 8);
			}
			
			/// <summary>
			/// Truncates the revision. This is done by trying to find the shortest matching number.
			/// </summary>
			/// <returns>
			/// The shortest revision number (down to a minimum length of initialLength).
			/// </returns>
			/// <param name='revision'>
			/// The revision.
			/// </param>
			/// <param name='initalLength'>
			/// Inital length.
			/// </param> 
			string TruncRevision (string revision, int initalLength)
			{
				if (initalLength >= revision.Length)
					return revision;
				string truncated = revision.Substring (0, initalLength);
				var history = widget.info.History;
				if (history != null) {
					bool isMisleadingMatch = history.Select (r => r.ToString ()).Any (rev => rev != revision && rev.StartsWith (truncated, StringComparison.Ordinal));
					if (isMisleadingMatch)
						truncated = TruncRevision (revision, initalLength + 1);
				}
				return truncated;
			}
			
			void UpdateWidth ()
			{
				int tmpwidth, height, width = 120;
				int dateTimeLength = -1;
				foreach (Annotation note in annotations) {
					if (!String.IsNullOrEmpty (note.Author)) { 
						if (dateTimeLength < 0 && note.HasDate) {
							layout.SetText (note.Date.ToShortDateString ());
							layout.GetPixelSize (out dateTimeLength, out height);
						}
						layout.SetText (note.Author + TruncRevision (note.Text));
						layout.GetPixelSize (out tmpwidth, out height);
						width = Math.Max (width, tmpwidth);
					}
				}
				WidthRequest = dateTimeLength + width + 30 + leftSpacer + margin * 2;
				QueueResize ();
			}

			const int leftSpacer = 4;
			const int margin = 4;

			
			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = Math.Max (1.0, widget.Editor.Options.Zoom);
					
					cr.Rectangle (leftSpacer, 0, Allocation.Width, Allocation.Height);
					cr.SetSourceColor (Styles.BlameView.AnnotationBackgroundColor.ToCairoColor ());
					cr.Fill ();
					
					int startLine = widget.Editor.YToLine ((int)widget.Editor.VAdjustment.Value);
					double startY = widget.Editor.LineToY (startLine);
					while (startLine > 1 && startLine < annotations.Count && annotations[startLine - 1] != null && annotations[startLine] != null && annotations[startLine - 1].Revision == annotations[startLine].Revision) {
						startLine--;
						startY -= widget.Editor.GetLineHeight (widget.Editor.Document.GetLine (startLine));
					}
					double curY = startY - widget.Editor.VAdjustment.Value;
					int line = startLine;
					while (curY < Allocation.Bottom && line <= widget.Editor.LineCount) {
						double curStart = curY;
//						widget.JumpOverFoldings (ref line);
						int lineStart = line;
						int authorWidth = 0, revisionWidth = 0, dateWidth = 0, h = 16;
						Annotation ann = line <= annotations.Count ? annotations[line - 1] : null;
						if (ann != null) {
							do {
								widget.JumpOverFoldings (ref line);
								line++;
							} while (line <= annotations.Count && annotations[line - 1] != null && annotations[line - 1].Revision == ann.Revision);

							double nextY = widget.editor.LineToY (line) - widget.editor.VAdjustment.Value;
							if (highlightAnnotation != null && highlightAnnotation.Revision == ann.Revision && curStart <= highlightPositon && highlightPositon < nextY) {
								cr.Rectangle (leftSpacer, curStart + cr.LineWidth, Allocation.Width - leftSpacer, nextY - curStart - cr.LineWidth);
								cr.SetSourceColor (Styles.BlameView.AnnotationHighlightColor.ToCairoColor ());
								cr.Fill ();
							}

							// use a fixed size revision to get a approx. revision width
							layout.SetText ("88888888");
							layout.GetPixelSize (out revisionWidth, out h);
							layout.SetText (TruncRevision (ann.Text));

							const int dateRevisionSpacing = 16;

							using (var gc = new Gdk.GC (e.Window)) {
								gc.RgbFgColor = Styles.BlameView.AnnotationTextColor.ToGdkColor ();
								e.Window.DrawLayout (gc, Allocation.Width - revisionWidth - margin, (int)(curY + (widget.Editor.LineHeight - h) / 2), layout);

								if (ann.HasDate) {
									string dateTime = ann.Date.ToShortDateString ();
									// use a fixed size date to get a approx. date width
									layout.SetText (new DateTime (1999, 10, 10).ToShortDateString ());
									layout.GetPixelSize (out dateWidth, out h);
									layout.SetText (dateTime);

									e.Window.DrawLayout (gc, Allocation.Width - revisionWidth - margin - revisionWidth - dateRevisionSpacing, (int)(curY + (widget.Editor.LineHeight - h) / 2), layout);
								}
							}

							using (var authorLayout = MonoDevelop.Components.PangoUtil.CreateLayout (this)) {
								authorLayout.FontDescription = FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11);
								authorLayout.SetText (ann.Author);
								authorLayout.GetPixelSize (out authorWidth, out h);

								var maxWidth = Allocation.Width - revisionWidth - margin - revisionWidth - dateRevisionSpacing;
				/*				if (authorWidth > maxWidth) {
									int idx = ann.Author.IndexOf ('<');
									if (idx > 0)
										authorLayout.SetText (ann.Author.Substring (0, idx) + Environment.NewLine + ann.Author.Substring (idx));
									authorLayout.GetPixelSize (out authorWidth, out h);
								}*/

								cr.Save ();
								cr.Rectangle (0, 0, maxWidth, Allocation.Height); 
								cr.Clip ();
								cr.Translate (leftSpacer + margin, (int)(curY + (widget.Editor.LineHeight - h) / 2)); 
								cr.SetSourceColor (Styles.BlameView.AnnotationTextColor.ToCairoColor ());
								cr.ShowLayout (authorLayout);
								cr.ResetClip ();
								cr.Restore ();
							}

							curY = nextY;
						} else {
							curY += widget.Editor.GetLineHeight (line);
							line++;
							widget.JumpOverFoldings (ref line);
						}

						if (ann != null && line - lineStart > 1) {
							string msg = GetCommitMessage (lineStart, false);
							if (!string.IsNullOrEmpty (msg)) {
								msg = Revision.FormatMessage (msg);

								layout.SetText (msg);
								layout.Width = (int)(Allocation.Width * Pango.Scale.PangoScale);
								using (var gc = new Gdk.GC (e.Window)) {
									gc.RgbFgColor = Styles.BlameView.AnnotationSummaryTextColor.ToGdkColor ();
									gc.ClipRectangle = new Rectangle (0, (int)curStart, Allocation.Width, (int)(curY - curStart));
									e.Window.DrawLayout (gc, (int)(leftSpacer + margin), (int)(curStart + h), layout);
								}
							}
						}
						
						cr.Rectangle (0, curStart, leftSpacer, curY - curStart);
						
						if (ann != null && !string.IsNullOrEmpty (ann.Author)) {
							double a;
							
							if (ann != null && (maxDate - minDate).TotalHours > 0) {
								a = 1 - (ann.Date  - minDate).TotalHours / (maxDate - minDate).TotalHours;
							} else {
								a = 1;
							}
							var color = Styles.BlameView.AnnotationMarkColor;
							color.Light = 0.4 + a / 2;
							color.Saturation = 1 - a / 2;
							cr.SetSourceColor (color.ToCairoColor ());
						} else {
							cr.SetSourceColor ((ann != null ? Styles.BlameView.AnnotationMarkModifiedColor : Styles.BlameView.AnnotationBackgroundColor).ToCairoColor ());
						}
						cr.Fill ();

						if (ann != null) {
							cr.MoveTo (0, curY + 0.5);
							cr.LineTo (Allocation.Width, curY + 0.5);
							cr.SetSourceColor (Styles.BlameView.AnnotationSplitterColor.ToCairoColor ());
							cr.Stroke ();
						}
					}
				}
				return true;
			}
			
		}	
	}
}

