// 
// ReorderParametersRefactoring.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang
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
using System.Collections.Generic;
using System.IO;
using System.Text;

using Mono.TextEditor;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.ProgressMonitoring;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp.Refactoring.ReorderParameters
{
	public class ReorderParametersRefactoring : RefactoringOperation
	{
		
		public class ReorderParametersProperties
		{
			public int[] Parameters { 
				get; 
				private set;
			}
			
			public ReorderParametersProperties (int paramCount)
			{
				Parameters = new int[paramCount];
			}
			
			public ReorderParametersProperties (IEnumerable<int> indices)
			{
				Parameters = indices.ToArray ();
			}
		}
		
		public ReorderParametersRefactoring ()
		{
			Name = "Reorder Parameters";
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("Reorder Parameters");
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			var data = options.GetTextEditorData ();
			if (data.MimeType != CSharpFormatter.MimeType)
				return false;
			return options.SelectedItem is IMethod && ((IMethod)options.SelectedItem).Parameters.Count > 1;
		}
		
		public override void Run (RefactoringOptions options)
		{
			MessageService.ShowCustomDialog (new ReorderParametersDialog (options, this));
		}
		
		/// <summary>
		/// Reorders the parameters/arguments given by nodes and adds the change to result
		/// </summary>
		static void ReorderParameters (IList<Change> result, ReorderParametersProperties properties,
		                        IList<AstNode> nodes, TextEditorData editor)
		{
			StringBuilder newList = new StringBuilder ();
			
			for (int i = 0; i < nodes.Count; i++) {
				var newParam = nodes [properties.Parameters [i]];
				var region = newParam.GetRegion ();
				newList.Append (editor.GetTextBetween (region.Begin, region.End));
				
				//append separator and possible formatting spaces
				if (i != nodes.Count - 1) {
					var currentRegion = nodes [i].GetRegion ();
					var nextRegion = nodes [i + 1].GetRegion ();
					newList.Append (editor.GetTextBetween (currentRegion.End, nextRegion.Begin));
				}
			}
			
			var replace = new TextReplaceChange ();
			replace.FileName = editor.Document.FileName;
			var beginOffset = editor.LocationToOffset (nodes [0].GetRegion ().Begin);
			var endOffset = editor.LocationToOffset (nodes [nodes.Count - 1].GetRegion ().End);
			replace.Offset = beginOffset;
			replace.RemovedChars = endOffset - beginOffset;
			replace.InsertedText = newList.ToString ();
			replace.Description = GettextCatalog.GetString ("Reorder parameters");
			result.Add (replace);
		}
		
		/// <summary>
		/// Checks if the signatures of the two methods are the same, only check parameters
		/// </summary>
		static bool CheckSignature(IMethod a, IMethod b)
		{
			if (a == b) return true;
			return ParameterListComparer.Instance.Equals(a.Parameters, b.Parameters);
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			var result = new List<Change> ();
			var reorderProperties = (ReorderParametersProperties)properties;
			
			var method = (IMethod)options.SelectedItem;
			
			//cache TextEditorData and CompilationUnit for unopen files
			var cachedEditors = new Dictionary<string, TextEditorData> ();
			var cachedUnits = new Dictionary<string, CompilationUnit> ();
			
			try {
				using (var monitor = new MessageDialogProgressMonitor (true, false, false, true)) {
					var references = ReferenceFinder.FindReferences (method, monitor);
					foreach (var methodRef in references) {
						//filter overloads
						var entity = methodRef.Entity as IMethod;
						if (entity == null || !CheckSignature (entity, method))
							continue;
						
						TextEditorData editor;
						CompilationUnit unit;
						
						var filename = methodRef.FileName;
						var doc = IdeApp.Workbench.GetDocument (filename);
						
						if (doc != null) {
							editor = doc.Editor;
							unit = doc.ParsedDocument.GetAst<CompilationUnit> ();
						} else if (cachedEditors.TryGetValue (filename, out editor)) {
							unit = cachedUnits [filename];
						} else {
							editor = new TextEditorData ();
							editor.Document.FileName = filename;
							editor.Text = File.ReadAllText (filename);
							
							unit = new CSharpParser ().Parse (editor);
							if (unit == null) {
								editor.Dispose ();
								continue;
							}
							cachedEditors [filename] = editor;
							cachedUnits [filename] = unit;
						}
						
						var node = unit.GetNodeAt (methodRef.Region.Begin);
						node = node.Parent;
						if (node is MethodDeclaration) {
							ReorderParameters (result, reorderProperties, 
							                   ((MethodDeclaration)node).Parameters.ToList<AstNode> (), 
							                   editor);
							continue;
						}
						
						while (node != null && !(node is InvocationExpression))
							node = node.Parent;
						if (node != null)
							ReorderParameters (result, reorderProperties, 
							                   ((InvocationExpression)node).Arguments.ToList<AstNode> (), 
							                   editor);
					}
				}
			} finally {
				foreach (var editor in cachedEditors.Values)
					editor.Dispose ();
			}
			return result;
		}
		
	}
}

