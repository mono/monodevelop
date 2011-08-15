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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Resolver;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Completion
{
	public class NRefactoryParameterDataProvider : IParameterDataProvider
	{
		List<IMethod> methods = new List<IMethod> ();
		CSharpAmbience ambience = new CSharpAmbience ();
		
		bool staticResolve = false;
		
		public NRefactoryParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, MethodResolveResult resolveResult)
		{
			this.staticResolve = resolveResult.StaticResolve;
			bool includeProtected = true;
			HashSet<string> alreadyAdded = new HashSet<string> ();
			foreach (IMethod method in resolveResult.Methods) {
				if (method.IsConstructor)
					continue;
				string str = ambience.GetString (method, OutputFlags.IncludeParameters | OutputFlags.GeneralizeGenerics | OutputFlags.IncludeGenerics);
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
		
		public NRefactoryParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, ThisResolveResult resolveResult)
		{
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
		
		public NRefactoryParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, BaseResolveResult resolveResult)
		{
			HashSet<string> alreadyAdded = new HashSet<string> ();
			if (resolveResult.CallingType != null) {
				IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
				foreach (IReturnType rt in resolveResult.CallingType.BaseTypes) {
					IType baseType = resolver.SearchType (rt);
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
		public NRefactoryParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, IType type)
		{
			if (type != null) {
				if (type.ClassType == MonoDevelop.Projects.Dom.ClassType.Delegate) {
					IMethod invokeMethod = ExtractInvokeMethod (type);
					if (type is InstantiatedType) {
						this.delegateName = ((InstantiatedType)type).UninstantiatedType.Name;
					} else {
						this.delegateName = type.Name;
					}
					if (invokeMethod != null) {
						methods.Add (invokeMethod);
					} else {
						// no invoke method -> tried to create an abstract delegate
					}
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
		public NRefactoryParameterDataProvider (TextEditorData editor, string delegateName, IType type)
		{
			this.delegateName = delegateName;
			if (type != null) {
				methods.Add (ExtractInvokeMethod (type));
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
				return 1; // parameters are 1 based
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpIndentEngine engine = new CSharpIndentEngine (MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types));
			int index = memberStart + 1;
			int parentheses = 0;
			int bracket = 0;
			do {
				char c = widget.GetChar (i - 1);
				engine.Push (c);
				switch (c) {
				case '{':
					if (!engine.IsInsideOrdinaryCommentOrString)
						bracket++;
					break;
				case '}':
					if (!engine.IsInsideOrdinaryCommentOrString)
						bracket--;
					break;
				case '(':
					if (!engine.IsInsideOrdinaryCommentOrString)
						parentheses++;
					break;
				case ')':
					if (!engine.IsInsideOrdinaryCommentOrString)
						parentheses--;
					break;
				case ',':
					if (!engine.IsInsideOrdinaryCommentOrString && parentheses == 1 && bracket == 0)
						index++;
					break;
				}
				i++;
			} while (i <= cursor && parentheses >= 0);
			
			return parentheses != 1 || bracket > 0 ? -1 : index;
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			if (staticResolve)
				flags |= OutputFlags.StaticUsage;
			string name = (this.delegateName ?? (methods [overload].IsConstructor ? ambience.GetString (methods [overload].DeclaringType, flags) : methods [overload].Name));
			StringBuilder parameters = new StringBuilder ();
			int curLen = 0;
			string prefix = !methods [overload].IsConstructor ? ambience.GetString (methods [overload].ReturnType, flags) + " " : "";

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
			if (!staticResolve && methods [overload].WasExtended)
				sb.Append (GettextCatalog.GetString ("(Extension) "));
			sb.Append (prefix);
			sb.Append ("<b>");
			sb.Append (CSharpAmbience.FilterName (name));
			sb.Append ("</b> (");
			sb.Append (parameters.ToString ());
			sb.Append (")");

			if (methods [overload].IsObsolete) {
				sb.AppendLine ();
				sb.Append (GettextCatalog.GetString ("[Obsolete]"));
			}
			IParameter curParameter = currentParameter >= 0 && currentParameter < methods [overload].Parameters.Count ? methods [overload].Parameters [currentParameter] : null;

			string docText = AmbienceService.GetDocumentation (methods [overload]);

			if (!string.IsNullOrEmpty (docText)) {
				string text = docText;
				if (curParameter != null) {
					Regex paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
					Match match = paramRegex.Match (docText);
					if (match.Success) {
						text = match.Groups [1].Value;
						text = "<summary>" + AmbienceService.GetDocumentationSummary (methods [overload]) + "</summary>" + text;
					}
				} else {
					text = "<summary>" + AmbienceService.GetDocumentationSummary (methods [overload]) + "</summary>";
				}
				sb.AppendLine ();
				sb.Append (AmbienceService.GetDocumentationMarkup (text, new AmbienceService.DocumentationFormatOptions {
					HighlightParameter = curParameter != null ? curParameter.Name : null,
					Ambience = ambience,
					SmallText = true
				}));
			}
			
			if (curParameter != null) {
				var returnType = curParameter.DeclaringMember.SourceProjectDom.GetType (curParameter.ReturnType);
				if (returnType != null && returnType.ClassType == MonoDevelop.Projects.Dom.ClassType.Delegate) {
					sb.AppendLine ();
					sb.AppendLine ();
					sb.Append ("<small>");
					sb.AppendLine (GettextCatalog.GetString ("Delegate information"));
					sb.Append (ambience.GetString (returnType, OutputFlags.ReformatDelegates | OutputFlags.IncludeReturnType | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName));
					sb.Append ("</small>");
				}
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
			if (overload >= OverloadCount)
				return -1;
			IMethod method = methods[overload];
			return method != null && method.Parameters != null ? method.Parameters.Count : 0;
		}
		
		public int OverloadCount {
			get {
				return methods != null ? methods.Count : 0;
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
