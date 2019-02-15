//
// MethodAtCaret.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.AspNet.Commands
{
	class MethodDeclarationAtCaret
	{
		MethodDeclarationAtCaret ()
		{
		}

		public static readonly MethodDeclarationAtCaret NullMethodDeclaration = new MethodDeclarationAtCaret ();

		public bool IsMethodFound {
			get {
				return TypeDeclaration != null && MethodDeclaration != null;
			}
		}

		public TypeDeclarationSyntax TypeDeclaration { get; private set; }
		public MethodDeclarationSyntax MethodDeclaration { get; private set; }

		public string Name {
			get {
				if (MethodDeclaration != null)
					return MethodDeclaration.Identifier.ValueText;
				return String.Empty;
			}
		}

		public static MethodDeclarationAtCaret Create (MonoDevelop.Ide.Gui.Document doc)
		{
			var parsedDocument = doc.DocumentContext.AnalysisDocument;
			if (parsedDocument == null)
				return NullMethodDeclaration;

			SyntaxNode root = null;
			if (!parsedDocument.TryGetSyntaxRoot (out root))
				return NullMethodDeclaration;

			SyntaxNode currentNode;
			try {
				int caretOffset = doc.Editor.CaretOffset;
				currentNode = root.FindNode (TextSpan.FromBounds (caretOffset, caretOffset));
			} catch (Exception) {
				return NullMethodDeclaration;
			}

			var currentType = currentNode.AncestorsAndSelf ().OfType<TypeDeclarationSyntax> ().FirstOrDefault ();
			var currentMethod = currentNode.AncestorsAndSelf ().OfType<MethodDeclarationSyntax> ().FirstOrDefault ();

			return new MethodDeclarationAtCaret {
				TypeDeclaration = currentType,
				MethodDeclaration = currentMethod
			};
		}

		public bool IsParentMvcController ()
		{
			return TypeDeclaration.Identifier.ValueText.EndsWith ("Controller", StringComparison.OrdinalIgnoreCase);
		}

		public bool IsMvcViewMethod ()
		{
			var correctReturnTypes = new [] { "ActionResult", "ViewResultBase", "ViewResult", "PartialViewResult" };

			string returnTypeName = MethodDeclaration.ReturnType.ToString ();

			return MethodDeclaration.Modifiers.Any (t => t.Kind () == SyntaxKind.PublicKeyword) &&
				correctReturnTypes.Any (t => t == returnTypeName);
		}

		public string GetParentMvcControllerName ()
		{
			if (TypeDeclaration == null)
				return String.Empty;

			string controllerName = TypeDeclaration.Identifier.ValueText;
			int pos = controllerName.LastIndexOf ("Controller", StringComparison.Ordinal);
			if (pos > 0)
				controllerName = controllerName.Remove (pos);

			return controllerName;
		}
	}
}

