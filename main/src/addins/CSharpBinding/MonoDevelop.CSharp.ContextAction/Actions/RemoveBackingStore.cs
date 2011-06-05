// 
// RemoveBackingStore.cs
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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.ContextAction
{
	public class RemoveBackingStore : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Remove backing store");
		}

		protected override void Run (CSharpContext context)
		{
			var property = context.GetNode<PropertyDeclaration> ();
			var field = GetBackingField (context);
			
			RemoveBackingField (context, field);
			ReplaceBackingFieldReferences (context, field, property);
			
			// create new auto property 
			var newProperty = (PropertyDeclaration)property.Clone ();	
			newProperty.Getter.Body = BlockStatement.Null;
			newProperty.Setter.Body = BlockStatement.Null;
			
			context.Do (property.Replace (context.Document, context.OutputNode (newProperty, context.GetIndentLevel (property)).Trim ()));
		}
		
		void RemoveBackingField (CSharpContext context, IField backingField)
		{
			FieldDeclaration field = context.Unit.GetNodeAt<FieldDeclaration> (backingField.Location.Line, backingField.Location.Column);
			
			var startLine = context.Document.Editor.GetLine (field.StartLocation.Line);
			var endLine = context.Document.Editor.GetLine (field.EndLocation.Line);
			
			context.DoRemove (startLine.Offset, endLine.EndOffset - startLine.Offset);
		}
		
		void ReplaceBackingFieldReferences (CSharpContext context, IField backingStore, PropertyDeclaration property)
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
				foreach (var memberRef in MonoDevelop.Ide.FindInFiles.ReferenceFinder.FindReferences (backingStore, monitor)) {
					if (property.Contains (memberRef.Line, memberRef.Column))
						continue;
					if (backingStore.Location.Line == memberRef.Line && backingStore.Location.Column == memberRef.Column)
						continue;
					context.Do (new TextReplaceChange () {
						FileName = memberRef.FileName,
						Offset = memberRef.Position,
						RemovedChars = memberRef.Name.Length,
						InsertedText = property.Name
					});
				}
			}
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			return GetBackingField (context) != null;
		}
		
		IField GetBackingField (CSharpContext context)
		{
			var propertyDeclaration = context.GetNode<PropertyDeclaration> ();
			// automatic properties always need getter & setter
			if (propertyDeclaration == null || propertyDeclaration.Getter.IsNull || propertyDeclaration.Setter.IsNull || propertyDeclaration.Getter.Body.IsNull || propertyDeclaration.Setter.Body.IsNull)
				return null;
			if (!context.HasCSharp3Support || propertyDeclaration.HasModifier (ICSharpCode.NRefactory.CSharp.Modifiers.Abstract) || ((TypeDeclaration)propertyDeclaration.Parent).ClassType == ICSharpCode.NRefactory.TypeSystem.ClassType.Interface)
				return null;
			var getterField = ScanGetter (context, propertyDeclaration);
			if (getterField == null)
				return null;
			var setterField = ScanSetter (context, propertyDeclaration);
			if (setterField == null)
				return null;
			if (getterField.Location != setterField.Location)
				return null;
			return getterField;
		}
		
		internal static IField ScanGetter (CSharpContext context, PropertyDeclaration propertyDeclaration)
		{
			if (propertyDeclaration.Getter.Body.Statements.Count != 1)
				return null;
			var returnStatement = propertyDeclaration.Getter.Body.Statements.First () as ReturnStatement;
			
			var result = returnStatement.Expression.Resolve (context.Document) as MemberResolveResult;
			Console.WriteLine (returnStatement.Expression.Resolve (context.Document));
			if (result == null)
				return null;
			return result.ResolvedMember as IField;
		}
		
		internal static IField ScanSetter (CSharpContext context, PropertyDeclaration propertyDeclaration)
		{
			if (propertyDeclaration.Setter.Body.Statements.Count != 1)
				return null;
			var setAssignment = propertyDeclaration.Setter.Body.Statements.First () as ExpressionStatement;
			var assignment = setAssignment != null ? setAssignment.Expression as AssignmentExpression : null;
			if (assignment == null || assignment.Operator != AssignmentOperatorType.Assign)
				return null;
			var result = assignment.Left.Resolve (context.Document) as MemberResolveResult;
			if (result == null)
				return null;
			return result.ResolvedMember as IField;
			
		}
	}
}

