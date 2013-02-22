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
using MonoDevelop.Projects;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Components.Commands.ExtensionNodes;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;


namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbar: Gtk.EventBox, ICommandBar
	{
		const string ToolbarExtensionPath = "/MonoDevelop/Ide/CommandBar";

		HBox contentBox = new HBox (false, 0);

		HBox configurationCombosBox;

		ComboBox configurationCombo;
		TreeStore configurationStore = new TreeStore (typeof(string), typeof(string));

		ComboBox runtimeCombo;
		TreeStore runtimeStore = new TreeStore (typeof(string), typeof(string), typeof (ExecutionTarget));

		StatusArea statusArea;

		SearchEntry matchEntry;

		ButtonBar buttonBar = new ButtonBar ();
		RoundButton button = new RoundButton ();
		Alignment buttonBarBox;

		HashSet<string> visibleBars = new HashSet<string> ();

		ConfigurationMerger configurationMerger = new ConfigurationMerger ();

		Solution currentSolution;
		bool settingGlobalConfig;
		SolutionEntityItem currentStartupProject;

		public Cairo.ImageSurface Background {
			get;
			set;
		}

		public int TitleBarHeight {
			get;
			set;
		}

		bool SearchForMembers {
			get {
				return PropertyService.Get ("MainToolbar.Search.IncludeMembers", true);
			}
			set {
				PropertyService.Set ("MainToolbar.Search.IncludeMembers", value);
			}
		}

		public MonoDevelop.Ide.StatusBar StatusBar {
			get {
				return statusArea;
			}
		}

		void SetSearchCategory (string category)
		{
			matchEntry.Entry.Text = category +":";
			matchEntry.Entry.GrabFocus ();
			var pos = matchEntry.Entry.Text.Length;
			matchEntry.Entry.SelectRegion (pos, pos);
		}

		public MainToolbar ()
		{
			IdeApp.Workspace.ActiveConfigurationChanged += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.ConfigurationsChanged += (sender, e) => UpdateCombos ();

			IdeApp.Workspace.SolutionLoaded += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.SolutionUnloaded += (sender, e) => UpdateCombos ();

			IdeApp.ProjectOperations.CurrentSelectedSolutionChanged += HandleCurrentSelectedSolutionChanged;


			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;

			AddWidget (button);
			AddSpace (8);

			configurationCombo = new Gtk.ComboBox ();
			configurationCombo.Model = configurationStore;
			var ctx = new Gtk.CellRendererText ();
			configurationCombo.PackStart (ctx, true);
			configurationCombo.AddAttribute (ctx, "text", 0);

			configurationCombosBox = new HBox (false, 8);

			var configurationComboVBox = new VBox ();
			configurationComboVBox.PackStart (configurationCombo, true, false, 0);
			configurationCombosBox.PackStart (configurationComboVBox, false, false, 0);

			runtimeCombo = new Gtk.ComboBox ();
			runtimeCombo.Model = runtimeStore;
			runtimeCombo.PackStart (ctx, true);
			runtimeCombo.AddAttribute (ctx, "text", 0);

			var runtimeComboVBox = new VBox ();
			runtimeComboVBox.PackStart (runtimeCombo, true, false, 0);
			configurationCombosBox.PackStart (runtimeComboVBox, false, false, 0);
			AddWidget (configurationCombosBox);

			buttonBarBox = new Alignment (0.5f, 0.5f, 0, 0);
			buttonBarBox.LeftPadding = 7;
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

			var searchFiles = this.matchEntry.AddMenuItem (GettextCatalog.GetString ("Search Files"));
			searchFiles.Activated += delegate {
				SetSearchCategory ("files");
			};
			var searchTypes = this.matchEntry.AddMenuItem (GettextCatalog.GetString ("Search Types"));
			searchTypes.Activated += delegate {
				SetSearchCategory ("type");
			};
			var searchMembers = this.matchEntry.AddMenuItem (GettextCatalog.GetString ("Search Members"));
			searchMembers.Activated += delegate {
				SetSearchCategory ("member");
			};

			matchEntry.ForceFilterButtonVisible = true;
			matchEntry.Entry.FocusOutEvent += delegate {
				matchEntry.Entry.Text = "";
			};
			var cmd = IdeApp.CommandService.GetCommand (Commands.NavigateTo);
			cmd.KeyBindingChanged += delegate {
				UpdateSearchEntryLabel ();
			};
			UpdateSearchEntryLabel ();

			matchEntry.Ready = true;
			matchEntry.Visible = true;
			matchEntry.IsCheckMenu = true;
			matchEntry.Entry.ModifyBase (StateType.Normal, Style.White);
			matchEntry.WidthRequest = 240;
			matchEntry.RoundedShape = true;
			matchEntry.Entry.Changed += HandleSearchEntryChanged;
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

			BuildToolbar ();
			IdeApp.CommandService.RegisterCommandBar (buttonBar);

			AddinManager.ExtensionChanged += delegate(object sender, ExtensionEventArgs args) {
				if (args.PathChanged (ToolbarExtensionPath))
					BuildToolbar ();
			};

			contentBox.PackStart (matchEntry, false, false, 0);

			var align = new Gtk.Alignment (0, 0, 1f, 1f);
			align.Show ();
			align.TopPadding = 5;
			align.LeftPadding = 9;
			align.RightPadding = 18;
			align.BottomPadding = 10;
			align.Add (contentBox);

			Add (align);
			UpdateCombos ();

			button.Clicked += HandleStartButtonClicked;
			IdeApp.CommandService.RegisterCommandBar (this);
			this.ShowAll ();
			this.statusArea.statusIconBox.HideAll ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && evnt.Window == this.GdkWindow) {
				(Toplevel as Gtk.Window).BeginMoveDrag (1, (int)evnt.XRoot, (int)evnt.YRoot, evnt.Time);
				return true;
			}
			return base.OnButtonPressEvent (evnt);
		}

		void HandleCurrentSelectedSolutionChanged (object sender, SolutionEventArgs e)
		{
			if (currentSolution != null) {
				currentSolution.StartupItemChanged -= HandleStartupItemChanged;
				currentSolution.Saved -= HandleSolutionSaved;
			}

			currentSolution = IdeApp.ProjectOperations.CurrentSelectedSolution;

			if (currentSolution != null) {
				currentSolution.StartupItemChanged += HandleStartupItemChanged;
				currentSolution.Saved += HandleSolutionSaved;
			}

			TrackStartupProject ();

			UpdateCombos ();
		}

		void TrackStartupProject ()
		{
			if (currentStartupProject != null && ((currentSolution != null && currentStartupProject != currentSolution.StartupItem) || currentSolution == null)) {
				currentStartupProject.ExecutionTargetsChanged -= HandleExecutionTargetsChanged;
				currentStartupProject.Saved -= HandleProjectSaved;
			}

			if (currentSolution != null) {
				currentStartupProject = currentSolution.StartupItem;
				if (currentStartupProject != null) {
					currentStartupProject.ExecutionTargetsChanged += HandleExecutionTargetsChanged;
					currentStartupProject.Saved += HandleProjectSaved;
				}
			}
			else
				currentStartupProject = null;
		}

		void HandleProjectSaved (object sender, SolutionItemEventArgs e)
		{
			UpdateCombos ();
		}

		void HandleSolutionSaved (object sender, WorkspaceItemEventArgs e)
		{
			UpdateCombos ();
		}

		void HandleExecutionTargetsChanged (object sender, EventArgs e)
		{
			UpdateCombos ();
		}

		void HandleStartupItemChanged (object sender, EventArgs e)
		{
			TrackStartupProject ();
			UpdateCombos ();
		}

		void UpdateSearchEntryLabel ()
		{
			var info = IdeApp.CommandService.GetCommand (Commands.NavigateTo);
			if (!string.IsNullOrEmpty (info.AccelKey)) {
				matchEntry.EmptyMessage = GettextCatalog.GetString ("Press '{0}' to search", KeyBindingManager.BindingToDisplayLabel (info.AccelKey, false));
			} else {
				matchEntry.EmptyMessage = GettextCatalog.GetString ("Search solution");
			}
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateSize (-1, 21);
		}

		void UpdateSize (int comboHeight, int height)
		{
			configurationCombo.SetSizeRequest (150, comboHeight);
			runtimeCombo.SetSizeRequest (150, comboHeight);
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

		void BuildToolbar ()
		{
			buttonBar.Clear ();
			var bars = AddinManager.GetExtensionNodes (ToolbarExtensionPath).Cast<ItemSetCodon> ().Where (n => visibleBars.Contains (n.Id));
			if (!bars.Any ()) {
				buttonBarBox.Hide ();
				return;
			}

			buttonBarBox.Show ();
			buttonBar.ShowAll ();
			foreach (var bar in bars) {
				foreach (CommandItemCodon node in bar.ChildNodes.OfType<CommandItemCodon> ())
					buttonBar.Add (node.Id);
				buttonBar.AddSeparator ();
			}
		}

		public void ShowCommandBar (string barId)
		{
			visibleBars.Add (barId);
			BuildToolbar ();
		}

		public void HideCommandBar (string barId)
		{
			visibleBars.Remove (barId);
			BuildToolbar ();
		}

		SearchPopupWindow popup = null;

		public SearchPopupWindow SearchPopupWindow {
			get {
				return popup;
			}
		}
	
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
				PositionPopup ();
				popup.ShowAll ();
			}
			popup.Update (matchEntry.Entry.Text);

		}

		void PositionPopup ()
		{
			if (popup == null)
				return;
			popup.ShowPopup (matchEntry, PopupPosition.TopRight);
		}

		string GetActiveConfiguration ()
		{
			int active = configurationCombo.Active;
			if (active < 0)
				return null;

			TreeIter iter;
			if (!configurationStore.GetIterFromString (out iter, active.ToString ()))
				return null;
			return (string)configurationStore.GetValue (iter, 1);
		}

		ExecutionTarget GetActiveTarget ()
		{
			int active = runtimeCombo.Active;
			if (active < 0)
				return null;

			TreeIter iter;
			if (!runtimeStore.GetIterFromString (out iter, active.ToString ()))
				return null;
			return (ExecutionTarget)runtimeStore.GetValue (iter, 2);
		}

		void HandleRuntimeChanged (object sender, EventArgs e)
		{
			NotifyConfigurationChange ();
		}

		void HandleConfigurationChanged (object sender, EventArgs e)
		{
			NotifyConfigurationChange ();
		}

		void NotifyConfigurationChange ()
		{
			var currentConfig = GetActiveConfiguration ();
			if (currentConfig == null)
				return;

			string fullConfig;
			ExecutionTarget newTarget;

			configurationMerger.ResolveConfiguration (currentConfig, GetActiveTarget (), out fullConfig, out newTarget);

			settingGlobalConfig = true;
			try {
				IdeApp.Workspace.ActiveExecutionTarget = newTarget;
				IdeApp.Workspace.ActiveConfigurationId = fullConfig;
			} finally {
				settingGlobalConfig = false;
			}
			FillRuntimes ();
			SelectActiveRuntime ();
		}

		void SelectActiveConfiguration ()
		{
			configurationCombo.Changed -= HandleConfigurationChanged;
			string name = configurationMerger.GetUnresolvedConfiguration (IdeApp.Workspace.ActiveConfigurationId);
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
			var validTargets = configurationMerger.GetTargetsForConfiguration (IdeApp.Workspace.ActiveConfigurationId, false).ToArray ();
			if (IdeApp.Workspace.PreferredActiveExecutionTarget == null || !validTargets.Any (t => t.Id == IdeApp.Workspace.PreferredActiveExecutionTarget))
				IdeApp.Workspace.ActiveExecutionTarget = validTargets.FirstOrDefault ();

			configurationCombo.Changed += HandleConfigurationChanged;
			SelectActiveRuntime ();
		}

		void SelectActiveRuntime ()
		{
			runtimeCombo.Changed -= HandleRuntimeChanged;
			var i = 0;
			Gtk.TreeIter iter;
			if (runtimeStore.GetIterFirst (out iter)) {
				do {
					var val = (ExecutionTarget)runtimeStore.GetValue (iter, 2);
					if (val.Id == IdeApp.Workspace.PreferredActiveExecutionTarget) {
						runtimeCombo.Active = i;
						break;
					}
					i++;
				}
				while (runtimeStore.IterNext (ref iter));
			}
			if (runtimeCombo.Active == -1)
				runtimeCombo.Active = 0;
			runtimeCombo.Changed += HandleRuntimeChanged;
		}

		void UpdateCombos ()
		{
			if (settingGlobalConfig)
				return;

			configurationMerger.Load (currentSolution);

			configurationCombo.Changed -= HandleConfigurationChanged;
			try {
				configurationStore.Clear ();
				if (!IdeApp.Workspace.IsOpen) {
					runtimeStore.Clear ();
					return;
				}
				var values = new HashSet<string> ();
				foreach (var conf in configurationMerger.SolutionConfigurations) {
					if (!values.Add (conf))
						continue;
					values.Add (conf);
					configurationStore.AppendValues (conf.Replace ("|", " | "), conf);
				}
			} finally {
				configurationCombo.Changed += HandleConfigurationChanged;
			}

			FillRuntimes ();
			SelectActiveConfiguration ();
		}

		void FillRuntimes ()
		{
			runtimeCombo.Changed -= HandleRuntimeChanged;
			try {
				runtimeStore.Clear ();
				if (!IdeApp.Workspace.IsOpen || currentSolution == null || !currentSolution.SingleStartup || currentSolution.StartupItem == null)
					return;

				// Check that the current startup project is enabled for the current configuration
				var solConf = currentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				if (solConf == null || !solConf.BuildEnabledForItem (currentSolution.StartupItem))
					return;

				var targets = configurationMerger.GetTargetsForConfiguration (IdeApp.Workspace.ActiveConfigurationId, true);
				foreach (var target in targets)
					runtimeStore.AppendValues (target.Name, target.Name, target);
				runtimeCombo.Sensitive = targets.Count () > 1;
			} finally {
				runtimeCombo.Changed += HandleRuntimeChanged;
			}
		}

		IEnumerable<ExecutionTarget> GetExecutionTargets (string configuration)
		{
			var sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol == null || !sol.SingleStartup || sol.StartupItem == null)
				return new ExecutionTarget [0];
			var conf = sol.Configurations[configuration];
			if (conf == null)
				return new ExecutionTarget [0];

			var project = sol.StartupItem;
			var confSelector = conf.Selector;

			return project.GetExecutionTargets (confSelector);
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
				context.MoveTo (0, Allocation.Height - 0.5);
				context.RelLineTo (Allocation.Width, 0);
				context.Color = Styles.ToolbarBottomBorderColor;
				context.Stroke ();

				context.MoveTo (0, Allocation.Height - 1.5);
				context.RelLineTo (Allocation.Width, 0);
				context.Color = Styles.ToolbarBottomGlowColor;
				context.Stroke ();

			}
			return base.OnExposeEvent (evnt);
		}

		[CommandHandler(Commands.NavigateTo)]
		public void NavigateToCommand ()
		{
			matchEntry.Entry.GrabFocus ();
		}

		[CommandHandler(SearchCommands.GotoFile)]
		public void GotoFileCommand ()
		{
			SetSearchCategory ("file");
		}

		[CommandHandler(SearchCommands.GotoType)]
		public void GotoTypeCommand ()
		{
			SetSearchCategory ("type");
		}

		CommandInfo GetStartButtonCommandInfo (out RoundButton.OperationIcon operation)
		{
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted || !IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) {
				operation = RoundButton.OperationIcon.Stop;
				return IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.Stop);
			}
			else {
				operation = RoundButton.OperationIcon.Run;
				var ci = IdeApp.CommandService.GetCommandInfo ("MonoDevelop.Debugger.DebugCommands.Debug");
				if (!ci.Enabled || !ci.Visible) {
					// If debug is not enabled, try Run
					ci = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.Run);
					if (!ci.Enabled || !ci.Visible) {
						// Running is not possible, then allow building
						var bci = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.BuildSolution);
						if (bci.Enabled && bci.Visible) {
							operation = RoundButton.OperationIcon.Build;
							ci = bci;
						}
					}
				}
				return ci;
			}
		}
		
		void HandleStartButtonClicked (object sender, EventArgs e)
		{
			RoundButton.OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled)
				IdeApp.CommandService.DispatchCommand (ci.Command.Id);
		}

		#region ICommandBar implementation
		bool toolbarEnabled = true;

		void ICommandBar.Update (object activeTarget)
		{
			if (!toolbarEnabled)
				return;
			RoundButton.OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled != button.Sensitive)
				button.Sensitive = ci.Enabled;

			button.Icon = operation;
			var stopped = operation != RoundButton.OperationIcon.Stop;
			if (configurationCombosBox.Sensitive != stopped)
				configurationCombosBox.Sensitive = stopped;
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			toolbarEnabled = enabled;
			button.Sensitive = enabled;
			matchEntry.Sensitive = enabled;
		}
		#endregion
	}
}

