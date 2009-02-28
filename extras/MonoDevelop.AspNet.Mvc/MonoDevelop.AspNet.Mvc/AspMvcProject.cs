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
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AspNet.Mvc
{
	
	
	public class AspMvcProject : AspNetAppProject
	{
		public AspMvcProject ()
		{
		}
		
		public AspMvcProject (string languageName)
			: base (languageName)
		{
		}
		
		public AspMvcProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}	
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			AspMvcProjectConfiguration conf = new AspMvcProjectConfiguration ();
			
			conf.Name = name;
			conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);			
			return conf;
		}
		
		public override string ProjectType {
			get  { return "AspNetMvc"; }
		}
		
		public override bool SupportsFramework (MonoDevelop.Core.TargetFramework framework)
		{
			return framework.IsCompatibleWithFramework ("3.5");
		}
		
		public override IEnumerable<string> GetSpecialDirectories ()
		{
			foreach (string s in base.GetSpecialDirectories ())
				yield return s;
			yield return "Views";
			yield return "Models";
			yield return "Controllers";
		}
	}
}
