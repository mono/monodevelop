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

using Gtk;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Debugger
{
	class ExceptionCaughtDialog : Gtk.Dialog
	{
		static readonly Gdk.Pixbuf WarningIconPixbuf = Gdk.Pixbuf.LoadFromResource ("exception-icon.png");
		protected ObjectValueTreeView ExceptionValueTreeView { get; private set; }
		protected TreeView StackTraceTreeView { get; private set; }
		protected CheckButton OnlyShowMyCodeCheckbox { get; private set; }
		protected Label ExceptionMessageLabel { get; private set; }
		protected Label ExceptionTypeLabel { get; private set; }
		readonly ExceptionCaughtMessage message;
		readonly ExceptionInfo exception;
		bool destroyed;

		protected enum ModelColumn {
			StackFrame,
			Markup
		}

		public ExceptionCaughtDialog (ExceptionInfo ex, ExceptionCaughtMessage msg)
		{
			exception = ex;
			message = msg;

			Build ();
			UpdateDisplay ();

			exception.Changed += ExceptionChanged;
		}

		Widget CreateExceptionInfoHeader ()
		{
			ExceptionMessageLabel = new Label () { Selectable = true, Wrap = true, WidthRequest = 500, Xalign = 0.0f };
			ExceptionTypeLabel = new Label () { UseMarkup = true, Xalign = 0.0f };

			ExceptionMessageLabel.Show ();
			ExceptionTypeLabel.Show ();

			var frame = new InfoFrame (ExceptionMessageLabel);
			frame.Show ();

			var vbox = new VBox (false, 6);
			vbox.PackStart (ExceptionTypeLabel, false, true, 0);
			vbox.PackStart (frame, true, true, 0);
			vbox.Show ();

			return vbox;
		}

		Widget CreateExceptionHeader ()
		{
			var icon = new Image (WarningIconPixbuf);
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
			ExceptionValueTreeView.AllowExpanding = true;
			ExceptionValueTreeView.AllowPinning = false;
			ExceptionValueTreeView.AllowEditing = false;
			ExceptionValueTreeView.AllowAdding = false;

			// TODO: set the bg color to a greyish blue
			ExceptionValueTreeView.Show ();

			var scrolled = new ScrolledWindow () { HeightRequest = 128 };
			scrolled.ShadowType = ShadowType.In;
			scrolled.Add (ExceptionValueTreeView);
			scrolled.Show ();

			return scrolled;
		}

		Widget CreateStackTraceTreeView ()
		{
			var store = new TreeStore (typeof (ExceptionStackFrame), typeof (string));
			StackTraceTreeView = new TreeView (store);
			StackTraceTreeView.HeadersVisible = false;
			StackTraceTreeView.ShowExpanders = false;
			StackTraceTreeView.RulesHint = true;
			StackTraceTreeView.Show ();

			var crt = new CellRendererText ();
			crt.Ellipsize = Pango.EllipsizeMode.End;
			crt.WrapWidth = -1;

			StackTraceTreeView.AppendColumn ("", crt, "markup", (int) ModelColumn.Markup);

			StackTraceTreeView.SizeAllocated += (o, args) => crt.WrapWidth = args.Allocation.Width;
			StackTraceTreeView.RowActivated += StackFrameActivated;

			var scrolled = new ScrolledWindow () { HeightRequest = 128 };
			scrolled.ShadowType = ShadowType.In;
			scrolled.Add (StackTraceTreeView);
			scrolled.Show ();

			return scrolled;
		}

		Widget CreateButtonBox ()
		{
			var buttons = new HButtonBox () { Layout = ButtonBoxStyle.End, Spacing = 12 };

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

		void Build ()
		{
			Title = GettextCatalog.GetString ("Exception Caught");
			DefaultHeight = 350;
			DefaultWidth = 500;
			VBox.Spacing = 0;

			VBox.PackStart (CreateExceptionHeader (), false, true, 0);

			var vbox = new VBox (false, 0);
			vbox.PackStart (CreateExceptionValueTreeView (), true, true, 0);
			vbox.PackStart (CreateStackTraceTreeView (), true, true, 0);
			vbox.Show ();

			VBox.PackStart (vbox, true, true, 0);

			var actionArea = new HBox (false, 12) { BorderWidth = 6 };

			OnlyShowMyCodeCheckbox = new CheckButton (GettextCatalog.GetString ("_Only show my code."));
			OnlyShowMyCodeCheckbox.Toggled += OnlyShowMyCodeToggled;
			OnlyShowMyCodeCheckbox.Show ();

			var alignment = new Alignment (0.0f, 0.5f, 0.0f, 0.0f) { Child = OnlyShowMyCodeCheckbox };
			alignment.Show ();

			actionArea.PackStart (alignment, true, true, 0);
			actionArea.PackStart (CreateButtonBox (), false, true, 0);
			actionArea.Show ();

			VBox.PackStart (actionArea, false, true, 0);
			ActionArea.Hide ();
		}

		void StackFrameActivated (object o, RowActivatedArgs args)
		{
			var model = StackTraceTreeView.Model;
			TreeIter iter;

			if (!model.GetIter (out iter, args.Path))
				return;

			var frame = (ExceptionStackFrame) model.GetValue (iter, (int) ModelColumn.StackFrame);

			if (frame != null && !string.IsNullOrEmpty (frame.File))
				IdeApp.Workbench.OpenDocument (frame.File, frame.Line, frame.Column);
		}

		void ShowStackTrace (ExceptionInfo ex, bool showExceptionNode)
		{
			var model = (TreeStore) StackTraceTreeView.Model;
			TreeIter iter = TreeIter.Zero;

			if (showExceptionNode) {
				var markup = ex.Type + ": " + ex.Message;
				iter = model.AppendValues (null, markup);
				StackTraceTreeView.ShowExpanders = true;
			}

			foreach (var frame in ex.StackTrace) {
				var markup = string.Format ("<b>{0}</b>", GLib.Markup.EscapeText (frame.DisplayText));

				if (!string.IsNullOrEmpty (frame.File)) {
					markup += "\n<small>" + GLib.Markup.EscapeText (frame.File);
					if (frame.Line > 0) {
						markup += ":" + frame.Line;
						if (frame.Column > 0)
							markup += "," + frame.Column;
					}
					markup += "</small>";
				}

				if (!iter.Equals (TreeIter.Zero))
					model.AppendValues (iter, frame, markup);
				else
					model.AppendValues (frame, markup);
			}

			var inner = ex.InnerException;
			if (inner != null)
				ShowStackTrace (inner, true);
		}

		void UpdateDisplay ()
		{
			if (destroyed)
				return;

			var stack = (TreeStore) StackTraceTreeView.Model;
			ExceptionValueTreeView.ClearValues ();
			stack.Clear ();

			ExceptionTypeLabel.Markup = GettextCatalog.GetString ("A <b>{0}</b> was thrown.", exception.Type);
			ExceptionMessageLabel.Text = string.IsNullOrEmpty (exception.Message) ? string.Empty : exception.Message;

			ShowStackTrace (exception, false);

			if (!exception.IsEvaluating && exception.Instance != null) {
				ExceptionValueTreeView.AddValue (exception.Instance);
				ExceptionValueTreeView.ExpandRow (new TreePath ("0"), false);
			}

			if (exception.StackIsEvaluating)
				stack.AppendValues (null, GettextCatalog.GetString ("Loading..."));
		}

		void ExceptionChanged (object sender, EventArgs e)
		{
			Application.Invoke (delegate {
				UpdateDisplay ();
			});
		}

		void OnlyShowMyCodeToggled (object sender, EventArgs e)
		{
			
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
			get; private set;
		}

		public bool IsMinimized {
			get { return miniButton != null; }
		}

		public void ShowDialog ()
		{
			if (dialog == null) {
				dialog = new ExceptionCaughtDialog (ex, this);
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
		readonly Gdk.Pixbuf closeSelOverImage;
		readonly Gdk.Pixbuf closeSelImage;
		readonly ExceptionCaughtMessage dlg;
		readonly ExceptionInfo exception;
		Gtk.Label messageLabel;

		public ExceptionCaughtButton (ExceptionInfo val, ExceptionCaughtMessage dlg, FilePath file, int line)
		{
			this.exception = val;
			this.dlg = dlg;
			OffsetX = 6;
			File = file;
			Line = line;
			closeSelImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Selected.png");
			closeSelOverImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Selected.Over.png");
		}

		protected override void OnLineDeleted ()
		{
			dlg.Dispose ();
		}

		public override Widget CreateWidget ()
		{
			var icon = Gdk.Pixbuf.LoadFromResource ("lightning.png");
			var image = new Gtk.Image (icon);

			HBox box = new HBox (false, 6);
			VBox vb = new VBox ();
			vb.PackStart (image, false, false, 0);
			box.PackStart (vb, false, false, 0);
			vb = new VBox (false, 6);
			vb.PackStart (new Gtk.Label () {
				Markup = GettextCatalog.GetString ("<b>{0}</b> has been thrown", exception.Type),
				Xalign = 0
			});
			messageLabel = new Gtk.Label () {
				Xalign = 0,
				NoShowAll = true
			};
			vb.PackStart (messageLabel);

			var detailsBtn = new Xwt.LinkLabel (GettextCatalog.GetString ("Show Details"));
			HBox hh = new HBox ();
			detailsBtn.NavigateToUrl += (o,e) => dlg.ShowDialog ();
			hh.PackStart (detailsBtn.ToGtkWidget (), false, false, 0);
			vb.PackStart (hh, false, false, 0);

			box.PackStart (vb, true, true, 0);

			vb = new VBox ();
			var closeButton = new ImageButton () {
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

			PopoverWidget eb = new PopoverWidget ();
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

		protected override void OnLineDeleted ()
		{
			dlg.Dispose ();
		}

		public override Widget CreateWidget ()
		{
			Gtk.EventBox box = new EventBox ();
			box.VisibleWindow = false;
			var icon = Gdk.Pixbuf.LoadFromResource ("lightning.png");
			box.Add (new Gtk.Image (icon));
			box.ButtonPressEvent += (o,e) => dlg.ShowButton ();
			PopoverWidget eb = new PopoverWidget ();
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

