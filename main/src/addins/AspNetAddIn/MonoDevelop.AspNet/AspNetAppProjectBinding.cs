//
// AspNetAppProjectBinding.cs: Project binding for AspNetAppProject
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using System.Xml;

namespace MonoDevelop.AspNet
{
	//based heavily on DotNetProjectBinding
	public class AspNetAppProjectBinding : IProjectBinding
	{
		
		public string Name {
			get { return "AspNetApp"; }
		}
		
		public Project CreateProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return CreateProject (lang, info, projectOptions);
		}
		
		public Project CreateProject (string language, ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new AspNetAppProject (language, info, projectOptions);
		}
		
		public Project CreateSingleFileProject (string file)
		{
			if (!CanCreateSingleFileProject (file))
				return null;
			
			//TODO: get page language
			string language = "C#";
					
			ProjectCreateInformation info = new ProjectCreateInformation ();
			info.ProjectName = Path.GetFileNameWithoutExtension (file);
			info.CombinePath = Path.GetDirectoryName (file);
			Project project = CreateProject (language, info, null);
			project.Files.Add (new ProjectFile (file));
			return project;
		}

		public bool CanCreateSingleFileProject (string sourceFile)
		{
			WebSubtype type = AspNetAppProject.DetermineWebSubtype (Path.GetExtension (sourceFile));
			
			return ((type == WebSubtype.WebForm)
			           || (type == WebSubtype.WebHandler)
			           || (type == WebSubtype.WebService));
		}
	}
}
