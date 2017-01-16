//
// MainToolbarController.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Commands.ExtensionNodes;
using MonoDevelop.Core;
using Gtk;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using System.Text;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbarController : ICommandBar
	{
		const string ToolbarExtensionPath = "/MonoDevelop/Ide/CommandBar";
		const string TargetsMenuPath = "/MonoDevelop/Ide/TargetSelectorCommands";

		internal IMainToolbarView ToolbarView {
			get;
			private set;
		}

		internal StatusBar StatusBar {
			get { return ToolbarView.StatusBar; }
		}

		readonly ConfigurationProperty<bool> searchForMembers = ConfigurationProperty.Create ("MainToolbar.Search.IncludeMembers", true);
		bool SearchForMembers {
			get { return searchForMembers; }
			set { searchForMembers.Value = value; }
		}

		Dictionary<SolutionItem, ConfigurationMerger> configurationMergers = new Dictionary<SolutionItem, ConfigurationMerger> ();
		int ignoreConfigurationChangedCount, ignoreRuntimeChangedCount;
		Solution currentSolution;
		bool settingGlobalConfig;
		Tuple<SolutionItem, SolutionItemRunConfiguration> [] startupProjects = new Tuple<SolutionItem, SolutionItemRunConfiguration> [0];
		EventHandler executionTargetsChanged;

		public MainToolbarController (IMainToolbarView toolbarView)
		{
			ToolbarView = toolbarView;
			// Attach run button click handler.
			toolbarView.RunButtonClicked += HandleStartButtonClicked;

			// Register Search Entry handlers.
			ToolbarView.SearchEntryChanged += HandleSearchEntryChanged;
			ToolbarView.SearchEntryActivated += HandleSearchEntryActivated;
			ToolbarView.SearchEntryKeyPressed += HandleSearchEntryKeyPressed;
			ToolbarView.SearchEntryResized += (o, e) => PositionPopup ();
			ToolbarView.SearchEntryLostFocus += (o, e) => {
				ToolbarView.SearchText = "";
				DestroyPopup ();
			};

			toolbarView.ConfigurationChanged += HandleConfigurationChanged;
			toolbarView.RunConfigurationChanged += HandleRunConfigurationChanged;
			toolbarView.RuntimeChanged += HandleRuntimeChanged;

			IdeApp.Workbench.RootWindow.WidgetEvent += delegate(object o, WidgetEventArgs args) {
				if (args.Event is Gdk.EventConfigure)
					PositionPopup ();
			};

			// Update Search Entry on keybinding change.
			var cmd = IdeApp.CommandService.GetCommand (Commands.NavigateTo);
			cmd.KeyBindingChanged += delegate {
				UpdateSearchEntryLabel ();
			};

			executionTargetsChanged = (sender, e) => UpdateCombos ();

			IdeApp.Workspace.LastWorkspaceItemClosed += (sender, e) => StatusBar.ShowReady ();
			IdeApp.Workspace.ActiveConfigurationChanged += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.ConfigurationsChanged += (sender, e) => UpdateCombos ();

			IdeApp.Workspace.SolutionLoaded += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.SolutionUnloaded += (sender, e) => UpdateCombos ();

			IdeApp.ProjectOperations.CurrentSelectedSolutionChanged += HandleCurrentSelectedSolutionChanged;

			AddinManager.ExtensionChanged += OnExtensionChanged;
		}

		public void Initialize ()
		{
			var items = new[] {
				new SearchMenuModel (GettextCatalog.GetString ("Search Files"), "file"),
				new SearchMenuModel (GettextCatalog.GetString ("Search Types"), "type"),
				new SearchMenuModel (GettextCatalog.GetString ("Search Members"), "member"),
			};

			// Attach menu category handlers.
			foreach (var item in items)
				item.Activated += (o, e) => SetSearchCategory (item.Category);
			ToolbarView.SearchMenuItems = items;

			// Update Search Entry label initially.
			UpdateSearchEntryLabel ();

			// Rebuild the button bars.
			RebuildToolbar ();

			UpdateCombos ();

			// Register this controller as a commandbar.
			IdeApp.CommandService.RegisterCommandBar (this);
		}

		void UpdateCombos ()
		{
			if (settingGlobalConfig)
				return;

			ignoreConfigurationChangedCount++;
			try {
				if (!IdeApp.Workspace.IsOpen) {
					ToolbarView.ConfigurationModel = Enumerable.Empty<IConfigurationModel> ();
					ToolbarView.RuntimeModel = Enumerable.Empty<IRuntimeModel> ();
					ToolbarView.RunConfigurationModel = Enumerable.Empty<IRunConfigurationModel> ();
					ToolbarView.RunConfigurationVisible = false;
					return;
				}
				if (currentSolution != null)
					ToolbarView.RunConfigurationModel = currentSolution.GetRunConfigurations ().Select (rc => new RunConfigurationModel (rc)).ToArray ();
				else
					ToolbarView.RunConfigurationModel = Enumerable.Empty<IRunConfigurationModel> ();
				SelectActiveRunConfiguration ();
				TrackStartupProject ();
				configurationMergers = new Dictionary<SolutionItem, ConfigurationMerger> ();
				foreach (var project in startupProjects) {
					var configurationMerger = new ConfigurationMerger ();
					configurationMerger.Load (currentSolution, project.Item1, project.Item2);
					configurationMergers [project.Item1] = configurationMerger;
				}
				if (configurationMergers.Count == 1)
					ToolbarView.ConfigurationModel = configurationMergers.First ().Value.SolutionConfigurations
						.Distinct ()
						.Select (conf => new ConfigurationModel (conf));
				else
					ToolbarView.ConfigurationModel = currentSolution?.Configurations.OfType<SolutionConfiguration> ()
						.Select (conf => new ConfigurationModel (conf.Id)) ?? new ConfigurationModel [0];
				
			} finally {
				ignoreConfigurationChangedCount--;
			}

			ToolbarView.RunConfigurationVisible = ToolbarView.RunConfigurationModel.Count () > 1;

			FillRuntimes ();
			SelectActiveConfiguration ();
		}

		void FillRuntimes ()
		{
			ignoreRuntimeChangedCount++;
			try {
				ToolbarView.RuntimeModel = Enumerable.Empty<IRuntimeModel> ();
				if (!IdeApp.Workspace.IsOpen || currentSolution == null)
					return;
				var list = new List<RuntimeModel> ();
				int runtimes = 0;
				if (currentSolution.StartupConfiguration is MultiItemSolutionRunConfiguration) {
					bool anyValid = false;
					foreach (var startConf in ((MultiItemSolutionRunConfiguration)currentSolution.StartupConfiguration).Items) {
						if (startConf?.SolutionItem == null)
							continue;

						// Check that the current startup project is enabled for the current configuration
						var solConf = currentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
						if (solConf == null || !solConf.BuildEnabledForItem (startConf.SolutionItem))
							continue;
						anyValid = true;
						var projectList = new List<RuntimeModel> ();
						FillRuntimesForProject (projectList, startConf.SolutionItem, ref runtimes);
						var parent = new RuntimeModel (this, startConf.SolutionItem.Name);
						parent.HasChildren = true;
						list.Add (parent);
						foreach (var p in projectList) {
							parent.AddChild (p);
						}
					}
					if (!anyValid)
						return;
				} else {
					var startConf = currentSolution.StartupConfiguration as SingleItemSolutionRunConfiguration;
					if (startConf == null || startConf.Item == null)
						return;

					// Check that the current startup project is enabled for the current configuration
					var solConf = currentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
					if (solConf == null || !solConf.BuildEnabledForItem (startConf.Item))
						return;
					FillRuntimesForProject (list, startConf.Item, ref runtimes);
				}


				var cmds = IdeApp.CommandService.CreateCommandEntrySet (TargetsMenuPath);
				if (cmds.Count > 0) {
					bool needsSeparator = runtimes > 0;
					foreach (CommandEntry ce in cmds) {
						if (ce.CommandId == Command.Separator) {
							needsSeparator = true;
							continue;
						}
						var cmd = ce.GetCommand (IdeApp.CommandService) as ActionCommand;
						if (cmd != null) {
							var ci = IdeApp.CommandService.GetCommandInfo (cmd.Id, new CommandTargetRoute (lastCommandTarget));
							if (ci.Visible) {
								if (needsSeparator) {
									list.Add (new RuntimeModel (this, displayText: null));
									needsSeparator = false;
								}
								list.Add (new RuntimeModel (this, cmd));
								runtimes++;
							}
						}
					}
				}

				ToolbarView.PlatformSensitivity = runtimes > 1;
				ToolbarView.RuntimeModel = list;
			} finally {
				ignoreRuntimeChangedCount--;
			}
		}

		void FillRuntimesForProject (List<RuntimeModel> list, SolutionItem project, ref int runtimes)
		{
			ExecutionTarget previous = null;

			foreach (var target in configurationMergers [project].GetTargetsForConfiguration (IdeApp.Workspace.ActiveConfigurationId, configurationMergers.Count < 2)) {
				if (target is ExecutionTargetGroup) {
					var devices = (ExecutionTargetGroup)target;

					if (previous != null)
						list.Add (new RuntimeModel (this, displayText: null));//Seperator

					list.Add (new RuntimeModel (this, target, true, project));
					foreach (var device in devices) {
						if (device is ExecutionTargetGroup) {
							var versions = (ExecutionTargetGroup)device;

							if (versions.Count > 1) {
								var parent = new RuntimeModel (this, device, true, project) {
									IsIndented = true,
								};
								list.Add (parent);

								foreach (var version in versions) {
									parent.AddChild (new RuntimeModel (this, version, false, project));
									runtimes++;
								}
							} else {
								list.Add (new RuntimeModel (this, versions [0], true, project) {
									IsIndented = true,
								});
								runtimes++;
							}
						} else {
							list.Add (new RuntimeModel (this, device, true, project) {
								IsIndented = true,
							});
							runtimes++;
						}
					}
				} else {
					if (previous is ExecutionTargetGroup) {
						list.Add (new RuntimeModel (this, displayText: null));//Seperator
					}

					list.Add (new RuntimeModel (this, target, true, project));
					runtimes++;
				}

				previous = target;
			}
		}

		void HandleRuntimeChanged (object sender, HandledEventArgs e)
		{
			if (ignoreRuntimeChangedCount == 0) {
				var runtime = (RuntimeModel)ToolbarView.ActiveRuntime;
				if (runtime != null && runtime.Command != null) {
					e.Handled = runtime.NotifyActivated ();
					return;
				}

				NotifyConfigurationChange ();
				SelectActiveRuntime (runtime);
			}
		}

		void HandleConfigurationChanged (object sender, EventArgs e)
		{
			if (ignoreConfigurationChangedCount == 0)
				NotifyConfigurationChange ();
		}

		void HandleRunConfigurationChanged (object sender, EventArgs e)
		{
			if (ignoreConfigurationChangedCount == 0)
				NotifyRunConfigurationChange ();
		}

		void UpdateBuildConfiguration ()
		{
			var config = ToolbarView.ActiveConfiguration;
			if (config == null || configurationMergers.Count == 0)
				return;
			if (configurationMergers.Count > 1) {
				settingGlobalConfig = true;
				try {
					IdeApp.Workspace.ActiveConfigurationId = config.OriginalId;
				} finally {
					settingGlobalConfig = false;
				}
				return;
			}

			ExecutionTarget newTarget;
			string fullConfig;

			var runtime = (RuntimeModel)ToolbarView.ActiveRuntime;
			configurationMergers.Values.First ().ResolveConfiguration (config.OriginalId, runtime != null ? runtime.ExecutionTarget : null, out fullConfig, out newTarget);
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
			if (ToolbarView.ActiveConfiguration == null)
				return;

			UpdateBuildConfiguration ();

			FillRuntimes ();
			SelectActiveRuntime (ToolbarView.ActiveRuntime as RuntimeModel);
		}

		void NotifyRunConfigurationChange ()
		{
			if (ToolbarView.ActiveRunConfiguration == null)
				return;

			var model = (RunConfigurationModel)ToolbarView.ActiveRunConfiguration;
			currentSolution.StartupConfiguration = model.RunConfiguration;
		}

		void SelectActiveRunConfiguration ()
		{
			var sconf = currentSolution?.StartupConfiguration;
			var confs = ToolbarView.RunConfigurationModel.Cast<RunConfigurationModel> ().ToList ();
			if (confs.Count > 0) {
				bool selected = false;

				foreach (var item in confs) {
					if (item.RunConfiguration.Id == sconf?.Id) {
						ToolbarView.ActiveRunConfiguration = item;
						selected = true;
						break;
					}
				}

				if (!selected) {
					var defaultConfig = confs.First ();
					ToolbarView.ActiveRunConfiguration = defaultConfig;
					if (currentSolution != null)
						currentSolution.StartupConfiguration = defaultConfig.RunConfiguration;
				}
			}
		}

		void SelectActiveConfiguration ()
		{
			var allNames = configurationMergers.Values.Select (cm => cm.GetUnresolvedConfiguration (IdeApp.Workspace.ActiveConfigurationId)).Distinct ();
			string name;
			if (allNames.Count () == 1)
				name = allNames.First ();
			else
				name = IdeApp.Workspace.ActiveConfigurationId;

			ignoreConfigurationChangedCount++;
			try {
				var confs = ToolbarView.ConfigurationModel.ToList ();
				if (confs.Count > 0) {
					string defaultConfig = ToolbarView.ActiveConfiguration != null ? ToolbarView.ActiveConfiguration.OriginalId : confs[0].OriginalId;
					bool selected = false;

					foreach (var item in confs) {
						string config = item.OriginalId;
						if (config == name) {
							IdeApp.Workspace.ActiveConfigurationId = config;
							ToolbarView.ActiveConfiguration = item;
							selected = true;
							break;
						}
					}

					if (!selected) {
						ToolbarView.ActiveConfiguration = ToolbarView.ConfigurationModel.First ();
						IdeApp.Workspace.ActiveConfigurationId = defaultConfig;
					}
				}
			} finally {
				ignoreConfigurationChangedCount--;
			}

			SelectActiveRuntime (ToolbarView.ActiveRuntime as RuntimeModel);
		}

		IEnumerable<IRuntimeModel> AllRuntimes (IEnumerable<IRuntimeModel> runtimes)
		{
			foreach (var runtime in runtimes) {
				yield return runtime;
				foreach (var childRuntime in AllRuntimes (runtime.Children))
					yield return childRuntime;
			}
		}

		RuntimeModel SelectActiveRuntime (SolutionItem project, RuntimeModel preferedRuntimeModel)
		{
			var runtimes = AllRuntimes (ToolbarView.RuntimeModel).Cast<RuntimeModel> ().ToList ();
			string lastRuntimeForProject = project.UserProperties.GetValue<string> ("PreferredExecutionTarget", defaultValue: null);
			var activeTarget = preferedRuntimeModel?.Project == project ? preferedRuntimeModel.ExecutionTarget : null;
			var multiProjectExecutionTarget = activeTarget as MultiProjectExecutionTarget;
			if (multiProjectExecutionTarget != null) {
				activeTarget = multiProjectExecutionTarget.GetTarget (project);
			}
			var activeTargetId = activeTarget?.Id;
			RuntimeModel defaultRuntime = null;

			foreach (var item in runtimes) {
				using (var model = item.GetMutableModel ()) {
					if (!model.Enabled)
						continue;
				}

				var target = item.ExecutionTarget;
				if (target == null || !target.Enabled || item.Project != project)
					continue;

				if (target is ExecutionTargetGroup)
					if (item.HasChildren)
						continue;

				if (defaultRuntime == null || lastRuntimeForProject == target.Id) {
					defaultRuntime = item;
				}

				if (target.Id == activeTargetId) {
					project.UserProperties.SetValue ("PreferredExecutionTarget", target.Id);
					return item;
				}

				if (target.Equals (activeTarget)) {
					defaultRuntime = item;
				}
			}
            if (defaultRuntime?.ExecutionTarget?.Id != null)
                project.UserProperties.SetValue("PreferredExecutionTarget", defaultRuntime.ExecutionTarget.Id);
			return defaultRuntime;
		}

		void SelectActiveRuntime (RuntimeModel preferedRuntimeModel)
		{
			ignoreRuntimeChangedCount++;

			try {
				if (ToolbarView.RuntimeModel.Any ()) {
					if (startupProjects.Length > 1) {
						var multiProjectTarget = new MultiProjectExecutionTarget ();
						var multipleRuntime = new RuntimeModel (this, multiProjectTarget, false, null);
						foreach (var startupProject in startupProjects) {
							var runtimeModel = SelectActiveRuntime (startupProject.Item1, preferedRuntimeModel);
							multiProjectTarget.SetExecutionTarget (startupProject.Item1, runtimeModel.ExecutionTarget);
							multipleRuntime.AddChild (runtimeModel);
						}
						ToolbarView.ActiveRuntime = multipleRuntime;
						IdeApp.Workspace.ActiveExecutionTarget = multipleRuntime.ExecutionTarget;
					} else if (startupProjects.Length == 1) {
						var runtimeModel = SelectActiveRuntime (startupProjects.First ().Item1, preferedRuntimeModel);
						ToolbarView.ActiveRuntime = runtimeModel;
						IdeApp.Workspace.ActiveExecutionTarget = runtimeModel?.ExecutionTarget;
						UpdateBuildConfiguration ();
					} else {
						ToolbarView.ActiveRuntime = null;
						IdeApp.Workspace.ActiveExecutionTarget = null;
					}
				}
			} finally {
				ignoreRuntimeChangedCount--;
			}
		}

		static IEnumerable<ExecutionTarget> GetExecutionTargets (string configuration)
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

		void HandleCurrentSelectedSolutionChanged (object sender, SolutionEventArgs e)
		{
			if (currentSolution != null) {
				currentSolution.StartupConfigurationChanged -= HandleStartupItemChanged;
				currentSolution.Saved -= HandleUpdateCombosWidthDelay;
				currentSolution.EntrySaved -= HandleUpdateCombosWidthDelay;
			}

			currentSolution = e.Solution;

			if (currentSolution != null) {
				currentSolution.StartupConfigurationChanged += HandleStartupItemChanged;
				currentSolution.Saved += HandleUpdateCombosWidthDelay;
				currentSolution.EntrySaved += HandleUpdateCombosWidthDelay;
			}

			TrackStartupProject ();

			UpdateCombos ();
		}

		void TrackStartupProject ()
		{
			Tuple<SolutionItem, SolutionItemRunConfiguration> [] projects;
			if (currentSolution?.StartupConfiguration is MultiItemSolutionRunConfiguration) {
				var multiRunConfigs = (MultiItemSolutionRunConfiguration)currentSolution.StartupConfiguration;
				projects = multiRunConfigs.Items.Select (rc => new Tuple<SolutionItem, SolutionItemRunConfiguration> (
					  rc.SolutionItem,
					  rc.RunConfiguration
				)).ToArray ();
			} else if (currentSolution?.StartupConfiguration is SingleItemSolutionRunConfiguration) {
				var singleRunConfig = (SingleItemSolutionRunConfiguration)currentSolution.StartupConfiguration;
				projects = new Tuple<SolutionItem, SolutionItemRunConfiguration> []{ new Tuple<SolutionItem, SolutionItemRunConfiguration> (
					singleRunConfig.Item, singleRunConfig.RunConfiguration)};
			} else {
				projects = new Tuple<SolutionItem, SolutionItemRunConfiguration> [0];
			}
			if (!startupProjects.SequenceEqual (projects)) {
				foreach (var item in startupProjects)
					item.Item1.ExecutionTargetsChanged -= executionTargetsChanged;
				foreach (var item in projects)
					item.Item1.ExecutionTargetsChanged += executionTargetsChanged;
				startupProjects = projects;
			}
		}

		bool updatingCombos;
		void HandleUpdateCombosWidthDelay (object sender, EventArgs e)
		{
			if (!updatingCombos) {
				updatingCombos = true;
				GLib.Timeout.Add (100, () => {
					updatingCombos = false;
					UpdateCombos ();
					return false;
				});
			}
		}

		void HandleStartupItemChanged (object sender, EventArgs e)
		{
			TrackStartupProject ();
			if (ignoreConfigurationChangedCount == 0)
				UpdateCombos ();
		}

		void OnExtensionChanged (object sender, ExtensionEventArgs args)
		{
			if (args.PathChanged (ToolbarExtensionPath))
				RebuildToolbar ();
		}

		void UpdateSearchEntryLabel ()
		{
			var info = IdeApp.CommandService.GetCommand (Commands.NavigateTo);
			ToolbarView.SearchPlaceholderMessage = !string.IsNullOrEmpty (info.AccelKey) ?
				GettextCatalog.GetString ("Press \u2018{0}\u2019 to search", KeyBindingManager.BindingToDisplayLabel (info.AccelKey, false)) :
				GettextCatalog.GetString ("Search solution");
		}

		SearchPopupWindow popup = null;
		static readonly SearchPopupSearchPattern emptyColonPattern = SearchPopupSearchPattern.ParsePattern (":");
		void PositionPopup ()
		{
			if (popup == null)
				return;

			popup.ShowPopup (ToolbarView.PopupAnchor, PopupPosition.TopRight);

			var window = ToolbarView.PopupAnchor.GdkWindow;
			if (window == null) {
				if (popup.IsRealized) {
					popup.Move (ToolbarView.PopupAnchor.Allocation.Width - popup.Allocation.Width, ToolbarView.PopupAnchor.Allocation.Y);
				} else {
					popup.Realized += (sender, e) =>
						popup.Move (ToolbarView.PopupAnchor.Allocation.Width - popup.Allocation.Width, ToolbarView.PopupAnchor.Allocation.Y);
				}
			}
		}

		void DestroyPopup ()
		{
			if (popup != null) {
				popup.Destroy ();
				popup = null;
			}
		}

		void HandleSearchEntryChanged (object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty (ToolbarView.SearchText))
				lastSearchText = ToolbarView.SearchText;
			
			if (string.IsNullOrEmpty (ToolbarView.SearchText)){
				DestroyPopup ();
				return;
			}
			var pattern = SearchPopupSearchPattern.ParsePattern (ToolbarView.SearchText);
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
					ToolbarView.SearchText = "";
				};
				PositionPopup ();
				popup.ShowAll ();
			}

			popup.Update (pattern);
		}

		void HandleSearchEntryActivated (object sender, EventArgs e)
		{
			var pattern = SearchPopupSearchPattern.ParsePattern (ToolbarView.SearchText);
			if (pattern.Pattern == null && pattern.LineNumber > 0) {
				DestroyPopup ();
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null && doc.Editor != null) {
					doc.Select ();
					doc.Editor.CaretLocation = new MonoDevelop.Ide.Editor.DocumentLocation (pattern.LineNumber, pattern.Column > 0 ? pattern.Column : 1);
					doc.Editor.CenterToCaret ();
					doc.Editor.StartCaretPulseAnimation ();
				}
				return;
			}
			if (popup != null)
				popup.OpenFile ();
		}

		void HandleSearchEntryKeyPressed (object sender, Xwt.KeyEventArgs e)
		{
			if (e.Key == Xwt.Key.Escape) {
				DestroyPopup();
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					doc.Select ();
				}
				return;
			}
			if (popup != null) {
				e.Handled = popup.ProcessKey (e.Key, e.Modifiers);
			}
		}

		string lastSearchText = string.Empty;
		public void FocusSearchBar ()
		{
			IdeApp.Workbench.Present ();
			var text = lastSearchText;
			var actDoc = IdeApp.Workbench.ActiveDocument;
			if (actDoc != null && actDoc.Editor != null && actDoc.Editor.IsSomethingSelected) {
				string selected = actDoc.Editor.SelectedText;
				int whitespaceIndex = selected.TakeWhile (c => !char.IsWhiteSpace (c)).Count ();
				text = selected.Substring (0, whitespaceIndex);
			}

			ToolbarView.FocusSearchBar ();
			ToolbarView.SearchText = text;
		}

		public void SetSearchCategory (string category)
		{
			IdeApp.Workbench.Present ();
			ToolbarView.SearchCategory = category + ":";
		}

		HashSet<string> visibleBars = new HashSet<string> ();
		public void ShowCommandBar (string barId)
		{
			visibleBars.Add (barId);
			RebuildToolbar ();
		}

		public void HideCommandBar (string barId)
		{
			visibleBars.Remove (barId);
			RebuildToolbar ();
		}

		List<ButtonBarButton> buttonBarButtons = new List<ButtonBarButton> ();
		void RebuildToolbar ()
		{
			var bars = AddinManager.GetExtensionNodes<ItemSetCodon> (ToolbarExtensionPath)
				.Where (n => visibleBars.Contains (n.Id))
				.Select (b => b.ChildNodes.OfType<CommandItemCodon> ().Select (n => n.Id));

			buttonBarButtons.Clear ();
			foreach (var bar in bars) {
				foreach (string commandId in bar)
					buttonBarButtons.Add (new ButtonBarButton (this, commandId));
				buttonBarButtons.Add (new ButtonBarButton (this));
			}

			ToolbarView.RebuildToolbar (buttonBarButtons);
		}

		static void HandleStartButtonClicked (object sender, EventArgs e)
		{
			OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled)
				IdeApp.CommandService.DispatchCommand (ci.Command.Id, CommandSource.MainToolbar);
		}

		static CommandInfo GetStartButtonCommandInfo (out OperationIcon operation)
		{
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted || !IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) {
				operation = OperationIcon.Stop;
				return IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.Stop);
			}
			else {
				operation = OperationIcon.Run;
				var ci = IdeApp.CommandService.GetCommandInfo ("MonoDevelop.Debugger.DebugCommands.Debug");
				if (!ci.Enabled || !ci.Visible) {
					// If debug is not enabled, try Run
					ci = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.Run);
					if (!ci.Enabled || !ci.Visible) {
						// Running is not possible, then allow building
						var bci = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.BuildSolution);
						if (bci.Enabled && bci.Visible) {
							operation = OperationIcon.Build;
							ci = bci;
						}
					}
				}
				return ci;
			}
		}

		#region ICommandBar implementation
		bool toolbarEnabled = true;
		object lastCommandTarget;

		void ICommandBar.Update (object activeTarget)
		{
			lastCommandTarget = activeTarget;
			if (!toolbarEnabled)
				return;
			OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled != ToolbarView.RunButtonSensitivity)
				ToolbarView.RunButtonSensitivity = ci.Enabled;

			ToolbarView.RunButtonIcon = operation;
			var stopped = operation != OperationIcon.Stop;
			if (ToolbarView.ConfigurationPlatformSensitivity != stopped)
				ToolbarView.ConfigurationPlatformSensitivity = stopped;

			foreach (var item in buttonBarButtons)
				item.Update ();
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			toolbarEnabled = enabled;
			ToolbarView.RunButtonSensitivity = enabled;
			ToolbarView.SearchSensivitity = enabled;
			ToolbarView.ButtonBarSensitivity = enabled;
		}
		#endregion

		class ButtonBarButton : IButtonBarButton
		{
			MainToolbarController Controller { get; set; }
			string CommandId { get; set; }

			public ButtonBarButton (MainToolbarController controller, string commandId) : this (controller)
			{
				CommandId = commandId;
			}

			public ButtonBarButton (MainToolbarController controller)
			{
				Controller = controller;
			}

			public IconId Image { get; set; }
			public bool Enabled { get; set; }
			public bool Visible { get; set; }
			public string Tooltip { get; set; }
			public bool IsSeparator {
				get { return CommandId == null; }
			}

			public void NotifyPushed ()
			{
				IdeApp.CommandService.DispatchCommand (CommandId, null, Controller.lastCommandTarget, CommandSource.MainToolbar);
			}

			public void Update ()
			{
				if (IsSeparator)
					return;

				var ci = IdeApp.CommandService.GetCommandInfo (CommandId, new CommandTargetRoute (Controller.lastCommandTarget));
				if (ci == null)
					return;

				if (ci.Icon != Image) {
					Image = ci.Icon;
					if (ImageChanged != null)
						ImageChanged (this, null);
				}
				if (ci.Enabled != Enabled) {
					Enabled = ci.Enabled;
					if (EnabledChanged != null)
						EnabledChanged (this, null);
				}
				if (ci.Description != Tooltip) {
					Tooltip = ci.Description;
					if (TooltipChanged != null)
						TooltipChanged (this, null);
				}
				if (ci.Visible != Visible) {
					Visible = ci.Visible;
					if (VisibleChanged != null)
						VisibleChanged (this, null);
				}
			}

			public event EventHandler EnabledChanged;
			public event EventHandler ImageChanged;
			public event EventHandler VisibleChanged;
			public event EventHandler TooltipChanged;
		}

		class RuntimeModel : IRuntimeModel
		{
			MainToolbarController Controller { get; set; }
			List<IRuntimeModel> children = new List<IRuntimeModel> ();
			public object Command { get; private set; }
			public ExecutionTarget ExecutionTarget { get; private set; }
			string DisplayText = null;
			bool fullText;

			RuntimeModel (MainToolbarController controller)
			{
				Controller = controller;
			}

			public RuntimeModel (MainToolbarController controller, string displayText) : this (controller)
			{
				DisplayText = displayText;
			}

			public RuntimeModel (MainToolbarController controller, ActionCommand command) : this (controller)
			{
				Command = command.Id;
			}

			public RuntimeModel (MainToolbarController controller, ExecutionTarget target, bool fullText, SolutionItem project) : this (controller)
			{
				if (target == null)
					throw new ArgumentNullException (nameof (target));
				ExecutionTarget = target;
				this.fullText = fullText;
				Project = project;
			}

			public void AddChild (IRuntimeModel child)
			{
				children.Add (child);
			}

			public IEnumerable<IRuntimeModel> Children {
				get { return children; }
			}

			public bool Notable {
				get { return ExecutionTarget != null && ExecutionTarget.Notable; }
			}

			public bool HasChildren {
				get;
				set;
			}

			public bool IsSeparator {
				get { return Command == null && ExecutionTarget == null && DisplayText == null; }
			}

			public bool IsIndented {
				get;
				set;
			}

			public bool NotifyActivated ()
			{
				if (Command != null && IdeApp.CommandService.DispatchCommand (Command, CommandSource.ContextMenu))
					return true;
				return false;
			}

			internal string TargetId {
				get {
					if (ExecutionTarget == null)
						return "";
					return ExecutionTarget.Id;
				}
			}

			public SolutionItem Project { get; }

			public IRuntimeMutableModel GetMutableModel ()
			{
				if (Command != null)
					return new RuntimeMutableModel (Controller, Command);
				else if (ExecutionTarget != null)
					return new RuntimeMutableModel (ExecutionTarget, fullText);
				else
					return new RuntimeMutableModel (DisplayText);
			}
		}

		class RuntimeMutableModel : IRuntimeMutableModel
		{
			public RuntimeMutableModel(string text)
			{
				Enabled = Visible = true;
				DisplayString = FullDisplayString = text;
			}

			public RuntimeMutableModel (MainToolbarController controller, object command)
			{
				var ci = IdeApp.CommandService.GetCommandInfo (command, new CommandTargetRoute (controller.lastCommandTarget));
				Visible = ci.Visible;
				Enabled = ci.Enabled;
				DisplayString = FullDisplayString = RemoveUnderline (ci.Text);
			}

			public RuntimeMutableModel (ExecutionTarget target, bool fullName)
			{
				Enabled = !(target is ExecutionTargetGroup);
				Visible = true;
				if (target == null)
					DisplayString = FullDisplayString = string.Empty;
				else {
					FullDisplayString = target.FullName;
					DisplayString = fullName ? target.FullName : target.Name;
				}
			}

			// Marker so it won't be reused.
			public void Dispose ()
			{
			}

			public bool Visible {
				get;
				private set;
			}

			public bool Enabled {
				get;
				private set;
			}

			public string DisplayString {
				get;
				private set;
			}

			public string FullDisplayString {
				get;
				private set;
			}

			static string RemoveUnderline (string s)
			{
				int i = s.IndexOf ('_');
				if (i == -1)
					return s;
				var sb = new StringBuilder (i);
				sb.Append (s, 0, i);
				for (; i < s.Length; i++) {
					if (s [i] == '_') {
						i++;
						if (i >= s.Length)
							break;
					}
					sb.Append (s [i]);
				}
				return sb.ToString ();
			}
		}

		class ConfigurationModel : IConfigurationModel
		{
			public ConfigurationModel (string originalId)
			{
				OriginalId = originalId;
				DisplayString = originalId.Replace ("|", " | ");
			}

			public string OriginalId { get; private set; }
			public string DisplayString { get; private set; }
		}

		class RunConfigurationModel : IRunConfigurationModel
		{
			public RunConfigurationModel (SolutionRunConfiguration config)
			{
				RunConfiguration = config;
				OriginalId = config.Id;
				DisplayString = config.Name;
			}

			public SolutionRunConfiguration RunConfiguration { get; set; }
			public string OriginalId { get; private set; }
			public string DisplayString { get; private set; }
		}

		class SearchMenuModel : ISearchMenuModel
		{
			public SearchMenuModel (string displayString, string category)
			{
				DisplayString = displayString;
				Category = category;
			}

			public void NotifyActivated ()
			{
				if (Activated != null)
					Activated (null, null);
			}

			public string DisplayString { get; private set; }
			public string Category { get; set; }
			public event EventHandler Activated;
		}
	}
}

