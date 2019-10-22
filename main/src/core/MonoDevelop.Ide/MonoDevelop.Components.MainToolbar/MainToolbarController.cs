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
using System.Threading.Tasks;
using Gtk;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Commands.ExtensionNodes;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Projects;

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
			ToolbarView.PerformCommand += HandleSearchEntryCommand;
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

			IdeApp.ProjectOperations.CurrentSelectedSolutionChanged += HandleCurrentSelectedSolutionChanged;

			IdeApp.Workspace.FirstWorkspaceItemRestored += (sender, e) => {
				IdeApp.Workspace.ConfigurationsChanged += HandleUpdateCombos;
				IdeApp.Workspace.ActiveConfigurationChanged += HandleUpdateCombos;

				IdeApp.Workspace.SolutionLoaded += HandleSolutionLoaded;
				IdeApp.Workspace.SolutionUnloaded += HandleUpdateCombos;
				IdeApp.ProjectOperations.CurrentSelectedSolutionChanged += HandleUpdateCombos;

				UpdateCombos ();
			};

			IdeApp.Workspace.LastWorkspaceItemClosed += (sender, e) => {
				IdeApp.Workspace.ConfigurationsChanged -= HandleUpdateCombos;
				IdeApp.Workspace.ActiveConfigurationChanged -= HandleUpdateCombos;

				IdeApp.Workspace.SolutionLoaded -= HandleSolutionLoaded;
				IdeApp.Workspace.SolutionUnloaded -= HandleUpdateCombos;

				IdeApp.ProjectOperations.CurrentSelectedSolutionChanged -= HandleUpdateCombos;

				StatusBar.ShowReady ();
			};

			AddinManager.ExtensionChanged += OnExtensionChanged;
		}

		public void Initialize ()
		{
			var items = new[] {
				new SearchMenuModel (GettextCatalog.GetString ("Search Files"), "file"),
				new SearchMenuModel (GettextCatalog.GetString ("Search Types"), "type"),
				new SearchMenuModel (GettextCatalog.GetString ("Search Members"), "member"),
				new SearchMenuModel (GettextCatalog.GetString ("Search Commands"), "command"),
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
					configurationMergers.Clear ();
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
					var an = DockNotebook.DockNotebook.ActiveNotebook;
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
					DockNotebook.DockNotebook.ActiveNotebook = an;
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
			if (config == null)
				return;
			if (configurationMergers.Count > 1 || configurationMergers.Count == 0) {
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
							ToolbarView.ActiveConfiguration = item;
							UpdateBuildConfiguration ();
							selected = true;
							break;
						}
					}

					if (!selected) {
						ToolbarView.ActiveConfiguration = ToolbarView.ConfigurationModel.First ();
						UpdateBuildConfiguration ();
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
				if (target == null || item.Project != project)
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
							if (runtimeModel == null) {
								LoggingService.LogError ($"No runtimeModel for {startupProject.Item1.Name}");
								continue;
							}
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

		void HandleSolutionLoaded (object sender, EventArgs e)
		{
			if (currentSolution != null)
				return;

			UpdateCombos ();
		}

		void HandleUpdateCombos (object sender, EventArgs e)
		{

			UpdateCombos ();
		}

		void HandleCurrentSelectedSolutionChanged (object sender, SolutionEventArgs e)
		{
			if (currentSolution != null) {
				currentSolution.StartupConfigurationChanged -= HandleStartupItemChanged;
				currentSolution.Saved -= HandleSolutionSaved;
				currentSolution.EntrySaved -= HandleSolutionEntrySaved;
			}

			currentSolution = e.Solution;

			if (currentSolution != null) {
				currentSolution.StartupConfigurationChanged += HandleStartupItemChanged;
				currentSolution.Saved += HandleSolutionSaved;
				currentSolution.EntrySaved += HandleSolutionEntrySaved;
			}

			TrackStartupProject ();
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

		void HandleSolutionSaved (object sender, EventArgs e)
		{
			UpdateCombos ();
		}

		void HandleSolutionEntrySaved (object sender, SolutionItemSavedEventArgs e)
		{
			// Skip the per-project update when a solution is being saved. The solution Saved callback will do the final update.
			if (!e.SavingSolution)
				HandleSolutionSaved (sender, e);
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

			popup.IgnoreRepositionWindow = false;

			var anchor = ToolbarView.PopupAnchor;
			if (IdeApp.Workbench.RootWindow.Visible)
				popup.ShowPopup (anchor, PopupPosition.TopRight);

			if (anchor.GdkWindow == null) {
				var location = new Xwt.Point (anchor.Allocation.X + anchor.Allocation.Width - popup.Size.Width, anchor.Allocation.Y);

				// Need to hard lock the location because Xwt doesn't know that the allocation might be coming from a
				// Cocoa control and thus has been changed to take macOS monitor layout into consideration
				popup.IgnoreRepositionWindow = true;
				popup.Location = location;
			}
		}

		void DestroyPopup ()
		{
			if (popup != null) {
				popup.Close ();
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
				popup.Disposed += delegate {
					popup = null;
					ToolbarView.SearchText = "";
				};
				popup.SelectedItemChanged += delegate {
					var si = popup?.Content?.SelectedItem;
					if (si == null || !si.IsValid)
						return;
					var text = si.DataSource [si.Item].AccessibilityMessage;
					if (string.IsNullOrEmpty (text))
						return;

					ToolbarView.ShowAccessibilityAnnouncement (text);
				};
				PositionPopup ();
				popup.Show ();
			}
			// popup.Update () is thread safe, so run it on a bg thread for faster results
			Task.Run (() => popup.Update (pattern)).Ignore ();
		}

		void HandleSearchEntryActivated (object sender, EventArgs e)
		{
			var pattern = SearchPopupSearchPattern.ParsePattern (ToolbarView.SearchText);
			if (pattern.Pattern == null && pattern.LineNumber > 0) {
				DestroyPopup ();
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc?.GetContent<ITextView> (true) is ITextView view) {
					doc.Select ();
					var snapshot = view.TextBuffer.CurrentSnapshot;
					int lineNumber = Math.Min (Math.Max (1, pattern.LineNumber), snapshot.LineCount);
					var line = snapshot.GetLineFromLineNumber (lineNumber - 1);
					if (line != null) {
						view.Caret.MoveTo (new SnapshotPoint (snapshot, line.Start + Math.Max (0, Math.Min (pattern.Column - 1, line.Length))));
						IdeApp.CommandService.DispatchCommand (ViewCommands.CenterAndFocusCurrentDocument);
						ToolbarView.SearchText = "";
					}
				}
				return;
			}
			if (popup != null)
				popup.OpenFile ();
		}

		void HandleSearchEntryCommand (object sender, SearchEntryCommandArgs args)
		{
			if (args.Command == SearchPopupCommand.Cancel) {
				DestroyPopup ();
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null)
					doc.Select ();
				return;
			}

			if (popup != null) {
				args.Handled = popup.ProcessCommand (args.Command);
			}
		}

		void HandleSearchEntryKeyPressed (object sender, Xwt.KeyEventArgs e)
		{
			if (e.Key == Xwt.Key.Escape) {
				DestroyPopup();
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					doc.Select ();
				return;
			}
			if (popup != null) 
				e.Handled = popup.ProcessKey (e.Key, e.Modifiers);
		}

		string lastSearchText = string.Empty;
		public void FocusSearchBar ()
		{
			IdeApp.Workbench.Present ();
			var text = lastSearchText;
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc?.GetContent<ITextView> () is ITextView view && !view.Selection.IsEmpty) {
				string selected = view.Selection.SelectedSpans[0].GetText ();
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
                .Select (b => new { Label = b.Label, Buttons = b.ChildNodes.OfType<CommandItemCodon> ().Select (n => n.Id) });

			var buttonGroups = new List<ButtonBarGroup> ();
			buttonBarButtons.Clear ();
			foreach (var bar in bars) {
				var group = new ButtonBarGroup (bar.Label);

				buttonGroups.Add (group);
				foreach (string commandId in bar.Buttons) {
					var button = new ButtonBarButton (this, commandId);
					group.Buttons.Add (button);
					buttonBarButtons.Add (button);
				}
			}

			ToolbarView.RebuildToolbar (buttonGroups);
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
			CommandInfo lastCmdInfo;
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
			public string Title { get; set; }
			public bool IsSeparator {
				get { return CommandId == null; }
			}

			public void NotifyPushed ()
			{
				IdeApp.CommandService.DispatchCommand (CommandId, null, Controller.lastCommandTarget, CommandSource.MainToolbar, lastCmdInfo);
			}

			public void Update ()
			{
				if (IsSeparator)
					return;

				if (lastCmdInfo != null) {
					lastCmdInfo.CancelAsyncUpdate ();
					lastCmdInfo.Changed -= LastCmdInfoChanged;
				}
				
				lastCmdInfo = IdeApp.CommandService.GetCommandInfo (CommandId, new CommandTargetRoute (Controller.lastCommandTarget));

				if (lastCmdInfo != null) {
					lastCmdInfo.Changed += LastCmdInfoChanged;
					Update (lastCmdInfo);
				}
			}

			void Update (CommandInfo ci)
			{
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
				if (ci.Text != Title) {
					Title = ci.Text;
					TitleChanged?.Invoke (this, null);
				}
			}

			void LastCmdInfoChanged (object sender, EventArgs e)
			{
				Update (lastCmdInfo); 
			}

			public event EventHandler EnabledChanged;
			public event EventHandler ImageChanged;
			public event EventHandler VisibleChanged;
			public event EventHandler TooltipChanged;
			public event EventHandler TitleChanged;
		}

		class RuntimeModel : IRuntimeModel
		{
			MainToolbarController Controller { get; set; }
			List<IRuntimeModel> children = new List<IRuntimeModel> ();
			public object Command { get; private set; }
			public ExecutionTarget ExecutionTarget { get; private set; }
			string DisplayText = null;
			string image, tooltip;
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
				image = command.Icon;
				tooltip = command.Description;
			}

			public RuntimeModel (MainToolbarController controller, ExecutionTarget target, bool fullText, SolutionItem project) : this (controller)
			{
				if (target == null)
					throw new ArgumentNullException (nameof (target));
				
				ExecutionTarget = target;
				image = target.Image;
				tooltip = target.Tooltip;

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

			public string Image => image;

			public string Tooltip => tooltip;

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
				Enabled = !(target is ExecutionTargetGroup) && target.Enabled;
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
				var sb = StringBuilderCache.Allocate ();
				sb.Append (s, 0, i);
				for (; i < s.Length; i++) {
					if (s [i] == '_') {
						i++;
						if (i >= s.Length)
							break;
					}
					sb.Append (s [i]);
				}
				return StringBuilderCache.ReturnAndFree (sb);
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

	public class SearchEntryCommandArgs : HandledEventArgs
	{
		public SearchPopupCommand Command { get; private set; }

		public SearchEntryCommandArgs (SearchPopupCommand command)
		{
			Command = command;
		}
	}
}

