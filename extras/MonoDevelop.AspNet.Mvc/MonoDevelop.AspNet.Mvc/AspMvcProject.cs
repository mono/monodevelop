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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AspNet.Mvc
{
	
	
	public class AspMvcProject : AspNetAppProject
	{
		static void Check24 ()
		{
			Type mr = Type.GetType ("Mono.Runtime");
			if (mr != null) {
				string version = (string) mr.GetMethod ("GetDisplayName", BindingFlags.NonPublic|BindingFlags.Static).Invoke (null, null);
				//MD only builds on 2.0 or later
				if (version.StartsWith ("Mono 2.0") || version.StartsWith ("Mono 2.2"))
					MonoDevelop.Core.Gui.MessageService.ShowWarning ("ASP.NET MVC projects only build and run on Mono 2.4 or later");
			}
		}
		
		public AspMvcProject ()
		{
			Check24 ();
		}
		
		public AspMvcProject (string languageName)
			: base (languageName)
		{
			Check24 ();
		}
		
		public AspMvcProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			Check24 ();
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
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
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
		
		public IList<string> GetCodeTemplates (string type)
		{
			List<string> files = new List<string> ();
			HashSet<string> names = new HashSet<string> ();
			
			string asmDir = Path.GetDirectoryName (typeof (AspMvcProject).Assembly.Location);
			
			string[] dirs = new string[] {
				Path.Combine (Path.Combine (this.BaseDirectory, "CodeTemplates"), type),
				Path.Combine (Path.Combine (asmDir, "CodeTemplates"), type)
			};
			
			foreach (string directory in dirs)
				if (Directory.Exists (directory))
					foreach (string file in Directory.GetFiles (directory, "*.tt", SearchOption.TopDirectoryOnly))
						if (names.Add (Path.GetFileName (file)))
						    files.Add (file);
			
			return files;
		}
		
		protected override void PopulateSupportFileList (MonoDevelop.Projects.FileCopySet list, string solutionConfiguration)
		{
			base.PopulateSupportFileList (list, solutionConfiguration);
			
			//HACK: workaround for MD not local-copying package references
			foreach (ProjectReference projectReference in References) {
				if (projectReference.Package != null && projectReference.Package.Name == "system.web.mvc") {
					if (projectReference.LocalCopy && projectReference.ReferenceType == ReferenceType.Gac)
						foreach (MonoDevelop.Core.Assemblies.SystemAssembly assem in projectReference.Package.Assemblies)
							list.Add (assem.Location);
					break;
				}
			}
		}
	}
}
