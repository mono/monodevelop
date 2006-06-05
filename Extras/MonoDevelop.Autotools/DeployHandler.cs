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
using System.Text;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Content;

using Gtk;

namespace MonoDevelop.Autotools
{
	public enum Commands
	{
		Deploy
	}
	
	class NodeExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (Combine).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof (AddinCommandHandler); }
		}
	}
	
	public class AddinCommandHandler : NodeCommandHandler
	{
		//private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		[CommandHandler (Commands.Deploy)]
		protected void OnDeploy()
		{
			Combine combine = (Combine) CurrentNode.DataItem;
			
			SolutionDeployer deployer = new SolutionDeployer();
			if ( !deployer.CanDeploy ( combine ) )
			{
				string msg =  GettextCatalog.GetString ( "An Autotools setup could not be created for this solution.  One (or more) child projects lack an Autotools implementation." ); 
				MessageDialog md = new MessageDialog (null, 
						DialogFlags.Modal, 
						MessageType.Error, 
						ButtonsType.Ok, 
						msg );
				md.Run ();
				md.Destroy();
				return;
			}

			//TODO: try/catch this.  various exceptions may occur in the process
			deployer.Deploy(combine);
		}		
	}	
}

