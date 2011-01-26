
using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	public enum Commands
	{
		AddAddinDependency,
		AddExtension,
		AddNode,
		AddNodeBefore,
		AddNodeAfter,
		ExtensionModelBrowser,
		AddExtensionPoint,
	}
	
	class ExtensionModelBrowserHandler: CommandHandler
	{
		protected override void Run ()
		{
			AddinAuthoringService.ShowExtensionModelBrowser ();
		}
	}
}
