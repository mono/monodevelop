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

using MonoDevelop.Core;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using System.Linq;

namespace MonoDevelop.CSharp.Completion
{
	public class MethodParameterDataProvider : IParameterDataProvider
	{
		protected CSharpCompletionTextEditorExtension ext;
		
		protected List<IMethod> methods = new List<IMethod> ();
		protected CSharpAmbience ambience = new CSharpAmbience ();
		int startOffset;
		protected bool staticResolve = false;

		public int StartOffset {
			get {
				return startOffset;
			}
		}
		
		protected MethodParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext)
		{
			this.startOffset = startOffset;
			this.ext = ext;	
		}
		
		public MethodParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IEnumerable<IMethod> m)
		{
			this.startOffset = startOffset;
			this.ext = ext;
			
			HashSet<string> alreadyAdded = new HashSet<string> ();
			foreach (var method in m) {
				if (method.IsConstructor)
					continue;
				string str = ambience.GetString (method, OutputFlags.IncludeParameters | OutputFlags.GeneralizeGenerics | OutputFlags.IncludeGenerics);
				if (alreadyAdded.Contains (str))
					continue;
				alreadyAdded.Add (str);
				methods.Add (method);
			}
			
			methods.Sort (MethodComparer);
		}
		
		public MethodParameterDataProvider (CSharpCompletionTextEditorExtension ext, IMethod method)
		{
			this.ext = ext;
			methods.Add (method);
		}
		
		static int MethodComparer (IMethod left, IMethod right)
		{
			return left.Parameters.Count - right.Parameters.Count;
		}
		
//		public NRefactoryParameterDataProvider (TextEditorData editor, TypeResolveResult resolveResult)
//		{
//			HashSet<string> alreadyAdded = new HashSet<string> ();
//			if (resolveResult.CallingType != null) {
//				bool includeProtected = true;
//				foreach (IMethod method in resolveResult.CallingType.Methods) {
//					if (!method.IsConstructor)
//						continue;
//					string str = ambience.GetString (method, OutputFlags.IncludeParameters);
//					if (alreadyAdded.Contains (str))
//						continue;
//					alreadyAdded.Add (str);
//					
//					if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
//						methods.Add (method);
//				}
//			}
//		}
//		
//		public NRefactoryParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, BaseResolveResult resolveResult)
//		{
//			HashSet<string> alreadyAdded = new HashSet<string> ();
//			if (resolveResult.CallingType != null) {
//				IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
//				foreach (IReturnType rt in resolveResult.CallingType.BaseTypes) {
//					IType baseType = resolver.SearchType (rt);
//					bool includeProtected = DomType.IncludeProtected (resolver.Dom, baseType, resolvedType);
//					
//					if (baseType != null) {
//						foreach (IMethod method in baseType.Methods) {
//							if (!method.IsConstructor)
//								continue;
//							string str = ambience.GetString (method, OutputFlags.IncludeParameters);
//							if (alreadyAdded.Contains (str))
//								continue;
//							alreadyAdded.Add (str);
//							
//							if (method.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected))
//								methods.Add (method);
//						}
//					}
//				}
//			}
//		}
//
//		// used for constructor completion
//		public NRefactoryParameterDataProvider (TextEditorData editor, NRefactoryResolver resolver, IType type)
//		{
//			if (type != null) {
//				if (type.ClassType == ClassType.Delegate) {
//					IMethod invokeMethod = ExtractInvokeMethod (type);
//					if (type is InstantiatedType) {
//						this.delegateName = ((InstantiatedType)type).UninstantiatedType.Name;
//					} else {
//						this.delegateName = type.Name;
//					}
//					if (invokeMethod != null) {
//						methods.Add (invokeMethod);
//					} else {
//						// no invoke method -> tried to create an abstract delegate
//					}
//					return;
//				}
//				bool includeProtected = DomType.IncludeProtected (resolver.Dom, type, resolver.CallingType);
//				bool constructorFound = false;
//				HashSet<string> alreadyAdded = new HashSet<string> ();
//				foreach (IMethod method in type.Methods) {
//					constructorFound |= method.IsConstructor;
//					string str = ambience.GetString (method, OutputFlags.IncludeParameters);
//					if (alreadyAdded.Contains (str))
//						continue;
//					alreadyAdded.Add (str);
//					if ((method.IsConstructor && method.IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected)))
//						methods.Add (method);
//				}
//				// No constructor - generating default
//				if (!constructorFound && (type.TypeModifier & TypeModifier.HasOnlyHiddenConstructors) != TypeModifier.HasOnlyHiddenConstructors) {
//					DomMethod defaultConstructor = new DomMethod ();
//					defaultConstructor.MethodModifier = MethodModifier.IsConstructor;
//					defaultConstructor.DeclaringType = type;
//					methods.Add (defaultConstructor);
//				}
//			}
//		}
//		IMethod ExtractInvokeMethod (IType type)
//		{
//			foreach (IMethod method in type.Methods) {
//				if (method.Name == "Invoke")
//					return method;
//			}
//			
//			return null;
//		}
//		
// 		string delegateName = null;
//		public NRefactoryParameterDataProvider (TextEditorData editor, string delegateName, IType type)
//		{
//			this.delegateName = delegateName;
//			if (type != null) {
//				methods.Add (ExtractInvokeMethod (type));
//			}
//		}
		
		#region IParameterDataProvider implementation
		
		protected virtual string GetPrefix (IMethod method)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			return ambience.GetString (method.ReturnType, flags) + " ";
		}
		
		public string GetHeading (int overload, string[] parameterMarkup, int currentParameter)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			if (staticResolve)
				flags |= OutputFlags.StaticUsage;
			
			var m = methods [overload];
			
			string name = m.EntityType == EntityType.Constructor || m.EntityType == EntityType.Destructor ? m.DeclaringType.Name : m.Name;
			var parameters = new StringBuilder ();
			int curLen = 0;
			string prefix = GetPrefix (m);
			
			foreach (string parameter in parameterMarkup) {
				if (parameters.Length > 0)
					parameters.Append (", ");
				string text;
				Pango.AttrList attrs;
				char ch;
				Pango.Global.ParseMarkup (parameter, '_', out attrs, out text, out ch);
				if (text != null)
					curLen += text.Length + 2;
				parameters.Append (parameter);
			}
			var sb = new StringBuilder ();
			if (m.IsExtensionMethod)
				sb.Append (GettextCatalog.GetString ("(Extension) "));
			sb.Append (prefix);
			sb.Append ("<b>");
			sb.Append (CSharpAmbience.FilterName (name));
			sb.Append ("</b> (");
			sb.Append (parameters.ToString ());
			sb.Append (")");
			
			return sb.ToString ();
		}
		
		public string GetDescription (int overload, int currentParameter)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			if (staticResolve)
				flags |= OutputFlags.StaticUsage;
			
			var m = methods [overload];
			
			var sb = new StringBuilder ();
	
			
			if (m.IsObsolete ()) {
				sb.AppendLine ();
				sb.Append (GettextCatalog.GetString ("[Obsolete]"));
			}
			
			var curParameter = currentParameter >= 0 && currentParameter < m.Parameters.Count ? m.Parameters [currentParameter] : null;
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
				sb.Append (AmbienceService.GetDocumentationMarkup (text, new AmbienceService.DocumentationFormatOptions {
					HighlightParameter = curParameter != null ? curParameter.Name : null,
					Ambience = ambience,
					SmallText = true,
					BoldHeadings = false
				}));
			}
			
			if (curParameter != null) {
				var returnType = curParameter.Type;
				if (returnType.Kind == TypeKind.Delegate) {
					Console.WriteLine (sb.ToString());
					if (sb.Length > 0) {
						sb.AppendLine ();
						sb.AppendLine ();
					}
					sb.Append ("<small>");
					sb.AppendLine (GettextCatalog.GetString ("Delegate information:"));
					sb.Append (ambience.GetString (returnType, OutputFlags.ReformatDelegates | OutputFlags.IncludeReturnType | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName));
					sb.Append ("</small>");
				}
			}
			return sb.ToString ();
		}
		
		public string GetParameterDescription (int overload, int paramIndex)
		{
			IMethod method = methods [overload];
			
			if (paramIndex < 0 || paramIndex >= method.Parameters.Count)
				return "";
			if (method.IsExtensionMethod)
				paramIndex++;
			return ambience.GetString (method, method.Parameters [paramIndex], OutputFlags.AssemblyBrowserDescription | OutputFlags.HideExtensionsParameter | OutputFlags.IncludeGenerics | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload >= Count)
				return -1;
			IMethod method = methods [overload];
			if (method == null || method.Parameters == null)
				return 0;

			if (method.IsExtensionMethod)
				return method.Parameters.Count - 1;
			return method.Parameters.Count;
		}
		
		public bool AllowParameterList (int overload)
		{
			if (overload >= Count)
				return false;
			var lastParam = methods[overload].Parameters.LastOrDefault ();
			return lastParam != null && lastParam.IsParams;
		}
		
		public int Count {
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
