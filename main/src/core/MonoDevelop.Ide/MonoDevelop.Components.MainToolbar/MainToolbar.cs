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
using Mono.TextEditor;


namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbar: Gtk.EventBox, ICommandBar
	{
		const string ToolbarExtensionPath = "/MonoDevelop/Ide/CommandBar";
		const int RuntimeExecutionTarget = 0;
		const int RuntimeIsIndented = 1;

		EventHandler executionTargetsChanged;

		HBox contentBox = new HBox (false, 0);

		HBox configurationCombosBox;

		ComboBox configurationCombo;
		TreeStore configurationStore = new TreeStore (typeof(string), typeof(string));

		ComboBox runtimeCombo;
		TreeStore runtimeStore = new TreeStore (typeof (ExecutionTarget), typeof (bool));

		StatusArea statusArea;

		SearchEntry matchEntry;
		static WeakReference lastCommandTarget;

		ButtonBar buttonBar = new ButtonBar ();
		RoundButton button = new RoundButton ();
		Alignment buttonBarBox;

		HashSet<string> visibleBars = new HashSet<string> ();

		ConfigurationMerger configurationMerger = new ConfigurationMerger ();

		Solution currentSolution;
		bool settingGlobalConfig;
		SolutionEntityItem currentStartupProject;

		int ignoreConfigurationChangedCount;
		int ignoreRuntimeChangedCount;

		public Cairo.ImageSurface Background {
			get;
			set;
		}

		public int TitleBarHeight {
			get;
			set;
		}

		readonly PropertyWrapper<bool> searchForMembers = new PropertyWrapper<bool> ("MainToolbar.Search.IncludeMembers", true);
		bool SearchForMembers {
			get {
				return searchForMembers;
			}
			set {
				searchForMembers.Value = value;
			}
		}

		public MonoDevelop.Ide.StatusBar StatusBar {
			get {
				return statusArea;
			}
		}

		internal static object LastCommandTarget {
			get { return lastCommandTarget.Target; }
		}

		void SetSearchCategory (string category)
		{
			matchEntry.Entry.Text = category + ":";
			matchEntry.Entry.GrabFocus ();
			var pos = matchEntry.Entry.Text.Length;
			matchEntry.Entry.SelectRegion (pos, pos);
		}

		static bool RuntimeIsSeparator (TreeModel model, TreeIter iter)
		{
			var target = (ExecutionTarget) model.GetValue (iter, RuntimeExecutionTarget);

			return target == null;
		}

		void RuntimeRenderCell (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var target = (ExecutionTarget) model.GetValue (iter, RuntimeExecutionTarget);
			var indent = (bool) model.GetValue (iter, RuntimeIsIndented);
			var renderer = (CellRendererText) cell;
			TreeIter parent;

			renderer.Sensitive = !(target is ExecutionTargetGroup);

			if (target == null) {
				renderer.Xpad = (uint) 0;
				return;
			}

			if (!runtimeCombo.PopupShown) {
				renderer.Text = target.FullName;
				renderer.Xpad = 3;
			} else {
				renderer.Xpad = indent ? (uint) 18 : (uint) 3;

				if (!runtimeStore.IterParent (out parent, iter))
					renderer.Text = target.FullName;
				else
					renderer.Text = target.Name;
			}
		}

		public MainToolbar ()
		{
			executionTargetsChanged = DispatchService.GuiDispatch (new EventHandler (HandleExecutionTargetsChanged));

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
			ctx = new Gtk.CellRendererText ();
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

				var pixel_scale = GtkWorkarounds.GetPixelScale ();

				int windowWidth = toplevel.Allocation.Width;
				int center = windowWidth / 2;
				int left = Math.Max (center - (int)(300 * pixel_scale), args.Allocation.Left);
				int right = Math.Min (left + (int)(600 * pixel_scale), args.Allocation.Right);
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
			if (!Platform.IsMac && !Platform.IsWindows)
				matchEntry.Entry.ModifyFont (Pango.FontDescription.FromString ("Sans 9")); // TODO: VV: "Segoe UI 9"
			matchEntry.RoundedShape = true;
			matchEntry.Entry.Changed += HandleSearchEntryChanged;
			matchEntry.Activated += (sender, e) => {
				var pattern = SearchPopupSearchPattern.ParsePattern (matchEntry.Entry.Text);
				if (pattern.Pattern == null && pattern.LineNumber > 0) {
					popup.Destroy ();
					var doc = IdeApp.Workbench.ActiveDocument;
					if (doc != null && doc.Editor != null) {
						doc.Select ();
						doc.Editor.Caret.Location = new Mono.TextEditor.DocumentLocation (pattern.LineNumber, pattern.Column > 0 ? pattern.Column : 1);
						doc.Editor.CenterToCaret ();
						doc.Editor.Parent.StartCaretPulseAnimation ();
					}
					return;
				}
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
			SizeAllocated += delegate {
				PositionPopup ();
			};

			BuildToolbar ();
			IdeApp.CommandService.RegisterCommandBar (buttonBar);

			AddinManager.ExtensionChanged += OnExtensionChanged;

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

			configurationCombo.Changed += HandleConfigurationChanged;
			runtimeCombo.Changed += HandleRuntimeChanged;
			UpdateCombos ();

			button.Clicked += HandleStartButtonClicked;
			IdeApp.CommandService.RegisterCommandBar (this);

			IdeApp.CommandService.ActiveWidgetChanged += (sender, e) => {
				lastCommandTarget = new WeakReference (e.OldActiveWidget);
			};

			this.ShowAll ();
			this.statusArea.statusIconBox.HideAll ();
		}
			
		void OnExtensionChanged (object sender, ExtensionEventArgs args)
		{
			if (args.PathChanged (ToolbarExtensionPath))
				BuildToolbar ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && evnt.Window == GdkWindow) {
				var window = (Window)Toplevel;
				if (!DesktopService.GetIsFullscreen (window)) {
					window.BeginMoveDrag (1, (int)evnt.XRoot, (int)evnt.YRoot, evnt.Time);
					return true;
				}
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
				currentStartupProject.ExecutionTargetsChanged -= executionTargetsChanged;
				currentStartupProject.Saved -= HandleProjectSaved;
			}

			if (currentSolution != null) {
				currentStartupProject = currentSolution.StartupItem;
				if (currentStartupProject != null) {
					currentStartupProject.ExecutionTargetsChanged += executionTargetsChanged;
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
//			var info = IdeApp.CommandService.GetCommand (Commands.NavigateTo);
//			if (!string.IsNullOrEmpty (info.AccelKey)) {
//				matchEntry.EmptyMessage = GettextCatalog.GetString ("Press '{0}' to search", KeyBindingManager.BindingToDisplayLabel (info.AccelKey, false));
//			} else {
//				matchEntry.EmptyMessage = GettextCatalog.GetString ("Search solution");
//			}
		}

		void SetDefaultSizes (int comboHeight, int height)
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
			var bars = AddinManager.GetExtensionNodes (ToolbarExtensionPath).Cast<ItemSetCodon> ().Where (n => visibleBars.Contains (n.Id)).ToList ();
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
	
		static readonly SearchPopupSearchPattern emptyColonPattern = SearchPopupSearchPattern.ParsePattern (":");

		void HandleSearchEntryChanged (object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty (matchEntry.Entry.Text)){
				if (popup != null)
					popup.Destroy ();
				return;
			}
			var pattern = SearchPopupSearchPattern.ParsePattern (matchEntry.Entry.Text);
			if (pattern.Pattern == null && pattern.LineNumber > 0 || pattern == emptyColonPattern) {
				if (popup != null) {
					popup.Hide ();
				}
				return;
			} else {
				if (popup != null && !popup.Visible)
					popup.Show ();
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

			popup.Update (pattern);

		}

		void PositionPopup ()
		{
			if (popup == null)
				return;
			popup.ShowPopup (matchEntry, PopupPosition.TopRight);
		}

		string GetActiveConfiguration ()
		{
			TreeIter iter;

			if (!configurationCombo.GetActiveIter (out iter))
				return null;

			return (string) configurationStore.GetValue (iter, 1);
		}

		ExecutionTarget GetActiveTarget ()
		{
			TreeIter iter;

			if (!runtimeCombo.GetActiveIter (out iter))
				return null;

			return (ExecutionTarget) runtimeStore.GetValue (iter, RuntimeExecutionTarget);
		}

		void HandleRuntimeChanged (object sender, EventArgs e)
		{
			if (ignoreRuntimeChangedCount == 0)
				NotifyConfigurationChange ();
		}

		void HandleConfigurationChanged (object sender, EventArgs e)
		{
			if (ignoreConfigurationChangedCount == 0)
				NotifyConfigurationChange ();
		}

		void UpdateBuildConfiguration ()
		{
			var currentConfig = GetActiveConfiguration ();
			if (currentConfig == null)
				return;

			ExecutionTarget newTarget;
			string fullConfig;

			configurationMerger.ResolveConfiguration (currentConfig, GetActiveTarget (), out fullConfig, out newTarget);
			settingGlobalConfig = true;
			try {
				IdeApp.Workspace.ActiveExecutionTarget = newTarget;
				IdeApp.Workspace.ActiveConfigurationId = fullConfig;
			} finally {
				settingGlobalConfig = false;
			}
		}

		void NotifyConfigurationChange ()
		{
			if (GetActiveConfiguration () == null)
				return;

			UpdateBuildConfiguration ();

			FillRuntimes ();
			SelectActiveRuntime ();
		}

		void SelectActiveConfiguration ()
		{
			string name = configurationMerger.GetUnresolvedConfiguration (IdeApp.Workspace.ActiveConfigurationId);

			ignoreConfigurationChangedCount++;
			try {
				TreeIter iter;

				if (configurationStore.GetIterFirst (out iter)) {
					var defaultConfig = (string) configurationStore.GetValue (iter, 1);
					bool selected = false;
					int i = 0;

					do {
						string config = (string) configurationStore.GetValue (iter, 1);
						if (config == name) {
							IdeApp.Workspace.ActiveConfigurationId = config;
							configurationCombo.Active = i;
							selected = true;
							break;
						}
						i++;
					} while (configurationStore.IterNext (ref iter));

					if (!selected) {
						IdeApp.Workspace.ActiveConfigurationId = defaultConfig;
						configurationCombo.Active = 0;
					}
				}
			} finally {
				ignoreConfigurationChangedCount--;
			}

			SelectActiveRuntime ();
		}

		bool SelectActiveRuntime (TreeIter iter, ref bool selected, ref ExecutionTarget defaultTarget, ref TreeIter defaultIter)
		{
			do {
				var target = (ExecutionTarget) runtimeStore.GetValue (iter, RuntimeExecutionTarget);

				if (target == null)
					continue;

				if (target is ExecutionTargetGroup) {
					TreeIter child;

					if (runtimeStore.IterHasChild (iter) && runtimeStore.IterChildren (out child, iter)) {
						if (SelectActiveRuntime (child, ref selected, ref defaultTarget, ref defaultIter))
							return true;
					}

					continue;
				}

				if (defaultTarget == null) {
					defaultTarget = target;
					defaultIter = iter;
				}

				if (target.Id == IdeApp.Workspace.PreferredActiveExecutionTarget) {
					IdeApp.Workspace.ActiveExecutionTarget = target;
					runtimeCombo.SetActiveIter (iter);
					UpdateBuildConfiguration ();
					selected = true;
					return true;
				}

				if (target.Equals (IdeApp.Workspace.ActiveExecutionTarget)) {
					runtimeCombo.SetActiveIter (iter);
					UpdateBuildConfiguration ();
					selected = true;
				}
			} while (runtimeStore.IterNext (ref iter));

			return false;
		}

		void SelectActiveRuntime ()
		{
			ignoreRuntimeChangedCount++;

			try {
				TreeIter iter;

				if (runtimeStore.GetIterFirst (out iter)) {
					ExecutionTarget defaultTarget = null;
					TreeIter defaultIter = TreeIter.Zero;
					bool selected = false;

					if (!SelectActiveRuntime (iter, ref selected, ref defaultTarget, ref defaultIter) && !selected) {
						if (defaultTarget != null) {
							IdeApp.Workspace.ActiveExecutionTarget = defaultTarget;
							runtimeCombo.SetActiveIter (defaultIter);
						}
						UpdateBuildConfiguration ();
					}
				}
			} finally {
				ignoreRuntimeChangedCount--;
			}
		}

		void UpdateCombos ()
		{
			if (settingGlobalConfig)
				return;

			configurationMerger.Load (currentSolution);

			ignoreConfigurationChangedCount++;
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
				ignoreConfigurationChangedCount--;
			}

			FillRuntimes ();
			SelectActiveConfiguration ();
		}

		void FillRuntimes ()
		{
			ignoreRuntimeChangedCount++;
			try {
				runtimeStore.Clear ();
				if (!IdeApp.Workspace.IsOpen || currentSolution == null || !currentSolution.SingleStartup || currentSolution.StartupItem == null)
					return;

				// Check that the current startup project is enabled for the current configuration
				var solConf = currentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				if (solConf == null || !solConf.BuildEnabledForItem (currentSolution.StartupItem))
					return;

				ExecutionTarget previous = null;
				int runtimes = 0;

				foreach (var target in configurationMerger.GetTargetsForConfiguration (IdeApp.Workspace.ActiveConfigurationId, true)) {
					if (target is ExecutionTargetGroup) {
						var devices = (ExecutionTargetGroup) target;

						if (previous != null)
							runtimeStore.AppendValues (null, false);

						runtimeStore.AppendValues (target, false);
						foreach (var device in devices) {
							if (device is ExecutionTargetGroup) {
								var versions = (ExecutionTargetGroup) device;

								if (versions.Count > 1) {
									var iter = runtimeStore.AppendValues (device, true);

									foreach (var version in versions) {
										runtimeStore.AppendValues (iter, version, false);
										runtimes++;
									}
								} else {
									runtimeStore.AppendValues (versions[0], true);
									runtimes++;
								}
							} else {
								runtimeStore.AppendValues (device, true);
								runtimes++;
							}
						}
					} else {
						if (previous is ExecutionTargetGroup)
							runtimeStore.AppendValues (null, false);

						runtimeStore.AppendValues (target, false);
						runtimes++;
					}

					previous = target;
				}

				runtimeCombo.Sensitive = runtimes > 1;
			} finally {
				ignoreRuntimeChangedCount--;
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
						lg.AddColorStop (0, Style.Light (StateType.Normal).ToCairoColor ());
						lg.AddColorStop (1, Style.Mid (StateType.Normal).ToCairoColor ());
						context.SetSource (lg);
					}
					context.Fill ();

				}
				context.MoveTo (0, Allocation.Height - 0.5);
				context.RelLineTo (Allocation.Width, 0);
				context.SetSourceColor (Styles.ToolbarBottomBorderColor);
				context.Stroke ();

				context.MoveTo (0, Allocation.Height - 1.5);
				context.RelLineTo (Allocation.Width, 0);
				context.SetSourceColor (Styles.ToolbarBottomGlowColor);
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

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			AddinManager.ExtensionChanged -= OnExtensionChanged;
			if (button != null)
				button.Clicked -= HandleStartButtonClicked;

			if (Background != null) {
				((IDisposable)Background).Dispose ();
				Background = null;
			}
		}
	}
}

