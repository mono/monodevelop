// 
// NRefactoryIndexerParameterDataProvider.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;

using System.Text.RegularExpressions;
using MonoDevelop.CSharp.Dom;

namespace MonoDevelop.CSharp.Completion
{
	public class NRefactoryIndexerParameterDataProvider : IParameterDataProvider
	{
//		IType type;
		string resolvedExpression;
		MonoDevelop.Ide.Gui.TextEditor editor;
		static CSharpAmbience ambience = new CSharpAmbience ();
		List<IProperty> indexers;
		
		public NRefactoryIndexerParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, IType type, string resolvedExpression)
		{
			this.editor = editor;
//			this.type = type;
			this.resolvedExpression = resolvedExpression;
			indexers = new List<IProperty> (type.Properties.Where (p => p.IsIndexer && !p.Name.Contains ('.')));
		}

		#region IParameterDataProvider implementation
		public int GetCurrentParameterIndex (CodeCompletionContext ctx)
		{
			return NRefactoryParameterDataProvider.GetCurrentParameterIndex (editor, ctx.TriggerOffset, 0);
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter)
		{
			StringBuilder result = new StringBuilder ();
//			int curLen = 0;
			result.Append (ambience.GetString (indexers[overload].ReturnType, OutputFlags.ClassBrowserEntries));
			result.Append (' ');
			result.Append ("<b>");
			result.Append (resolvedExpression);
			result.Append ("</b>");
			result.Append ('[');
			int parameterCount = 0;
			foreach (string parameter in parameterMarkup) {
				if (parameterCount > 0)
					result.Append (", ");
				result.Append (parameter);
				parameterCount++;
			}
			result.Append (']');
			IParameter curParameter = currentParameter >= 0 && currentParameter < indexers[overload].Parameters.Count ? indexers[overload].Parameters[currentParameter] : null;
			if (curParameter != null) {
				string docText = AmbienceService.GetDocumentation (indexers[overload]);
				if (!string.IsNullOrEmpty (docText)) {
					Regex paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
					Match match = paramRegex.Match (docText);
					if (match.Success) {
						result.AppendLine ();
						string text = match.Groups[1].Value;
						text = "<summary>" + AmbienceService.GetDocumentationSummary (indexers[overload]) + "</summary>" + text;
						result.Append (AmbienceService.GetDocumentationMarkup (text, new AmbienceService.DocumentationFormatOptions {
							HighlightParameter = curParameter.Name,
							MaxLineLength = 60
						}));
					}
				}
			}
			
			return result.ToString ();
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			return ambience.GetString (indexers[overload].Parameters[paramIndex], OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName  | OutputFlags.IncludeReturnType | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload < 0 || overload >= OverloadCount)
				return 0;
			return indexers[overload].Parameters.Count;
		}
		
		public int OverloadCount {
			get {
				return indexers.Count;
			}
		}
		#endregion
	}
}
