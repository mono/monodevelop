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
using Mono.TextEditor;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Parser
{
	public class CSharpParser
	{
		class ConversionVisitor : AbstractStructuralVisitor
		{
			MonoDevelop.CSharp.Dom.CompilationUnit unit = new MonoDevelop.CSharp.Dom.CompilationUnit ();
			
			#region IStructuralVisitor implementation
			public override void Visit (MemberCore member)
			{
				Console.WriteLine ("Unknown member:");
				Console.WriteLine (member.GetType () + "-> Member {0}", member.GetSignatureForError ());
			}
			
			Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration> ();
			
			public override void Visit (Class c)
			{
				TypeDeclaration newType = CreateTypeDeclaration (c);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Class;
				newType.AddChild (new CSharpTokenNode (Convert (c.BodyStartLocation)), AbstractCSharpNode.Roles.LBrace);
				
				typeStack.Push (newType);
				base.Visit (c);
				newType.AddChild (new CSharpTokenNode  (Convert (c.BodyEndLocation)), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
			}

			TypeDeclaration CreateTypeDeclaration (Mono.CSharp.TypeContainer tc)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				Identifier nameIdentifier = new Identifier () {
					Name = tc.Name
				};
				
				newType.AddChild (nameIdentifier, AbstractNode.Roles.Identifier);
				unit.AddChild (newType);
				return newType;
			}

			
			public override void Visit (Struct s)
			{
				TypeDeclaration newType = CreateTypeDeclaration (s);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Struct;
				typeStack.Push (newType);
				base.Visit (s);
				typeStack.Pop ();
			}
			
			public override void Visit (Interface i)
			{
				TypeDeclaration newType = CreateTypeDeclaration (i);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Interface;
				typeStack.Push (newType);
				base.Visit (i);
				typeStack.Pop ();
			}
			
			public override void Visit (Mono.CSharp.Delegate d)
			{
			}
			
			public override void Visit (Mono.CSharp.Enum e)
			{
				TypeDeclaration newType = CreateTypeDeclaration (e);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Enum;
				typeStack.Push (newType);
				base.Visit (e);
				typeStack.Pop ();
			}
			
			public override void Visit (FixedField f)
			{
			}
			
			
			public static DomLocation Convert (Mono.CSharp.Location loc)
			{
				return new DomLocation (loc.Row, loc.Column);
			}
			public static DomReturnType ConvertToReturnType (FullNamedExpression typeName)
			{
				return new DomReturnType ("TODO");
			}
			
			public override void Visit (Field f)
			{
				TypeDeclaration typeDeclaration = typeStack.Peek ();
				
				FieldDeclaration newField = new FieldDeclaration ();
				newField.AddChild (ConvertToReturnType (f.TypeName), AbstractNode.Roles.ReturnType);
				Identifier fieldName = new Identifier () {
					Name = f.MemberName.Name,
					Location = Convert (f.MemberName.Location)
				};
				newField.AddChild (fieldName, AbstractNode.Roles.Identifier);
				
				typeDeclaration.AddChild (newField);
			}
			
			public override void Visit (Operator o)
			{
			}
			
			public override void Visit (Indexer i)
			{
			}
			
			public override void Visit (Method m)
			{
			}
			
			public override void Visit (Property p)
			{
			}
			
			public override void Visit (Constructor c)
			{
			}
			
			public override void Visit (Destructor d)
			{
			}
			
			public override void Visit (Event e)
			{
			}
			#endregion
			public MonoDevelop.CSharp.Dom.CompilationUnit Unit {
				get {
					return unit;
				}
				set {
					unit = value;
				}
			}
		}

		public MonoDevelop.CSharp.Dom.CompilationUnit Parse (TextEditorData data)
		{
		//	data.Document
			ModuleContainer top;
			using (Stream stream = data.OpenStream ()) {
				top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v"}, stream, data.Document.FileName, Console.Out);
			}

			if (top == null)
				return null;
			CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor ();
			top.Accept (conversionVisitor);
			return conversionVisitor.Unit;
		}
	}
}
*/