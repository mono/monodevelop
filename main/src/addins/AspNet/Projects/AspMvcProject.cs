// 
// AspMvcProject.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNet.Projects
{
	abstract class AspMvcProject : AspNetAppProject
	{
		protected AspMvcProject ()
		{
		}
		
		protected AspMvcProject (string languageName)
			: base (languageName)
		{
		}
		
		protected AspMvcProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}	
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new AspMvcProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));			
			return conf;
		}

		public override IEnumerable<string> GetProjectTypes ()
		{
			yield return "AspNetMvc";
			foreach (var t in base.GetProjectTypes ())
				yield return t;
		}

		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.CanReferenceAssembliesTargetingFramework (MonoDevelop.Core.Assemblies.TargetFrameworkMoniker.NET_3_5);
		}

		public override bool IsAspMvcProject {
			get {
				return true;
			}
		}
	}

	class AspMvc1Project : AspMvcProject
	{
		public AspMvc1Project ()
		{
		}

		public AspMvc1Project (string languageName)
			: base (languageName)
		{
		}

		public AspMvc1Project (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}

		protected override string GetDefaultAspNetMvcVersion ()
		{
			return "1.0.0.0";
		}
	}

	class AspMvc2Project : AspMvcProject
	{
		public AspMvc2Project ()
		{
		}

		public AspMvc2Project (string languageName)
			: base (languageName)
		{
		}

		public AspMvc2Project (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}

		protected override string GetDefaultAspNetMvcVersion ()
		{
			return "2.0.0.0";
		}
	}

	class AspMvc3Project : AspMvcProject
	{
		public AspMvc3Project ()
		{
		}

		public AspMvc3Project (string languageName)
			: base (languageName)
		{
		}

		public AspMvc3Project (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}

		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.CanReferenceAssembliesTargetingFramework (MonoDevelop.Core.Assemblies.TargetFrameworkMoniker.NET_4_0);
		}

		protected override string GetDefaultAspNetMvcVersion ()
		{
			return "3.0.0.0";
		}
	}

	class AspMvc4Project : AspMvcProject
	{
		public AspMvc4Project ()
		{
		}
		
		public AspMvc4Project (string languageName)
			: base (languageName)
		{
		}
		
		public AspMvc4Project (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.CanReferenceAssembliesTargetingFramework (MonoDevelop.Core.Assemblies.TargetFrameworkMoniker.NET_4_0);
		}

		protected override string GetDefaultAspNetMvcVersion ()
		{
			return "4.0.0.0";
		}
	}
}
