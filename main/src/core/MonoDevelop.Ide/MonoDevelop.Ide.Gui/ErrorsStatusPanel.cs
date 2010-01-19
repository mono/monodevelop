// 
// ErrorsStatusPanel.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	class ErrorsStatusPanel: ExpandableStatusBarPanel
	{
		Label labelErrors;
		Label labelWarnings;
		
		Gtk.HBox expandedPanel;
		Button listButton;
		CheckButton errorsCheck;
		CheckButton warningsCheck;
		Button outputButton;
		Button firstErrorButton;
		
		public ErrorsStatusPanel ()
		{
			MainBox.Spacing = 3;
			
			Image img = new Image (Gtk.Stock.DialogError, IconSize.Menu);
			MainBox.PackStart (img, false, false, 0);
			labelErrors = new Label ();
			MainBox.PackStart (labelErrors, false, false, 0);
			
			img = new Image (Gtk.Stock.DialogWarning, IconSize.Menu);
			MainBox.PackStart (img, false, false, 0);
			labelWarnings = new Label ();
			MainBox.PackStart (labelWarnings, true, true, 0);
			
			Gtk.Arrow arrow = new Gtk.Arrow (ArrowType.Down, ShadowType.None);
			MainBox.PackStart (arrow, false, false, 0);
			ShowAll ();
			
			TaskService.Errors.TasksAdded += HandleTaskServiceErrorsTasksAdded;
			TaskService.Errors.TasksRemoved += HandleTaskServiceErrorsTasksAdded;
			
			expandedPanel = new HBox (false, 3);
			
			outputButton = new Button (GettextCatalog.GetString ("Build Output"));
			outputButton.Relief = ReliefStyle.None;
			outputButton.Clicked += delegate {
				IdeApp.ProjectOperations.LastBuildJob.ToggleStatusView (outputButton, PositionType.Top, 0, false);
			};
			expandedPanel.PackStart (outputButton, false, false, 0);
			
			firstErrorButton = new Button (GettextCatalog.GetString ("Show First Error"));
			firstErrorButton.Relief = ReliefStyle.None;
			firstErrorButton.Clicked += delegate {
				TaskService.Errors.ResetLocationList ();
				IdeApp.Workbench.ActiveLocationList = TaskService.Errors;
				IdeApp.Workbench.ShowNext ();
			};
			expandedPanel.PackStart (firstErrorButton, false, false, 0);
			
			listButton = new Button (GettextCatalog.GetString ("Error List"));
			listButton.Relief = ReliefStyle.None;
			listButton.Clicked += delegate {
				TaskService.ShowErrors ();
			};
			expandedPanel.PackStart (listButton, false, false, 0);
			
			errorsCheck = new CheckButton ();
			errorsCheck.Add (new Image (Gtk.Stock.DialogError, IconSize.Menu));
			errorsCheck.ShowAll ();
			expandedPanel.PackStart (errorsCheck, false, false, 0);
			
			warningsCheck = new CheckButton ();
			warningsCheck.Add (new Image (Gtk.Stock.DialogWarning, IconSize.Menu));
			warningsCheck.ShowAll ();
			expandedPanel.PackStart (warningsCheck, false, false, 0);
			
			expandedPanel.ShowAll ();
			
			UpdateStatus ();
			
			IdeApp.Preferences.ShowMessageBubblesChanged += HandleIdeAppPreferencesShowMessageBubblesChanged;
			HandleIdeAppPreferencesShowMessageBubblesChanged (null, null);
			
			TaskService.BubblesVisibilityChanged += HandleTaskServiceBubblesVisibilityChanged;
			HandleTaskServiceBubblesVisibilityChanged (null, null);
			
			errorsCheck.Toggled += delegate {
				TaskService.ErrorBubblesVisible = errorsCheck.Active;
			};
			warningsCheck.Toggled += delegate {
				TaskService.WarningBubblesVisible = warningsCheck.Active;
			};
		}

		void HandleTaskServiceBubblesVisibilityChanged (object sender, EventArgs e)
		{
			if (errorsCheck.Active != TaskService.ErrorBubblesVisible)
				errorsCheck.Active = TaskService.ErrorBubblesVisible;
			if (warningsCheck.Active != TaskService.WarningBubblesVisible)
				warningsCheck.Active = TaskService.WarningBubblesVisible;
		}

		void HandleIdeAppPreferencesShowMessageBubblesChanged (object sender, PropertyChangedEventArgs e)
		{
			errorsCheck.Visible = IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrors || IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrorsAndWarnings;
			warningsCheck.Visible = IdeApp.Preferences.ShowMessageBubbles == ShowMessageBubbles.ForErrorsAndWarnings;
		}
		
		void UpdateStatus ()
		{
			int ec=0, wc=0;
			foreach (Task t in TaskService.Errors) {
				if (t.Severity == TaskSeverity.Error)
					ec++;
				else if (t.Severity == TaskSeverity.Warning)
					wc++;
			}
			labelErrors.Text = ec.ToString ();
			labelWarnings.Text = wc.ToString ();
			outputButton.Sensitive = IdeApp.IsInitialized && IdeApp.ProjectOperations.LastBuildJob != null;
			firstErrorButton.Sensitive = ec + wc > 0;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			TaskService.Errors.TasksAdded -= HandleTaskServiceErrorsTasksAdded;
			TaskService.Errors.TasksRemoved -= HandleTaskServiceErrorsTasksAdded;
		}


		void HandleTaskServiceErrorsTasksAdded (object sender, TaskEventArgs e)
		{
			UpdateStatus ();
		}
		
		public override Widget ExpandedPanel {
			get {
				return expandedPanel;
			}
		}
		
		public override Widget MainPanelWidget {
			get { return listButton; }
		}
	}
}
