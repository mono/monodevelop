// 
// DotNetAssemblyProject.cs
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
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Projects
{
	[ProjectModelDataItem ("DotNetProject")]
	public class DotNetAssemblyProject: DotNetProject
	{
		public DotNetAssemblyProject ()
		{
		}

		public DotNetAssemblyProject (string languageName) : base (languageName)
		{
		}

		public DotNetAssemblyProject (string languageName, ProjectCreateInformation projectCreateInfo, XmlElement projectOptions):
			base (languageName, projectCreateInfo, projectOptions)
		{
		}
		
		public override bool SupportsFramework (TargetFramework framework)
		{
			// DotNetAssemblyProject can only generate assemblies for the regular framework.
			// Special frameworks such as Moonlight or MonoTouch must subclass DotNetProject directly.
			if (!framework.CanReferenceAssembliesTargetingFramework (TargetFrameworkMoniker.NET_1_1))
				return false;
			else
				return base.SupportsFramework (framework);
		}
		
		public override TargetFrameworkMoniker GetDefaultTargetFrameworkForFormat (FileFormat format)
		{
			switch (format.Id) {
			case "MSBuild05":
				return TargetFrameworkMoniker.NET_2_0;
			case "MSBuild08":
				return TargetFrameworkMoniker.NET_2_0;
			case "MSBuild10":
			case "MSBuild12":
				return TargetFrameworkMoniker.NET_4_0;
			}
			return Services.ProjectService.DefaultTargetFramework.Id;
		}
		
		protected override string GetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
		{
			if (CompileTarget == CompileTarget.Library)
				return string.Empty;
			
			// Guess a good default platform for the project
			if (projectCreateInfo.ParentFolder != null && projectCreateInfo.ParentFolder.ParentSolution != null) {
				ItemConfiguration conf = projectCreateInfo.ParentFolder.ParentSolution.GetConfiguration (projectCreateInfo.ActiveConfiguration);
				if (conf != null)
					return conf.Platform;
				else {
					string curName, curPlatform, bestPlatform = null;
					string sconf = projectCreateInfo.ActiveConfiguration.ToString ();
					ItemConfiguration.ParseConfigurationId (sconf, out curName, out curPlatform);
					foreach (ItemConfiguration ic in projectCreateInfo.ParentFolder.ParentSolution.Configurations) {
						if (ic.Platform == curPlatform)
							return curPlatform;
						if (ic.Name == curName)
							bestPlatform = ic.Platform;
					}
					if (bestPlatform != null)
						return bestPlatform;
				}
			}
			return Services.ProjectService.DefaultPlatformTarget;
		}
	}
}
