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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Completion;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.Completion
{
	class MethodParameterDataProvider : AbstractParameterDataProvider
	{
		protected List<IMethod> methods = new List<IMethod> ();
		protected CSharpAmbience ambience = new CSharpAmbience ();
		protected bool staticResolve = false;

		ICompilation compilation;
		CSharpUnresolvedFile file;


		protected MethodParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext) : base (ext, startOffset)
		{
			compilation = ext.UnresolvedFileCompilation;
			file = ext.CSharpUnresolvedFile;
		}
		
		public MethodParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IEnumerable<IMethod> m) : base (ext, startOffset)
		{
			compilation = ext.UnresolvedFileCompilation;
			file = ext.CSharpUnresolvedFile;

			HashSet<string> alreadyAdded = new HashSet<string> ();
			foreach (var method in m) {
				if (method.IsConstructor)
					continue;
				if (!method.IsBrowsable ())
					continue;
				string str = ambience.GetString (method, OutputFlags.IncludeParameters | OutputFlags.GeneralizeGenerics | OutputFlags.IncludeGenerics);
				if (alreadyAdded.Contains (str))
					continue;
				alreadyAdded.Add (str);
				methods.Add (method);
			}
			
			methods.Sort (MethodComparer);
		}
		
		public MethodParameterDataProvider (CSharpCompletionTextEditorExtension ext, IMethod method) : base (ext, 0)
		{
			methods.Add (method);
		}
		
		protected internal static int MethodComparer (IMethod left, IMethod right)
		{
			bool lObs = left.IsObsolete ();
			bool rObs = right.IsObsolete ();

			if (lObs && !rObs)
				return 1;
			if (!lObs && rObs)
				return -1;

			var lstate = left.GetEditorBrowsableState ();
			var rstate = right.GetEditorBrowsableState ();
			if (lstate == rstate) {
				if (left.Parameters.Any (p => p.IsParams) && !right.Parameters.Any (p => p.IsParams))
					return 1;
				if (!left.Parameters.Any (p => p.IsParams) && right.Parameters.Any (p => p.IsParams))
					return -1;
				var cnt = left.Parameters.Count (p => !p.IsOptional) - right.Parameters.Count (p => !p.IsOptional);
				if (cnt == 0)
					cnt = left.Parameters.Count (p => p.IsOptional) - right.Parameters.Count (p => p.IsOptional);
				if (cnt == 0) {
					for (int i = 0; i < left.Parameters.Count; i++) {
						if (left.Parameters [i].Type.Name == "NSDictionary" && right.Parameters [i].Type.Name != "NSDictionary")
							return 1;
						if (right.Parameters [i].Type.Name == "NSDictionary" && left.Parameters [i].Type.Name != "NSDictionary")
							return -1;
					}
				}
				return cnt;
			}
			return lstate.CompareTo (rstate);
		}


		public override MonoDevelop.Ide.CodeCompletion.TooltipInformation CreateTooltipInformation (int overload, int currentParameter, bool smartWrap)
		{
			return CreateTooltipInformation (ext, compilation, file, methods[overload], currentParameter, smartWrap);
		}

		public static TooltipInformation CreateTooltipInformation (CSharpCompletionTextEditorExtension ext, ICompilation compilation, CSharpUnresolvedFile file, IParameterizedMember entity, int currentParameter, bool smartWrap)
		{
			return CreateTooltipInformation (compilation, file, ext.Editor, ext.FormattingPolicy, entity, currentParameter, smartWrap);
		}

		public static TooltipInformation CreateTooltipInformation (ICompilation compilation, CSharpUnresolvedFile file, TextEditor textEditorData, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy formattingPolicy, IParameterizedMember entity, int currentParameter, bool smartWrap)
		{
			var tooltipInfo = new TooltipInformation ();
//			var resolver = file.GetResolver (compilation, textEditorData.Caret.Location);
//			var sig = new SignatureMarkupCreator (resolver, formattingPolicy.CreateOptions ());
//			sig.HighlightParameter = currentParameter;
//			sig.BreakLineAfterReturnType = smartWrap;
//			try {
//				tooltipInfo.SignatureMarkup = sig.GetMarkup (entity);
//			} catch (Exception e) {
//				LoggingService.LogError ("Got exception while creating markup for :" + entity, e);
//				return new TooltipInformation ();
//			}
//			tooltipInfo.SummaryMarkup = AmbienceService.GetSummaryMarkup (entity) ?? "";
//			
//			if (entity is IMethod) {
//				var method = (IMethod)entity;
//				if (method.IsExtensionMethod) {
//					tooltipInfo.AddCategory (GettextCatalog.GetString ("Extension Method from"), method.DeclaringTypeDefinition.FullName);
//				}
//			}
//			int paramIndex = currentParameter;
//
//			if (entity is IMethod && ((IMethod)entity).IsExtensionMethod)
//				paramIndex++;
//			paramIndex = Math.Min (entity.Parameters.Count - 1, paramIndex);
//
//			var curParameter = paramIndex >= 0  && paramIndex < entity.Parameters.Count ? entity.Parameters [paramIndex] : null;
//			if (curParameter != null) {
//
//				string docText = AmbienceService.GetDocumentation (entity);
//				if (!string.IsNullOrEmpty (docText)) {
//					string text = docText;
//					Regex paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
//					Match match = paramRegex.Match (docText);
//					
//					if (match.Success) {
//						text = AmbienceService.GetDocumentationMarkup (entity, match.Groups [1].Value);
//						if (!string.IsNullOrWhiteSpace (text))
//							tooltipInfo.AddCategory (GettextCatalog.GetString ("Parameter"), text);
//					}
//				}
//		
//				if (curParameter.Type.Kind == TypeKind.Delegate)
//					tooltipInfo.AddCategory (GettextCatalog.GetString ("Delegate Info"), sig.GetDelegateInfo (curParameter.Type));
//			}
			return tooltipInfo;
		}

		#region IParameterDataProvider implementation
		
		protected virtual string GetPrefix (IMethod method)
		{
			return GetShortType (method.ReturnType) + " ";
		}
		/*
		public override string GetHeading (int overload, string[] parameterMarkup, int currentParameter)
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
		
		public override string GetDescription (int overload, int currentParameter)
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
					if (sb.Length > 0) {
						sb.AppendLine ();
						sb.AppendLine ();
					}
					sb.Append ("<span font='11'>");
					sb.AppendLine (GettextCatalog.GetString ("Delegate information:"));
					sb.Append (ambience.GetString (returnType, OutputFlags.ReformatDelegates | OutputFlags.IncludeReturnType | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName));
					sb.Append ("<span>");
				}
			}
			return sb.ToString ();
		}

		public override string GetParameterDescription (int overload, int paramIndex)
		{
			IMethod method = methods [overload];
			
			if (paramIndex < 0 || paramIndex >= method.Parameters.Count)
				return "";
			if (method.IsExtensionMethod)
				paramIndex++;
			var parameter = method.Parameters [paramIndex];

			return GetParameterString (parameter);
		}*/
		
		public override int GetParameterCount (int overload)
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

		public override string GetParameterName (int overload, int paramIndex)
		{
			IMethod method = methods [overload];
			return method.Parameters[paramIndex].Name;
		}
		public override bool AllowParameterList (int overload)
		{
			if (overload >= Count)
				return false;
			var lastParam = methods[overload].Parameters.LastOrDefault ();
			return lastParam != null && lastParam.IsParams;
		}
		
		public override int Count {
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
