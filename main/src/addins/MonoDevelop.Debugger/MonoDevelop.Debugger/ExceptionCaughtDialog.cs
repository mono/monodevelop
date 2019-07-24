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
using System.Collections.Generic;

using Gtk;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Debugger
{
	class ExceptionCaughtDialog : Gtk.Window
	{
		static readonly Xwt.Drawing.Image WarningIconPixbuf = Xwt.Drawing.Image.FromResource ("toolbar-icon.png");
		static readonly Xwt.Drawing.Image WarningIconPixbufInner = Xwt.Drawing.Image.FromResource ("exception-outline-16.png");
		static bool UseNewTreeView = true;

		readonly Dictionary<ExceptionInfo, ExceptionInfo> reverseInnerExceptions = new Dictionary<ExceptionInfo, ExceptionInfo> ();
		readonly ExceptionCaughtMessage message;
		readonly ExceptionInfo exception;

		Label exceptionMessageLabel, exceptionTypeLabel, innerExceptionTypeLabel, innerExceptionMessageLabel;
		VBox vboxAroundInnerExceptionMessage, rightVBox, container;
		Button close, helpLinkButton, innerExceptionHelpLinkButton;
		TreeView exceptionValueTreeView, stackTraceTreeView;
		Expander expanderProperties, expanderStacktrace;
		InnerExceptionsTree innerExceptionsTreeView;
		ObjectValueTreeViewController controller;
		CheckButton onlyShowMyCodeCheckbox;
		bool destroyed, hadInnerException;
		TreeStore innerExceptionsStore;
		string innerExceptionHelpLink;
		string exceptionHelpLink;
		ExceptionInfo selected;
		VPanedThin paned;

		protected enum ModelColumn
		{
			StackFrame,
			Markup,
			IsUserCode
		}

		public ExceptionCaughtDialog (ExceptionInfo ex, ExceptionCaughtMessage msg)
			: base (WindowType.Toplevel)
		{
			Child = container = new VBox ();
			container.Show ();
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

			exceptionTypeLabel = new Label { Xalign = 0.0f, Selectable = true, CanFocus = false };
			exceptionMessageLabel = new Label { Wrap = true, Xalign = 0.0f, Selectable = true, CanFocus = false };
			helpLinkButton = new Button { HasFocus = true, Xalign = 0, Relief = ReliefStyle.None, BorderWidth = 0 };
			helpLinkButton.Name = "exception_help_link_label";
			Gtk.Rc.ParseString (@"style ""exception-help-link-label""
{
	GtkWidget::link-color = ""#ffffff""
	GtkWidget::visited-link-color = ""#ffffff""
}
widget ""*.exception_help_link_label"" style ""exception-help-link-label""
");
			var textColor = Styles.ExceptionCaughtDialog.HeaderTextColor.ToGdkColor ();
			var headerColor = Styles.ExceptionCaughtDialog.HeaderBackgroundColor.ToGdkColor ();

			helpLinkButton.ModifyBg (StateType.Selected, headerColor);

			helpLinkButton.Clicked += ExceptionHelpLinkLabel_Clicked;
			helpLinkButton.KeyPressEvent += EventBoxLink_KeyPressEvent;

			exceptionTypeLabel.ModifyFg (StateType.Normal, textColor);
			exceptionMessageLabel.ModifyFg (StateType.Normal, textColor);
			helpLinkButton.ModifyFg (StateType.Normal, textColor);

			if (Platform.IsWindows) {
				exceptionTypeLabel.ModifyFont (Pango.FontDescription.FromString ("bold 19"));
				exceptionMessageLabel.ModifyFont (Pango.FontDescription.FromString ("10"));
				helpLinkButton.ModifyFont (Pango.FontDescription.FromString ("10"));
			} else {
				exceptionTypeLabel.ModifyFont (Pango.FontDescription.FromString ("21"));
				exceptionMessageLabel.ModifyFont (Pango.FontDescription.FromString ("12"));
				helpLinkButton.ModifyFont (Pango.FontDescription.FromString ("12"));
			}

			//Force rendering of background with EventBox
			var eventBox = new EventBox ();
			var hBox = new HBox ();
			var leftVBox = new VBox ();
			rightVBox = new VBox ();
			leftVBox.PackStart (icon, false, false, (uint)(Platform.IsWindows ? 5 : 0)); // as we change frame.BorderWidth below, we need to compensate

			rightVBox.PackStart (exceptionTypeLabel, false, false, (uint)(Platform.IsWindows ? 0 : 2));

			var exceptionHContainer = new HBox ();
			exceptionHContainer.PackStart (helpLinkButton, false, false, 0);
			exceptionHContainer.PackStart (new Fixed (), true, true, 0);

			rightVBox.PackStart (exceptionMessageLabel, true, true, (uint)(Platform.IsWindows ? 6 : 5));
			rightVBox.PackStart (exceptionHContainer, false, false, 2);

			hBox.PackStart (leftVBox, false, false, (uint)(Platform.IsWindows ? 5 : 0)); // as we change frame.BorderWidth below, we need to compensate
			hBox.PackStart (rightVBox, true, true, (uint)(Platform.IsWindows ? 5 : 10));

			var frame = new Frame ();
			frame.Add (hBox);
			frame.BorderWidth = (uint)(Platform.IsWindows ? 5 : 10); // on Windows we need to have smaller border due to ExceptionTypeLabel vertical misalignment
			frame.Shadow = ShadowType.None;
			frame.ShadowType = ShadowType.None;

			eventBox.Add (frame);
			eventBox.ShowAll ();
			eventBox.ModifyBg (StateType.Normal, headerColor);

			return eventBox;
		}

		void ExceptionHelpLinkLabel_Clicked (object sender, EventArgs e) => IdeServices.DesktopService.ShowUrl (exceptionHelpLink);

		void EventBoxLink_KeyPressEvent (object o, KeyPressEventArgs args)
		{ 
			if (args.Event.Key == Gdk.Key.KP_Enter || args.Event.Key == Gdk.Key.KP_Space)
				IdeServices.DesktopService.ShowUrl (exceptionHelpLink);
		}

		void EventBoxLink_ExceptionHelpLink (object o, ButtonPressEventArgs args) => IdeServices.DesktopService.ShowUrl (exceptionHelpLink);

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			exceptionMessageLabel.WidthRequest = rightVBox.Allocation.Width;
			if (vboxAroundInnerExceptionMessage != null) {
				innerExceptionMessageLabel.WidthRequest = vboxAroundInnerExceptionMessage.Allocation.Width;
			}
		}

		Widget CreateExceptionValueTreeView ()
		{
			if (UseNewTreeView) {
				controller = new ObjectValueTreeViewController ();
				controller.SetStackFrame (DebuggingService.CurrentFrame);
				controller.AllowExpanding = true;

				exceptionValueTreeView = (TreeView) controller.GetControl (allowPopupMenu: false);
			} else {
				var objValueTreeView = new ObjectValueTreeView ();
				objValueTreeView.Frame = DebuggingService.CurrentFrame;
				objValueTreeView.AllowPopupMenu = false;
				objValueTreeView.AllowExpanding = true;
				objValueTreeView.AllowPinning = false;
				objValueTreeView.AllowEditing = false;
				objValueTreeView.AllowAdding = false;
			}

			exceptionValueTreeView.ModifyBase (StateType.Normal, Styles.ExceptionCaughtDialog.ValueTreeBackgroundColor.ToGdkColor ());
			exceptionValueTreeView.ModifyFont (Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11"));
			exceptionValueTreeView.RulesHint = false;
			exceptionValueTreeView.CanFocus = true;
			exceptionValueTreeView.Show ();

			var scrolled = new ScrolledWindow {
				HeightRequest = 180,
				CanFocus = true,
				HscrollbarPolicy = PolicyType.Automatic,
				VscrollbarPolicy = PolicyType.Automatic
			};

			scrolled.ShadowType = ShadowType.None;
			scrolled.Add (exceptionValueTreeView);
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
			expander.CanFocus = true;
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
			stackTraceTreeView = new TreeView (store);
			stackTraceTreeView.SearchColumn = -1; // disable the interactive search
			stackTraceTreeView.FixedHeightMode = false;
			stackTraceTreeView.HeadersVisible = false;
			stackTraceTreeView.ShowExpanders = false;
			stackTraceTreeView.RulesHint = false;
			stackTraceTreeView.Show ();

			var renderer = new StackFrameCellRenderer (stackTraceTreeView.PangoContext);

			stackTraceTreeView.AppendColumn ("", renderer, (CellLayoutDataFunc)StackFrameLayout);

			stackTraceTreeView.RowActivated += StackFrameActivated;

			var scrolled = new ScrolledWindow {
				HeightRequest = 180,
				HscrollbarPolicy = PolicyType.Never,
				VscrollbarPolicy = PolicyType.Automatic
			};
			scrolled.ShadowType = ShadowType.None;
			scrolled.Add (stackTraceTreeView);
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

		void Build ()
		{
			Title = GettextCatalog.GetString ("Exception Caught");
			DefaultWidth = 500;
			DefaultHeight = 500;
			HeightRequest = 350;
			WidthRequest = 350;
			container.Foreach (container.Remove);
			container.PackStart (CreateExceptionHeader (), false, true, 0);
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
				vbox.PackStart (box, true, true, 0);
				DefaultWidth = 900;
				DefaultHeight = 700;
				WidthRequest = 550;
				HeightRequest = 450;
			} else {
				vbox.PackStart (whiteBackground, true, true, 0);
			}
			var actionArea = new HBox (false, 0) { BorderWidth = 14 };

			onlyShowMyCodeCheckbox = new CheckButton (GettextCatalog.GetString ("_Only show my code."));
			onlyShowMyCodeCheckbox.Toggled += OnlyShowMyCodeToggled;
			onlyShowMyCodeCheckbox.Show ();
			onlyShowMyCodeCheckbox.Active = DebuggingService.GetUserOptions ().ProjectAssembliesOnly;

			var alignment = new Alignment (0.0f, 0.5f, 0.0f, 0.0f) { Child = onlyShowMyCodeCheckbox };
			alignment.Show ();

			actionArea.PackStart (alignment, true, true, 0);
			actionArea.PackStart (CreateButtonBox (), false, true, 0);
			actionArea.PackStart (new VBox (), false, true, 3); // dummy just to take extra 6px at end to make it 20pixels
			actionArea.ShowAll ();

			vbox.PackStart (actionArea, false, true, 0);
		}

		Widget CreateInnerExceptionMessage ()
		{
			var hboxMain = new HBox ();
			vboxAroundInnerExceptionMessage = new VBox ();
			var hbox = new HBox ();

			var icon = new ImageView (WarningIconPixbufInner);
			icon.Yalign = 0;
			hbox.PackStart (icon, false, false, 0);

			innerExceptionTypeLabel = new Label ();
			innerExceptionTypeLabel.UseMarkup = true;
			innerExceptionTypeLabel.Xalign = 0;
			innerExceptionTypeLabel.Selectable = true;
			innerExceptionTypeLabel.CanFocus = false;
			hbox.PackStart (innerExceptionTypeLabel, false, true, 4);

			innerExceptionMessageLabel = new Label ();
			innerExceptionMessageLabel.Wrap = true;
			innerExceptionMessageLabel.Selectable = true;
			innerExceptionMessageLabel.CanFocus = false;
			innerExceptionMessageLabel.Xalign = 0;
			innerExceptionMessageLabel.ModifyFont (Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11"));

			innerExceptionHelpLinkButton = new Button {
				CanFocus = true,
				BorderWidth = 0,
				Relief = ReliefStyle.Half,
				Xalign = 0
			};
			innerExceptionHelpLinkButton.ModifyFont (Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11"));
			innerExceptionHelpLinkButton.KeyPressEvent += InnerExceptionHelpLinkLabel_KeyPressEvent;
			innerExceptionHelpLinkButton.Clicked += InnerExceptionHelpLinkLabel_Pressed;

			innerExceptionHelpLinkButton.ModifyBg (StateType.Selected, Styles.ExceptionCaughtDialog.TreeSelectedBackgroundColor.ToGdkColor ());

			vboxAroundInnerExceptionMessage.PackStart (hbox, false, true, 0);
			vboxAroundInnerExceptionMessage.PackStart (innerExceptionMessageLabel, true, true, 10);

			var innerExceptionHContainer = new HBox ();

			innerExceptionHContainer.PackStart (innerExceptionHelpLinkButton, false, false, 0);
			innerExceptionHContainer.PackStart (new Fixed (), true, true, 0);

			vboxAroundInnerExceptionMessage.PackStart (innerExceptionHContainer, true, true, 2);
			hboxMain.PackStart (vboxAroundInnerExceptionMessage, true, true, 10);
			hboxMain.ShowAll ();
			return hboxMain;
		}

		void InnerExceptionHelpLinkLabel_Pressed (object sender, EventArgs e) => IdeServices.DesktopService.ShowUrl (innerExceptionHelpLink);
		void InnerExceptionHelpLinkLabel_KeyPressEvent (object o, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.KP_Enter || args.Event.Key == Gdk.Key.KP_Space)
				IdeServices.DesktopService.ShowUrl (innerExceptionHelpLink);
		}

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

		Widget CreateInnerExceptionsTree ()
		{
			innerExceptionsTreeView = new InnerExceptionsTree ();
			innerExceptionsTreeView.ModifyBase (StateType.Normal, Styles.ExceptionCaughtDialog.TreeBackgroundColor.ToGdkColor ()); // background
			innerExceptionsTreeView.ModifyBase (StateType.Selected, Styles.ExceptionCaughtDialog.TreeSelectedBackgroundColor.ToGdkColor ()); // selected
			innerExceptionsTreeView.HeadersVisible = false;
			innerExceptionsStore = new TreeStore (typeof (ExceptionInfo));

			FillInnerExceptionsStore (innerExceptionsStore, exception);
			innerExceptionsTreeView.AppendColumn ("Exception", new CellRendererInnerException (), new TreeCellDataFunc ((tree_column, cell, tree_model, iter) => {
				var c = (CellRendererInnerException)cell;
				c.Text = ((ExceptionInfo)tree_model.GetValue (iter, 0)).Type;
			}));
			innerExceptionsTreeView.ShowExpanders = false;
			innerExceptionsTreeView.LevelIndentation = 10;
			innerExceptionsTreeView.Model = innerExceptionsStore;
			innerExceptionsTreeView.ExpandAll ();
			innerExceptionsTreeView.Selection.Changed += (sender, e) => {
				TreeIter selectedIter;
				if (innerExceptionsTreeView.Selection.GetSelected (out selectedIter)) {
					UpdateSelectedException ((ExceptionInfo)innerExceptionsTreeView.Model.GetValue (selectedIter, 0));
				}
			};
			var eventBox = new EventBox ();
			eventBox.ModifyBg (StateType.Normal, Styles.ExceptionCaughtDialog.TreeBackgroundColor.ToGdkColor ()); // top and bottom padders
			var vbox = new VBox ();
			var scroll = new ScrolledWindow ();
			scroll.WidthRequest = 200;
			scroll.Child = innerExceptionsTreeView;
			vbox.PackStart (scroll, true, true, 12);
			eventBox.Add (vbox);
			eventBox.ShowAll ();
			return eventBox;
		}

		void FillInnerExceptionsStore (TreeStore store, ExceptionInfo exception, TreeIter parentIter = default (TreeIter))
		{
			TreeIter iter;
			if (parentIter.Equals (TreeIter.Zero)) {
				iter = store.AppendValues (exception);
				reverseInnerExceptions [exception] = null;
			} else {
				reverseInnerExceptions [exception] = (ExceptionInfo)store.GetValue (parentIter, 0);
				iter = store.AppendValues (parentIter, exception);
			}
			var updateInnerExceptions = new System.Action (() => {
				if (!innerExceptionsStore.IterHasChild (iter)) {
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
				Application.Invoke ((o, args) => {
					innerExceptionsStore.EmitRowChanged (innerExceptionsStore.GetPath (iter), iter);
					updateInnerExceptions ();
					innerExceptionsTreeView.ExpandRow (innerExceptionsStore.GetPath (iter), true);
				});
			};
			updateInnerExceptions ();
		}

		async void StackFrameActivated (object o, RowActivatedArgs args)
		{
			var model = stackTraceTreeView.Model;
			TreeIter iter;

			if (!model.GetIter (out iter, args.Path))
				return;

			var frame = (ExceptionStackFrame)model.GetValue (iter, (int)ModelColumn.StackFrame);

			if (frame != null && !string.IsNullOrEmpty (frame.File) && File.Exists (frame.File)) {
				try {
					await IdeApp.Workbench.OpenDocument (frame.File, null, frame.Line, frame.Column, MonoDevelop.Ide.Gui.OpenDocumentOptions.Debugger);
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
			var model = (ListStore)stackTraceTreeView.Model;
			bool external = false;

			model.Clear ();
			var parentException = ex;
			while (parentException != null) {
				foreach (var frame in parentException.StackTrace) {
					bool isUserCode = IsUserCode (frame);

					if (onlyShowMyCodeCheckbox.Active && !isUserCode) {
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
				if (!reverseInnerExceptions.TryGetValue (parentException, out parentException))
					parentException = null;
			}

			if (UseNewTreeView) {
				controller.ClearAll ();
			} else {
				((ObjectValueTreeView) exceptionValueTreeView).ClearAll ();
			}

			if (!ex.IsEvaluating && ex.Instance != null) {
				var opts = DebuggingService.GetUserOptions ().EvaluationOptions.Clone ();
				opts.FlattenHierarchy = true;

				var values = ex.Instance.GetAllChildren (opts);

				if (UseNewTreeView) {
					controller.AddValues (values);
				} else {
					((ObjectValueTreeView) exceptionValueTreeView).AddValues (values);
				}
			}

			if (ex.StackIsEvaluating) {
				var str = GettextCatalog.GetString ("Loading...");
				model.AppendValues (null, str, false);
			}

			if (innerExceptionTypeLabel != null) {
				innerExceptionTypeLabel.Markup = "<b>" + GLib.Markup.EscapeText (ex.Type) + "</b>";
				innerExceptionMessageLabel.Text = ex.Message;
				if (!string.IsNullOrEmpty (ex.HelpLink)) {
					innerExceptionHelpLinkButton.Label = GettextCatalog.GetString ("Read More…");
					innerExceptionHelpLink = ex.HelpLink;
					innerExceptionHelpLinkButton.Show ();
				} else {
					innerExceptionHelpLink = string.Empty;
					innerExceptionHelpLinkButton.Hide ();
				}
			}
		}

		void UpdateDisplay ()
		{
			if (destroyed)
				return;

			exceptionTypeLabel.Text = exception.Type;
			exceptionMessageLabel.Text = exception.Message ?? string.Empty;
			if (!string.IsNullOrEmpty (exception.HelpLink)) {
				helpLinkButton.Show ();
				exceptionHelpLink = exception.HelpLink;
				helpLinkButton.Label = GettextCatalog.GetString ("More information");
			} else {
				helpLinkButton.Hide ();
			}

			UpdateSelectedException (exception);
		}

		void ExceptionChanged (object sender, EventArgs e)
		{
			Application.Invoke ((o, args) => {
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

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				this.Destroy ();
				return true;
			}
			return base.OnKeyPressEvent (evnt);
		}
	}

	class StackFrameCellRenderer : CellRendererText
	{
		const int Padding = 6;

		public readonly Pango.Context Context;
		ExceptionStackFrame frame;
		public bool IsUserCode;
		public new string Markup;
		Pango.FontDescription font = Pango.FontDescription.FromString (Platform.IsWindows ? "9" : "11");

		public ExceptionStackFrame Frame {
			get { return frame; }
			set {
				frame = value;
				Text = value?.DisplayText;
			}
		}

		public StackFrameCellRenderer (Pango.Context ctx)
		{
			Context = ctx;
		}

		string GetMethodMarkup (bool selected, string foregroundColor)
		{
			if (Markup != null)
				return $"<span foreground='{Styles.ExceptionCaughtDialog.ExternalCodeTextColor.ToHexString (false)}'>{Markup}</span>";

            if (Frame == null)
                return "";

			var methodText = Frame.DisplayText;
			var endOfMethodName = methodText.IndexOf ('(');
			var methodName = methodText.Remove (endOfMethodName).Trim ();
			var endOfParameters = methodText.IndexOf (')') + 1;
			var parameters = methodText.Substring (endOfMethodName, endOfParameters - endOfMethodName).Trim ();

			var markup = $"<b>{GLib.Markup.EscapeText (methodName)}</b> {GLib.Markup.EscapeText (parameters)}";

			if (string.IsNullOrEmpty (foregroundColor)) {
				return markup;
			}
			return $"<span foreground='{foregroundColor}'>{markup}</span>";
		}

		string GetFileMarkup (bool selected, string foregroundColor)
		{
			if (Frame == null || string.IsNullOrEmpty (Frame.File)) {
				return "";
			}

			var markup = GLib.Markup.EscapeText (Path.GetFileName (Frame.File));
			if (Frame.Line > 0) {
				markup += ":" + Frame.Line;
				if (Frame.Column > 0)
					markup += "," + Frame.Column;
			}
			if (string.IsNullOrEmpty (foregroundColor)) {
				return markup;
			}
			return $"<span foreground='{foregroundColor}'>{markup}</span>";
		}

		public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			using (var layout = new Pango.Layout (Context)) {
				Pango.Rectangle ink, logical;
				layout.FontDescription = font;

				var selected = false;
				var foregroundColor = Styles.GetStackFrameForegroundHexColor (selected, IsUserCode);

				layout.SetMarkup (GetMethodMarkup (selected, foregroundColor));
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
				if (!widget.HasFocus) {
					cr.Rectangle (background_area.ToCairoRect ());
					cr.SetSourceColor (Styles.ObjectValueTreeDisabledBackgroundColor);
					cr.Fill ();
				}

				Pango.Rectangle ink, logical;
				using (var layout = new Pango.Layout (Context)) {
					layout.FontDescription = font;

					var selected = (flags & CellRendererState.Selected) != 0;
					var foregroundColor = Styles.GetStackFrameForegroundHexColor (selected, IsUserCode);

					layout.SetMarkup (GetFileMarkup (selected, foregroundColor));
					layout.GetPixelExtents (out ink, out logical);
					var width = widget.Allocation.Width;
					cr.Translate (width - logical.Width - 10, cell_area.Y);
					cr.ShowLayout (layout);

					cr.IdentityMatrix ();

					layout.SetMarkup (GetMethodMarkup (selected, foregroundColor));
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
				dialog.Destroyed += Dialog_Destroyed;
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
				IdeServices.TextEditorService.RegisterExtension (button);
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
				IdeServices.TextEditorService.RegisterExtension (miniButton);
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
		internal readonly ExceptionCaughtMessage dlg;
		internal readonly ExceptionInfo exception;
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
				Application.Invoke ((o, args) => {
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
		internal readonly ExceptionCaughtMessage dlg;

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
			if (DebuggingService.ExceptionCaughtMessage != null &&
				!DebuggingService.ExceptionCaughtMessage.IsMinimized &&
				DebuggingService.ExceptionCaughtMessage.File.CanonicalPath == new FilePath (DocumentContext.Name).CanonicalPath) {

				if (descriptor.SpecialKey == SpecialKey.Escape) {
					DebuggingService.ExceptionCaughtMessage.ShowMiniButton ();
					return true;
				}

				if (descriptor.SpecialKey == SpecialKey.Return) {
					DebuggingService.ExceptionCaughtMessage.ShowDialog ();
					return false;
				}
			}

			return base.KeyPress (descriptor);
		}
	}
}

