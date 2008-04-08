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
using MonoDevelop.Projects.Parser;
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
			
			Document doc = proj.GetDocument (file);
			
			if (doc == null || string.IsNullOrEmpty (doc.Info.InheritedClass))
				return null;
			
			return doc.Info.InheritedClass;
		}
		
		public static IClass GetDesignerClass (IClass cls)
		{
			if (cls.Parts.Length <= 1)
				return null;
			
			string designerEnding = ".designer" + Path.GetExtension (cls.Region.FileName);
			
			foreach (IClass c in cls.Parts)
				if (c.Region.FileName.EndsWith (designerEnding))
				    return c;
			
			return null;
		}
		
		public static IClass GetNonDesignerClass (IClass cls)
		{
			if (cls.Parts.Length <= 1)
				return null;
			
			string designerEnding = ".designer" + Path.GetExtension (cls.Region.FileName);
			
			foreach (IClass c in cls.Parts)
				if (!c.Region.FileName.EndsWith (designerEnding))
				    return c;
			
			return null;
		}
	}
}
