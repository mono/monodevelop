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
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp
{
	public class AstAmbience
	{
		CSharpFormattingOptions options;
		
		public AstAmbience (ICSharpCode.NRefactory.CSharp.CSharpFormattingOptions options)
		{
			this.options = options;
		}
		
		static bool IsObsolete (EntityDeclaration entity)
		{
			if (entity == null)
				return false;
			foreach (var section in entity.Attributes) {
				foreach (var attr in section.Attributes) {
					var attrText = attr.Type.GetText ();
					if (attrText == "Obsolete" || attrText == "ObsoleteAttribute" || attrText == "System.Obsolete" || attrText == "System.ObsoleteAttribute" )
						return true;
				}
			}
			return false;
		}
		
		void AppendTypeParameter (StringBuilder sb, IEnumerable<TypeParameterDeclaration> parameters)
		{
			if (!parameters.Any ()) 
				return;
			sb.Append ("&lt;");
			bool first = true;
			foreach (var param in parameters) {
				if (!first) {
					sb.Append (", ");
				} else {
					first = false;
				}
				AppendEscaped (sb, param.GetText (options));
			}
			sb.Append ("&gt;");
		}
		
		void AppendParameter (StringBuilder sb, IEnumerable<ParameterDeclaration> parameters)
		{
			if (options.SpaceBeforeMethodDeclarationParentheses)
				sb.Append (" ");
			sb.Append ("(");
			var hasParameters = parameters.Any ();
			if (!hasParameters && options.SpaceBetweenEmptyMethodDeclarationParentheses) {
				sb.Append (" )");
				return;
			}
			if (hasParameters && options.SpaceWithinMethodDeclarationParentheses)
				sb.Append (" ");
			
			bool first = true;
			foreach (var param in parameters) {
				if (!first) {
					if (options.SpaceBeforeMethodDeclarationParameterComma)
						sb.Append (" ");
					sb.Append (",");
					if (options.SpaceAfterMethodDeclarationParameterComma)
						sb.Append (" ");
				} else {
					first = false;
				}
				AppendEscaped (sb, param.GetText (options));
			}
			if (hasParameters && options.SpaceWithinMethodDeclarationParentheses)
				sb.Append (" ");
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
		
		public string GetEntityMarkup (AstNode e)
		{
			var sb = new StringBuilder ();
			if (e is TypeDeclaration) {
				var type = e as TypeDeclaration;
				sb.Append (type.Name);
				AppendTypeParameter (sb, type.TypeParameters);
			} else if (e is DelegateDeclaration) {
				var del = e as DelegateDeclaration;
				sb.Append (del.Name);
				AppendTypeParameter (sb, del.TypeParameters);
				AppendParameter (sb, del.Parameters);
			} else if (e is Accessor) {
				if (e.Role == PropertyDeclaration.GetterRole) {
					sb.Append ("get");
				} else if (e.Role == PropertyDeclaration.SetterRole) {
					sb.Append ("set");
				} else if (e.Role == CustomEventDeclaration.AddAccessorRole) {
					sb.Append ("add");
				} else if (e.Role == CustomEventDeclaration.RemoveAccessorRole) {
					sb.Append ("remove");
				}
			} else if (e is OperatorDeclaration) {
				var op = e as OperatorDeclaration;
				sb.Append ("operator");
				if (!op.OperatorTypeToken.IsNull)
					AppendEscaped (sb, op.OperatorTypeToken.GetText ());
				AppendParameter (sb, op.Parameters);
			} else if (e is MethodDeclaration) {
				var method = e as MethodDeclaration;
				if (!method.PrivateImplementationType.IsNull)
					AppendEscaped (sb, method.PrivateImplementationType.GetText () + ".");
				sb.Append (method.Name);
				AppendTypeParameter (sb, method.TypeParameters);
				AppendParameter (sb, method.Parameters);
				if (method.Body.IsNull) {
					string tag = null;
					if (method.HasModifier (Modifiers.Abstract))
						tag = GettextCatalog.GetString ("(abstract)");
					if (method.HasModifier (Modifiers.Partial))
						tag = GettextCatalog.GetString ("(partial)");
					if (tag != null)
						sb.Append (" <small>" + tag + "</small>");
				}
			} else if (e is ConstructorDeclaration) {
				var constructor = e as ConstructorDeclaration;
				sb.Append (constructor.Name);
				AppendParameter (sb, constructor.Parameters);
			} else if (e is DestructorDeclaration) {
				var destructror = e as DestructorDeclaration;
				sb.Append ("~");
				sb.Append (destructror.Name);
				if (options.SpaceBeforeMethodDeclarationParentheses)
					sb.Append (" ");
				sb.Append ("()");
			} else if (e is IndexerDeclaration) {
				var indexer = e as IndexerDeclaration;
				sb.Append ("this");
				if (options.SpaceBeforeIndexerDeclarationBracket)
					sb.Append (" ");
				sb.Append ("[");
				if (options.SpaceWithinIndexerDeclarationBracket)
					sb.Append (" ");
				
				bool first = true;
				foreach (var param in indexer.Parameters) {
					if (!first) {
						if (options.SpaceBeforeIndexerDeclarationParameterComma)
							sb.Append (" ");
						sb.Append (",");
						if (options.SpaceAfterIndexerDeclarationParameterComma)
							sb.Append (" ");
					} else {
						first = false;
					}
					sb.Append (param.GetText (options));
				}
				if (options.SpaceWithinIndexerDeclarationBracket)
					sb.Append (" ");
				sb.Append ("]");
			} else if (e is VariableInitializer) {
				var initializer = (VariableInitializer)e;
				sb.Append (initializer.Name);
				if (IsObsolete (initializer.Parent as EntityDeclaration))
					return "<s>" + sb.ToString () + "</s>";
			} else if (e is FieldDeclaration) {
				var field = (FieldDeclaration)e;
				if (!field.Variables.Any ())
					return "";
				sb.Append (field.Variables.First ().Name);
			} else if (e is EventDeclaration) {
				var evt = (EventDeclaration)e;
				if (!evt.Variables.Any ())
					return "";
				sb.Append (evt.Variables.First ().Name);
			} else if (e is PropertyDeclaration) {
				var property = (PropertyDeclaration)e;
				if (!property.PrivateImplementationType.IsNull)
					AppendEscaped (sb, property.PrivateImplementationType.GetText () + ".");
				sb.Append (property.Name);
			} else if (e is CustomEventDeclaration) {
				var customEvent = (CustomEventDeclaration)e;
				if (!customEvent.PrivateImplementationType.IsNull)
					AppendEscaped (sb, customEvent.PrivateImplementationType.GetText () + ".");
				sb.Append (customEvent.Name);
			} else if (e is EntityDeclaration) {
				var entity = (EntityDeclaration)e;
				sb.Append (entity.Name);
			}

			string markup = sb.ToString ();
			if (IsObsolete (e as EntityDeclaration))
				return "<s>" + markup + "</s>";
			return markup;
		}
	}
}
