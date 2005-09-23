//
// DotNetProjectBinding.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using MonoDevelop.Internal.Serialization;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Project
{
	public class DotNetProjectBinding : IProjectBinding
	{
		public virtual string Name {
			get { return "DotNet"; }
		}
		
		public Project CreateProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return CreateProject (lang, info, projectOptions);
		}
		
		protected virtual DotNetProject CreateProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new DotNetProject (languageName, info, projectOptions);
		}
		
		public Project CreateSingleFileProject (string file)
		{
			ILanguageBinding binding = Runtime.Languages.GetBindingPerFileName (file);
			if (binding != null) {
				ProjectCreateInformation info = new ProjectCreateInformation ();
				info.ProjectName = Path.GetFileNameWithoutExtension (file);
				info.CombinePath = Path.GetDirectoryName (file);
				Project project = CreateProject (binding.Language, info, null);
				project.ProjectFiles.Add (new ProjectFile (file));
				return project;
			}
			return null;
		}
	}
}
