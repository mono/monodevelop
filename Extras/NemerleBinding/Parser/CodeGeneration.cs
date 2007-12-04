//
// CodeGenerator.cs
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
using System.CodeDom;
using System.CodeDom.Compiler;
using NemerleBinding.Parser;
using nemerle = Nemerle.Compiler;
using System.IO;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;
using System.Drawing;
using System.Text.RegularExpressions;

namespace NemerleBinding.Parser
{
	class NemerleRefactorer: BaseRefactorer
	{
		nemerle.NemerleCodeProvider nemerleProvider = new nemerle.NemerleCodeProvider ();
		
		public override RefactorOperations SupportedOperations {
			get { return RefactorOperations.All; }
		}
	
		protected override ICodeGenerator GetGenerator ()
		{
			return nemerleProvider.CreateGenerator ();
		}
		
		public override IClass RenameClass (RefactorerContext ctx, IClass cls, string newName)
		{
			IEditableTextFile file = ctx.GetFile (cls.Region.FileName);
			if (file == null)
				return null;

			int pos1 = file.GetPositionFromLineColumn (cls.Region.BeginLine, cls.Region.BeginColumn);
			int pos2 = file.GetPositionFromLineColumn (cls.Region.EndLine, cls.Region.EndColumn);
			string txt = file.GetText (pos1, pos2);
			
			Regex targetExp = new Regex(@"\sclass\s*(" + cls.Name + @")\s", RegexOptions.Multiline);
			Match match = targetExp.Match (" " + txt + " ");
			if (!match.Success)
				return null;
			
			int pos = pos1 + match.Groups [1].Index - 1;
			file.DeleteText (pos, cls.Name.Length);
			file.InsertText (pos, newName);
			
			return GetGeneratedClass (ctx, file, cls);
		}
		
		public override MemberReferenceCollection FindClassReferences (RefactorerContext ctx, string fileName, IClass cls)
		{
			return null;
		}
		
		public override MemberReferenceCollection FindMemberReferences (RefactorerContext ctx, string fileName, IClass cls, IMember member)
		{
			return null;
		}
	}
}
