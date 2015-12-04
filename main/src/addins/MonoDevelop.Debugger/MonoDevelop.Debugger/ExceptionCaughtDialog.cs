//
// ExceptionCaughtDialog.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2011 Xamarin Inc. (http://www.xamarin.com)
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
using System.IO;
using System.Linq;

using Gtk;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Debugger
{
	class ExceptionCaughtDialog : Dialog
	{
		static readonly Xwt.Drawing.Image WarningIconPixbuf = Xwt.Drawing.Image.FromResource ("exception-48.png");
		protected ObjectValueTreeView ExceptionValueTreeView { get; private set; }
		protected TreeView StackTraceTreeView { get; private set; }
		protected CheckButton OnlyShowMyCodeCheckbox { get; private set; }
		protected Label ExceptionMessageLabel { get; private set; }
		protected Label ExceptionTypeLabel { get; private set; }
		readonly ExceptionCaughtMessage message;
		readonly ExceptionInfo exception;
		ExceptionInfo selected;
		bool destroyed;

		protected enum ModelColumn {
			StackFrame,
			Markup,
			IsUserCode
		}

		public ExceptionCaughtDialog (ExceptionInfo ex, ExceptionCaughtMessage msg)
		{
			selected = exception = ex;
			message = msg;

			Build ();
			UpdateDisplay ();

			exception.Changed += ExceptionChanged;
		}

		Widget CreateExceptionInfoHeader ()
		{
			ExceptionMessageLabel = new Label { UseMarkup = true, Selectable = true, Wrap = true, WidthRequest = 500, Xalign = 0.0f, Yalign = 0.0f };
			ExceptionTypeLabel = new Label { UseMarkup = true, Xalign = 0.0f };

			ExceptionMessageLabel.Show ();
			ExceptionTypeLabel.Show ();

			var alignment = new Alignment (0.0f, 0.0f, 0.0f, 0.0f);
			alignment.Child = ExceptionMessageLabel;
			alignment.BorderWidth = 6;
			alignment.Show ();

			var frame = new InfoFrame (alignment);
			frame.Show ();

			var vbox = new VBox (false, 12);
			vbox.PackStart (ExceptionTypeLabel, false, true, 0);
			vbox.PackStart (frame, true, true, 0);
			vbox.Show ();

			return vbox;
		}

		Widget CreateExceptionHeader ()
		{
			var icon = new ImageView (WarningIconPixbuf);
			icon.Show ();

			var hbox = new HBox (false, 12) { BorderWidth = 12 };
			hbox.PackStart (icon, false, true, 0);
			hbox.PackStart (CreateExceptionInfoHeader (), true, true, 0);
			hbox.Show ();

			return hbox;
		}

		Widget CreateExceptionValueTreeView ()
		{
			ExceptionValueTreeView = new ObjectValueTreeView ();
			ExceptionValueTreeView.Frame = DebuggingService.CurrentFrame;
			ExceptionValueTreeView.ModifyBase (StateType.Normal, new Gdk.Color (223, 228, 235));
			ExceptionValueTreeView.AllowPopupMenu = false;
			ExceptionValueTreeView.AllowExpanding = true;
			ExceptionValueTreeView.AllowPinning = false;
			ExceptionValueTreeView.AllowEditing = false;
			ExceptionValueTreeView.AllowAdding = false;
			ExceptionValueTreeView.RulesHint = false;

			ExceptionValueTreeView.Selection.Changed += ExceptionValueSelectionChanged;
			ExceptionValueTreeView.Show ();

			var scrolled = new ScrolledWindow { HeightRequest = 180, HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };

			scrolled.ShadowType = ShadowType.None;
			scrolled.Add (ExceptionValueTreeView);
			scrolled.Show ();

			return scrolled;
		}

		static void StackFrameLayout (CellLayout layout, CellRenderer cr, TreeModel model, TreeIter iter)
		{
			var frame = (ExceptionStackFrame) model.GetValue (iter, (int) ModelColumn.StackFrame);
			var renderer = (StackFrameCellRenderer) cr;

			renderer.Markup = (string) model.GetValue (iter, (int) ModelColumn.Markup);
			renderer.Frame = frame;

			if (frame == null) {
				renderer.IsUserCode = false;
				return;
			}

			renderer.IsUserCode = (bool) model.GetValue (iter, (int) ModelColumn.IsUserCode);
		}

		Widget CreateStackTraceTreeView ()
		{
			var store = new ListStore (typeof (ExceptionStackFrame), typeof (string), typeof (bool));
			StackTraceTreeView = new TreeView (store);
			StackTraceTreeView.FixedHeightMode = false;
			StackTraceTreeView.HeadersVisible = false;
			StackTraceTreeView.ShowExpanders = false;
			StackTraceTreeView.RulesHint = true;
			StackTraceTreeView.Show ();

			var renderer = new StackFrameCellRenderer (StackTraceTreeView.PangoContext);
			renderer.Width = DefaultWidth;

			StackTraceTreeView.AppendColumn ("", renderer, (CellLayoutDataFunc) StackFrameLayout);

			StackTraceTreeView.SizeAllocated += (o, args) => renderer.Width = args.Allocation.Width;
			StackTraceTreeView.RowActivated += StackFrameActivated;

			var scrolled = new ScrolledWindow { HeightRequest = 180, HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };
			scrolled.ShadowType = ShadowType.None;
			scrolled.Add (StackTraceTreeView);
			scrolled.Show ();

			return scrolled;
		}

		Widget CreateButtonBox ()
		{
			var buttons = new HButtonBox { Layout = ButtonBoxStyle.End, Spacing = 12 };

			var copy = new Button (Stock.Copy);
			copy.Clicked += CopyClicked;
			copy.Show ();

			buttons.PackStart (copy, false, true, 0);

			var close = new Button (Stock.Close);
			close.Activated += CloseClicked;
			close.Clicked += CloseClicked;
			close.Show ();

			buttons.PackStart (close, false, true, 0);

			buttons.Show ();

			return buttons;
		}

		static Widget CreateSeparator ()
		{
			var separator = new HSeparator ();
			separator.Show ();
			return separator;
		}

		void Build ()
		{
			Title = GettextCatalog.GetString ("Exception Caught");
			DefaultHeight = 500;
			DefaultWidth = 600;
			VBox.Spacing = 0;

			VBox.PackStart (CreateExceptionHeader (), false, true, 0);

			var paned = new VPaned ();
			paned.Add1 (CreateExceptionValueTreeView ());
			paned.Add2 (CreateStackTraceTreeView ());
			paned.Show ();

			var vbox = new VBox (false, 0);
			vbox.PackStart (CreateSeparator (), false, true, 0);
			vbox.PackStart (paned, true, true, 0);
			vbox.PackStart (CreateSeparator (), false, true, 0);
			vbox.Show ();

			VBox.PackStart (vbox, true, true, 0);

			var actionArea = new HBox (false, 12) { BorderWidth = 6 };

			OnlyShowMyCodeCheckbox = new CheckButton (GettextCatalog.GetString ("_Only show my code."));
			OnlyShowMyCodeCheckbox.Toggled += OnlyShowMyCodeToggled;
			OnlyShowMyCodeCheckbox.Show ();
			OnlyShowMyCodeCheckbox.Active = DebuggingService.GetUserOptions ().ProjectAssembliesOnly;

			var alignment = new Alignment (0.0f, 0.5f, 0.0f, 0.0f) { Child = OnlyShowMyCodeCheckbox };
			alignment.Show ();

			actionArea.PackStart (alignment, true, true, 0);
			actionArea.PackStart (CreateButtonBox (), false, true, 0);
			actionArea.Show ();

			VBox.PackStart (actionArea, false, true, 0);
			ActionArea.Hide ();
		}

		bool TryGetExceptionInfo (TreePath path, out ExceptionInfo ex)
		{
			var model = (TreeStore) ExceptionValueTreeView.Model;
			TreeIter iter, parent;

			ex = exception;

			if (!model.GetIter (out iter, path))
				return false;

			var value = (ObjectValue) model.GetValue (iter, ObjectValueTreeView.ObjectColumn);
			if (value.Name != "InnerException")
				return false;

			int depth = 0;
			while (model.IterParent (out parent, iter)) {
				iter = parent;
				depth++;
			}

			while (ex != null) {
				if (depth == 0)
					return true;

				ex = ex.InnerException;
				depth--;
			}

			return false;
		}

		void ExceptionValueSelectionChanged (object sender, EventArgs e)
		{
			var selectedRows = ExceptionValueTreeView.Selection.GetSelectedRows ();
			ExceptionInfo ex;

			if (selectedRows.Length > 0 && TryGetExceptionInfo (selectedRows[0], out ex)) {
				ShowStackTrace (ex);
				selected = ex;
			} else if (selected != exception) {
				ShowStackTrace (exception);
				selected = exception;
			}
		}

		void StackFrameActivated (object o, RowActivatedArgs args)
		{
			var model = StackTraceTreeView.Model;
			TreeIter iter;

			if (!model.GetIter (out iter, args.Path))
				return;

			var frame = (ExceptionStackFrame) model.GetValue (iter, (int) ModelColumn.StackFrame);

			if (frame != null && !string.IsNullOrEmpty (frame.File) && File.Exists (frame.File)) {
				try {
					IdeApp.Workbench.OpenDocument (frame.File, null, frame.Line, frame.Column, MonoDevelop.Ide.Gui.OpenDocumentOptions.Debugger);
				} catch (FileNotFoundException) {
				}
			}
		}

		static bool IsUserCode (ExceptionStackFrame frame)
		{
			if (frame == null || string.IsNullOrEmpty (frame.File))
				return false;

			return IdeApp.Workspace.GetProjectsContainingFile (frame.File).Any ();
		}

		void ShowStackTrace (ExceptionInfo ex)
		{
			var model = (ListStore) StackTraceTreeView.Model;
			bool external = false;

			model.Clear ();

			foreach (var frame in ex.StackTrace) {
				bool isUserCode = IsUserCode (frame);

				if (OnlyShowMyCodeCheckbox.Active && !isUserCode) {
					if (!external) {
						var str = GettextCatalog.GetString ("<b>[External Code]</b>");
						model.AppendValues (null, str, false);
						external = true;
					}

					continue;
				}

				model.AppendValues (frame, null, isUserCode);
				external = false;
			}

			if (ex.StackIsEvaluating) {
				var str = GettextCatalog.GetString ("Loading...");
				model.AppendValues (null, str, false);
			}
		}

		void UpdateDisplay ()
		{
			if (destroyed)
				return;

			ExceptionValueTreeView.ClearValues ();

			ExceptionTypeLabel.Markup = GettextCatalog.GetString ("A <b>{0}</b> was thrown.", exception.Type);
			ExceptionMessageLabel.Markup = "<small>" + (exception.Message ?? string.Empty) + "</small>";

			if (!exception.IsEvaluating && exception.Instance != null) {
				ExceptionValueTreeView.AddValue (exception.Instance);
				ExceptionValueTreeView.ExpandRow (new TreePath ("0"), false);
			}

			ShowStackTrace (exception);
		}

		void ExceptionChanged (object sender, EventArgs e)
		{
			Application.Invoke (delegate {
				UpdateDisplay ();
			});
		}

		void OnlyShowMyCodeToggled (object sender, EventArgs e)
		{
			ShowStackTrace (selected);
		}

		void CloseClicked (object sender, EventArgs e)
		{
			message.Close ();
		}

		void CopyClicked (object sender, EventArgs e)
		{
			var text = exception.ToString ();

			var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = text;

			var primary = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			primary.Text = text;
		}

		protected override bool OnDeleteEvent (Gdk.Event evnt)
		{
			message.Close ();
			return true;
		}

		protected override void OnDestroyed ()
		{
			destroyed = true;
			exception.Changed -= ExceptionChanged;
			base.OnDestroyed ();
		}
	}

	class StackFrameCellRenderer : CellRenderer
	{
		static readonly Pango.FontDescription LineNumberFont = FontService.MonospaceFont.CopyModified (0.9d);
		const int RoundedRectangleRadius = 2;
		const int RoundedRectangleHeight = 14;
		const int RoundedRectangleWidth = 28;
		const int Padding = 6;

		public readonly Pango.Context Context;
		public ExceptionStackFrame Frame;
		public bool IsUserCode;
		public string Markup;

		public StackFrameCellRenderer (Pango.Context ctx)
		{
			Context = ctx;
		}

		int MaxMarkupWidth {
			get {
				if (Width < 0)
					return Width;

				return Width - (Padding + RoundedRectangleWidth + Padding + Padding + Padding);
			}
		}

		public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			using (var layout = new Pango.Layout (Context)) {
				Pango.Rectangle ink, logical;

				layout.Width = (int) (MaxMarkupWidth * Pango.Scale.PangoScale);
				layout.SetMarkup (GetMarkup (false));

				layout.GetPixelExtents (out ink, out logical);

				width = Padding + RoundedRectangleWidth + Padding + Padding + logical.Width + Padding;
				height = Padding + Math.Max (RoundedRectangleHeight, logical.Height) + Padding;

				x_offset = 0;
				y_offset = 0;
			}
		}

		void RenderLineNumberIcon (Widget widget, Cairo.Context cr, Gdk.Rectangle cell_area, int markupHeight, int yOffset)
		{
			if (Frame == null)
				return;

			cr.Save ();

			#if CENTER_ROUNDED_RECTANGLE
			cr.Translate (cell_area.X + Padding, (cell_area.Y + (cell_area.Height - RoundedRectangleHeight) / 2.0));
			#else
			cr.Translate (cell_area.X + Padding, cell_area.Y + Padding + yOffset);
			#endif

			cr.Antialias = Cairo.Antialias.Subpixel;

			cr.RoundedRectangle (0.0, 0.0, RoundedRectangleWidth, RoundedRectangleHeight, RoundedRectangleRadius);
			cr.Clip ();

			if (IsUserCode)
				cr.SetSourceRGBA (0.90, 0.60, 0.87, 1.0); // 230, 152, 223
			else
				cr.SetSourceRGBA (0.77, 0.77, 0.77, 1.0); // 197, 197, 197

			cr.RoundedRectangle (0.0, 0.0, RoundedRectangleWidth, RoundedRectangleHeight, RoundedRectangleRadius);
			cr.Fill ();

			cr.SetSourceRGBA (0.0, 0.0, 0.0, 0.11);
			cr.RoundedRectangle (0.0, 0.0, RoundedRectangleWidth, RoundedRectangleHeight, RoundedRectangleRadius);
			cr.LineWidth = 2;
			cr.Stroke ();

			var lineNumber = !string.IsNullOrEmpty (Frame.File) ? Frame.Line : -1;

			using (var layout = PangoUtil.CreateLayout (widget, lineNumber != -1 ? lineNumber.ToString () : "???")) {
				layout.Alignment = Pango.Alignment.Left;
				layout.FontDescription = LineNumberFont;

				int width, height;
				layout.GetPixelSize (out width, out height);

				double y_offset = (RoundedRectangleHeight - height) / 2.0;
				double x_offset = (RoundedRectangleWidth - width) / 2.0;

				// render the text shadow
				cr.Save ();
				cr.SetSourceRGBA (0.0, 0.0, 0.0, 0.34);
				cr.Translate (x_offset, y_offset + 1);
				cr.ShowLayout (layout);
				cr.Restore ();

				cr.SetSourceRGBA (1.0, 1.0, 1.0, 1.0);
				cr.Translate (x_offset, y_offset);
				cr.ShowLayout (layout);
			}

			cr.Restore ();
		}

		string GetMarkup (bool selected)
		{
			if (Markup != null)
				return Markup;

			var markup = string.Format ("<b>{0}</b>", GLib.Markup.EscapeText (Frame.DisplayText));

			if (selected)
				markup = "<span foreground='#FFFFFF'>" + markup + "</span>";

			if (!string.IsNullOrEmpty (Frame.File)) {
				markup += string.Format ("\n<span size='smaller' foreground='{0}'>{1}", selected ? "#FFFFFF" : "#777777", GLib.Markup.EscapeText (Frame.File));
				if (Frame.Line > 0) {
					markup += ":" + Frame.Line;
					if (Frame.Column > 0)
						markup += "," + Frame.Column;
				}
				markup += "</span>";
			}

			return markup;
		}

		protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			using (var cr = Gdk.CairoHelper.Create (window)) {
				using (var layout = new Pango.Layout (Context)) {
					Pango.Rectangle ink, logical;

					layout.Width = (int) (MaxMarkupWidth * Pango.Scale.PangoScale);
					layout.SetMarkup (GetMarkup ((flags & CellRendererState.Selected) != 0));

					layout.GetPixelExtents (out ink, out logical);

					RenderLineNumberIcon (widget, cr, cell_area, logical.Height, ink.Y);

					cr.Rectangle (expose_area.X, expose_area.Y, expose_area.Width, expose_area.Height);
					cr.Clip ();

					cr.Translate (cell_area.X + Padding + RoundedRectangleWidth + Padding + Padding, cell_area.Y + Padding);
					cr.ShowLayout (layout);
				}
			}
		}
	}

	class ExceptionCaughtMessage : IDisposable
	{
		ExceptionCaughtMiniButton miniButton;
		ExceptionCaughtDialog dialog;
		ExceptionCaughtButton button;
		readonly ExceptionInfo ex;

		public ExceptionCaughtMessage (ExceptionInfo val, FilePath file, int line, int col)
		{
			File = file;
			Line = line;
			ex = val;
		}

		public FilePath File {
			get; private set;
		}

		public int Line {
			get; set;
		}

		public bool IsMinimized {
			get { return miniButton != null; }
		}

		public void ShowDialog ()
		{
			if (dialog == null) {
				using (dialog = new ExceptionCaughtDialog (ex, this))
					MessageService.ShowCustomDialog (dialog, IdeApp.Workbench.RootWindow);
				dialog = null;
			}
		}

		public void ShowButton ()
		{
			if (dialog != null) {
				dialog.Destroy ();
				dialog = null;
			}
			if (button == null) {
				button = new ExceptionCaughtButton (ex, this, File, Line);
				TextEditorService.RegisterExtension (button);
				button.ScrollToView ();
			}
			if (miniButton != null) {
				miniButton.Dispose ();
				miniButton = null;
			}
		}

		public void ShowMiniButton ()
		{
			if (dialog != null) {
				dialog.Destroy ();
				dialog = null;
			}
			if (button != null) {
				button.Dispose ();
				button = null;
			}
			if (miniButton == null) {
				miniButton = new ExceptionCaughtMiniButton (this, File, Line);
				TextEditorService.RegisterExtension (miniButton);
				miniButton.ScrollToView ();
			}
		}

		public void Dispose ()
		{
			if (dialog != null) {
				dialog.Destroy ();
				dialog = null;
			}
			if (button != null) {
				button.Dispose ();
				button = null;
			}
			if (miniButton != null) {
				miniButton.Dispose ();
				miniButton = null;
			}
			if (Closed != null)
				Closed (this, EventArgs.Empty);
		}

		public void Close ()
		{
			ShowButton ();
		}

		public event EventHandler Closed;
	}

	class ExceptionCaughtButton: TopLevelWidgetExtension
	{
		readonly Xwt.Drawing.Image closeSelOverImage;
		readonly Xwt.Drawing.Image closeSelImage;
		readonly ExceptionCaughtMessage dlg;
		readonly ExceptionInfo exception;
		Label messageLabel;

		public ExceptionCaughtButton (ExceptionInfo val, ExceptionCaughtMessage dlg, FilePath file, int line)
		{
			this.exception = val;
			this.dlg = dlg;
			OffsetX = 6;
			File = file;
			Line = line;
			closeSelImage = ImageService.GetIcon ("md-popup-close", IconSize.Menu);
			closeSelOverImage = ImageService.GetIcon ("md-popup-close-hover", IconSize.Menu);
		}

		protected override void OnLineChanged ()
		{
			base.OnLineChanged ();
			dlg.Line = Line;
		}

		public override Widget CreateWidget ()
		{
			var icon = Xwt.Drawing.Image.FromResource ("lightning-16.png");
			var image = new Xwt.ImageView (icon).ToGtkWidget ();

			var box = new HBox (false, 6);
			var vb = new VBox ();
			vb.PackStart (image, false, false, 0);
			box.PackStart (vb, false, false, 0);
			vb = new VBox (false, 6);
			vb.PackStart (new Label {
				Markup = GettextCatalog.GetString ("<b>{0}</b> has been thrown", exception.Type),
				Xalign = 0
			});
			messageLabel = new Label {
				Xalign = 0,
				NoShowAll = true
			};
			vb.PackStart (messageLabel);

			var detailsBtn = new Xwt.LinkLabel (GettextCatalog.GetString ("Show Details"));
			var hh = new HBox ();
			detailsBtn.NavigateToUrl += (o,e) => dlg.ShowDialog ();
			hh.PackStart (detailsBtn.ToGtkWidget (), false, false, 0);
			vb.PackStart (hh, false, false, 0);

			box.PackStart (vb, true, true, 0);

			vb = new VBox ();
			var closeButton = new ImageButton {
				InactiveImage = closeSelImage,
				Image = closeSelOverImage
			};
			closeButton.Clicked += delegate {
				dlg.ShowMiniButton ();
			};
			vb.PackStart (closeButton, false, false, 0);
			box.PackStart (vb, false, false, 0);

			exception.Changed += delegate {
				Application.Invoke (delegate {
					LoadData ();
				});
			};
			LoadData ();

			var eb = new PopoverWidget ();
			eb.ShowArrow = true;
			eb.EnableAnimation = true;
			eb.PopupPosition = PopupPosition.Left;
			eb.ContentBox.Add (box);
			eb.ShowAll ();
			return eb;
		}

		void LoadData ()
		{
			if (!string.IsNullOrEmpty (exception.Message)) {
				messageLabel.Show ();
				messageLabel.Text = exception.Message;
				if (messageLabel.SizeRequest ().Width > 400) {
					messageLabel.WidthRequest = 400;
					messageLabel.Wrap = true;
				}
			} else {
				messageLabel.Hide ();
			}
		}
	}

	class ExceptionCaughtMiniButton: TopLevelWidgetExtension
	{
		readonly ExceptionCaughtMessage dlg;

		public ExceptionCaughtMiniButton (ExceptionCaughtMessage dlg, FilePath file, int line)
		{
			this.dlg = dlg;
			OffsetX = 6;
			File = file;
			Line = line;
		}

		protected override void OnLineChanged ()
		{
			base.OnLineChanged ();
			dlg.Line = Line;
		}

		public override Widget CreateWidget ()
		{
			var box = new EventBox ();
			box.VisibleWindow = false;
			var icon = Xwt.Drawing.Image.FromResource ("lightning-16.png");
			box.Add (new Xwt.ImageView (icon).ToGtkWidget ());
			box.ButtonPressEvent += (o,e) => dlg.ShowButton ();
			var eb = new PopoverWidget ();
			eb.Theme.Padding = 2;
			eb.ShowArrow = true;
			eb.EnableAnimation = true;
			eb.PopupPosition = PopupPosition.Left;
			eb.ContentBox.Add (box);
			eb.ShowAll ();
			return eb;
		}
	}

	class ExceptionCaughtTextEditorExtension: TextEditorExtension
	{
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape && DebuggingService.ExceptionCaughtMessage != null &&
			    !DebuggingService.ExceptionCaughtMessage.IsMinimized &&
			    DebuggingService.ExceptionCaughtMessage.File.CanonicalPath == Document.FileName.CanonicalPath) {

				DebuggingService.ExceptionCaughtMessage.ShowMiniButton ();
				return true;
			}

			return base.KeyPress (key, keyChar, modifier);
		}
	}
}

