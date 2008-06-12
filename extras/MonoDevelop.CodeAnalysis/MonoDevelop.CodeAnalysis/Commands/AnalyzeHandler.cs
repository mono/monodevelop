using System;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;


namespace MonoDevelop.CodeAnalysis {
	
	/// <summary>
	/// Class that handles CodeAnalysisCommands.
	/// </summary>
	internal class AnalyzeHandler: CommandHandler {
		
		protected override void Run ()
		{
			SolutionItem entry = IdeApp.ProjectOperations.CurrentSelectedItem as SolutionItem;
			
			if (entry != null && MainAnalyzer.CanAnalyze (entry))
				MainAnalyzer.BeginAnalysis (entry);
		}

		protected override void Update (CommandInfo info)
		{
			SolutionItem entry = IdeApp.ProjectOperations.CurrentSelectedItem as SolutionItem;

			if (entry == null) {
				info.Enabled = info.Visible = false;
				return;
			}
			
			info.Enabled = !(MainAnalyzer.IsBusy);
			info.Visible = MainAnalyzer.CanAnalyze (entry);
			info.Text = AddinCatalog.GetString ("Analyze {0}", entry.Name);
		}	
	}
}
