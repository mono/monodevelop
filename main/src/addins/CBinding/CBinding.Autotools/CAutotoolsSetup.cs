//
// CAutotoolsSetup.cs
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

using System;

using MonoDevelop.Projects;
using MonoDevelop.Autotools;

using CBinding;

namespace CBinding.Autotools
{
	public class CAutotoolsSetup : ISimpleAutotoolsSetup
	{
		public string GetCompilerCommand (Project project, string configuration)
		{
			if (!CanDeploy (project))
				throw new Exception ("Not a deployable project.");
			
			CProject cproj = project as CProject;
			
			return cproj.Compiler.CompilerCommand;
		}

		// FIXME: Currently only the compiler flags are sent, no linker flags are sent.
		public string GetCompilerFlags (Project project, string configuration)
		{
			if (!CanDeploy (project))
				throw new Exception ("Not a deployable project.");
			
			CProjectConfiguration config = project.Configurations[configuration] as CProjectConfiguration;
			
			if (config == null)
				return string.Empty;
			
			CProject cproj = project as CProject;
			
			return cproj.Compiler.GetCompilerFlags (cproj, config);
		}

		public bool CanDeploy (Project project)
		{
			return project is CProject;
		}
	}
}