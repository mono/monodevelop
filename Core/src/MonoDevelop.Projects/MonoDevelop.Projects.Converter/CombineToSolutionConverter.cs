//
// CombineToSolutionConverter.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Text.RegularExpressions;

using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Projects.Converter
{
	public class CombineToSolutionConverter : IConverter 
	{
		public bool CanLoad (string sourceFile)
		{
			return Path.GetExtension (sourceFile) == ".mds";
		}
		static Regex entryLinePattern = new Regex("<Entry\s+filename=\"(?<File>.*)\"", RegexOptions.Compiled);
	
		public Solution Read (string sourceFile)
		{
			Solution result = new Solution (Path.ChangeExtension (sourceFile, ".sln"));
			ReadSolution (result, sourceFile);
			return result;
		}
		
		void ReadSolution (Solution solution, string sourceFile)
		{
			string sourcePath = Path.GetDirectoryName (sourceFile);
			string content = File.ReadAllText (sourceFile);
			Match match = combineLinePattern.Match (content);
			while (match.Success) {
				string fileName = Path.GetFullPath (Path.Combine (sourcePath, match.Result("${File}")));
				if (Path.GetExtension (fileName) == ".mds")
					ReadSolution (result, fileName);
				else if (Path.GetExtension (fileName) == ".mdp") 
					result.AddItem (ReadProject (fileName));
				
				match = match.NextMatch();
			}
		}
		
		SolutionProject ReadProject (string sourceFile)
		{
			SolutionProject result = new SolutionProject ("todo", 
			                                              "{" + Guid.NewGuid () + "}", 
			                                              Path.GetFileNameWithoutExtension (sourceFile),
			                                              sourceFile);
			
			return result;
		}
	}
}
