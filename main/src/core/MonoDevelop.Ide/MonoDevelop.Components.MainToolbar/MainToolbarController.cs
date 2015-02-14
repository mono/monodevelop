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
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbarController : ICommandBar
	{
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

		public MainToolbarController (IMainToolbarView toolbarView)
		{
			ToolbarView = toolbarView;
			// Attach run button click handler.
			toolbarView.RunButtonClicked += HandleStartButtonClicked;
			var items = new[] {
				new SearchMenuItem (GettextCatalog.GetString ("Search Files"), "file"),
				new SearchMenuItem (GettextCatalog.GetString ("Search Types"), "type"),
				new SearchMenuItem (GettextCatalog.GetString ("Search Members"), "member"),
			};

			// Attach menu category handlers.
			foreach (var item in items)
				item.Activated += (o, e) => SetSearchCategory (item.Category);
			ToolbarView.SearchMenuItems = items;

			// Register Search Entry handlers.
			ToolbarView.SearchEntryChanged += HandleSearchEntryChanged;
			ToolbarView.SearchEntryActivated += HandleSearchEntryActivated;
			ToolbarView.SearchEntryKeyPressed += HandleSearchEntryKeyPressed;
			ToolbarView.SearchEntryResized += (o, e) => PositionPopup ();
			ToolbarView.SearchEntryLostFocus += (o, e) => ToolbarView.SearchText = "";

			IdeApp.Workbench.RootWindow.WidgetEvent += delegate(object o, WidgetEventArgs args) {
				if (args.Event is Gdk.EventConfigure)
					PositionPopup ();
			};

			// Update Search Entry label initially and on keybinding change.
			var cmd = IdeApp.CommandService.GetCommand (Commands.NavigateTo);
			cmd.KeyBindingChanged += delegate {
				UpdateSearchEntryLabel ();
			};
			UpdateSearchEntryLabel ();

			// Register this controller as a commandbar.
			IdeApp.CommandService.RegisterCommandBar (this);
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
			ToolbarView.FocusSearchBar ();
		}

		public void SetSearchCategory (string category)
		{
			IdeApp.Workbench.Present ();
			ToolbarView.SearchCategory = category + ":";
		}

		public void ShowCommandBar (string barId)
		{
			ToolbarView.ShowCommandBar (barId);
		}

		public void HideCommandBar (string barId)
		{
			ToolbarView.HideCommandBar (barId);
		}

		static void HandleStartButtonClicked (object sender, EventArgs e)
		{
			OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled)
				IdeApp.CommandService.DispatchCommand (ci.Command.Id);
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

		void ICommandBar.Update (object activeTarget)
		{
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
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			toolbarEnabled = enabled;
			ToolbarView.RunButtonSensitivity = enabled;
			ToolbarView.SearchSensivitity = enabled;
		}
		#endregion
	}
}

