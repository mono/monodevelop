// 
// UseVarKeywordInspector.cs
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
using MonoDevelop.AnalysisCore;
using MonoDevelop.CSharp.ContextAction;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using System.Linq;

namespace MonoDevelop.CSharp.Inspection
{
	public class UseVarKeywordInspector	 : CSharpInspector
	{
		protected override void Attach (ObservableAstVisitor<InspectionData, object>  visitior)
		{
			visitior.VariableDeclarationStatementVisited += HandleVisitiorVariableDeclarationStatementVisited;
		}

		void HandleVisitiorVariableDeclarationStatementVisited (VariableDeclarationStatement node, InspectionData data)
		{
			if (node.Type is PrimitiveType)
				return;
			if (node.Type is SimpleType && ((SimpleType)node.Type).Identifier == "var") 
				return;
			var severity = base.node.GetSeverity ();
			if (severity == MonoDevelop.SourceEditor.QuickTaskSeverity.None)
				return;
			//only checks for cases where the type would be obvious - assignment of new, cast, etc.
			//also check the type actually matches else the user might want to assign different subclasses later
			foreach (var v in node.Variables) {
				if (v.Initializer.IsNull)
					return;
				
				var arrCreate = v.Initializer as ArrayCreateExpression;
				if (arrCreate != null) {
					var n = node.Type as ComposedType;
					//FIXME: check the specifier compatibility
					if (n != null && n.ArraySpecifiers.Any () && n.BaseType.IsMatch (arrCreate.Type))
						continue;
					return;
				}
				var objCreate = v.Initializer as ObjectCreateExpression;
				if (objCreate != null) {
					if (objCreate.Type.IsMatch (node.Type))
						continue;
					return;
				}
				var asCast = v.Initializer as AsExpression;
				if (asCast != null) {
					if (asCast.Type.IsMatch (node.Type))
						continue;
					return;
				}
				var cast = v.Initializer as CastExpression;
				if (cast != null) {
					if (cast.Type.IsMatch (node.Type))
						continue;
					return;
				}
				return;
			}
			
			data.Add (new Result (
					new DomRegion (node.Type.StartLocation.Line, node.Type.StartLocation.Column, node.Type.EndLocation.Line, node.Type.EndLocation.Column),
					GettextCatalog.GetString ("Use implicitly typed local variable decaration"),
					severity,
					ResultCertainty.High, 
					ResultImportance.Medium,
					severity != MonoDevelop.SourceEditor.QuickTaskSeverity.Suggestion)
				);
		}
	}
}

