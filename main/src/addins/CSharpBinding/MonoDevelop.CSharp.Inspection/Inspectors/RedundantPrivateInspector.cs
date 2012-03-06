// 
// RedundantPrivateInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharp.Inspection
{
	public class RedundantPrivateInspector : CSharpInspector
		
	{
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitor)
		{
			visitor.MethodDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.FieldDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.PropertyDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.IndexerDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.EventDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.CustomEventDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.ConstructorDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.ConstructorDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.OperatorDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.FixedFieldDeclarationVisited += (node, data) => CheckNode (node, data);
			visitor.TypeDeclarationVisited += delegate(TypeDeclaration node, InspectionData data) {
				if (node.Parent is TypeParameterDeclaration)
					CheckNode (node, data);
			};
		}
		
		void CheckNode (EntityDeclaration node, InspectionData data)
		{
			foreach (var token in node.ModifierTokens) {
				if (token.Modifier == Modifiers.Private) {
					AddResult (data,
						new DomRegion (token.StartLocation, token.EndLocation),
						GettextCatalog.GetString ("Remove redundant 'private' modifier"),
						delegate {
							int offset = data.Document.Editor.LocationToOffset (token.StartLocation);
							int end = data.Document.Editor.LocationToOffset (token.GetNextNode ().StartLocation);
							data.Document.Editor.Remove (offset, end - offset);
						}
					);
				}
			}
		}
	}
}
