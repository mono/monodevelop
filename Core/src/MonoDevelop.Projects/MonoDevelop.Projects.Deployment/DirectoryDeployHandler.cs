//
// DirectoryDeployHandler.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Deployment.Extensions;

namespace MonoDevelop.Projects.Deployment
{
	public class DirectoryDeployHandler: IDeployHandler
	{
		public string Id {
			get { return "MonoDevelop.DirectoryDeploy"; } 
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Directory"); } 
		}
		
		public string Icon {
			get { return "md-closed-folder"; }
		}
		
		public bool CanDeploy (CombineEntry entry) 
		{
			return entry is Project || entry is Combine;
		}
		
		public DeployTarget CreateTarget (CombineEntry entry)
		{
			return new DirectoryDeployTarget ();
		}
		
		public void Deploy (IProgressMonitor monitor, DeployTarget target)
		{
			if (target.CombineEntry is Project)
				DeployProject (monitor, target, (Project) target.CombineEntry);
			else {
				Combine c = (Combine) target.CombineEntry;
				foreach (Project p in c.GetAllProjects ())
					DeployProject (monitor, target, p);
			}
		}
		
		void DeployProject (IProgressMonitor monitor, DeployTarget target, Project p)
		{
			DirectoryDeployTarget dt = (DirectoryDeployTarget) target;
			DeployFileCollection deployFiles = p.GetDeployFiles ();
			
			dt.CopierConfiguration.CopyFiles (monitor, new SimpleFileReplacePolicy (FileReplaceMode.Replace), deployFiles);
		}
	}
}
