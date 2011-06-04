// 
// NamingConventions.cs
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
using System.Linq;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.ContextAction;
using MonoDevelop.Refactoring;
using MonoDevelop.Inspection;
using Mono.Addins;

namespace MonoDevelop.CSharp.Inspection
{
	public class InspectionData	
	{
		readonly List<Result> results = new List<Result> ();
		
		public IEnumerable<Result> Results {
			get { return results; }
		}
		
		public CallGraph Graph { get; set; }
		public Document Document { get; set; }
		
		public MonoDevelop.CSharp.Resolver.NRefactoryResolver Resolver {
			get {
				return Document.GetResolver ();
			}
		}
		
		public void Add (Result result)
		{
			results.Add (result);
		}
	}
	
	public static class CodeAnalysis
	{
		static List<InspectorAddinNode> inspectorNodes = new List<InspectorAddinNode> ();
		static ObservableAstVisitor<InspectionData, object> visitor = new ObservableAstVisitor<InspectionData, object> ();
		
		static CodeAnalysis ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/Inspectors", delegate(object sender, ExtensionNodeEventArgs args) {
				InspectorAddinNode node = (InspectorAddinNode)args.ExtensionNode;
				if (node.MimeType != "text/x-csharp")
					return;
				switch (args.Change) {
				case ExtensionChange.Add:
					inspectorNodes.Add (node);
					((CSharpInspector)node.Inspector).Attach (node, visitor);
					break;
				}
			});
			
			NamingInspector inspector = new NamingInspector ();
			inspector.Attach (null, visitor);
		}
		
		public static IEnumerable<Result> Check (Document input)
		{
			var unit = input != null ? input.ParsedDocument.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit : null;
			if (unit == null)
				return Enumerable.Empty<Result> ();
				
			var cg = new CallGraph ();
			cg.Inspect (input, input.GetResolver (), unit);
			var data = new InspectionData () { Graph = cg, Document = input };
			unit.AcceptVisitor (visitor, data);
			return data.Results;
		}
	}
}
