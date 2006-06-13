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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Content;

using Gtk;

namespace MonoDevelop.Autotools
{
	public enum Commands
	{
		Generate,
		Create
	}
	
	class NodeExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (Combine).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof ( AutotoolsCommandHandler ); }
		}
	}
	
	public class AutotoolsCommandHandler : NodeCommandHandler
	{
		[CommandHandler (Commands.Generate)]
		protected void OnGenerate()
		{
			Combine combine = (Combine) CurrentNode.DataItem;
			SolutionDeployer deployer = new SolutionDeployer();
			
			if ( deployer.IsDeployed ( combine ) )
			{
				string msg = GettextCatalog.GetString ( "Autotools files already exist for this solution.  Would you like to overwrite them?" );
				if ( !MonoDevelop.Core.Gui.Services.MessageService.AskQuestion ( msg ) )
					return;
			}
			
			if ( !deployer.CanDeploy ( combine ) )
			{
				ShowCannotGenMessage ();
				return;
			}

			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ( GettextCatalog.GetString("Autotools Output"), "md-package", true, true))	
			{
				deployer.Deploy (combine, monitor);
			}
		}	

		[CommandHandler (Commands.Create)]
		protected void OnCreate()
		{
			Combine combine = (Combine) CurrentNode.DataItem;
			SolutionDeployer deployer = new SolutionDeployer();

			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor 
					( GettextCatalog.GetString("Autotools Output"), "md-package", true, true))	
			{
				if ( !deployer.IsDeployed ( combine ) )
				{
					string msg = GettextCatalog.GetString ( "In order to continue, Autotools files must be generated.  Is this okay?" );
					if ( !MonoDevelop.Core.Gui.Services.MessageService.AskQuestion ( msg ) )
						return;

					if ( !deployer.CanDeploy ( combine ) )
					{
						ShowCannotGenMessage ();
						return;
					}
					deployer.Deploy (combine, monitor);

					if ( !deployer.IsDeployed ( combine ) ) return;
				}

				MonoDevelop.Core.Gui.Services.DispatchService.BackgroundDispatch (
						new StatefulMessageHandler (RunMakeDist), 
						new MakeDistCommand ( Path.GetDirectoryName ( combine.FileName), combine.Name ) );
			}
		}

		void RunMakeDist ( object base_dir )
		{
			MakeDistCommand mdc = base_dir as MakeDistCommand;
			
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor 
					( GettextCatalog.GetString("Autotools Output"), "md-package", true, true))	
			{
				monitor.BeginTask ( GettextCatalog.GetString( "Running 'make dist'" ) , 1 );
				StringWriter sw = new StringWriter ();
				try
				{
					LogTextWriter chainedOutput = new LogTextWriter ();
					chainedOutput.ChainWriter (monitor.Log);
					chainedOutput.ChainWriter (sw);

					ProcessWrapper process = Runtime.ProcessService.StartProcess ( "make", 
							"dist", 
							mdc.Dir, 
							chainedOutput, 
							monitor.Log, 
							null );
					process.WaitForOutput ();
					
					if ( process.ExitCode > 0 )
						throw new Exception ( GettextCatalog.GetString ("An unspecified error occurred while running 'make dist'") );
								
					monitor.Step ( 1 );
				}
				catch ( Exception e )
				{
					monitor.ReportError ( GettextCatalog.GetString ( "An error occured: "), e );
					return;
				}
				finally 
				{
					monitor.EndTask ();
				}
				monitor.ReportSuccess ( GettextCatalog.GetString ( "Make Dist Successfully Completed" ) );

				// FIXME: hackish way to get the created tarball's filename
				string output = sw.ToString();
				int targz = output.LastIndexOf  ( "tar.gz" );
				int begin = output.LastIndexOf ( '>', targz );
				try { mdc.Name = output.Substring ( begin + 1, (targz - begin) + 5 ); }
				catch ( Exception e )
				{
					monitor.ReportError ( GettextCatalog.GetString ( "An error occured: "), e );
					return;
				}

				MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (
						new StatefulMessageHandler (SaveTarball), 
						mdc );
			}
		}

		void SaveTarball ( object ob )
		{
			MakeDistCommand mdc = ob as MakeDistCommand;
			//string tarball = mdc.Name.ToLower() + ".tar.gz";

			string filename;
			using ( FileSelector fdiag = new FileSelector ( 
						GettextCatalog.GetString ("Save as..."), 
						Gtk.FileChooserAction.Save ) )
			{
				fdiag.CurrentName =  mdc.Name;
				int response = fdiag.Run ();
				fdiag.Hide ();

				if (response != (int) Gtk.ResponseType.Ok) return;

				filename = fdiag.Filename;
			}

			if (filename == null) return;

			// detect preexisting file
			if(File.Exists(filename))
			{
				if (!MonoDevelop.Core.Gui.Services.MessageService.AskQuestion ( 
							GettextCatalog.GetString ("File {0} already exists.  Overwrite?", filename ) ) )
					return;
				else File.Delete ( filename );
			}

			File.Move ( mdc.Dir + "/" + mdc.Name, filename );
		}

		void ShowCannotGenMessage ()
		{
			string msg =  GettextCatalog.GetString ( "An Autotools setup could not be created for this solution.  MonoDevelop does not currently support generating autotools files for one (or more) child projects." ); 
			MessageDialog md = new MessageDialog (null, 
					DialogFlags.Modal, 
					MessageType.Error, 
					ButtonsType.Ok, 
					msg );
			md.Run ();
			md.Destroy();
		}
	}	

	class MakeDistCommand 
	{
		public MakeDistCommand ( string dir, string name )
		{
			Dir = dir;
			Name = name;
		}

		public string Dir;
		public string Name;
	}
}
