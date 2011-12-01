// 
// CodeBehind.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Projects;
using System.Globalization;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.DesignerSupport
{
	public static class CodeBehind
	{
		public static IEnumerable<string> GuessDependencies (DotNetProject proj, ProjectFile file,
		                                                     IEnumerable<string> groupedExtensions)
		{
			//only change the grouping if it's not already been set
			if (!string.IsNullOrEmpty (file.DependsOn) || proj.LanguageBinding == null)
				return null;
			
			//we only handle names that end with the language extension
			string langExt = proj.LanguageBinding.GetFileName ("a");
			langExt = langExt.Substring (1, langExt.Length - 1);
			
			//if filename ends with lang extension, it could be a child file
			if (file.Name.EndsWith (langExt, StringComparison.OrdinalIgnoreCase)) {
				
				//get the parent's name, amputating ".designer" if encountered
				string parentName = Path.GetFileName (file.Name);
				parentName = parentName.Substring (0, parentName.Length - langExt.Length);
				if (parentName.EndsWith (".designer", StringComparison.OrdinalIgnoreCase))
					parentName = parentName.Substring (0, parentName.Length - 9);
				
				//for each ASP.NET extension that allows codebehind, check whether the filename matches this extension
				foreach (string ext in groupedExtensions) {
					if (!parentName.EndsWith (ext, StringComparison.OrdinalIgnoreCase))
						continue;
					
					//if the file exists, set the dependency
					string path = Path.Combine (Path.GetDirectoryName (file.FilePath), parentName);
					if (File.Exists (path)) {
						file.DependsOn = parentName;
						return new string[] { path };
					}
				}
			} 
			//else, it may be a parent
			else {
				//check whether its extension matches known parent extensions
				foreach (string ext in groupedExtensions) {
					if (!file.FilePath.ToString ().EndsWith (ext, StringComparison.OrdinalIgnoreCase))
						continue;
					
					//check for codebehind files
					string codebehind = file.FilePath + langExt;
					if (!File.Exists (codebehind))
						codebehind = null;
					string designer = file.FilePath + ".designer" + langExt;
					if (!File.Exists (designer)) {
						designer = file.FilePath + ".Designer" + langExt;
						if (!File.Exists (designer))
							designer = null;
					}
					
					//return any found files that match
					if (designer != null) {
						return codebehind != null ? new string[] { designer, codebehind }
							: new string[] { designer };
					} else {
						return codebehind != null?  new string[] { codebehind }
							: null;
					}
				}
			}
			return null;
		}
		
		public static IUnresolvedTypeDefinition GetDesignerClass (IType cls)
		{
			if (cls.GetDefinition ().Parts.Count == 1)
				return null;
			
			string designerEnding = ".designer" + Path.GetExtension (cls.GetDefinition ().Region.FileName);
			
			foreach (var c in cls.GetDefinition ().Parts)
				if (c.Region.FileName.EndsWith (designerEnding, StringComparison.OrdinalIgnoreCase))
				    return c;
			
			return null;
		}
		
		public static IUnresolvedTypeDefinition GetNonDesignerClass (IType cls)
		{
			if (cls.GetDefinition ().Parts.Count == 1)
				return null;
			
			string designerEnding = ".designer" + Path.GetExtension (cls.GetDefinition ().Region.FileName);
			
			foreach (var c in cls.GetDefinition ().Parts)
				if (!c.Region.FileName.EndsWith (designerEnding, StringComparison.OrdinalIgnoreCase))
				    return c;
			
			return null;
		}

		public static bool IsDesignerFile (string name)
		{
			var ext = Path.GetExtension (name);
			return !string.IsNullOrEmpty (ext) && name.EndsWith (".designer" + ext, StringComparison.OrdinalIgnoreCase);
		}
	}
}