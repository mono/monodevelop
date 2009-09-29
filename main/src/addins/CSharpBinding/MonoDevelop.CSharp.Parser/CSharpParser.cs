// 
// CSharpParser.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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

/*
using System;
using System.Collections.Generic;
using System.IO;
using Mono.CSharp;
using System.Text;
using MonoDevelop.CSharp.Dom;

namespace MonoDevelop.CSharp.Parser
{
	public class CSharpParser
	{
		class Dumper : IStructuralVisitor
		{
			
			public Dumper ()
			{
				currentNode = new NamespaceDeclaration ();
			}
			
			#region IStructuralVisitor Members
			
			public void Visit (MemberCore member)
			{
				Console.WriteLine ("Unknown member:");
	//			Console.WriteLine (member.GetType () + "-> Member {0}", member.GetSignatureForError ());
			}
			
			AbstractCSharpNode currentNode;
			
			public void Visit (TypeContainer typeContainer)
			{
	//			TypeDeclaration typeDeclaration = new TypeDeclaration ();
				Console.WriteLine (typeContainer.MemberName);
				
				
			}
		
			public void Visit (Method member)
			{
				
			}
				
			public void Visit (Field member)
			{
				
			}
			
			public void Visit (Constructor member)
			{
				
			}
			
			public void Visit (Destructor member)
			{
				
			}
			
			public void Visit (Operator member)
			{
				
			}
			
			public void Visit (Property member)
			{
				
			}
			
			public void Visit (Event member)
			{
				
			}
			#endregion
		}

		public void Parse (string fileName)
		{
			ModuleContainer top;
			using (FileStream fs = File.OpenRead (fileName)) {
				top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v"}, fs, fs.Name, Console.Out);
			}

			if (top == null)
				return;

			top.Accept (new Dumper ());
			
		}
	}
}
*/