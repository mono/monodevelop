//
// MSBuildProject.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
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

using MonoDevelop.Projects;

using System;
using System.IO;

namespace MonoDevelop.Prj2Make
{
	class MSBuildProject : DotNetProject
	{
		public MSBuildProject () : base ()
		{
		}

		public MSBuildProject (string languageName) : base (languageName)
		{
		}

		public MSBuildData Data {
			get {
				if (!ExtendedProperties.Contains (typeof (MSBuildFileFormat)))
					return null;
				return (MSBuildData) ExtendedProperties [typeof (MSBuildFileFormat)];
			}
			set {
				ExtendedProperties [typeof (MSBuildFileFormat)] = value;
			}
		}

		protected override string GetDefaultResourceId (ProjectFile pf)
		{
			return GetDefaultResourceIdInternal (pf);
		}

		internal string GetDefaultResourceIdInternal (ProjectFile pf)
		{
			IResourceIdBuilder rb;
			DotNetProject project = (DotNetProject) pf.Project;

			if (project.LanguageName == "C#") {
				rb = new CSharpResourceIdBuilder ();
			} else if (project.LanguageName == "VBNet") {
				rb = new VBNetResourceIdBuilder ();
			} else {
				Console.WriteLine ("Language '{0}' not supported for building resource ids.", project.LanguageName);
				return null;
			}

			return rb.GetResourceId (pf);
		}
	}

}
