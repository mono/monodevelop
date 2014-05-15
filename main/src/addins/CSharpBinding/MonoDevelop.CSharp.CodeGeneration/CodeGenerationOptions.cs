// 
// CodeGenerationOptions.cs
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
using System.Linq;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Simplification;

namespace MonoDevelop.CodeGeneration
{
	public class CodeGenerationOptions
	{
		readonly int offset;

		public MonoDevelop.Ide.Gui.Document Document {
			get;
			private set;
		}

		public ITypeSymbol EnclosingType {
			get;
			private set;
		}
		
		public TypeDeclarationSyntax EnclosingPart {
			get;
			private set;
		}
		
		public MemberDeclarationSyntax EnclosingMember {
			get;
			private set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (Document.FileName);
			}
		}
		
		public ICSharpCode.NRefactory.CSharp.CSharpFormattingOptions FormattingOptions {
			get {
				var doc = Document;
				var policyParent = doc.Project != null ? doc.Project.Policies : null;
				var types = DesktopService.GetMimeTypeInheritanceChain (doc.Editor.MimeType);
				var codePolicy = policyParent != null ? policyParent.Get<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types);
				return codePolicy.CreateOptions ();
			}
		}

		public SemanticModel CurrentState {
			get;
			private set;
		}

		internal CodeGenerationOptions (MonoDevelop.Ide.Gui.Document document)
		{
			CurrentState = document.AnalysisDocument.GetSemanticModelAsync ().Result;
			offset = document.Editor.Caret.Offset;
			var node = CurrentState.SyntaxTree.GetRoot ().FindNode (TextSpan.FromBounds (offset, offset));
			EnclosingMember = node.AncestorsAndSelf ().OfType<MemberDeclarationSyntax> ().FirstOrDefault ();
			EnclosingPart = node.AncestorsAndSelf ().OfType<TypeDeclarationSyntax> ().FirstOrDefault ();
			if (EnclosingPart != null)
				EnclosingType = CurrentState.GetDeclaredSymbol (EnclosingPart) as ITypeSymbol;
		}
		
		public string CreateShortType (ITypeSymbol fullType)
		{
			return fullType.ToMinimalDisplayString (CurrentState, offset);
		}
		
		public CodeGenerator CreateCodeGenerator ()
		{
			var result = CodeGenerator.CreateGenerator (Document);
			if (result == null)
				LoggingService.LogError ("Generator can't be generated for : " + Document.Editor.MimeType);
			return result;
		}
		
		public static CodeGenerationOptions CreateCodeGenerationOptions (MonoDevelop.Ide.Gui.Document document)
		{
			return new CodeGenerationOptions (document);
		}
		
	}
}
