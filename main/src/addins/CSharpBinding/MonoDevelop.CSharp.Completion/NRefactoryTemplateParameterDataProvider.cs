// 
// NRefactoryTemplateParameterDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Text;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Resolver;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Completion
{
	public class NRefactoryTemplateParameterDataProvider : IParameterDataProvider
	{
		List<ITypeParameterMember> types = new List<ITypeParameterMember> ();
		static CSharpAmbience ambience = new CSharpAmbience ();
		
		public NRefactoryTemplateParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, IEnumerable<string> namespaces, ExpressionResult expressionResult, DomLocation loc)
		{
//			this.editor = editor;
			ResolveResult plainResolveResult = resolver.Resolve (expressionResult, loc);
			MethodResolveResult resolveResult = plainResolveResult as MethodResolveResult;
			if (resolveResult != null) {
				foreach (IMethod method in resolveResult.Methods) {
					if (method.TypeParameters.Count > 0)
						this.types.Add (method);
				}
			} else {
				string typeName = expressionResult.Expression.Trim ();
				foreach (string ns in namespaces) {
					string prefix = ns + (ns.Length > 0 ? "." : "") + typeName + "`";
					for (int i = 1; i < 99; i++) {
						IType possibleType = resolver.Dom.GetType (prefix + i);
						if (possibleType != null)
							this.types.Add (possibleType);
					}
				}
				IType resolvedType = plainResolveResult != null ? resolver.Dom.GetType (plainResolveResult.ResolvedType) : null;
				if (resolvedType == null) {
					int idx = expressionResult.Expression.LastIndexOf (".");
					if (idx < 0)
						return;
					typeName = expressionResult.Expression.Substring (idx + 1);
					expressionResult.Expression = expressionResult.Expression.Substring (0, idx);
					plainResolveResult = resolver.Resolve (expressionResult, loc);
					resolvedType = resolver.Dom.GetType (plainResolveResult.ResolvedType);
				}
				if (resolvedType == null)
					return;
				foreach (IType innerType in resolvedType.InnerTypes) {
					this.types.Add (innerType);
				}
			}
		}
		
		#region IParameterDataProvider implementation
		public int GetCurrentParameterIndex (ICompletionWidget widget, CodeCompletionContext ctx)
		{
			return GetCurrentParameterIndex (widget, ctx.TriggerOffset, 0);
		}
	
		internal static int GetCurrentParameterIndex (ICompletionWidget widget, int offset, int memberStart)
		{
			int cursor = widget.CurrentCodeCompletionContext.TriggerOffset;
			int i = offset;
			
			if (i > cursor)
				return -1;
			if (i == cursor)
				return 1;
			
			int index = memberStart + 1;
			int depth = 0;
			do {
				char c = widget.GetChar (i - 1);
				
				if (c == ',' && depth == 1)
					index++;
				if (c == '<')
					depth++;
				if (c == '>')
					depth--;
				i++;
			} while (i <= cursor && depth > 0);
			
			return depth == 0 ? -1 : index;
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter)
		{
			string name = ambience.GetString (types[overload], OutputFlags.UseFullName | OutputFlags.IncludeMarkup);
			StringBuilder parameters = new StringBuilder ();
			int curLen = 0;
			
			foreach (string parameter in parameterMarkup) {
				if (parameters.Length > 0)
					parameters.Append (", ");
				string text;
				Pango.AttrList attrs;
				char ch;
				Pango.Global.ParseMarkup (parameter, '_', out attrs, out text, out ch);
				if (curLen > 80) {
					parameters.AppendLine ();
					//parameters.Append (new string (' ', (prefix != null ? prefix.Length : 0) + name.Length + 4));
					curLen = 0;
				}
				curLen += text.Length + 2;
				parameters.Append (parameter);
			}
			return "<b>" + name + "</b>&lt;" + parameters + "&gt;";
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			ITypeParameterMember type = types[overload] is InstantiatedType ? ((InstantiatedType)types[overload]).UninstantiatedType : types[overload];
			if (paramIndex < 0 || paramIndex >= type.TypeParameters.Count)
				return "";
			return type.TypeParameters[paramIndex].Name;//ambience.GetString (, OutputFlags.AssemblyBrowserDescription | OutputFlags.HideExtensionsParameter | OutputFlags.IncludeGenerics | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload < 0 || overload >= OverloadCount)
				return 0;
			ITypeParameterMember type = types[overload] is InstantiatedType ? ((InstantiatedType)types[overload]).UninstantiatedType : types[overload];
			return type.TypeParameters.Count;
		}
		
		public int OverloadCount {
			get {
				return types.Count;
			}
		}
		#endregion 
	}
}
