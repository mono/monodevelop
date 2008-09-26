// 
// CodeBehind.cs:
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2007 Michael Hutchinson
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
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.DesignerSupport;

using MonoDevelop.AspNet.Parser;

namespace MonoDevelop.AspNet
{
	
	
	public static class CodeBehind
	{
		
		public static string GetCodeBehindClassName (ProjectFile file)
		{
			AspNetAppProject proj = file.Project as AspNetAppProject;
			if (proj == null)
				return null;
			
			AspNetParsedDocument cu = ProjectDomService.Parse (file.Project, file.FilePath, null) as AspNetParsedDocument;
			
			if (cu != null && string.IsNullOrEmpty (cu.PageInfo.InheritedClass))
				return cu.PageInfo.InheritedClass;
			else
				return null;
		}
		
		public static IType GetDesignerClass (IType cls)
		{
			if (!cls.HasParts)
				return null;
			
			string designerEnding = ".designer" + Path.GetExtension (cls.CompilationUnit.FileName);
			
			foreach (IType c in cls.Parts)
				if (c.CompilationUnit.FileName.EndsWith (designerEnding, StringComparison.OrdinalIgnoreCase))
				    return c;
			
			return null;
		}
		
		public static IType GetNonDesignerClass (IType cls)
		{
			if (!cls.HasParts)
				return null;
			
			string designerEnding = ".designer" + Path.GetExtension (cls.CompilationUnit.FileName);
			
			foreach (IType c in cls.Parts)
				if (!c.CompilationUnit.FileName.EndsWith (designerEnding, StringComparison.OrdinalIgnoreCase))
				    return c;
			
			return null;
		}
	}
}
