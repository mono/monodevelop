// 
// CodeGenerationService.cs
//  
// Author:
//       mkrueger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MonoDevelop.Core;
using System.CodeDom;
using MonoDevelop.Projects;
using System.CodeDom.Compiler;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Refactoring
{
	public static class CodeGenerationService
	{
//		public static IUnresolvedMember AddCodeDomMember (MonoDevelop.Projects.Project project, IUnresolvedTypeDefinition type, CodeTypeMember newMember)
//		{
//			bool isOpen;
//			var data = TextFileProvider.Instance.GetTextEditorData (type.Region.FileName, out isOpen);
//			var parsedDocument = TypeSystemService.ParseFile (data.FileName, data.MimeType, data.Text);
//			
//			var insertionPoints = GetInsertionPoints (data, parsedDocument, type);
//			
//			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, type, newMember);
//			
//			var dotNetProject = project as DotNetProject;
//			if (dotNetProject == null) {
//				LoggingService.LogError ("Only .NET projects are supported.");
//				return null;
//			}
//			
//			var generator = dotNetProject.LanguageBinding.GetCodeDomProvider ();
//			StringWriter sw = new StringWriter ();
//			var options = new CodeGeneratorOptions ();
//			options.IndentString = data.GetLineIndent (type.Region.BeginLine) + "\t";
//			if (newMember is CodeMemberMethod)
//				options.BracingStyle = "C";
//			generator.GenerateCodeFromMember (newMember, sw, options);
//
//			var code = sw.ToString ();
//			if (!string.IsNullOrEmpty (code))
//				suitableInsertionPoint.Insert (data, code);
//			if (!isOpen) {
//				try {
//					File.WriteAllText (type.Region.FileName, data.Text);
//				} catch (Exception e) {
//					LoggingService.LogError (string.Format ("Failed to write file '{0}'.", type.Region.FileName), e);
//					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.Region.FileName));
//				}
//			}
//			var newDocument = TypeSystemService.ParseFile (data.FileName, data.MimeType, data.Text);
//			return newDocument.ParsedFile.GetMember (suitableInsertionPoint.Location.Line, int.MaxValue);
//		}
		
		public static void AddNewMember (ITypeSymbol type, Location part, SyntaxNode newMember, bool implementExplicit = false)
		{
			bool isOpen;
			var filePath = part.SourceTree.FilePath;
			var data = TextFileProvider.Instance.GetTextEditorData (filePath, out isOpen);
			var parsedDocument = TypeSystemService.ParseFile (null, data.FileName, data.MimeType, data).Result;
			
			var insertionPoints = InsertionPointService.GetInsertionPoints (data, parsedDocument, type, part);
			
			var suitableInsertionPoint = InsertionPointService.GetSuitableInsertionPoint (data, insertionPoints, type, part, newMember);

			/*
			var generator = CreateCodeGenerator (data, type.Compilation);

			generator.IndentLevel = CalculateBodyIndentLevel (parsedDocument.GetInnermostTypeDefinition (type.Region.Begin));
			var generatedCode = generator.CreateMemberImplementation (type, part, newMember, implementExplicit);
			*/
			suitableInsertionPoint.Insert (data, newMember.ToString ());
			if (!isOpen) {
				try {
					File.WriteAllText (filePath, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Failed to write file '{0}'.", filePath), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", filePath));
				}
			}
		}

		public static Task<bool> InsertMemberWithCursor (string operation, ITypeSymbol type, Location part, SyntaxNode newMember, bool implementExplicit = false)
		{
			//TODO: Add dialog for inserting position
			AddNewMember (type, part, newMember, implementExplicit);
			return Task.FromResult (true);
		}
//
//		public static int CalculateBodyIndentLevel (IUnresolvedTypeDefinition declaringType)
//		{
//			if (declaringType == null)
//				return 0;
//			int indentLevel = 1;
//			while (declaringType.DeclaringTypeDefinition != null) {
//				indentLevel++;
//				declaringType = declaringType.DeclaringTypeDefinition;
//			}
//			var file = declaringType.UnresolvedFile as CSharpUnresolvedFile;
//			if (file == null)
//				return indentLevel;
//			var scope = file.GetUsingScope (declaringType.Region.Begin);
//			while (scope != null && !string.IsNullOrEmpty (scope.NamespaceName)) {
//				indentLevel++;
//				// skip virtual scopes.
//				while (scope.Parent != null && scope.Parent.Region == scope.Region)
//					scope = scope.Parent;
//				scope = scope.Parent;
//			}
//			return indentLevel;
//		}
		public static MonoDevelop.Ide.TypeSystem.CodeGenerator CreateCodeGenerator (this Ide.Gui.Document doc)
		{
			return MonoDevelop.Ide.TypeSystem.CodeGenerator.CreateGenerator (doc);
		}


//		public static MonoDevelop.Ide.TypeSystem.CodeGenerator CreateCodeGenerator (this ITextDocument data, ICompilation compilation)
//		{
//			return MonoDevelop.Ide.TypeSystem.CodeGenerator.CreateGenerator (data, compilation);
//		}
//		
//		static IUnresolvedTypeDefinition GetMainPart (IType t)
//		{
//			return t.GetDefinition ().Parts.First ();
//		}
		

		public static void AddAttribute (INamedTypeSymbol cls, string name, params object[] parameters)
		{
			bool isOpen;
			string fileName = cls.Locations.First ().SourceTree.FilePath;
			var buffer = TextFileProvider.Instance.GetTextEditorData (fileName, out isOpen);
			
			
			var code = new StringBuilder ();
			int pos = cls.Locations.First ().SourceSpan.Start;
			var line = buffer.GetLineByOffset (pos);
			code.Append (buffer.GetLineIndent (line));
			code.Append ("[");
			code.Append (name);
			if (parameters != null && parameters.Length > 0) {
				code.Append ("(");
				for (int i = 0; i < parameters.Length; i++) {
					if (i > 0)
						code.Append (", ");
					code.Append (parameters [i]);
				}
				code.Append (")");
			}
 			code.Append ("]");
			code.AppendLine ();
			
			buffer.InsertText (line.Offset, code.ToString ());

			if (!isOpen) {
				File.WriteAllText (fileName, buffer.Text);
			}
		}
		
		public static ITypeSymbol AddType (DotNetProject project, string folder, string namspace, ClassDeclarationSyntax type)
		{
			var ns = SyntaxFactory.NamespaceDeclaration (SyntaxFactory.ParseName (namspace)).WithMembers (new SyntaxList<MemberDeclarationSyntax> () { type });
			
			string fileName = project.LanguageBinding.GetFileName (Path.Combine (folder, type.Identifier.ToString ()));
			using (var sw = new StreamWriter (fileName)) {
				sw.WriteLine (ns.ToString ());
			}
			FileService.NotifyFileChanged (fileName); 
			var roslynProject = TypeSystemService.GetProject (project); 
			var id = TypeSystemService.Workspace.GetDocumentId (roslynProject.Id, fileName); 
			if (id == null)
				return null;
			var model = roslynProject.GetDocument (id).GetSemanticModelAsync ().Result;
			var typeSyntax = model.SyntaxTree.GetCompilationUnitRoot ().ChildNodes ().First ().ChildNodes ().First () as ClassDeclarationSyntax;
			return model.GetDeclaredSymbol (typeSyntax);
		}
	}
}
