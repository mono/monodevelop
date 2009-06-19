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
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Visitors;

namespace CSharpBinding
{
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
		
		static CodeCompileUnit ParseInternal (TextReader codeStream)
		{
			IParser parser = ParserFactory.CreateParser (
				SupportedLanguage.CSharp,
				codeStream);
			parser.ParseMethodBodies = true;
			parser.Parse ();
			
			if (parser.Errors.Count > 0)
				throw new ArgumentException (parser.Errors.ErrorOutput);
			
			CodeDomVisitor cdv = new CodeDomVisitor (parser.Lexer.SpecialTracker.CurrentSpecials);
			parser.CompilationUnit.AcceptVisitor (cdv, null);
			
			parser.Dispose ();
			
			CodeCompileUnit ccu = cdv.codeCompileUnit;
			
			//C# parser seems to insist on putting imports in the "Global" namespace; fix it up
			for (int i = 0; i < ccu.Namespaces.Count; i++) {
				CodeNamespace global = ccu.Namespaces [i];
				if ((global.Name == "Global") && (global.Types.Count == 0)) {
					global.Name = "";
					ccu.Namespaces.RemoveAt (i);
					ccu.Namespaces.Insert (0, global);
					
					//clear out repeat imports...
					for (int j = 1; j < ccu.Namespaces.Count; j++) {
						CodeNamespace cn = ccu.Namespaces [j];
						
						//why can't we remove imports? will have to collect ones to keep
						//then clear and refill
						CodeNamespaceImportCollection imps = new CodeNamespaceImportCollection ();
						
						for (int m = 0; m < cn.Imports.Count; m++) {
							bool found = false;
							
							for (int n = 0; n < global.Imports.Count; n++)
								if (global.Imports [n] == cn.Imports [m])
									found = true;
						
							if (!found)
								imps.Add (cn.Imports [m]);
						}
						
						cn.Imports.Clear ();
						
						foreach (CodeNamespaceImport imp in imps)
							cn.Imports.Add (imp);
					}
					
					break;
				}
			}
			return ccu;
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
