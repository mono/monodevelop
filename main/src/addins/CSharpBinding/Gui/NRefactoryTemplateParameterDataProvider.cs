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
using System.Xml;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using CSharpBinding.FormattingStrategy;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryTemplateParameterDataProvider : IParameterDataProvider
	{
		MonoDevelop.Ide.Gui.TextEditor editor;
		List<IType> types = new List<IType> ();
		CSharpAmbience ambience = new CSharpAmbience ();
		
		public NRefactoryTemplateParameterDataProvider (MonoDevelop.Ide.Gui.TextEditor editor, NRefactoryResolver resolver, IEnumerable<string> namespaces, string typeName)
		{
			this.editor = editor;
			foreach (string ns in namespaces) {
				string prefix = ns + (ns.Length > 0 ? "." : "") + typeName + "`";
				for (int i = 1; i < 99; i++) {
					IType possibleType = resolver.Dom.GetType (prefix + i);
					if (possibleType != null)
						types.Add (possibleType);
				}
			}
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
				if (c == '<')
					depth++;
				if (c == '>')
					depth--;
				i++;
			} while (i <= cursor && depth > 0);
			
			return depth == 0 ? -1 : index;
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup)
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
			if (paramIndex < 0 || paramIndex >= types[overload].TypeParameters.Count)
				return "";
			return types[overload].TypeParameters[paramIndex].Name;//ambience.GetString (, OutputFlags.AssemblyBrowserDescription | OutputFlags.HideExtensionsParameter | OutputFlags.IncludeGenerics | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload < 0 || overload >= OverloadCount)
				return 0;
			int result = types[overload].TypeParameters.Count;
			
			return result;
		}
		
		public int OverloadCount {
			get {
				return types.Count;
			}
		}
		#endregion 
	}
}
