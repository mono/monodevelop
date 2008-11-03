// NRefactoryParameterDataProvider.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using CSharpBinding.FormattingStrategy;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryParameterDataProvider : IParameterDataProvider
	{
		TextEditor editor;
		List<IMethod> methods = new List<IMethod> ();
		CSharpAmbience ambience = new CSharpAmbience ();
		NRefactoryResolver resolver;
		bool staticResolve = false;
		
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, MethodResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolver = resolver;
			this.staticResolve = resolveResult.StaticResolve;
			methods.AddRange (resolveResult.Methods);
			if (resolveResult.Methods.Count > 0)
				this.prefix = ambience.GetString (resolveResult.Methods[0].ReturnType, OutputFlags.ClassBrowserEntries | OutputFlags.EmitMarkup  | OutputFlags.IncludeGenerics) + " ";
		}
		
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, ThisResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolver = resolver;
			if (resolveResult.CallingType != null) {
				bool includeProtected = true;
				foreach (IMethod method in resolveResult.CallingType.Methods) {
					if (!method.IsConstructor)
						continue;
					if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
						methods.Add (method);
				}
			}
		}
		
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, BaseResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolver = resolver;
			if (resolveResult.CallingType != null) {
				IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
				foreach (IReturnType rt in resolveResult.CallingType.BaseTypes) {
					IType baseType = resolver.Dom.SearchType (new SearchTypeRequest (resolver.Unit, rt));
					bool includeProtected = DomType.IncludeProtected (resolver.Dom, baseType, resolvedType);
					
					if (baseType != null) {
						foreach (IMethod method in baseType.Methods) {
							if (!method.IsConstructor)
								continue;
							if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
								methods.Add (method);
						}
					}
				}
			}
		}

		// used for constructor completion
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, IType type)
		{
			this.editor = editor;
			this.resolver = resolver;
			if (type != null) {
				if (type.ClassType == ClassType.Delegate) {
					IMethod invokeMethod = ExtractInvokeMethod (type);
					if (type is InstantiatedType) {
						this.delegateName = ((InstantiatedType)type).UninstantiatedType.Name;
					} else {
						this.delegateName = type.Name;
					}
					this.prefix = ambience.GetString (invokeMethod.ReturnType, OutputFlags.ClassBrowserEntries | OutputFlags.EmitMarkup  | OutputFlags.IncludeGenerics) + " ";
					
					methods.Add (invokeMethod);
					return;
				}
				bool includeProtected = DomType.IncludeProtected (resolver.Dom, type, resolver.CallingType);
				bool constructorFound = false;
				foreach (IMethod method in type.Methods) {
					constructorFound |= method.IsConstructor;
					if ((method.IsConstructor && method.IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected)))
						methods.Add (method);
				}
				// No constructor - generating default
				if (!constructorFound) {
					DomMethod defaultConstructor = new DomMethod ();
					defaultConstructor.MethodModifier = MethodModifier.IsConstructor;
					defaultConstructor.DeclaringType = type;
					methods.Add (defaultConstructor);
				}
			}
		}
		IMethod ExtractInvokeMethod (IType type)
		{
			foreach (IMethod method in type.Methods) {
				if (method.Name == "Invoke")
					return method;
			}
			return null;
		}
		
 		string delegateName = null;
		string prefix = null;
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, string delegateName, IType type)
		{
			this.editor = editor;
			this.resolver = resolver;
			this.delegateName = delegateName;
			if (type != null) {
				methods.Add (ExtractInvokeMethod (type));
			}
		}
		
		#region IParameterDataProvider implementation
		
		public int GetCurrentParameterIndex (ICodeCompletionContext ctx)
		{
			return GetCurrentParameterIndex (editor, ctx.TriggerOffset);
		}
		
		internal static int GetCurrentParameterIndex (TextEditor editor, int offset)
		{
			int cursor = editor.CursorPosition;
			int i = offset;
			
			if (i > cursor)
				return -1;
			if (i == cursor)
				return 0;
			
			CSharpIndentEngine engine = new CSharpIndentEngine ();
			int index = 1;
			do {
				char c = editor.GetCharAt (i - 1);
				
				engine.Push (c);
				
				if (c == ',' && engine.StackDepth == 1)
					index++;
				
				i++;
			} while (i <= cursor && engine.StackDepth > 0);
			
			return engine.StackDepth == 0 ? -1 : index;
		}
		
		void GeneratePango (StringBuilder sb, XmlNode node)
		{
			if (node == null)
				return;
			if (node is XmlText) {
				sb.Append (node.InnerText);
			} else if (node is XmlElement) {
				XmlElement el = node as XmlElement;
				switch (el.Name) {
					case "see":
					case "seealso":
						sb.Append ("<span foreground=\"blue\" underline=\"single\">");
						sb.Append (el.GetAttribute ("cref"));
						sb.Append ("</span> ");
						break;
				}
			}
			foreach (XmlNode child in node.ChildNodes) {
				GeneratePango (sb, child);
			}
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup)
		{
			return prefix + "<b>" + (this.delegateName ?? (methods[overload].IsConstructor ? ambience.GetString (methods[overload].DeclaringType, OutputFlags.ClassBrowserEntries | OutputFlags.EmitMarkup  | OutputFlags.IncludeGenerics) : methods[overload].Name)) + "</b> (" + string.Join (", ", parameterMarkup)  + ")";
//			return ambience.GetIntellisenseDescription (methods[overload]);
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			if (!this.staticResolve && methods[overload].IsExtension)
				paramIndex++;
			
			if (methods[overload].Parameters == null || paramIndex < 0 || paramIndex >= methods[overload].Parameters.Count)
				return "";
			return ambience.GetString (methods[overload].Parameters [paramIndex], OutputFlags.AssemblyBrowserDescription | OutputFlags.HighlightName | OutputFlags.HideExtensionsParameter);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload < 0 || overload >= OverloadCount)
				return 0;
			int result = methods[overload].Parameters != null ? methods[overload].Parameters.Count : 0;
			if (!this.staticResolve && methods[overload].IsExtension)
				result--;
			return result;
		}
		
		public int OverloadCount {
			get {
				return methods.Count;
			}
		}
		#endregion 
	}
}
