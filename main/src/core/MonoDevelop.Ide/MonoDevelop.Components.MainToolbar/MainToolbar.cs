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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide.NavigateToDialog;
using MonoDevelop.Projects;
using System.Collections.Generic;


namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbar: Gtk.EventBox, ICommandDelegatorRouter
	{
		HBox contentBox = new HBox (false, 6);

		ComboBox configurationCombo;
		TreeStore configurationStore = new TreeStore (typeof(string), typeof(string));

		ComboBox runtimeCombo;
		TreeStore runtimeStore = new TreeStore (typeof(string), typeof(string));

		StatusArea statusArea;

		SearchEntry matchEntry;
		SearchEntryBorder searchEntryComboVBox;

		ButtonBar buttonBar = new ButtonBar ();
		RoundButton button = new RoundButton ();

		public Cairo.ImageSurface Background {
			get;
			set;
		}

		public int TitleBarHeight {
			get;
			set;
		}

		public Cairo.Color BottomColor {
			get;
			set;
		}

		public ButtonBar ButtonBar {
			get {
				return buttonBar;
			}
		}

		public RoundButton StartButton {
			get {
				return button;
			}
		}

		bool SearchForMembers {
			get {
				return PropertyService.Get ("MainToolbar.Search.IncludeMembers", true);
			}
			set {
				PropertyService.Set ("MainToolbar.Search.IncludeMembers", value);
			}
		}

		/*
		internal class SelectActiveRuntimeHandler : CommandHandler
		{
			protected override void Update (CommandArrayInfo info)
			{
			}
	
			protected override void Run (object dataItem)
			{
			}
		}
		*/
		public MainToolbar ()
		{
			IdeApp.Workspace.ActiveConfigurationChanged += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.ActiveRuntimeChanged += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.ConfigurationsChanged += (sender, e) => UpdateCombos ();

			IdeApp.Workspace.SolutionLoaded += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.SolutionUnloaded += (sender, e) => UpdateCombos ();
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			contentBox.BorderWidth = 6;

			IdeApp.ProjectOperations.CurrentRunOperationChanged += delegate {
				button.QueueDraw ();
				if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted)
					IdeApp.ProjectOperations.CurrentRunOperation.Completed += (op) => button.QueueDraw ();
			};

			IdeApp.Workspace.WorkspaceItemOpened += delegate {
				button.Sensitive = true;
			};
			IdeApp.Workspace.WorkspaceItemClosed += delegate {
				button.Sensitive = false;
			};
			button.Sensitive = false;

			AddWidget (button);

			configurationCombo = new Gtk.ComboBox ();
			configurationCombo.SetSizeRequest (150, -1);
			configurationCombo.Model = configurationStore;
			var ctx = new Gtk.CellRendererText ();
			configurationCombo.PackStart (ctx, true);
			configurationCombo.AddAttribute (ctx, "text", 0);

			var configurationComboVBox = new VBox ();
			configurationComboVBox.PackStart (configurationCombo, true, false, 0);
			AddWidget (configurationComboVBox);

			runtimeCombo = new Gtk.ComboBox ();
			runtimeCombo.SetSizeRequest (150, -1);
			runtimeCombo.Model = runtimeStore;
			runtimeCombo.PackStart (ctx, true);
			runtimeCombo.AddAttribute (ctx, "text", 0);

			var runtimeComboVBox = new VBox ();
			runtimeComboVBox.PackStart (runtimeCombo, true, false, 0);
			AddWidget (runtimeComboVBox);



			buttonBar.Add (new LazyImage ("icoDebug-Pause.png"));
			buttonBar.Add (new LazyImage ("icoDebug-StepOver.png"));
			buttonBar.Add (new LazyImage ("icoDebug-StepIn.png"));
			buttonBar.Add (new LazyImage ("icoDebug-StepOut.png"));

			var buttonBarVBox = new VBox ();
			buttonBarVBox.PackStart (buttonBar, true, false, 0);
			AddWidget (buttonBarVBox);

			statusArea = new StatusArea ();

			var statusAreaVBox = new VBox ();
			statusAreaVBox.PackStart (statusArea, true, false, 0);

			contentBox.PackStart (statusAreaVBox, true, true, 0);

			matchEntry = new SearchEntry ();

			CheckMenuItem includeMembers = this.matchEntry.AddFilterOption (2, GettextCatalog.GetString ("Include _Members"));
			includeMembers.DrawAsRadio = false;
			includeMembers.Active = SearchForMembers;
			includeMembers.Toggled += delegate {
				SearchForMembers = !SearchForMembers;
				if (popup != null) {
					string entry = matchEntry.Entry.Text;
					popup.Destroy ();
					popup = null;
					matchEntry.Entry.Text = entry;
				}
			};

			matchEntry.VisibleWindow = false;
			matchEntry.ForceFilterButtonVisible = true;

			var info = IdeApp.CommandService.GetCommand (MonoDevelop.Ide.NavigateToDialog.Commands.NavigateTo);

			if (info != null && !string.IsNullOrEmpty (info.AccelKey)) {
				matchEntry.EmptyMessage = GettextCatalog.GetString ("Press {0} for search.", KeyBindingManager.BindingToDisplayLabel (info.AccelKey, false));
			} else {
				matchEntry.EmptyMessage = GettextCatalog.GetString ("Enter text to search.");
			}
			matchEntry.Ready = true;
			matchEntry.Visible = true;
			matchEntry.IsCheckMenu = true;
			matchEntry.Entry.ModifyBase (StateType.Normal, Style.White);
			matchEntry.SetSizeRequest (240, 26);
			matchEntry.Entry.Changed += HandleSearchEntryChanged;
			matchEntry.SizeAllocated += (o, args) => PositionPopup ();
			matchEntry.Activated += (sender, e) => {
				if (popup != null)
					popup.OpenFile ();
			};
			matchEntry.Entry.KeyPressEvent += (o, args) => {
				if (args.Event.Key == Gdk.Key.Escape) {
					var doc = IdeApp.Workbench.ActiveDocument;
					if (doc != null) {
						if (popup != null)
							popup.Destroy ();
						doc.Select ();
					}
					return;
				}
				if (popup != null) {
					args.RetVal = popup.ProcessKey (args.Event.Key, args.Event.State);
				}
			};
			IdeApp.Workbench.RootWindow.WidgetEvent += delegate(object o, WidgetEventArgs args) {
				if (args.Event is Gdk.EventConfigure)
					PositionPopup ();
			};

			searchEntryComboVBox = new SearchEntryBorder ();
			searchEntryComboVBox.PackStart (this.matchEntry, true, false, SearchEntryBorder.Height / 2);
			AddWidget (searchEntryComboVBox);

			Add (contentBox);
			UpdateCombos ();
		}

		SearchPopupWindow popup = null;
		void HandleSearchEntryChanged (object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty (matchEntry.Entry.Text)){
				if (popup != null)
					popup.Destroy ();
				return;
			}
			if (popup == null) {
				popup = new SearchPopupWindow ();
				popup.SearchForMembers = SearchForMembers;
				popup.Destroyed += delegate {
					popup = null;
					matchEntry.Entry.Text = "";
				};
				popup.SizeAllocated += delegate {
					PositionPopup ();
				};
				PositionPopup ();
				popup.ShowAll ();
			}
			popup.Update (matchEntry.Entry.Text);

		}

		void PositionPopup ()
		{
			if (popup == null)
				return;
			popup.ShowPopup (searchEntryComboVBox, PopupPosition.TopRight);
		}

		void HandleRuntimeChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!runtimeStore.GetIterFromString (out iter, runtimeCombo.Active.ToString ()))
				return;
			var runtime = (string)runtimeStore.GetValue (iter, 1);
			string activeName;
			string platform;
			ItemConfiguration.ParseConfigurationId (IdeApp.Workspace.ActiveConfigurationId, out activeName, out platform);
			
			foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
				string name;
				ItemConfiguration.ParseConfigurationId (conf, out name, out platform);
				if (activeName == name && platform == runtime) {
					IdeApp.Workspace.ActiveConfigurationId = conf;
					SelectActiveConfiguration ();
					break;
				}
			}
		}

		void HandleConfigurationChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!configurationStore.GetIterFromString (out iter, configurationCombo.Active.ToString ()))
				return;
			var config = (string)configurationStore.GetValue (iter, 1);
			foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
				string name;
				string platform;
				ItemConfiguration.ParseConfigurationId (conf, out name, out platform);
				if (name == config) {
					IdeApp.Workspace.ActiveConfigurationId = conf;
					break;
				}
			}
		}

		void SelectActiveConfiguration ()
		{
			UpdateRuntimes ();
			configurationCombo.Changed -= HandleConfigurationChanged;
			runtimeCombo.Changed -= HandleRuntimeChanged;
			string name;
			string platform;
			ItemConfiguration.ParseConfigurationId (IdeApp.Workspace.ActiveConfigurationId, out name, out platform);
			int i = 0;
			Gtk.TreeIter iter;
			if (configurationStore.GetIterFirst (out iter)) {
				do {
					string val = (string)configurationStore.GetValue (iter, 1);
					if (name == val) {
						configurationCombo.Active = i;
						break;
					}
					i++;
				}
				while (configurationStore.IterNext (ref iter));
			}

			i = 0;
			if (runtimeStore.GetIterFirst (out iter)) {
				do {
					string val = (string)runtimeStore.GetValue (iter, 1);
					if (platform == val || platform == null && string.IsNullOrEmpty (val)) {
						runtimeCombo.Active = i;
						break;
					}
					i++;
				}
				while (runtimeStore.IterNext (ref iter));
			}
			runtimeCombo.Changed += HandleRuntimeChanged;
			configurationCombo.Changed += HandleConfigurationChanged;
		}

		void UpdateCombos ()
		{
			configurationCombo.Changed -= HandleConfigurationChanged;
			configurationStore.Clear ();
			if (!IdeApp.Workspace.IsOpen)
				return;
			var values = new HashSet<string> ();
			foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
				string name;
				string platform;
				ItemConfiguration.ParseConfigurationId (conf, out name, out platform);
				if (values.Contains (name))
					continue;
				values.Add (name);
				configurationStore.AppendValues (name, name);
			}
			configurationCombo.Changed += HandleConfigurationChanged;

			SelectActiveConfiguration ();
		}

		void UpdateRuntimes ()
		{
			runtimeCombo.Changed -= HandleRuntimeChanged;
			runtimeStore.Clear ();
			if (!IdeApp.Workspace.IsOpen)
				return;

			string name;
			string platform;
			ItemConfiguration.ParseConfigurationId (IdeApp.Workspace.ActiveConfigurationId, out name, out platform);
			var values = new HashSet<string> ();
			foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
				string curName;
				ItemConfiguration.ParseConfigurationId (conf, out curName, out platform);
				if (curName != name)
					continue;
				if (platform == null)
					platform = "";
				if (values.Contains (platform))
					continue;
				values.Add (platform);
				runtimeStore.AppendValues (string.IsNullOrEmpty (platform) ? "Any CPU" : platform, platform);
			}

			runtimeCombo.Changed += HandleRuntimeChanged;
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
						lg.AddColorStop (0, (HslColor)Style.Light (StateType.Normal));
						lg.AddColorStop (1, (HslColor)Style.Mid (StateType.Normal));
						context.Pattern = lg;
					}
					context.Fill ();

				}
				context.MoveTo (0, Allocation.Bottom + 0.5);
				context.LineTo (Allocation.Width + evnt.Area.Width, Allocation.Bottom + 0.5);
				context.Color = BottomColor;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

		[CommandHandler(MonoDevelop.Ide.NavigateToDialog.Commands.NavigateTo)]
		public void NavigateToCommand ()
		{
			matchEntry.Entry.GrabFocus ();
		}

		#region ICommandDelegatorRouter implementation
		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			// This is the last object of the chain
			return null;
		}

		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			return null;
		}
		#endregion
	}
}

