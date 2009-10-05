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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using CSharpBinding.FormattingStrategy;
using System.Text.RegularExpressions;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryParameterDataProvider : IParameterDataProvider
	{
		MonoDevelop.Ide.Gui.TextEditor editor;
		List<IMethod> methods = new List<IMethod> ();
		CSharpAmbience ambience = new CSharpAmbience ();
		
		bool staticResolve = false;
		
		public NRefactoryParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, NRefactoryResolver resolver, MethodResolveResult resolveResult)
		{
			this.editor = editor;
			this.staticResolve = resolveResult.StaticResolve;
			bool includeProtected = true;
			
			HashSet<string> alreadyAdded = new HashSet<string> ();
			foreach (IMethod method in resolveResult.Methods) {
				if (method.IsConstructor)
					continue;
				string str = ambience.GetString (method, OutputFlags.IncludeParameters);
				if (alreadyAdded.Contains (str))
					continue;
				alreadyAdded.Add (str);
				if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
					methods.Add (method);
			}
			methods.Sort (MethodComparer);
		}
		
		static int MethodComparer (IMethod left, IMethod right)
		{
			return left.Parameters.Count - right.Parameters.Count;
		}
		
		public NRefactoryParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, NRefactoryResolver resolver, ThisResolveResult resolveResult)
		{
			this.editor = editor;
			HashSet<string> alreadyAdded = new HashSet<string> ();
			if (resolveResult.CallingType != null) {
				bool includeProtected = true;
				foreach (IMethod method in resolveResult.CallingType.Methods) {
					if (!method.IsConstructor)
						continue;
					string str = ambience.GetString (method, OutputFlags.IncludeParameters);
					if (alreadyAdded.Contains (str))
						continue;
					alreadyAdded.Add (str);
					
					if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
						methods.Add (method);
				}
			}
		}
		
		public NRefactoryParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, NRefactoryResolver resolver, BaseResolveResult resolveResult)
		{
			this.editor = editor;
			HashSet<string> alreadyAdded = new HashSet<string> ();
			if (resolveResult.CallingType != null) {
				IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
				foreach (IReturnType rt in resolveResult.CallingType.BaseTypes) {
					IType baseType = resolver.Dom.SearchType (new SearchTypeRequest (resolver.Unit, rt, resolver.CallingType));
					bool includeProtected = DomType.IncludeProtected (resolver.Dom, baseType, resolvedType);
					
					if (baseType != null) {
						foreach (IMethod method in baseType.Methods) {
							if (!method.IsConstructor)
								continue;
							string str = ambience.GetString (method, OutputFlags.IncludeParameters);
							if (alreadyAdded.Contains (str))
								continue;
							alreadyAdded.Add (str);
							
							if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
								methods.Add (method);
						}
					}
				}
			}
		}

		// used for constructor completion
		public NRefactoryParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, NRefactoryResolver resolver, IType type)
		{
			this.editor = editor;
			
			if (type != null) {
				if (type.ClassType == ClassType.Delegate) {
					IMethod invokeMethod = ExtractInvokeMethod (type);
					if (type is InstantiatedType) {
						this.delegateName = ((InstantiatedType)type).UninstantiatedType.Name;
					} else {
						this.delegateName = type.Name;
					}
					methods.Add (invokeMethod);
					return;
				}
				bool includeProtected = DomType.IncludeProtected (resolver.Dom, type, resolver.CallingType);
				bool constructorFound = false;
				HashSet<string> alreadyAdded = new HashSet<string> ();
				foreach (IMethod method in type.Methods) {
					constructorFound |= method.IsConstructor;
					string str = ambience.GetString (method, OutputFlags.IncludeParameters);
					if (alreadyAdded.Contains (str))
						continue;
					alreadyAdded.Add (str);
					if ((method.IsConstructor && method.IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected)))
						methods.Add (method);
				}
				// No constructor - generating default
				if (!constructorFound && (type.TypeModifier & TypeModifier.HasOnlyHiddenConstructors) != TypeModifier.HasOnlyHiddenConstructors) {
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
		public NRefactoryParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, string delegateName, IType type)
		{
			this.editor = editor;
			this.delegateName = delegateName;
			if (type != null) {
				methods.Add (ExtractInvokeMethod (type));
			}
		}
		
		#region IParameterDataProvider implementation
		
		public int GetCurrentParameterIndex (CodeCompletionContext ctx)
		{
			return GetCurrentParameterIndex (editor, ctx.TriggerOffset, 0);
		}
		
		internal static int GetCurrentParameterIndex (MonoDevelop.Ide.Gui.TextEditor editor, int offset, int memberStart)
		{
			int cursor = editor.CursorPosition;
			int i = offset;
			
			if (i > cursor)
				return -1;
			if (i == cursor) 
				return 1; // parameters are 1 based
			CSharpIndentEngine engine = new CSharpIndentEngine ();
			int index = memberStart + 1;
			do {
				char c = editor.GetCharAt (i - 1);
				
				engine.Push (c);
				
				if (c == ',' && engine.StackDepth == 1)
					index++;
				
				i++;
			} while (i <= cursor && engine.StackDepth > 0);
			
			return engine.StackDepth == 0 ? -1 : index;
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter)
		{
			string name = (this.delegateName ?? (methods[overload].IsConstructor ? ambience.GetString (methods[overload].DeclaringType, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics) : methods[overload].Name));
			StringBuilder parameters = new StringBuilder ();
			int curLen = 0;
			string prefix = ambience.GetString (methods[overload].ReturnType, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup  | OutputFlags.IncludeGenerics) + " ";

			foreach (string parameter in parameterMarkup) {
				if (parameters.Length > 0)
					parameters.Append (", ");
				string text;
				Pango.AttrList attrs;
				char ch;
				Pango.Global.ParseMarkup (parameter, '_', out attrs, out text, out ch);
				if (curLen > 80) {
					parameters.AppendLine ();
					parameters.Append (new string (' ', (prefix != null ? prefix.Length : 0) + name.Length + 4));
					curLen = 0;
				}
				curLen += text.Length + 2;
				parameters.Append (parameter);
			}
			StringBuilder sb = new StringBuilder ();
			if (methods[overload].WasExtended)
				sb.Append (GettextCatalog.GetString ("(Extension) "));
			sb.Append (prefix);
			sb.Append ("<b>");
			sb.Append (name);
			sb.Append ("</b> (");
			sb.Append (parameters.ToString ());
			sb.Append (")");

			if (methods[overload].IsObsolete) {
				sb.AppendLine ();
				sb.Append (GettextCatalog.GetString ("[Obsolete]"));
			}
			IParameter curParameter = currentParameter >= 0 && currentParameter < methods[overload].Parameters.Count ? methods[overload].Parameters[currentParameter] : null;

			if (curParameter != null) {
				string docText = AmbienceService.GetDocumentation (methods[overload]);

				if (!string.IsNullOrEmpty (docText)) {
					Regex paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
					Match match = paramRegex.Match (docText);
					if (match.Success) {
						sb.AppendLine ();
						string text = match.Groups[1].Value;
						text = "<summary>" + AmbienceService.GetDocumentationSummary (methods[overload]) + "</summary>" + text;
						sb.Append (AmbienceService.GetDocumentationMarkup (text, new AmbienceService.DocumentationFormatOptions {
							HighlightParameter = curParameter.Name,
							MaxLineLength = 60
						}));
					}
				}
			} else {
				sb.AppendLine ();
				sb.Append (AmbienceService.GetSummaryMarkup (methods[overload]));
			}
			return sb.ToString ();
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			IMethod method = methods[overload];
			
			if (paramIndex < 0 || paramIndex >= method.Parameters.Count)
				return "";
			
			return ambience.GetString (method.Parameters [paramIndex], OutputFlags.AssemblyBrowserDescription | OutputFlags.HideExtensionsParameter | OutputFlags.IncludeGenerics | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			return methods[overload].Parameters.Count;
		}
		
		public int OverloadCount {
			get {
				return methods.Count;
			}
		}

		#endregion 

		public List<IMethod> Methods {
			get {
				return methods;
			}
			set {
				methods = value;
			}
		}
	}
}
