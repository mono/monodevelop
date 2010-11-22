// 
// MonoDroidBuildExtension.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Assemblies;
using System.Xml;
using System.Text;
using System.Diagnostics;

namespace MonoDevelop.MonoDroid
{
	
	public class MonoDroidBuildExtension : ProjectServiceExtension
	{
		
		public MonoDroidBuildExtension ()
		{
		}

		protected override BuildResult Build (IProgressMonitor monitor, IBuildTarget item, ConfigurationSelector configuration)
		{
			if (!(item is MonoDroidProject))
				return base.Build (monitor, item, configuration);

			MonoDroidProject project = (MonoDroidProject) item;
			TargetFramework requiredFramework = Runtime.SystemAssemblyService.GetTargetFramework ("4.0");

			// Check that we support 4.0 to infer we are at Mono 2.8 at least.
			if (!project.TargetRuntime.IsInstalled (requiredFramework)) {
				var message = "Mono 2.8 or newer is required.";
				MessageService.GenericAlert (MonoDevelop.Ide.Gui.Stock.MonoDevelop, message,
						"Mono 2.8 or newer is requiered. Please go to http://www.mono-project.com to update your installation.",
						AlertButton.Ok);

				var buildResult = new BuildResult ();
				buildResult.AddError (message);
				return buildResult;
			}

			return base.Build (monitor, item, configuration);
		}
	}
}
