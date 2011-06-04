// 
// CreateBackingStore.cs
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

namespace MonoDevelop.CSharp.ContextAction
{
	public class CreateBackingStore : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Create backing store");
		}
		
		protected override void Run (CSharpContext context)
		{
			var property = context.GetNode<PropertyDeclaration> ();
			
			string backingStoreName = GetBackingStoreName (context, property.Name);
//			string indent = context.Document.Editor.GetLineIndent (property.StartLocation.Line);
			
		
			var offsets = new List<int> ();
			
			// create field
			var backingStore = new FieldDeclaration ();
			backingStore.ReturnType = property.ReturnType.Clone ();
			backingStore.Variables.Add (new VariableInitializer (backingStoreName));
			
			int offset = context.Document.Editor.LocationToOffset (property.StartLocation.Line, property.StartLocation.Column);
			
			string fieldOutput = context.OutputNode (backingStore, 0, (nodeOffset, node) => {
				if (node is VariableInitializer)
					offsets.Add (nodeOffset);
			}) + context.Document.Editor.EolMarker;
	
			// create property with backing field getter/setters
			var newProperty = context.GetNode<PropertyDeclaration> ();
			newProperty.Modifiers = property.Modifiers;
			newProperty.Name = property.Name;
//			newProperty.Attributes = property.Attributes.Clone ();
			newProperty.ReturnType = property.ReturnType;
			if (!property.Getter.IsNull) {
				newProperty.Getter = new Accessor () {
					Modifiers = property.Getter.Modifiers,
//					Attributes = proprty.Getter.Attributes.Clone,
					Body = new BlockStatement () {
						new ReturnStatement (new IdentifierExpression (backingStoreName))
					}
				};
			}
			
			if (!property.Setter.IsNull) {
				newProperty.Setter = new Accessor () {
					Modifiers = property.Setter.Modifiers,
//					Attributes = proprty.Setter.Attributes.Clone,
					Body = new BlockStatement () {
						new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (backingStoreName),AssignmentOperatorType.Assign, new IdentifierExpression ("value")))
					}
				};
			}
			
			string propertyOutput = context.OutputNode (newProperty, context.GetIndentLevel (property), delegate(int nodeOffset, AstNode astNode) {
				if (astNode is IdentifierExpression && ((IdentifierExpression)astNode).Identifier == backingStoreName)
					offsets.Add (fieldOutput.Length + nodeOffset);
			});
			
			property.Replace (context.Document, propertyOutput);
			context.Document.Editor.Insert (offset, fieldOutput);
			
			context.StartTextLinkMode (offset, backingStoreName.Length, offsets);
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			var propertyDeclaration = context.GetNode<PropertyDeclaration> ();
			return propertyDeclaration != null && 
				propertyDeclaration.Getter.Body.IsNull &&
				propertyDeclaration.Setter.Body.IsNull;
		}
		
		static string GetBackingStoreName (CSharpContext context, string name)
		{
			string baseName = char.ToLower (name [0]) + name.Substring (1);
			int number = -1;
			
			var type = context.GetNode<TypeDeclaration> ();
			
			bool nameInUse;
			do { 
				nameInUse = false;
				string proposedName = GenNumberedName (baseName, number);
				
				foreach (var member in type.Members) {
					var memberName = member.GetChildByRole (AstNode.Roles.Identifier);
					if (memberName == null)
						continue;
					if (memberName.Name == proposedName) {
						nameInUse = true;
						number++;
						break;
					}
				}
			} while (nameInUse);
			return GenNumberedName (baseName, number);
		}

		static string GenNumberedName (string baseName, int number)
		{
			return baseName + (number > 0 ? (number + 1).ToString () : "");
		}
		
	}
}

