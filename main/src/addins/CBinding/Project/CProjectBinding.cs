//
// CProjectBinding.cs: binding with the CProject
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
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

using System.Xml;
using System.IO;

using Mono.Addins;

using MonoDevelop.Projects;

namespace CBinding
{
	public class CProjectBinding : IProjectBinding
	{
		public string Name {
			get { return "C/C++"; }
		}
		
		public Project CreateProject (ProjectCreateInformation info,
		                              XmlElement projectOptions)
		{
			string language = projectOptions.GetAttribute ("language");
			return new CProject (info, projectOptions, language);
		}
		
		public Project CreateSingleFileProject (string sourceFile)
		{
			ProjectCreateInformation info = new ProjectCreateInformation ();
			info.ProjectName = Path.GetFileNameWithoutExtension (sourceFile);
			info.CombinePath = Path.GetDirectoryName (sourceFile);
			info.ProjectBasePath = Path.GetDirectoryName (sourceFile);
			
			string language = string.Empty;
			
			switch (Path.GetExtension (sourceFile))
			{
			case ".c":
				language = "C";
				break;
			case ".cpp":
				language = "CPP";
				break;
			case ".cxx":
				language = "CPP";
				break;
			}
			
			if (language.Length > 0) {
				Project project =  new CProject (info, null, language);
				project.ProjectFiles.Add (new ProjectFile (sourceFile));
				return project;
			}
			
			return null;
		}
		
		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return true;
		}
	}
}
