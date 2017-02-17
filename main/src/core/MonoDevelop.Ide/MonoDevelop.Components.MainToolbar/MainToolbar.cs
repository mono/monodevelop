// 
// MainToolbar.cs
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
using Gtk;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Projects;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Components.Commands.ExtensionNodes;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.Editor;
using System.Text;

namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbar: Gtk.EventBox, IMainToolbarView
	{
		HBox contentBox = new HBox (false, 0);

		HBox configurationCombosBox;

		ComboBox configurationCombo;
		TreeStore configurationStore = new TreeStore (typeof(string), typeof(IConfigurationModel));

		ComboBox runConfigurationCombo;
		TreeStore runConfigurationStore = new TreeStore (typeof (string), typeof (IRunConfigurationModel));
	
		ComboBox runtimeCombo;
		TreeStore runtimeStore = new TreeStore (typeof(IRuntimeModel));

		StatusArea statusArea;

		SearchEntry matchEntry;
		static WeakReference lastCommandTarget;

		ButtonBar buttonBar = new ButtonBar ();
		RoundButton button = new RoundButton ();
		Alignment buttonBarBox;

		// attributes for the runtime combo (bold / normal)
		readonly Pango.AttrList boldAttributes = new Pango.AttrList ();
		readonly Pango.AttrList normalAttributes = new Pango.AttrList ();

		public Cairo.ImageSurface Background {
			get;
			set;
		}

		public int TitleBarHeight {
			get;
			set;
		}

		public MonoDevelop.Ide.StatusBar StatusBar {
			get {
				return statusArea;
			}
		}

		internal static object LastCommandTarget {
			get { return lastCommandTarget != null ? lastCommandTarget.Target : null; }
		}

		static bool RuntimeIsSeparator (TreeModel model, TreeIter iter)
		{
			var runtime = (IRuntimeModel)model.GetValue (iter, 0);
			return runtime == null || runtime.IsSeparator;
		}

		void RuntimeRenderCell (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var runtime = (IRuntimeModel)model.GetValue (iter, 0);
			var renderer = (CellRendererText) cell;

			if (runtime == null || runtime.IsSeparator) {
				renderer.Xpad = (uint)0;
				return;
			}

			using (var mutableModel = runtime.GetMutableModel ()) {
				renderer.Visible = mutableModel.Visible;
				renderer.Sensitive = mutableModel.Enabled;
				renderer.Xpad = (uint)(runtime.IsIndented ? 18 : 3);

				if (!runtimeCombo.PopupShown) {
					// no need to ident text when the combo dropdown is not showing
					if (Platform.IsWindows)
						renderer.Xpad = 3;
					renderer.Text = mutableModel.FullDisplayString;
					renderer.Attributes = normalAttributes;
				} else {
					renderer.Text = mutableModel.DisplayString;
					renderer.Attributes = runtime.Notable ? boldAttributes : normalAttributes;
				}

			}
		}

		TreeIter lastSelection = TreeIter.Zero;
		public MainToolbar ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;

			AddWidget (button);
			AddSpace (8);

			configurationCombosBox = new HBox (false, 8);

			var ctx = new Gtk.CellRendererText ();

			runConfigurationCombo = new Gtk.ComboBox ();
			runConfigurationCombo.Model = runConfigurationStore;
			runConfigurationCombo.PackStart (ctx, true);
			runConfigurationCombo.AddAttribute (ctx, "text", 0);

			var runConfigurationComboVBox = new VBox ();
			runConfigurationComboVBox.PackStart (runConfigurationCombo, true, false, 0);
			configurationCombosBox.PackStart (runConfigurationComboVBox, false, false, 0);
		
			configurationCombo = new Gtk.ComboBox ();
			configurationCombo.Model = configurationStore;
			configurationCombo.PackStart (ctx, true);
			configurationCombo.AddAttribute (ctx, "text", 0);
		
			var configurationComboVBox = new VBox ();
			configurationComboVBox.PackStart (configurationCombo, true, false, 0);
			configurationCombosBox.PackStart (configurationComboVBox, false, false, 0);

			// bold attributes for running runtime targets / (emulators)
			boldAttributes.Insert (new Pango.AttrWeight (Pango.Weight.Bold));

			runtimeCombo = new Gtk.ComboBox ();
			runtimeCombo.Model = runtimeStore;
			ctx = new Gtk.CellRendererText ();
			if (Platform.IsWindows)
				ctx.Ellipsize = Pango.EllipsizeMode.Middle;
			runtimeCombo.PackStart (ctx, true);
			runtimeCombo.SetCellDataFunc (ctx, RuntimeRenderCell);
			runtimeCombo.RowSeparatorFunc = RuntimeIsSeparator;

			var runtimeComboVBox = new VBox ();
			runtimeComboVBox.PackStart (runtimeCombo, true, false, 0);
			configurationCombosBox.PackStart (runtimeComboVBox, false, false, 0);
			AddWidget (configurationCombosBox);

			buttonBarBox = new Alignment (0.5f, 0.5f, 0, 0);
			buttonBarBox.LeftPadding = (uint) 7;
			buttonBarBox.Add (buttonBar);
			buttonBarBox.NoShowAll = true;
			AddWidget (buttonBarBox);
			AddSpace (24);

			statusArea = new StatusArea ();
			statusArea.ShowMessage (BrandingService.ApplicationName);

			var statusAreaAlign = new Alignment (0, 0, 1, 1);
			statusAreaAlign.Add (statusArea);
			contentBox.PackStart (statusAreaAlign, true, true, 0);
			AddSpace (24);

			statusAreaAlign.SizeAllocated += (object o, SizeAllocatedArgs args) => {
				Gtk.Widget toplevel = this.Toplevel;
				if (toplevel == null)
					return;

				int windowWidth = toplevel.Allocation.Width;
				int center = windowWidth / 2;
				int left = Math.Max (center - 300, args.Allocation.Left);
				int right = Math.Min (left + 600, args.Allocation.Right);
				uint left_padding = (uint) (left - args.Allocation.Left);
				uint right_padding = (uint) (args.Allocation.Right - right);

				if (left_padding != statusAreaAlign.LeftPadding || right_padding != statusAreaAlign.RightPadding)
					statusAreaAlign.SetPadding (0, 0, (uint) left_padding, (uint) right_padding);
			};

			matchEntry = new SearchEntry ();

			matchEntry.ForceFilterButtonVisible = true;
			matchEntry.Entry.FocusOutEvent += (o, e) => {
				if (SearchEntryLostFocus != null)
					SearchEntryLostFocus (o, e);
			};

			matchEntry.Ready = true;
			matchEntry.Visible = true;
			matchEntry.IsCheckMenu = true;
			matchEntry.WidthRequest = 240;
			if (!Platform.IsMac && !Platform.IsWindows)
				matchEntry.Entry.ModifyFont (Pango.FontDescription.FromString ("Sans 9")); // TODO: VV: "Segoe UI 9"
			matchEntry.RoundedShape = true;
			matchEntry.Entry.Changed += HandleSearchEntryChanged;
			matchEntry.Activated += HandleSearchEntryActivated;
			matchEntry.Entry.KeyPressEvent += HandleSearchEntryKeyPressed;
			SizeAllocated += (o, e) => {
				if (SearchEntryResized != null)
					SearchEntryResized (o, e);
			};

			contentBox.PackStart (matchEntry, false, false, 0);

			var align = new Gtk.Alignment (0, 0, 1f, 1f);
			align.Show ();
			align.TopPadding = (uint) 5;
			align.LeftPadding = (uint) 9;
			align.RightPadding = (uint) 18;
			align.BottomPadding = (uint) 10;
			align.Add (contentBox);

			Add (align);
			SetDefaultSizes (-1, 21);

			configurationCombo.Changed += (o, e) => {
				if (ConfigurationChanged != null)
					ConfigurationChanged (o, e);
			};
			runConfigurationCombo.Changed += (o, e) => {
				if (RunConfigurationChanged != null)
					RunConfigurationChanged (o, e);
			};
			runtimeCombo.Changed += (o, e) => {
				var ea = new HandledEventArgs ();
				if (RuntimeChanged != null)
					RuntimeChanged (o, ea);

				TreeIter it;
				if (runtimeCombo.GetActiveIter (out it)) {
					if (ea.Handled) {
						runtimeCombo.SetActiveIter (lastSelection);
						return;
					}
					lastSelection = it;
				}
			};

			button.Clicked += HandleStartButtonClicked;

			IdeApp.CommandService.ActiveWidgetChanged += (sender, e) => {
				lastCommandTarget = new WeakReference (e.OldActiveWidget);
			};

			this.ShowAll ();
			this.statusArea.statusIconBox.HideAll ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && evnt.Window == GdkWindow) {
				var window = (Gtk.Window)Toplevel;
				if (!DesktopService.GetIsFullscreen (window)) {
					window.BeginMoveDrag (1, (int)evnt.XRoot, (int)evnt.YRoot, evnt.Time);
					return true;
				}
			}
			return base.OnButtonPressEvent (evnt);
		}

		void SetDefaultSizes (int comboHeight, int height)
		{
			configurationCombo.SetSizeRequest (150, comboHeight);
			runConfigurationCombo.SetSizeRequest (150, comboHeight);
			// make the windows runtime slightly wider to accomodate select devices text
			runtimeCombo.SetSizeRequest (Platform.IsWindows ? 175 : 150, comboHeight);
			statusArea.SetSizeRequest (32, 32);
			matchEntry.HeightRequest = height + 4;
			buttonBar.HeightRequest = height + 2;
		}

		void AddSpace (int w)
		{
			Label la = new Label ("");
			la.WidthRequest = w;
			la.Show ();
			contentBox.PackStart (la, false, false, 0);
		}

		void HandleSearchEntryChanged (object sender, EventArgs e)
		{
			SearchEntryChanged?.Invoke (sender, e);
		}

		void HandleSearchEntryActivated (object sender, EventArgs e)
		{
			if (SearchEntryActivated != null)
				SearchEntryActivated (sender, e);
		}

		void HandleSearchEntryKeyPressed (object sender, KeyPressEventArgs e)
		{
			if (SearchEntryKeyPressed != null) {
				// TODO: Refactor this in Xwt as an extension method.
				var k = (Xwt.Key)e.Event.KeyValue;
				var m = Xwt.ModifierKeys.None;
				if ((e.Event.State & Gdk.ModifierType.ShiftMask) != 0)
					m |= Xwt.ModifierKeys.Shift;
				if ((e.Event.State & Gdk.ModifierType.ControlMask) != 0)
					m |= Xwt.ModifierKeys.Control;
				if ((e.Event.State & Gdk.ModifierType.Mod1Mask) != 0)
					m |= Xwt.ModifierKeys.Alt;
				// TODO: Backport this one.
				if ((e.Event.State & Gdk.ModifierType.Mod2Mask) != 0)
					m |= Xwt.ModifierKeys.Command;
				var kargs = new Xwt.KeyEventArgs (k, m, false, (long)e.Event.Time);
				SearchEntryKeyPressed (sender, kargs);
				e.RetVal = kargs.Handled;
			}
		}

		public void AddWidget (Gtk.Widget widget)
		{
			contentBox.PackStart (widget, false, false, 0);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Rectangle (
					evnt.Area.X,
					evnt.Area.Y,
					evnt.Area.Width,
					evnt.Area.Height
				);
				context.Clip ();
				context.LineWidth = 1;
				if (Background != null && Background.Width > 0) {
					for (int x=0; x < Allocation.Width; x += Background.Width) {
						Background.Show (context, x, -TitleBarHeight);
					}
				} else {
					context.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					using (var lg = new LinearGradient (0, 0, 0, Allocation.Height)) {
						lg.AddColorStop (0, Style.Light (StateType.Normal).ToCairoColor ());
						lg.AddColorStop (1, Style.Mid (StateType.Normal).ToCairoColor ());
						context.SetSource (lg);
					}
					context.Fill ();

				}
				context.MoveTo (0, Allocation.Height - 0.5);
				context.RelLineTo (Allocation.Width, 0);
				context.SetSourceColor (Styles.ToolbarBottomBorderColor.ToCairoColor ());
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

		void HandleStartButtonClicked (object sender, EventArgs e)
		{
			if (RunButtonClicked != null)
				RunButtonClicked (sender, e);
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			if (button != null)
				button.Clicked -= HandleStartButtonClicked;

			if (Background != null) {
				((IDisposable)Background).Dispose ();
				Background = null;
			}
		}

		#region IMainToolbarView implementation
		public event EventHandler RunButtonClicked;

		public bool RunButtonSensitivity {
			get { return button.Sensitive; }
			set { button.Sensitive = value; }
		}

		public OperationIcon RunButtonIcon {
			set { button.Icon = value; }
		}

		public bool ConfigurationPlatformSensitivity {
			get { return configurationCombosBox.Sensitive; }
			set { configurationCombosBox.Sensitive = value; }
		}

		static bool FindIter<T> (TreeStore store, Func<T, bool> match, out TreeIter iter)
		{
			if (store.GetIterFirst (out iter)) {
				do {
					if (match((T)store.GetValue (iter, 1)))
						return true;
				} while (store.IterNext (ref iter));
			}
			return false;
		}

		public IConfigurationModel ActiveConfiguration {
			get {
				TreeIter iter;
				if (!configurationCombo.GetActiveIter (out iter))
					return null;

				return (IConfigurationModel) configurationStore.GetValue (iter, 1);
			}
			set {
				TreeIter iter;
				if (FindIter<IConfigurationModel> (configurationStore, it => value.OriginalId == it.OriginalId, out iter))
					configurationCombo.SetActiveIter (iter);
				else
					configurationCombo.Active = 0;
			}
		}

		public IRunConfigurationModel ActiveRunConfiguration {
			get {
				TreeIter iter;
				if (!runConfigurationCombo.GetActiveIter (out iter))
					return null;

				return (IRunConfigurationModel)runConfigurationStore.GetValue (iter, 1);
			}
			set {
				TreeIter iter;
				if (FindIter<IRunConfigurationModel> (runConfigurationStore, it => value.OriginalId == it.OriginalId, out iter))
					runConfigurationCombo.SetActiveIter (iter);
				else
					runConfigurationCombo.Active = 0;
			}
		}

		public IRuntimeModel ActiveRuntime {
			get {
				TreeIter iter;
				if (!runtimeCombo.GetActiveIter (out iter))
					return null;

				return (IRuntimeModel)runtimeStore.GetValue (iter, 0);
			}
			set {
				TreeIter iter;
				bool found = false;
				if (runtimeStore.GetIterFirst (out iter)) {
					do {
						if (value == runtimeStore.GetValue (iter, 0)) {
							found = true;
							break;
						}
					} while (runtimeStore.IterNext (ref iter));
				}
				if (found)
					runtimeCombo.SetActiveIter (iter);
				else
					runtimeCombo.Active = 0;
			}
		}

		public bool PlatformSensitivity {
			set { runtimeCombo.Sensitive = value; }
		}

		IEnumerable<IConfigurationModel> configurationModel;
		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get { return configurationModel; }
			set {
				configurationModel = value;
				configurationStore.Clear ();
				foreach (var item in value) {
					configurationStore.AppendValues (item.DisplayString, item);
				}
			}
		}

		IEnumerable<IRunConfigurationModel> runConfigurationModel;
		public IEnumerable<IRunConfigurationModel> RunConfigurationModel {
			get { return runConfigurationModel; }
			set {
				runConfigurationModel = value;
				runConfigurationStore.Clear ();
				foreach (var item in value) {
					runConfigurationStore.AppendValues (item.DisplayString, item);
				}
			}
		}

		IEnumerable<IRuntimeModel> runtimeModel;
		public IEnumerable<IRuntimeModel> RuntimeModel {
			get { return runtimeModel; }
			set {
				runtimeModel = value;
				runtimeStore.Clear ();
				foreach (var item in value) {
					var iter = runtimeStore.AppendValues (item);
					foreach (var subitem in item.Children)
						runtimeStore.AppendValues (iter, subitem);
				}
			}
		}

		public bool RunConfigurationVisible {
			get { return runConfigurationCombo.Visible; }
			set { runConfigurationCombo.Visible = value; }
		}

		public bool SearchSensivitity {
			set { matchEntry.Sensitive = value; }
		}

		public IEnumerable<ISearchMenuModel> SearchMenuItems {
			set {
				foreach (var item in value) {
					var menuItem = matchEntry.AddMenuItem (item.DisplayString);
					menuItem.Activated += delegate {
						item.NotifyActivated ();
					};
				}
			}
		}

		public string SearchCategory {
			set {
				matchEntry.Entry.Text = value;
				matchEntry.Entry.GrabFocus ();
				var pos = matchEntry.Entry.Text.Length;
				matchEntry.Entry.SelectRegion (pos, pos);
			}
		}

		public void FocusSearchBar ()
		{
			matchEntry.Entry.GrabFocus ();
		}

		public event EventHandler SearchEntryChanged;
		public event EventHandler SearchEntryActivated;
		public event EventHandler<Xwt.KeyEventArgs> SearchEntryKeyPressed;
		public event EventHandler SearchEntryResized;
		public event EventHandler SearchEntryLostFocus;

		public Widget PopupAnchor {
			get { return matchEntry; }
		}

		public string SearchText {
			get { return matchEntry.Entry.Text; }
			set { matchEntry.Entry.Text = value; }
		}

		public string SearchPlaceholderMessage {
			set { matchEntry.EmptyMessage = value; }
		}

		public void RebuildToolbar (IEnumerable<ButtonBarGroup> groups)
		{
			if (!groups.Any ()) {
				buttonBarBox.Hide ();
				return;
			}

			buttonBarBox.Show ();
			buttonBar.ShowAll ();
			buttonBar.Groups = groups;
		}

		public bool ButtonBarSensitivity {
			set { buttonBar.Sensitive = value; }
		}

		public event EventHandler ConfigurationChanged;
		public event EventHandler RunConfigurationChanged;
		public event EventHandler<HandledEventArgs> RuntimeChanged;

		#endregion
	}
}

