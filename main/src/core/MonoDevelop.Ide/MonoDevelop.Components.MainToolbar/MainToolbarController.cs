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

		readonly PropertyWrapper<bool> searchForMembers = new PropertyWrapper<bool> ("MainToolbar.Search.IncludeMembers", true);
		bool SearchForMembers {
			get { return searchForMembers; }
			set { searchForMembers.Value = value; }
		}

		ConfigurationMerger configurationMerger = new ConfigurationMerger ();
		int ignoreConfigurationChangedCount, ignoreRuntimeChangedCount;
		Solution currentSolution;
		bool settingGlobalConfig;
		SolutionEntityItem currentStartupProject;
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

			executionTargetsChanged = DispatchService.GuiDispatch (new EventHandler ((sender, e) => UpdateCombos ()));

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

			configurationMerger.Load (currentSolution);

			ignoreConfigurationChangedCount++;
			try {
				ToolbarView.ConfigurationModel = Enumerable.Empty<IConfigurationModel> ();
				if (!IdeApp.Workspace.IsOpen) {
					ToolbarView.RuntimeModel = Enumerable.Empty<IRuntimeModel> ();
					return;
				}

				ToolbarView.ConfigurationModel = configurationMerger
					.SolutionConfigurations
					.Distinct ()
					.Select (conf => new ConfigurationModel (conf));
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
				ToolbarView.RuntimeModel = Enumerable.Empty<IRuntimeModel> ();
				if (!IdeApp.Workspace.IsOpen || currentSolution == null || !currentSolution.SingleStartup || currentSolution.StartupItem == null)
					return;

				// Check that the current startup project is enabled for the current configuration
				var solConf = currentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				if (solConf == null || !solConf.BuildEnabledForItem (currentSolution.StartupItem))
					return;

				ExecutionTarget previous = null;
				int runtimes = 0;

				var list = new List<RuntimeModel> ();
				foreach (var target in configurationMerger.GetTargetsForConfiguration (IdeApp.Workspace.ActiveConfigurationId, true)) {
					if (target is ExecutionTargetGroup) {
						var devices = (ExecutionTargetGroup) target;

						if (previous != null)
							list.Add (new RuntimeModel (this, target: null));

						list.Add (new RuntimeModel (this, target));
						foreach (var device in devices) {
							if (device is ExecutionTargetGroup) {
								var versions = (ExecutionTargetGroup) device;

								if (versions.Count > 1) {
									var parent = new RuntimeModel (this, device) {
										IsIndented = true,
									};
									list.Add (parent);

									foreach (var version in versions) {
										list.Add (new RuntimeModel (this, version, parent));
										runtimes++;
									}
								} else {
									list.Add (new RuntimeModel (this, versions[0]) {
										IsIndented = true,
									});
									runtimes++;
								}
							} else {
								list.Add (new RuntimeModel (this, device) {
									IsIndented = true,
								});
								runtimes++;
							}
						}
					} else {
						if (previous is ExecutionTargetGroup) {
							list.Add (new RuntimeModel (this, target: null));
						}

						list.Add (new RuntimeModel (this, target));
						runtimes++;
					}

					previous = target;
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
									list.Add (new RuntimeModel (this, target: null));
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

		void HandleRuntimeChanged (object sender, HandledEventArgs e)
		{
			if (ignoreRuntimeChangedCount == 0) {
				var runtime = (RuntimeModel)ToolbarView.ActiveRuntime;
				if (runtime != null && runtime.Command != null) {
					e.Handled = runtime.NotifyActivated ();
					return;
				}

				NotifyConfigurationChange ();
			}
		}

		void HandleConfigurationChanged (object sender, EventArgs e)
		{
			if (ignoreConfigurationChangedCount == 0)
				NotifyConfigurationChange ();
		}

		void UpdateBuildConfiguration ()
		{
			var config = ToolbarView.ActiveConfiguration;
			if (config == null)
				return;

			ExecutionTarget newTarget;
			string fullConfig;

			var runtime = (RuntimeModel)ToolbarView.ActiveRuntime;
			configurationMerger.ResolveConfiguration (config.OriginalId, runtime != null ? runtime.ExecutionTarget : null, out fullConfig, out newTarget);
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
			SelectActiveRuntime ();
		}

		void SelectActiveConfiguration ()
		{
			string name = configurationMerger.GetUnresolvedConfiguration (IdeApp.Workspace.ActiveConfigurationId);

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

			SelectActiveRuntime ();
		}

		bool SelectActiveRuntime (ref bool selected, ref ExecutionTarget defaultTarget, ref int defaultIter)
		{
			var runtimes = ToolbarView.RuntimeModel.Cast<RuntimeModel> ().ToList ();
			for (int iter = 0; iter < runtimes.Count; ++iter) {
				var item = runtimes [iter];
				using (var model = item.GetMutableModel ()) {
					if (!model.Enabled)
						continue;
				}

				var target = item.ExecutionTarget;
				if (target == null || !target.Enabled)
					continue;

				if (target is ExecutionTargetGroup)
					if (item.HasChildren)
						continue;

				if (defaultTarget == null) {
					defaultTarget = target;
					defaultIter = iter;
				}

				if (target.Id == IdeApp.Workspace.PreferredActiveExecutionTarget) {
					IdeApp.Workspace.ActiveExecutionTarget = target;
					ToolbarView.ActiveRuntime = ToolbarView.RuntimeModel.ElementAt (iter);
					UpdateBuildConfiguration ();
					selected = true;
					return true;
				}

				if (target.Equals (IdeApp.Workspace.ActiveExecutionTarget)) {
					ToolbarView.ActiveRuntime = ToolbarView.RuntimeModel.ElementAt (iter);
					UpdateBuildConfiguration ();
					selected = true;
				}
			}

			return false;
		}

		void SelectActiveRuntime ()
		{
			ignoreRuntimeChangedCount++;

			try {
				if (ToolbarView.RuntimeModel.Any ()) {
					ExecutionTarget defaultTarget = null;
					bool selected = false;
					int defaultIter = 0;

					if (!SelectActiveRuntime (ref selected, ref defaultTarget, ref defaultIter) && !selected) {
						if (defaultTarget != null) {
							IdeApp.Workspace.ActiveExecutionTarget = defaultTarget;
							ToolbarView.ActiveRuntime = ToolbarView.RuntimeModel.ElementAt (defaultIter);
						}
						UpdateBuildConfiguration ();
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
				currentSolution.StartupItemChanged -= HandleStartupItemChanged;
				currentSolution.Saved -= HandleUpdateCombos;
			}

			currentSolution = e.Solution;

			if (currentSolution != null) {
				currentSolution.StartupItemChanged += HandleStartupItemChanged;
				currentSolution.Saved += HandleUpdateCombos;
			}

			TrackStartupProject ();

			UpdateCombos ();
		}

		void TrackStartupProject ()
		{
			if (currentStartupProject != null && ((currentSolution != null && currentStartupProject != currentSolution.StartupItem) || currentSolution == null)) {
				currentStartupProject.ExecutionTargetsChanged -= executionTargetsChanged;
				currentStartupProject.Saved -= HandleUpdateCombos;
			}

			if (currentSolution != null) {
				currentStartupProject = currentSolution.StartupItem;
				if (currentStartupProject != null) {
					currentStartupProject.ExecutionTargetsChanged += executionTargetsChanged;
					currentStartupProject.Saved += HandleUpdateCombos;
				}
			}
			else
				currentStartupProject = null;
		}

		void HandleUpdateCombos (object sender, EventArgs e)
		{
			UpdateCombos ();
		}

		void HandleStartupItemChanged (object sender, EventArgs e)
		{
			TrackStartupProject ();
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
				GettextCatalog.GetString ("Press '{0}' to search", KeyBindingManager.BindingToDisplayLabel (info.AccelKey, false)) :
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
					doc.Editor.Caret.Location = new Mono.TextEditor.DocumentLocation (pattern.LineNumber, pattern.Column > 0 ? pattern.Column : 1);
					doc.Editor.CenterToCaret ();
					doc.Editor.Parent.StartCaretPulseAnimation ();
				}
				return;
			}
			if (popup != null)
				popup.OpenFile ();
		}

		void HandleSearchEntryKeyPressed (object sender, Xwt.KeyEventArgs e)
		{
			if (e.Key == Xwt.Key.Escape) {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					DestroyPopup ();
					doc.Select ();
				}
				return;
			}
			if (popup != null) {
				e.Handled = popup.ProcessKey (e.Key, e.Modifiers);
			}
		}

		public void FocusSearchBar ()
		{
			IdeApp.Workbench.Present ();
			ToolbarView.FocusSearchBar ();
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

			RuntimeModel (MainToolbarController controller)
			{
				Controller = controller;
			}

			public RuntimeModel (MainToolbarController controller, ActionCommand command) : this (controller)
			{
				Command = command.Id;
			}

			public RuntimeModel (MainToolbarController controller, ExecutionTarget target) : this (controller)
			{
				ExecutionTarget = target;
			}

			public RuntimeModel (MainToolbarController controller, ExecutionTarget target, RuntimeModel parent) : this (controller, target)
			{
				if (parent == null)
					HasParent = false;
				else {
					HasParent = true;
					parent.HasChildren = true;
					parent.AddChild (this);
				}
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
				private set;
			}

			public bool HasParent {
				get;
				set;
			}

			public bool IsSeparator {
				get { return Command == null && ExecutionTarget == null; }
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

			public IRuntimeMutableModel GetMutableModel ()
			{
				return Command != null ? new RuntimeMutableModel (Controller, Command) : new RuntimeMutableModel (ExecutionTarget, HasParent);
			}
		}

		class RuntimeMutableModel : IRuntimeMutableModel
		{
			public RuntimeMutableModel (MainToolbarController controller, object command)
			{
				var ci = IdeApp.CommandService.GetCommandInfo (command, new CommandTargetRoute (controller.lastCommandTarget));
				Visible = ci.Visible;
				Enabled = ci.Enabled;
				DisplayString = FullDisplayString = RemoveUnderline (ci.Text);
			}

			public RuntimeMutableModel (ExecutionTarget target, bool hasParent)
			{
				Enabled = !(target is ExecutionTargetGroup);
				Visible = true;
				if (target == null)
					DisplayString = FullDisplayString = string.Empty;
				else {
					FullDisplayString = target.FullName;
					DisplayString = !hasParent ? target.FullName : target.Name;
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
				var sb = new StringBuilder (s.Substring (0, i));
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

