// 
// MoveTypeToFile.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Refactoring;
using System.IO;
using System.Text;
using MonoDevelop.Ide.StandardHeader;
using MonoDevelop.Core.ProgressMonitoring;
using ICSharpCode.NRefactory;
using System.Threading;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class MoveTypeToFile : MonoDevelop.CodeActions.CodeActionProvider
	{
		public override IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions (MonoDevelop.Ide.Gui.Document document, object refactoringContext, TextLocation loc, CancellationToken cancellationToken)
		{
			var context = (MDRefactoringContext)refactoringContext;
			return GetActions (context);
		}
		protected IEnumerable<MonoDevelop.CodeActions.CodeAction> GetActions (MDRefactoringContext context)
		{
			if (context.IsInvalid)
				yield break;
			var type = GetTypeDeclaration (context);
			if (type == null)
				yield break;
			if (Path.GetFileNameWithoutExtension (context.Document.FileName) == type.Name)
				yield break;
			string title;
			if (IsSingleType (context)) {
				title = String.Format (GettextCatalog.GetString ("_Rename file to '{0}'"), Path.GetFileName (GetCorrectFileName (context, type)));
			} else {
				title = String.Format (GettextCatalog.GetString ("_Move type to file '{0}'"), Path.GetFileName (GetCorrectFileName (context, type)));
			}
			yield return new MonoDevelop.CodeActions.DefaultCodeAction (title, (d, l) => {
				var ctx = new MDRefactoringContext (d, l);
				string correctFileName = GetCorrectFileName (ctx, type);
				if (IsSingleType (ctx)) {
					FileService.RenameFile (ctx.Document.FileName, correctFileName);
					if (ctx.Document.Project != null)
						ctx.Document.Project.Save (new NullProgressMonitor ());
					return;
				}
				
				CreateNewFile (ctx, type, correctFileName);
				using (var script = ctx.StartScript ()) {
					script.Remove (type);
				}
			});
		}

		static void CreateNewFile (MDRefactoringContext context, TypeDeclaration type, string correctFileName)
		{
			var content = context.Document.Editor.Text;
			
			var types = new List<EntityDeclaration> (context.Unit.GetTypes ().Where (t => t.StartLocation != type.StartLocation));
			types.Sort ((x, y) => y.StartLocation.CompareTo (x.StartLocation));

			foreach (var removeType in types) {
				var start = context.GetOffset (removeType.StartLocation);
				var end = context.GetOffset (removeType.EndLocation);
				content = content.Remove (start, end - start);
			}
			
			if (context.Document.Project is MonoDevelop.Projects.DotNetProject) {
				string header = StandardHeaderService.GetHeader (context.Document.Project, correctFileName, true);
				if (!string.IsNullOrEmpty (header))
					content = header + context.Document.Editor.EolMarker + StripHeader (content);
			}
			content = StripDoubleBlankLines (content);
			
			File.WriteAllText (correctFileName, content);
			context.Document.Project.AddFile (correctFileName);
			MonoDevelop.Ide.IdeApp.ProjectOperations.Save (context.Document.Project);
		}

		static bool IsBlankLine (TextDocument doc, int i)
		{
			var line = doc.GetLine (i);
			return line.Length == line.GetIndentation (doc).Length;
		}

		static string StripDoubleBlankLines (string content)
		{
			var doc = new Mono.TextEditor.TextDocument (content);
			for (int i = 1; i + 1 <= doc.LineCount; i++) {
				if (IsBlankLine (doc, i) && IsBlankLine (doc, i + 1)) {
					doc.Remove (doc.GetLine (i).SegmentIncludingDelimiter);
					i--;
					continue;
				}
			}
			return doc.Text;
		}

		static string StripHeader (string content)
		{
			var doc = new Mono.TextEditor.TextDocument (content);
			while (true) {
				string lineText = doc.GetLineText (1);
				if (lineText == null)
					break;
				if (lineText.StartsWith ("//")) {
					doc.Remove (doc.GetLine (1).SegmentIncludingDelimiter);
					continue;
				}
				break;
			}
			return doc.Text;
		}
		
		bool IsSingleType (MDRefactoringContext context)
		{
			return context.Unit.GetTypes ().Count () == 1;
		}
		
		TypeDeclaration GetTypeDeclaration (MDRefactoringContext context)
		{
			var result = context.GetNode<TypeDeclaration> ();
			if (result == null || result.Parent is TypeDeclaration)
				return null;
			if (result != null && result.NameToken.Contains (context.Location))
				return result;
			return null;
		}
		
		internal static string GetCorrectFileName (MDRefactoringContext context, EntityDeclaration type)
		{
			if (type == null)
				return context.Document.FileName;
			return Path.Combine (Path.GetDirectoryName (context.Document.FileName), type.Name + Path.GetExtension (context.Document.FileName));
		}
	}
}

