// 
// DelegateDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Resolver;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	public class DelegateDataProvider : IParameterDataProvider
	{
		CSharpCompletionTextEditorExtension ext;
		
		IType delegateType;
		IMethod delegateMethod;
		CSharpAmbience ambience = new CSharpAmbience ();
		
		public DelegateDataProvider (CSharpCompletionTextEditorExtension ext, IType delegateType)
		{
			this.ext = ext;
			this.delegateType = delegateType;
			this.delegateMethod = delegateType.GetDelegateInvokeMethod ();
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
			
			string name = delegateType.Name;
			var parameters = new StringBuilder ();
			int curLen = 0;
			string prefix = !delegateMethod.IsConstructor ? ambience.GetString (ext.ctx, delegateMethod.ReturnType, flags) + " " : "";

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
			var sb = new StringBuilder ();
			sb.Append (prefix);
			sb.Append ("<b>");
			sb.Append (CSharpAmbience.FilterName (name));
			sb.Append ("</b> (");
			sb.Append (parameters.ToString ());
			sb.Append (")");
			
			if (delegateType.GetDefinition ().IsObsolete ()) {
				sb.AppendLine ();
				sb.Append (GettextCatalog.GetString ("[Obsolete]"));
			}
//			var curParameter = currentParameter >= 0 && currentParameter < m.Parameters.Count ? m.Parameters [currentParameter] : null;
//
//			string docText = AmbienceService.GetDocumentation (methods [overload]);
//
//			if (!string.IsNullOrEmpty (docText)) {
//				string text = docText;
//				if (curParameter != null) {
//					Regex paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
//					Match match = paramRegex.Match (docText);
//					if (match.Success) {
//						text = match.Groups [1].Value;
//						text = "<summary>" + AmbienceService.GetDocumentationSummary (methods [overload]) + "</summary>" + text;
//					}
//				} else {
//					text = "<summary>" + AmbienceService.GetDocumentationSummary (methods [overload]) + "</summary>";
//				}
//				sb.AppendLine ();
//				sb.Append (AmbienceService.GetDocumentationMarkup (text, new AmbienceService.DocumentationFormatOptions {
//					HighlightParameter = curParameter != null ? curParameter.Name : null,
//					Ambience = ambience,
//					SmallText = true
//				}));
//			}
//			
//			if (curParameter != null) {
//				var returnType = curParameter.DeclaringMember.GetProjectContent ().GetType (curParameter.ReturnType);
//				if (returnType != null && returnType.ClassType == ClassType.Delegate) {
//					sb.AppendLine ();
//					sb.AppendLine ();
//					sb.Append ("<small>");
//					sb.AppendLine (GettextCatalog.GetString ("Delegate information"));
//					sb.Append (ambience.GetString (returnType, OutputFlags.ReformatDelegates | OutputFlags.IncludeReturnType | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName));
//					sb.Append ("</small>");
//				}
//			}
			return sb.ToString ();
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			if (paramIndex < 0 || paramIndex >= delegateMethod.Parameters.Count)
				return "";
			
			return ambience.GetString (ext.ctx, delegateMethod, delegateMethod.Parameters [paramIndex], OutputFlags.AssemblyBrowserDescription | OutputFlags.HideExtensionsParameter | OutputFlags.IncludeGenerics | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload >= OverloadCount)
				return -1;
			return delegateMethod.Parameters != null ? delegateMethod.Parameters.Count : 0;
		}
		
		public int OverloadCount {
			get {
				return 1;
			}
		}
		#endregion 
	}
}
