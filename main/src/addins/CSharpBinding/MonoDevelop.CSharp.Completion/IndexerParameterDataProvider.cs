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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;

using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	class IndexerParameterDataProvider : AbstractParameterDataProvider
	{
		AstNode resolvedExpression;
		List<IProperty> indexers;

		public IndexerParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IType type, AstNode resolvedExpression) : base (ext, startOffset)
		{
			this.resolvedExpression = resolvedExpression;
			indexers = new List<IProperty> (type.GetProperties (p => p.IsIndexer));
		}

		#region IParameterDataProvider implementation
		public override string GetHeading (int overload, string[] parameterMarkup, int currentParameter)
		{
			StringBuilder result = new StringBuilder ();
//			int curLen = 0;
			result.Append (GetShortType (indexers [overload].ReturnType));
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
			return result.ToString ();
		}
		
		public override string GetDescription (int overload, int currentParameter)
		{
			StringBuilder result = new StringBuilder ();
			var curParameter = currentParameter >= 0 && currentParameter < indexers [overload].Parameters.Count ? indexers [overload].Parameters [currentParameter] : null;
			if (curParameter != null) {
				string docText = AmbienceService.GetDocumentation (indexers [overload]);
				if (!string.IsNullOrEmpty (docText)) {
					var paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
					var match = paramRegex.Match (docText);
					if (match.Success) {
						result.AppendLine ();
						string text = match.Groups [1].Value;
						text = "<summary>" + AmbienceService.GetDocumentationSummary (indexers [overload]) + "</summary>" + text;
						result.Append (AmbienceService.GetDocumentationMarkup (text, new AmbienceService.DocumentationFormatOptions {
							HighlightParameter = curParameter.Name,
							MaxLineLength = 60
						}));
					}
				}
			}
			
			return result.ToString ();
		}
		
		public override string GetParameterDescription (int overload, int paramIndex)
		{
			var indexer = indexers[overload];
			
			if (paramIndex < 0 || paramIndex >= indexer.Parameters.Count)
				return "";

			return GetParameterString (indexer.Parameters [paramIndex]);
		}

		public override int GetParameterCount (int overload)
		{
			if (overload >= Count)
				return -1;
			var indexer = indexers[overload];
			return indexer != null && indexer.Parameters != null ? indexer.Parameters.Count : 0;
		}

		public override bool AllowParameterList (int overload)
		{
			if (overload >= Count)
				return false;
			var lastParam = indexers[overload].Parameters.LastOrDefault ();
			return lastParam != null && lastParam.IsParams;
		}
		
		public override int Count {
			get {
				return indexers != null ? indexers.Count : 0;
			}
		}
		#endregion
	}
}
