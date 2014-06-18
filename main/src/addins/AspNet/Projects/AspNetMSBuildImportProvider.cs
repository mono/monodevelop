// 
// AspNetMSBuildImportProvider.cs
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.MSBuild;
using Mono.Addins;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.Projects
{
	/*
	[Extension]
	public class AspNetMSBuildImportProvider: IMSBuildImportProvider
	{
		const string target05 = @"$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v8.0\WebApplications\Microsoft.WebApplication.targets";
		const string target08 = @"$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v9.0\WebApplications\Microsoft.WebApplication.targets";
		const string target10 = @"$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v10.0\WebApplications\Microsoft.WebApplication.targets";
			
		public void UpdateImports (SolutionEntityItem item, List<string> imports)
		{
			imports.Remove (target05);
			imports.Remove (target08);
			imports.Remove (target10);
			
			var project = item as AspNetAppProject;
			if (project != null) {
				if (project.FileFormat.Id == "MSBuild05")
					imports.Add (target05);
				else if (project.FileFormat.Id == "MSBuild08")
					imports.Add (target08);
				else
					imports.Add (target10);
			}
		}
	}
	*/
}
	
