//
// JavaScriptProjectBinding.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects;
using System.Xml;

namespace JavaScriptBinding
{
	public class JavaScriptProjectBinding : IProjectBinding
	{
		#region IProjectBinding implementation

		public Project CreateProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new JavaScriptProject (info, projectOptions);
		}

		public Project CreateSingleFileProject (string sourceFile)
		{
			Project result = new JavaScriptProject ();
			result.Files.Add (new ProjectFile (sourceFile));
			return result;
		}

		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return sourceFile.EndsWith (".js", StringComparison.OrdinalIgnoreCase) || 
				sourceFile.EndsWith (".ts", StringComparison.OrdinalIgnoreCase) ;
		}

		public string Name {
			get {
				return "JavaScript";
			}
		}
		#endregion
	}
}

