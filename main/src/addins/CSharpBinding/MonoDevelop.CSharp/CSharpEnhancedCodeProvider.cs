//
// CSharpEnhancedCodeProvider.cs
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharp
{
	[System.ComponentModel.DesignerCategory ("Code")]
	public class CSharpEnhancedCodeProvider : CSharpCodeProvider
	{
		private ICodeParser codeParser;
		
		[Obsolete]
		public override ICodeParser CreateParser ()
		{
			if (codeParser == null)
				codeParser = new CodeParser ();
			return codeParser;
		}
		
		public override CodeCompileUnit Parse (TextReader codeStream)
		{
			return ParseInternal (codeStream);
		}
		
		static readonly Lazy<IUnresolvedAssembly> mscorlib = new Lazy<IUnresolvedAssembly>(
			delegate {
			return new CecilLoader().LoadAssemblyFile(typeof(object).Assembly.Location);
		});
		
		static readonly Lazy<IUnresolvedAssembly> systemCore = new Lazy<IUnresolvedAssembly>(
			delegate {
			return new CecilLoader().LoadAssemblyFile(typeof(System.Linq.Enumerable).Assembly.Location);
		});

		static readonly Lazy<ICompilation> Compilation = new Lazy<ICompilation>(
			delegate {
				var project = new CSharpProjectContent().AddAssemblyReferences (new [] { mscorlib.Value, systemCore.Value });
				return project.CreateCompilation();
		});

		static CodeCompileUnit ParseInternal (TextReader codeStream)
		{
			var cs = codeStream.ReadToEnd ();
			var tree = SyntaxTree.Parse (cs, "a.cs");
			if (tree.Errors.Count > 0)
				throw new ArgumentException ("Stream contained errors.");

			var convertVisitor = new CodeDomConvertVisitor ();
		
			return convertVisitor.Convert (Compilation.Value, tree, tree.ToTypeSystem ());
		}
		
		private class CodeParser : ICodeParser
		{
			public CodeCompileUnit Parse (TextReader codeStream)
			{
				return ParseInternal (codeStream);
			}
		}
	}
	
	
}
