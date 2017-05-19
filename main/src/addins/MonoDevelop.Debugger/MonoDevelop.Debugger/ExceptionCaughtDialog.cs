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
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Fonts;
using System.Collections.Generic;

namespace MonoDevelop.Debugger
{
	class ExceptionCaughtDialog : Gtk.Window
	{
		VBox VBox;
		static readonly Xwt.Drawing.Image WarningIconPixbuf = Xwt.Drawing.Image.FromResource ("toolbar-icon.png");
		static readonly Xwt.Drawing.Image WarningIconPixbufInner = Xwt.Drawing.Image.FromResource ("exception-outline-16.png");

		protected ObjectValueTreeView ExceptionValueTreeView { get; private set; }

		protected TreeView StackTraceTreeView { get; private set; }

		protected CheckButton OnlyShowMyCodeCheckbox { get; private set; }

		protected Label ExceptionMessageLabel { get; private set; }

		protected Label ExceptionHelpLinkLabel { get; private set; }

		protected Label ExceptionTypeLabel { get; private set; }

		readonly ExceptionCaughtMessage message;
		readonly ExceptionInfo exception;
		ExceptionInfo selected;
		bool destroyed;
		VPanedThin paned;
		Expander expanderProperties;
		Expander expanderStacktrace;
		Button close;
		VBox rightVBox;
		VBox vboxAroundInnerExceptionMessage;

		protected enum ModelColumn
		{
			StackFrame,
			Markup,
			IsUserCode
		}

		public ExceptionCaughtDialog (ExceptionInfo ex, ExceptionCaughtMessage msg)
			: base (WindowType.Toplevel)
		{
			this.Child = VBox = new VBox ();
			VBox.Show ();
			this.Name = "wizard_dialog";
			this.ApplyTheme ();
			selected = exception = ex;
			message = msg;

			Build ();
			UpdateDisplay ();

			exception.Changed += ExceptionChanged;
		}

		Widget CreateExceptionHeader ()
		{
			var icon = new ImageView (WarningIconPixbuf);
			icon.Yalign = 0;

			ExceptionTypeLabel = new Label { Xalign = 0.0f, Selectable = true, CanFocus = false };
			ExceptionMessageLabel = new Label { Wrap = true, Xalign = 0.0f, Selectable = true, CanFocus = false };
			ExceptionHelpLinkLabel = new Label { Wrap = true, Xalign = 0.0f, Selectable = true, CanFocus = false, UseMarkup = true, LineWrapMode = Pango.WrapMode.Char };
			ExceptionHelpLinkLabel.Name = "exception_help_link_label";
			Gtk.Rc.ParseString (@"style ""exception-help-link-label""
{
	GtkWidget::link-color = ""#ffffff""
	GtkWidget::visited-link-color = ""#ffffff""
}
widget ""*.exception_help_link_label"" style ""exception-help-link-label""
");
			ExceptionHelpLinkLabel.ModifyBase (StateType.Prelight, new Gdk.Color (119, 130, 140));

			ExceptionHelpLinkLabel.SetLinkHandler ((str) => DesktopService.ShowUrl (str));
			ExceptionTypeLabel.ModifyFg (StateType.Normal, new Gdk.Color (255, 255, 255));
			ExceptionMessageLabel.ModifyFg (StateType.Normal, new Gdk.Color (255, 255, 255));
			ExceptionHelpLinkLabel.ModifyFg (StateType.Normal, new Gdk.Color (255, 255, 255));

			if (Platform.IsWindows) {
				ExceptionTypeLabel.ModifyFont (Pango.FontDescription.FromString ("bold 19"));
				ExceptionMessageLabel.ModifyFont (Pango.FontDescription.FromString ("10"));
				ExceptionHelpLinkLabel.ModifyFont (Pango.FontDescription.FromString ("10"));
			} else {
				ExceptionTypeLabel.ModifyFont (Pango.FontDescription.FromString ("21"));
				ExceptionMessageLabel.ModifyFont (Pango.FontDescription.FromString ("12"));
				ExceptionHelpLinkLabel.ModifyFont (Pango.FontDescription.FromString ("12"));
			}

			//Force rendering of background with EventBox
			var eventBox = new EventBox ();
			var hBox = new HBox ();
			var leftVBox = new VBox ();
			rightVBox = new VBox ();
			leftVBox.PackStart (icon, false, false, (uint)(Platform.IsWindows ? 5 : 0)); // as we change frame.BorderWidth below, we need to compensate

			rightVBox.PackStart (ExceptionTypeLabel, false, false, (uint)(Platform.IsWindows ? 0 : 2));
			rightVBox.PackStart (ExceptionMessageLabel, true, true, (uint)(Platform.IsWindows ? 6 : 5));
			rightVBox.PackStart (ExceptionHelpLinkLabel, false, false, 2);

			hBox.PackStart (leftVBox, false, false, (uint)(Platform.IsWindows ? 5 : 0)); // as we change frame.BorderWidth below, we need to compensate
			hBox.PackStart (rightVBox, true, true, (uint)(Platform.IsWindows ? 5 : 10));

			var frame = new Frame ();
			frame.Add (hBox);
			frame.BorderWidth = (uint)(Platform.IsWindows ? 5 : 10); // on Windows we need to have smaller border due to ExceptionTypeLabel vertical misalignment
			frame.Shadow = ShadowType.None;
			frame.ShadowType = ShadowType.None;

			eventBox.Add (frame);
			eventBox.ShowAll ();
			eventBox.ModifyBg (StateType.Normal, new Gdk.Color (119, 130, 140));

			return eventBox;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			ExceptionMessageLabel.WidthRequest = rightVBox.Allocation.Width;
			ExceptionHelpLinkLabel.WidthRequest = rightVBox.Allocation.Width;
			if (vboxAroundInnerExceptionMessage != null) {
				InnerExceptionMessageLabel.WidthRequest = vboxAroundInnerExceptionMessage.Allocation.Width;
				InnerExceptionHelpLinkLabel.WidthRequest = vboxAroundInnerExceptionMessage.Allocation.Width;
			}
		}

		Widget CreateExceptionValueTreeView ()
		{
			ExceptionValueTreeView = new ObjectValueTreeView ();
			ExceptionValueTreeView.Frame = DebuggingService.CurrentFrame;
			ExceptionValueTreeView.ModifyBase (StateType.Normal, Styles.ExceptionCaughtDialog.ValueTreeBackgroundColor.ToGdkColor ());
			ExceptionValueTreeView.AllowPopupMenu = false;
			ExceptionValueTreeView.AllowExpanding = true;
			ExceptionValueTreeView.AllowPinning = false;
			ExceptionValueTreeView.AllowEditing = false;
			ExceptionValueTreeView.AllowAdding = false;
			ExceptionValueTreeView.RulesHint = true;
			ExceptionValueTreeView.ModifyFont (Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11"));
			ExceptionValueTreeView.RulesHint = false;

			ExceptionValueTreeView.Show ();

			var scrolled = new ScrolledWindow {
				HeightRequest = 180,
				HscrollbarPolicy = PolicyType.Automatic,
				VscrollbarPolicy = PolicyType.Automatic
			};

			scrolled.ShadowType = ShadowType.None;
			scrolled.Add (ExceptionValueTreeView);
			scrolled.Show ();
			var vbox = new VBox ();
			expanderProperties = WrapInExpander (GettextCatalog.GetString ("Properties"), scrolled);
			vbox.PackStart (new VBox (), false, false, 5);
			vbox.PackStart (expanderProperties, true, true, 0);
			vbox.ShowAll ();
			return vbox;
		}

		class ExpanderWithMinSize : Expander
		{
			public ExpanderWithMinSize (string label) : base (label)
			{
			}

			protected override void OnSizeRequested (ref Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				requisition.Height = 28;
			}
		}

		Expander WrapInExpander (string title, Widget widget)
		{
			var expander = new ExpanderWithMinSize ($"<b>{GLib.Markup.EscapeText (title)}</b>");
			expander.Name = "exception_dialog_expander";
			Gtk.Rc.ParseString (@"style ""exception-dialog-expander""
{
	GtkExpander::expander-spacing = 10
}
widget ""*.exception_dialog_expander"" style ""exception-dialog-expander""
");
			expander.Child = widget;
			expander.Spacing = 0;
			expander.Show ();
			expander.CanFocus = false;
			expander.UseMarkup = true;
			expander.Expanded = true;
			expander.Activated += Expander_Activated;
			expander.ModifyBg (StateType.Prelight, Ide.Gui.Styles.PrimaryBackgroundColor.ToGdkColor ());
			return expander;
		}

		void Expander_Activated (object sender, EventArgs e)
		{
			if (expanderProperties.Expanded && expanderStacktrace.Expanded)
				paned.PositionSet = false;
			else if (expanderStacktrace.Expanded)
				paned.Position = paned.MaxPosition;
			else
				paned.Position = paned.MinPosition;
		}

		static void StackFrameLayout (CellLayout layout, CellRenderer cr, TreeModel model, TreeIter iter)
		{
			var frame = (ExceptionStackFrame)model.GetValue (iter, (int)ModelColumn.StackFrame);
			var renderer = (StackFrameCellRenderer)cr;

			renderer.Markup = (string)model.GetValue (iter, (int)ModelColumn.Markup);
			renderer.Frame = frame;

			if (frame == null) {
				renderer.IsUserCode = false;
				return;
			}

			renderer.IsUserCode = (bool)model.GetValue (iter, (int)ModelColumn.IsUserCode);
		}

		Widget CreateStackTraceTreeView ()
		{
			var store = new ListStore (typeof (ExceptionStackFrame), typeof (string), typeof (bool));
			StackTraceTreeView = new TreeView (store);
			StackTraceTreeView.SearchColumn = -1; // disable the interactive search
			StackTraceTreeView.FixedHeightMode = false;
			StackTraceTreeView.HeadersVisible = false;
			StackTraceTreeView.ShowExpanders = false;
			StackTraceTreeView.RulesHint = false;
			StackTraceTreeView.Show ();

			var renderer = new StackFrameCellRenderer (StackTraceTreeView.PangoContext);

			StackTraceTreeView.AppendColumn ("", renderer, (CellLayoutDataFunc)StackFrameLayout);

			StackTraceTreeView.RowActivated += StackFrameActivated;

			var scrolled = new ScrolledWindow {
				HeightRequest = 180,
				HscrollbarPolicy = PolicyType.Never,
				VscrollbarPolicy = PolicyType.Automatic
			};
			scrolled.ShadowType = ShadowType.None;
			scrolled.Add (StackTraceTreeView);
			scrolled.Show ();
			var vbox = new VBox ();
			vbox.PackStart (CreateSeparator (), false, true, 0);
			vbox.PackStart (scrolled, true, true, 0);
			vbox.Show ();

			var vbox2 = new VBox ();
			expanderStacktrace = WrapInExpander (GettextCatalog.GetString ("Stacktrace"), vbox);
			vbox2.PackStart (new VBox (), false, false, 5);
			vbox2.PackStart (expanderStacktrace, true, true, 0);
			vbox2.ShowAll ();
			return vbox2;
		}

		Widget CreateButtonBox ()
		{
			var buttons = new HButtonBox { Layout = ButtonBoxStyle.End, Spacing = 18 };

			var copy = new Button (Stock.Copy);
			copy.Clicked += CopyClicked;
			copy.Show ();

			buttons.PackStart (copy, false, true, 0);

			close = new Button (Stock.Close);
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

		bool HasInnerException ()
		{
			return exception.InnerException != null;
		}

		bool hadInnerException;

		void Build ()
		{
			Title = GettextCatalog.GetString ("Exception Caught");
			DefaultWidth = 500;
			DefaultHeight = 500;
			HeightRequest = 350;
			WidthRequest = 350;
			VBox.Foreach (VBox.Remove);
			VBox.PackStart (CreateExceptionHeader (), false, true, 0);
			paned = new VPanedThin ();
			paned.GrabAreaSize = 10;
			paned.Pack1 (CreateStackTraceTreeView (), true, false);
			paned.Pack2 (CreateExceptionValueTreeView (), true, false);
			paned.Show ();
			var vbox = new VBox (false, 0);
			var whiteBackground = new EventBox ();
			whiteBackground.Show ();
			whiteBackground.ModifyBg (StateType.Normal, Ide.Gui.Styles.PrimaryBackgroundColor.ToGdkColor ());
			whiteBackground.Add (vbox);
			hadInnerException = HasInnerException ();
			if (hadInnerException) {
				vbox.PackStart (new VBox (), false, false, 6);
				vbox.PackStart (CreateInnerExceptionMessage (), false, true, 0);
				vbox.ShowAll ();
			}
			vbox.PackStart (paned, true, true, 0);
			vbox.Show ();

			if (hadInnerException) {
				var box = new HBox ();
				box.PackStart (CreateInnerExceptionsTree (), false, false, 0);
				box.PackStart (whiteBackground, true, true, 0);
				box.Show ();
				VBox.PackStart (box, true, true, 0);
				DefaultWidth = 900;
				DefaultHeight = 700;
				WidthRequest = 550;
				HeightRequest = 450;
			} else {
				VBox.PackStart (whiteBackground, true, true, 0);
			}
			var actionArea = new HBox (false, 0) { BorderWidth = 14 };

			OnlyShowMyCodeCheckbox = new CheckButton (GettextCatalog.GetString ("_Only show my code."));
			OnlyShowMyCodeCheckbox.Toggled += OnlyShowMyCodeToggled;
			OnlyShowMyCodeCheckbox.Show ();
			OnlyShowMyCodeCheckbox.Active = DebuggingService.GetUserOptions ().ProjectAssembliesOnly;

			var alignment = new Alignment (0.0f, 0.5f, 0.0f, 0.0f) { Child = OnlyShowMyCodeCheckbox };
			alignment.Show ();

			actionArea.PackStart (alignment, true, true, 0);
			actionArea.PackStart (CreateButtonBox (), false, true, 0);
			actionArea.PackStart (new VBox (), false, true, 3); // dummy just to take extra 6px at end to make it 20pixels
			actionArea.ShowAll ();

			VBox.PackStart (actionArea, false, true, 0);
		}

		Label InnerExceptionTypeLabel;
		Label InnerExceptionMessageLabel;
		Label InnerExceptionHelpLinkLabel;

		Widget CreateInnerExceptionMessage ()
		{
			var hboxMain = new HBox ();
			vboxAroundInnerExceptionMessage = new VBox ();
			var hbox = new HBox ();

			var icon = new ImageView (WarningIconPixbufInner);
			icon.Yalign = 0;
			hbox.PackStart (icon, false, false, 0);

			InnerExceptionTypeLabel = new Label ();
			InnerExceptionTypeLabel.UseMarkup = true;
			InnerExceptionTypeLabel.Xalign = 0;
			InnerExceptionTypeLabel.Selectable = true;
			InnerExceptionTypeLabel.CanFocus = false;
			hbox.PackStart (InnerExceptionTypeLabel, false, true, 4);

			InnerExceptionMessageLabel = new Label ();
			InnerExceptionMessageLabel.Wrap = true;
			InnerExceptionMessageLabel.Selectable = true;
			InnerExceptionMessageLabel.CanFocus = false;
			InnerExceptionMessageLabel.Xalign = 0;
			InnerExceptionMessageLabel.ModifyFont (Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11"));

			InnerExceptionHelpLinkLabel = new Label ();
			InnerExceptionHelpLinkLabel.UseMarkup = true;
			InnerExceptionHelpLinkLabel.Wrap = true;
			InnerExceptionHelpLinkLabel.LineWrapMode = Pango.WrapMode.Char;
			InnerExceptionHelpLinkLabel.Selectable = true;
			InnerExceptionHelpLinkLabel.CanFocus = false;
			InnerExceptionHelpLinkLabel.Xalign = 0;
			InnerExceptionHelpLinkLabel.ModifyFont (Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11"));
			InnerExceptionHelpLinkLabel.SetLinkHandler ((str) => DesktopService.ShowUrl (str));

			vboxAroundInnerExceptionMessage.PackStart (hbox, false, true, 0);
			vboxAroundInnerExceptionMessage.PackStart (InnerExceptionMessageLabel, true, true, 10);
			vboxAroundInnerExceptionMessage.PackStart (InnerExceptionHelpLinkLabel, true, true, 2);
			hboxMain.PackStart (vboxAroundInnerExceptionMessage, true, true, 10);
			hboxMain.ShowAll ();
			return hboxMain;
		}

		TreeStore InnerExceptionsStore;

		class InnerExceptionsTree : TreeView
		{
			public InnerExceptionsTree ()
			{
				Events |= Gdk.EventMask.PointerMotionMask;
			}

			protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
			{
				TreePath path;
				//We want effect that when user has mouse button pressed and is moving over tree to autoselect exception
				if (evnt.State == Gdk.ModifierType.Button1Mask && GetPathAtPos ((int)evnt.X, (int)evnt.Y, out path)) {
					Selection.SelectPath (path);
				}
				return base.OnMotionNotifyEvent (evnt);
			}
		}

		InnerExceptionsTree InnerExceptionsTreeView;

		Dictionary<ExceptionInfo, ExceptionInfo> ReverseInnerExceptions = new Dictionary<ExceptionInfo, ExceptionInfo> ();

		Widget CreateInnerExceptionsTree ()
		{
			InnerExceptionsTreeView = new InnerExceptionsTree ();
			InnerExceptionsTreeView.ModifyBase (StateType.Normal, Styles.ExceptionCaughtDialog.TreeBackgroundColor.ToGdkColor ()); // background
			InnerExceptionsTreeView.ModifyBase (StateType.Selected, Styles.ExceptionCaughtDialog.TreeSelectedBackgroundColor.ToGdkColor ()); // selected
			InnerExceptionsTreeView.HeadersVisible = false;
			InnerExceptionsStore = new TreeStore (typeof (ExceptionInfo));

			FillInnerExceptionsStore (InnerExceptionsStore, exception);
			InnerExceptionsTreeView.AppendColumn ("Exception", new CellRendererInnerException (), new TreeCellDataFunc ((tree_column, cell, tree_model, iter) => {
				var c = (CellRendererInnerException)cell;
				c.Text = ((ExceptionInfo)tree_model.GetValue (iter, 0)).Type;
			}));
			InnerExceptionsTreeView.ShowExpanders = false;
			InnerExceptionsTreeView.LevelIndentation = 10;
			InnerExceptionsTreeView.Model = InnerExceptionsStore;
			InnerExceptionsTreeView.ExpandAll ();
			InnerExceptionsTreeView.Selection.Changed += (sender, e) => {
				TreeIter selectedIter;
				if (InnerExceptionsTreeView.Selection.GetSelected (out selectedIter)) {
					UpdateSelectedException ((ExceptionInfo)InnerExceptionsTreeView.Model.GetValue (selectedIter, 0));
				}
			};
			var eventBox = new EventBox ();
			eventBox.ModifyBg (StateType.Normal, Styles.ExceptionCaughtDialog.TreeBackgroundColor.ToGdkColor ()); // top and bottom padders
			var vbox = new VBox ();
			vbox.PackStart (InnerExceptionsTreeView, true, true, 12);
			eventBox.Add (vbox);
			eventBox.ShowAll ();
			return eventBox;
		}

		void FillInnerExceptionsStore (TreeStore store, ExceptionInfo exception, TreeIter parentIter = default (TreeIter))
		{
			TreeIter iter;
			if (parentIter.Equals (TreeIter.Zero)) {
				iter = store.AppendValues (exception);
				ReverseInnerExceptions [exception] = null;
			} else {
				ReverseInnerExceptions [exception] = (ExceptionInfo)store.GetValue (parentIter, 0);
				iter = store.AppendValues (parentIter, exception);
			}
			var updateInnerExceptions = new System.Action (() => {
				if (!InnerExceptionsStore.IterHasChild (iter)) {
					var innerExceptions = exception.InnerExceptions;
					if (innerExceptions != null && innerExceptions.Count > 0) {
						foreach (var inner in innerExceptions) {
							FillInnerExceptionsStore (store, inner, iter);
						}
					} else {
						var inner = exception.InnerException;
						if (inner != null)
							FillInnerExceptionsStore (store, inner, iter);
					}
				}
			});
			exception.Changed += delegate {
				Application.Invoke (delegate {
					InnerExceptionsStore.EmitRowChanged (InnerExceptionsStore.GetPath (iter), iter);
					updateInnerExceptions ();
					InnerExceptionsTreeView.ExpandRow (InnerExceptionsStore.GetPath (iter), true);
				});
			};
			updateInnerExceptions ();
		}

		void StackFrameActivated (object o, RowActivatedArgs args)
		{
			var model = StackTraceTreeView.Model;
			TreeIter iter;

			if (!model.GetIter (out iter, args.Path))
				return;

			var frame = (ExceptionStackFrame)model.GetValue (iter, (int)ModelColumn.StackFrame);

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

		void UpdateSelectedException (ExceptionInfo ex)
		{
			selected = ex;
			var model = (ListStore)StackTraceTreeView.Model;
			bool external = false;

			model.Clear ();
			var parentException = ex;
			while (parentException != null) {
				foreach (var frame in parentException.StackTrace) {
					bool isUserCode = IsUserCode (frame);

					if (OnlyShowMyCodeCheckbox.Active && !isUserCode) {
						if (!external) {
							var str = "<b>" + GettextCatalog.GetString ("[External Code]") + "</b>";
							model.AppendValues (null, str, false);
							external = true;
						}

						continue;
					}

					model.AppendValues (frame, null, isUserCode);
					external = false;
				}
				if (!ReverseInnerExceptions.TryGetValue (parentException, out parentException))
					parentException = null;
			}
			ExceptionValueTreeView.ClearAll ();
			if (!ex.IsEvaluating && ex.Instance != null) {
				var opts = DebuggingService.GetUserOptions ().EvaluationOptions.Clone ();
				opts.FlattenHierarchy = true;
				ExceptionValueTreeView.AddValues (ex.Instance.GetAllChildren (opts));
			}

			if (ex.StackIsEvaluating) {
				var str = GettextCatalog.GetString ("Loading...");
				model.AppendValues (null, str, false);
			}

			if (InnerExceptionTypeLabel != null) {
				InnerExceptionTypeLabel.Markup = "<b>" + GLib.Markup.EscapeText (ex.Type) + "</b>";
				InnerExceptionMessageLabel.Text = ex.Message;
				if (!string.IsNullOrEmpty (ex.HelpLink)) {
					InnerExceptionHelpLinkLabel.Markup = GetHelpLinkMarkup (ex);
					InnerExceptionHelpLinkLabel.Show ();
				} else {
					InnerExceptionHelpLinkLabel.Hide ();
				}
			}
		}

		void UpdateDisplay ()
		{
			if (destroyed)
				return;

			ExceptionTypeLabel.Text = exception.Type;
			ExceptionMessageLabel.Text = exception.Message ?? string.Empty;
			if (!string.IsNullOrEmpty (exception.HelpLink)) {
				ExceptionHelpLinkLabel.Show ();
				ExceptionHelpLinkLabel.Markup = GetHelpLinkMarkup(exception);
			} else {
				ExceptionHelpLinkLabel.Hide ();
			}

			UpdateSelectedException (exception);
		}

		internal static string GetHelpLinkMarkup (ExceptionInfo exception)
		{
			return $"<a href=\"{System.Security.SecurityElement.Escape (exception.HelpLink)}\">{GettextCatalog.GetString ("More information")}</a>";
		}

		void ExceptionChanged (object sender, EventArgs e)
		{
			Application.Invoke (delegate {
				if (hadInnerException != HasInnerException ())
					Build ();
				UpdateDisplay ();
			});
		}

		void OnlyShowMyCodeToggled (object sender, EventArgs e)
		{
			UpdateSelectedException (selected);
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

		class CellRendererInnerException : CellRenderer
		{
			public string Text { get; set; }

			Pango.FontDescription font = Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11");

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					layout.FontDescription = font;
					Pango.Rectangle ink, logical;
					layout.SetMarkup ("<b>" + Text + "</b>");
					layout.GetPixelExtents (out ink, out logical);
					width = logical.Width + 10;
					height = logical.Height + 2;

					x_offset = 0;
					y_offset = 0;
				}
			}

			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				using (var cr = Gdk.CairoHelper.Create (window)) {
					cr.Rectangle (background_area.X, background_area.Y, background_area.Width, background_area.Height);

					using (var layout = new Pango.Layout (widget.PangoContext)) {
						layout.FontDescription = font;

						if ((flags & CellRendererState.Selected) != 0) {
							cr.SetSourceRGB (Styles.ExceptionCaughtDialog.TreeSelectedBackgroundColor.Red,
											 Styles.ExceptionCaughtDialog.TreeSelectedBackgroundColor.Green,
											 Styles.ExceptionCaughtDialog.TreeSelectedBackgroundColor.Blue); // selected
							cr.Fill ();
							cr.SetSourceRGB (Styles.ExceptionCaughtDialog.TreeSelectedTextColor.Red,
											 Styles.ExceptionCaughtDialog.TreeSelectedTextColor.Green,
											 Styles.ExceptionCaughtDialog.TreeSelectedTextColor.Blue);
						} else {
							cr.SetSourceRGB (Styles.ExceptionCaughtDialog.TreeBackgroundColor.Red,
											 Styles.ExceptionCaughtDialog.TreeBackgroundColor.Green,
											 Styles.ExceptionCaughtDialog.TreeBackgroundColor.Blue); // background
							cr.Fill ();
							cr.SetSourceRGB (Styles.ExceptionCaughtDialog.TreeTextColor.Red,
											 Styles.ExceptionCaughtDialog.TreeTextColor.Green,
											 Styles.ExceptionCaughtDialog.TreeTextColor.Blue);
						}

						layout.SetMarkup (Text);
						cr.Translate (cell_area.X + 10, cell_area.Y + 1);
						cr.ShowLayout (layout);
					}
				}
			}
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				this.Destroy ();
				return true;
			}
			return base.OnKeyReleaseEvent (evnt);
		}
	}

	class StackFrameCellRenderer : CellRenderer
	{
		const int Padding = 6;

		public readonly Pango.Context Context;
		public ExceptionStackFrame Frame;
		public bool IsUserCode;
		public string Markup;
		Pango.FontDescription font = Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11");

		public StackFrameCellRenderer (Pango.Context ctx)
		{
			Context = ctx;
		}

		string GetMethodMarkup (bool selected)
		{
			if (Markup != null)
				return $"<span foreground='{Styles.ExceptionCaughtDialog.ExternalCodeTextColor.ToHexString (false)}'>{Markup}</span>";
			var methodText = Frame.DisplayText;
			var endOfMethodName = methodText.IndexOf ('(');
			var methodName = methodText.Remove (endOfMethodName).Trim ();
			var endOfParameters = methodText.IndexOf (')') + 1;
			var parameters = methodText.Substring (endOfMethodName, endOfParameters - endOfMethodName).Trim ();

			var markup = $"<b>{GLib.Markup.EscapeText (methodName)}</b> {GLib.Markup.EscapeText (parameters)}";

			if (selected)
				markup = $"<span foreground='#FFFFFF'>{markup}</span>";
			else {
				var textColor = IsUserCode ? Ide.Gui.Styles.BaseForegroundColor.ToHexString (false) : Styles.ExceptionCaughtDialog.ExternalCodeTextColor.ToHexString (false);
				markup = $"<span foreground='{textColor}'>{markup}</span>";
			}

			return markup;
		}

		string GetFileMarkup (bool selected)
		{
			if (Frame == null || string.IsNullOrEmpty (Frame.File)) {
				return "";
			}

			var markup = string.Format ("<span foreground='{0}'>{1}", selected ? "#FFFFFF" : Styles.ExceptionCaughtDialog.LineNumberTextColor.ToHexString (false), GLib.Markup.EscapeText (Path.GetFileName (Frame.File)));
			if (Frame.Line > 0) {
				markup += ":" + Frame.Line;
				if (Frame.Column > 0)
					markup += "," + Frame.Column;
			}
			markup += "</span>";
			return markup;
		}

		public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			using (var layout = new Pango.Layout (Context)) {
				Pango.Rectangle ink, logical;
				layout.FontDescription = font;
				layout.SetMarkup (GetMethodMarkup (false));
				layout.GetPixelExtents (out ink, out logical);

				height = logical.Height;
				width = 0;
				x_offset = 0;
				y_offset = 0;
			}
		}

		protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			using (var cr = Gdk.CairoHelper.Create (window)) {
				Pango.Rectangle ink, logical;
				using (var layout = new Pango.Layout (Context)) {
					layout.FontDescription = font;
					layout.SetMarkup (GetFileMarkup ((flags & CellRendererState.Selected) != 0));
					layout.GetPixelExtents (out ink, out logical);
					var width = widget.Allocation.Width;
					cr.Translate (width - logical.Width - 10, cell_area.Y);
					cr.ShowLayout (layout);

					cr.IdentityMatrix ();

					layout.SetMarkup (GetMethodMarkup ((flags & CellRendererState.Selected) != 0));
					layout.Width = (int)((width - logical.Width - 35) * Pango.Scale.PangoScale);
					layout.Ellipsize = Pango.EllipsizeMode.Middle;
					cr.Translate (cell_area.X + 10, cell_area.Y);
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
			get;
			private set;
		}

		public int Line {
			get;
			set;
		}

		public bool IsMinimized {
			get { return miniButton != null; }
		}

		public void ShowDialog ()
		{
			if (dialog == null) {
				dialog = new ExceptionCaughtDialog (ex, this);
				IdeApp.CommandService.RegisterTopWindow (dialog);
				dialog.TransientFor = IdeApp.Workbench.RootWindow;
				dialog.Show ();
				MessageService.PlaceDialog (dialog, IdeApp.Workbench.RootWindow);
				dialog.Destroyed += Dialog_Destroyed;;
			}
		}

		void Dialog_Destroyed (object sender, EventArgs e)
		{
			if (dialog != null) {
				dialog.Destroyed -= Dialog_Destroyed;
				dialog = null;
			}
		}


		public void ShowButton ()
		{
			if (dialog != null) {
				dialog.Destroyed -= Dialog_Destroyed;
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
				dialog.Destroyed -= Dialog_Destroyed;
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

	class ExceptionCaughtButton : TopLevelWidgetExtension
	{
		readonly Xwt.Drawing.Image closeSelOverImage;
		readonly Xwt.Drawing.Image closeSelImage;
		readonly ExceptionCaughtMessage dlg;
		readonly ExceptionInfo exception;
		Label messageLabel;
		Label typeLabel;

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

		public override Control CreateWidget ()
		{
			var icon = Xwt.Drawing.Image.FromResource ("lightning-16.png");
			var image = new Xwt.ImageView (icon).ToGtkWidget ();

			var box = new HBox (false, 6) { Name = "exceptionCaughtButtonBox" };
			var vb = new VBox ();
			vb.PackStart (image, false, false, 0);
			box.PackStart (vb, false, false, 0);
			vb = new VBox (false, 6);
			typeLabel = new Label {
				Xalign = 0,
				Selectable = true,
				CanFocus = false,
				Name = "exceptionTypeLabel"
			};
			vb.PackStart (typeLabel);
			messageLabel = new Label {
				Xalign = 0,
				NoShowAll = true,
				Selectable = true,
				CanFocus = false,
				Name = "exceptionMessageLabel"
			};
			vb.PackStart (messageLabel);

			var detailsBtn = new Xwt.LinkLabel (GettextCatalog.GetString ("Show Details"));
			var hh = new HBox ();
			detailsBtn.CanGetFocus = false;
			detailsBtn.NavigateToUrl += (o, e) => dlg.ShowDialog ();
			hh.PackStart (detailsBtn.ToGtkWidget (), false, false, 0);
			vb.PackStart (hh, false, false, 0);

			box.PackStart (vb, true, true, 0);

			vb = new VBox ();
			var closeButton = new ImageButton {
				InactiveImage = closeSelImage,
				Image = closeSelOverImage,
				Name = "closeExceptionCaughtButton"
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
			eb.Name = "exceptionCaughtPopoverWidget";
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
			if (!string.IsNullOrEmpty (exception.Type)) {
				typeLabel.Show ();
				typeLabel.Markup = GettextCatalog.GetString ("<b>{0}</b> has been thrown", GLib.Markup.EscapeText (exception.Type));
			} else {
				typeLabel.Hide ();
			}
		}
	}

	class ExceptionCaughtMiniButton : TopLevelWidgetExtension
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

		public override Control CreateWidget ()
		{
			var box = new EventBox ();
			box.Name = "exceptionCaughtMiniButtonEventBox";
			box.VisibleWindow = false;
			var icon = Xwt.Drawing.Image.FromResource ("lightning-16.png");
			box.Add (new Xwt.ImageView (icon).ToGtkWidget ());
			box.ButtonPressEvent += (o, e) => dlg.ShowButton ();
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

	class ExceptionCaughtTextEditorExtension : TextEditorExtension
	{
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			if (descriptor.SpecialKey == SpecialKey.Escape && DebuggingService.ExceptionCaughtMessage != null &&
				!DebuggingService.ExceptionCaughtMessage.IsMinimized &&
				DebuggingService.ExceptionCaughtMessage.File.CanonicalPath == new FilePath (DocumentContext.Name).CanonicalPath) {

				DebuggingService.ExceptionCaughtMessage.ShowMiniButton ();
				return true;
			}

			return base.KeyPress (descriptor);
		}
	}
}

