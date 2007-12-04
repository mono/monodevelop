/*
Copyright (C) 2006  Matthias Braun <matze@braunis.de>
					Scott Ellington <scott.ellington@gmail.com>
 
This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 59 Temple Place - Suite 330,
Boston, MA 02111-1307, USA.
*/

using System;
using System.Collections;
using System.IO;
using MonoDevelop.Core;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;

using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Deployment;

using Gtk;

namespace MonoDevelop.Autotools
{
	public enum Commands
	{
		GenerateFiles,
		SynchWithMakefile
	}
	
	class NodeExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (CombineEntry).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof ( AutotoolsCommandHandler ); }
		}
	}
	
	public class AutotoolsCommandHandler : NodeCommandHandler
	{
		[CommandHandler (Commands.GenerateFiles)]
		protected void OnGenerate()
		{
			CombineEntry entry = (CombineEntry) CurrentNode.DataItem;
			Combine combine = entry as Combine;
			if (combine == null) {
				if (MonoDevelop.Core.Gui.Services.MessageService.AskQuestion (
						GettextCatalog.GetString ("Generating Makefiles is not supported for single projects. Do you want " +
							"to generate them for the full solution - '{0}' ?", entry.RootCombine.Name),
						GettextCatalog.GetString ("Generate Makefiles...")))
					combine = entry.RootCombine;
				else
					return;
			}

			DeployContext ctx = null;
			IProgressMonitor monitor = null;

			GenerateMakefilesDialog dialog = new GenerateMakefilesDialog (combine);
			try {
				if (dialog.Run () != (int) Gtk.ResponseType.Ok)
					return;

				SolutionDeployer deployer = new SolutionDeployer (dialog.GenerateAutotools);
				if ( deployer.HasGeneratedFiles ( combine ) )
				{
					string msg = GettextCatalog.GetString ( "{0} already exist for this solution.  Would you like to overwrite them?", dialog.GenerateAutotools ? "Autotools files" : "Makefiles" );
					if ( !MonoDevelop.Core.Gui.Services.MessageService.AskQuestion ( msg ) )
						return;
				}

				ctx = new DeployContext (new TarballDeployTarget (dialog.GenerateAutotools), "Linux", null);
				monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ( GettextCatalog.GetString("Makefiles Output"), "md-package", true, true);
				deployer.GenerateFiles (ctx, combine, dialog.DefaultConfiguration, monitor);
			} finally {
				dialog.Destroy ();
				if (ctx != null)
					ctx.Dispose ();
				if (monitor != null)
					monitor.Dispose ();
			}
		}
	}
}
