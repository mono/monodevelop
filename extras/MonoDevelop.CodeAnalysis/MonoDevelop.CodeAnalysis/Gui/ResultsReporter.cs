using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.CodeAnalysis.Gui {
	
	/// <summary>
	/// Class that interacts with MonoDevelop GUI.
	/// </summary>
	static class ResultsReporter {
		private static TaskService task_service = IdeApp.Services.TaskService;	
		private static double work_complete = 0.0;
		
		/// <value>
		/// Status bar complete work amount (0 to 1).
		/// </value>
		public static double WorkComplete {
			get { return work_complete; }
			set {
				work_complete = value;
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workbench.StatusBar.SetProgressFraction (value);
				});
			}
		}		
		
		/// <summary>
		/// Informs the GUI that analysis has started.
		/// </summary>
		public static void AnalysisStarted (string entryName)
		{
			DispatchService.GuiDispatch (delegate {
				ResetProgressBar ();
				IdeApp.Workbench.StatusBar.BeginProgress (AddinCatalog.GetString ("Analyzing {0}...", entryName));
				ResultsReporter.task_service.ClearExceptCommentTasks ();
			});
		}		

		/// <summary>
		/// Informs the GUI that analysis has finished.
		/// </summary>
		public static void AnalysisFinished ()
		{
			DispatchService.GuiDispatch (delegate {
				IdeApp.Workbench.StatusBar.EndProgress ();
				IdeApp.Workbench.StatusBar.ShowMessage (AddinCatalog.GetString ("Analysis has finished."));
				ResetProgressBar ();
			});
		}		
		
		/// <summary>
		/// Reports an error to GUI.
		/// </summary>
		public static void ReportError (CodeAnalysisException ex)
		{
			DispatchService.GuiDispatch (delegate {
				MessageService.ShowError (ex.Message, ex.StackTrace);
			});
		}

		/// <summary>
		/// Displays violation list in GUI.
		/// </summary>
		public static void ReportViolations (IEnumerable<IViolation> violations)
		{
			ResultsReporter.task_service.ShowErrors ();
			
			foreach (IViolation v in violations)
				AddViolation (v);
		}

		/// <summary>
		/// Adds a violation to GUI (currently, Task View)
		/// </summary>
		private static void AddViolation (IViolation v)
		{
			// TODO: replace Task View with our own GUI			
			TaskType type = TaskType.Warning;
			
			if ((v.Severity == Severity.Critical || v.Severity == Severity.High)
			    && (v.Confidence == Confidence.Total || v.Confidence == Confidence.High))
				type = TaskType.Error;
			
			string text = v.Problem + Environment.NewLine + v.Solution;

			// TODO: handle Location
			Task task = new Task (v.Location.File, text, v.Location.Column, v.Location.Line, type, MainAnalyzer.CurrentProject);
			task_service.Add (task);				  
		}
		
		static void ResetProgressBar ()
		{
			WorkComplete = 0.0;
		}
	}
}
