// 
// AstAmbience.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace MonoDevelop.CSharp
{
	class AstAmbience
	{
		OptionSet options;
		
		public AstAmbience (OptionSet options)
		{
			this.options = options;
		}
		
		static bool IsObsolete (MemberDeclarationSyntax entity)
		{
			if (entity == null)
				return false;
			// TODO!
//			foreach (var section in entity.Attributes) {
//				foreach (var attr in section.Attributes) {
//					var attrText = attr.Type.ToString ();
//					if (attrText == "Obsolete" || attrText == "ObsoleteAttribute" || attrText == "System.Obsolete" || attrText == "System.ObsoleteAttribute" )
//						return true;
//				}
//			}
			return false;
		}
		
		void AppendTypeParameter (StringBuilder sb, TypeParameterListSyntax parameters)
		{
			if (parameters == null || parameters.Parameters.Count == 0) 
				return;
			sb.Append ("&lt;");
			bool first = true;
			foreach (var param in parameters.Parameters) {
				if (!first) {
					sb.Append (", ");
				} else {
					first = false;
				}
				AppendEscaped (sb, param.ToString ());
			}
			sb.Append ("&gt;");
		}
		
		void AppendParameter (StringBuilder sb, ParameterListSyntax parameters)
		{
			// Missing roslyn formatting option ?
			//			if (options.GetOption (CSharpFormattingOptions.Spacing ???))
			//	sb.Append (" ");
			sb.Append ("(");
			var hasParameters = parameters != null && parameters.Parameters.Count > 0;

			// Missing roslyn formatting option ?
			//if (hasParameters && options.GetOption (SpaceWithinMethodDeclarationParentheses))
			//	sb.Append (" ");

			bool first = true;
			if (hasParameters) {
				foreach (var param in parameters.Parameters) {
					if (!first) {
						//if (options.SpaceBeforeMethodDeclarationParameterComma)
						//	sb.Append (" ");
						sb.Append (",");
						//if (options.SpaceAfterMethodDeclarationParameterComma)
						sb.Append (" ");
					} else {
						first = false;
					}
					AppendEscaped (sb, param.ToString ());
				}
			}

			// Missing roslyn formatting option ?
			//if (hasParameters && options.SpaceWithinMethodDeclarationParentheses)
			//	sb.Append (" ");
			sb.Append (")");
		}
		
		static void AppendEscaped (StringBuilder result, string text)
		{
			if (text == null)
				return;
			foreach (char ch in text) {
				switch (ch) {
				case '<':
					result.Append ("&lt;");
					break;
				case '>':
					result.Append ("&gt;");
					break;
				case '&':
					result.Append ("&amp;");
					break;
				case '\'':
					result.Append ("&apos;");
					break;
				case '"':
					result.Append ("&quot;");
					break;
				default:
					result.Append (ch);
					break;
				}
			}
		}
		
		public string GetEntityMarkup (SyntaxNode e)
		{
			var sb = new StringBuilder ();
			if (e is NamespaceDeclarationSyntax) {
				return ((NamespaceDeclarationSyntax)e).Name.ToString ();
			}
			if (e is TypeDeclarationSyntax) {
				var type = e as TypeDeclarationSyntax;
				sb.Append (type.Identifier);
				AppendTypeParameter (sb, type.TypeParameterList);
			} else if (e is DelegateDeclarationSyntax) {
				var del = e as DelegateDeclarationSyntax;
				sb.Append (del.Identifier);
				AppendTypeParameter (sb, del.TypeParameterList);
				AppendParameter (sb, del.ParameterList);
			} else if (e is AccessorDeclarationSyntax) {
				if (e.CSharpKind () == SyntaxKind.GetAccessorDeclaration) {
					sb.Append ("get");
				} else if (e.CSharpKind () == SyntaxKind.SetAccessorDeclaration) {
					sb.Append ("set");
				} else if (e.CSharpKind () == SyntaxKind.AddAccessorDeclaration) {
					sb.Append ("add");
				} else if (e.CSharpKind () == SyntaxKind.RemoveAccessorDeclaration) {
					sb.Append ("remove");
				}
			} else if (e is OperatorDeclarationSyntax) {
				var op = (OperatorDeclarationSyntax)e;
				sb.Append ("operator ");
				AppendEscaped (sb, op.OperatorToken.ToString ());
				AppendParameter (sb, op.ParameterList);
			} else if (e is MethodDeclarationSyntax) {
				var method = (MethodDeclarationSyntax)e;
				if (method.ExplicitInterfaceSpecifier != null)
					AppendEscaped (sb, method.ExplicitInterfaceSpecifier + ".");
				sb.Append (method.Identifier);
				AppendTypeParameter (sb, method.TypeParameterList);
				AppendParameter (sb, method.ParameterList);
				if (method.Body != null && !method.Body.IsMissing) {
					string tag = null;
					if (method.Modifiers.Any (m => m.CSharpKind () == SyntaxKind.AbstractKeyword))
						tag = GettextCatalog.GetString ("(abstract)");
					if (method.Modifiers.Any (m => m.CSharpKind () == SyntaxKind.PartialKeyword))
						tag = GettextCatalog.GetString ("(partial)");
					if (tag != null)
						sb.Append (" <small>" + tag + "</small>");
				}
			} else if (e is ConstructorDeclarationSyntax) {
				var constructor = e as ConstructorDeclarationSyntax;
				sb.Append (constructor.Identifier);
				AppendParameter (sb, constructor.ParameterList);
			} else if (e is DestructorDeclarationSyntax) {
				var destructror = e as DestructorDeclarationSyntax;
				sb.Append ("~");
				sb.Append (destructror.Identifier);
				//				if (options.SpaceBeforeMethodDeclarationParentheses)
				//	sb.Append (" ");
				sb.Append ("()");
			} else if (e is IndexerDeclarationSyntax) {
				var indexer = e as IndexerDeclarationSyntax;
				sb.Append ("this");
				//if (options.SpaceBeforeIndexerDeclarationBracket)
				//	sb.Append (" ");
				sb.Append ("[");
				//if (options.SpaceWithinIndexerDeclarationBracket)
				//	sb.Append (" ");
				
				bool first = true;
				foreach (var param in indexer.ParameterList.Parameters) {
					if (!first) {
						//if (options.SpaceBeforeIndexerDeclarationParameterComma)
						//	sb.Append (" ");
						sb.Append (",");
						//if (options.SpaceAfterIndexerDeclarationParameterComma)
						//	sb.Append (" ");
					} else {
						first = false;
					}
					sb.Append (param.ToString ());
				}
				//if (options.SpaceWithinIndexerDeclarationBracket)
				//	sb.Append (" ");
				sb.Append ("]");
			} else if (e is VariableDeclaratorSyntax) {
				var initializer = (VariableDeclaratorSyntax)e;
				sb.Append (initializer.Identifier);
				if (IsObsolete (initializer.Parent as MemberDeclarationSyntax))
					return "<s>" + sb.ToString () + "</s>";
			} else if (e is FieldDeclarationSyntax) {
				var field = (FieldDeclarationSyntax)e;
				if (!field.Declaration.Variables.Any ())
					return "";
				sb.Append (field.Declaration.Variables.First ().Identifier);
			} else if (e is EventFieldDeclarationSyntax) {
				var evt = (EventFieldDeclarationSyntax)e;
				if (!evt.Declaration.Variables.Any ())
					return "";
				sb.Append (evt.Declaration.Variables.First ().Identifier);
			} else if (e is PropertyDeclarationSyntax) {
				var property = (PropertyDeclarationSyntax)e;
				if (property.ExplicitInterfaceSpecifier != null)
					AppendEscaped (sb, property.ExplicitInterfaceSpecifier + ".");
				sb.Append (property.Identifier);
			} else if (e is EventDeclarationSyntax) {
				var customEvent = (EventDeclarationSyntax)e;
				if (customEvent.ExplicitInterfaceSpecifier != null)
					AppendEscaped (sb, customEvent.ExplicitInterfaceSpecifier + ".");
				sb.Append (customEvent.Identifier);
			} /*else if (e is MemberDeclarationSyntax) {
				LoggingService.LogWarning ("can't display : " + e);
				//				var entity = (MemberDeclarationSyntax)e;
				// sb.Append (entity.Name);
			}*/

			string markup = sb.ToString ();
			if (IsObsolete (e as MemberDeclarationSyntax))
				return "<s>" + markup + "</s>";
			return markup;
		}
	}
}
