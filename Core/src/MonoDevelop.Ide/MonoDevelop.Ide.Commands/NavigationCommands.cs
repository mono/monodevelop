
using System;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands {
	public enum NavigationCommands {
		NavigateBack,
		NavigateForward
	}
	
	internal class NavigateBack : CommandHandler {
		protected override void Run ()
		{
			NavigationService.Go (-1);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationService.CanNavigateBack;
		}
	}
	
	internal class NavigateForward : CommandHandler {
		protected override void Run ()
		{
			NavigationService.Go (1);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationService.CanNavigateForward;
		}
	}
}
