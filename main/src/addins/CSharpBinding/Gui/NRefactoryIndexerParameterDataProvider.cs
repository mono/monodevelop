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
using CSharpBinding.FormattingStrategy;
using MonoDevelop.CSharpBinding;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryIndexerParameterDataProvider : IParameterDataProvider
	{
		IType type;
		string resolvedExpression;
		MonoDevelop.Ide.Gui.TextEditor editor;
		static CSharpAmbience ambience = new CSharpAmbience ();
		List<IProperty> indexers;
		
		public NRefactoryIndexerParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, IType type, string resolvedExpression)
		{
			this.editor = editor;
			this.type = type;
			this.resolvedExpression = resolvedExpression;
			indexers = new List<IProperty> (type.Properties.Where (p => p.IsIndexer));
		}

		#region IParameterDataProvider implementation
		public int GetCurrentParameterIndex (ICodeCompletionContext ctx)
		{
			int result =  GetCurrentParameterIndex (editor, ctx.TriggerOffset, 0);
			return result;
		}
		
		internal static int GetCurrentParameterIndex (MonoDevelop.Ide.Gui.TextEditor editor, int offset, int memberStart)
		{
			int cursor = editor.CursorPosition;
			int i = offset;
			
			if (i > cursor)
				return -1;
			if (i == cursor)
				return 0;
			
			int index = memberStart + 1;
			int depth = 0;
			do {
				char c = editor.GetCharAt (i - 1);
				
				if (c == ',' && depth == 1)
					index++;
				if (c == '[')
					depth++;
				if (c == ']')
					depth--;
				i++;
			} while (i <= cursor && depth > 0);
			
			return depth == 0 ? -1 : index;
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter)
		{
			Console.WriteLine (indexers[overload] + "/" + indexers[overload].Parameters.Count);
			StringBuilder result = new StringBuilder ();
			int curLen = 0;
			result.Append (ambience.GetString (indexers[overload].ReturnType, OutputFlags.ClassBrowserEntries));
			result.Append (' ');
			result.Append (resolvedExpression);
			result.Append ('[');
			int parameterCount = 0;
			foreach (IParameter parameter in indexers[overload].Parameters) {
				if (parameterCount > 0)
					result.Append (", ");
				if (parameterCount == currentParameter)
					result.Append ("<b>");
				result.Append (ambience.GetString (parameter, OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName  | OutputFlags.IncludeReturnType));
				if (parameterCount == currentParameter)
					result.Append ("</b>");
				parameterCount++;
			}
			result.Append (']');
			return result.ToString ();
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			return type.Name;
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
