//
// DataProvider.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;

using CBinding.Parser;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.Completion;

namespace CBinding
{
	public class ParameterDataProvider : MonoDevelop.Ide.CodeCompletion.ParameterDataProvider
	{
		private Mono.TextEditor.TextEditorData editor;
		private List<Function> functions = new List<Function> ();

		public ParameterDataProvider (int startOffset, Document document, ProjectInformation info, string functionName) :base (startOffset)
		{
			this.editor = document.Editor;
			
			foreach (Function f in info.Functions) {
				if (f.Name == functionName) {
					functions.Add (f);
				}
			}
			
			string currentFile = document.FileName;
			
			if (info.IncludedFiles.ContainsKey (currentFile)) {
				foreach (CBinding.Parser.FileInformation fi in info.IncludedFiles[currentFile]) {
					foreach (Function f in fi.Functions) {
						if (f.Name == functionName) {
							functions.Add (f);
						}
					}
				}
			}
		}
		
		// Returns the number of methods
		public override int Count {
			get { return functions.Count; }
		}
		
		// Returns the index of the parameter where the cursor is currently positioned.
		// -1 means the cursor is outside the method parameter list
		// 0 means no parameter entered
		// > 0 is the index of the parameter (1-based)
		public int GetCurrentParameterIndex (ICompletionWidget widget, CodeCompletionContext ctx)
		{
			int cursor = widget.CurrentCodeCompletionContext.TriggerOffset;
			int i = ctx.TriggerOffset;
			if (i < 0 || i >= editor.Length || editor.GetCharAt (i) == ')')
				return -1;
			
			if (i > cursor)
				return -1;
			else if (i == cursor)
				return 0;
			
			int parameterIndex = 1;
			
			while (i++ < cursor) {
				if (i >= widget.TextLength)
					break;
				char ch = widget.GetChar (i);
				if (ch == ',')
					parameterIndex++;
				else if (ch == ')')
					return -1;
			}
			
			return parameterIndex;
		}
		
		// Returns the markup to use to represent the specified method overload
		// in the parameter information window.
		public string GetHeading (int overload, string[] parameterMarkup, int currentParameter)
		{
			Function function = functions[overload];
			string paramTxt = string.Join (", ", parameterMarkup);
			
			int len = function.FullName.LastIndexOf ("::");
			string prename = null;
			
			if (len > 0)
				prename = GLib.Markup.EscapeText (function.FullName.Substring (0, len + 2));
			
			string cons = string.Empty;
			
			if (function.IsConst)
				cons = " const";
			
			return prename + "<b>" + function.Name + "</b>" + " (" + paramTxt + ")" + cons;
		}
		
		public string GetDescription (int overload, int currentParameter)
		{
			return "";
		}
		
		// Returns the text to use to represent the specified parameter
		public string GetParameterDescription (int overload, int paramIndex)
		{
			Function function = functions[overload];
			
			return GLib.Markup.EscapeText (function.Parameters[paramIndex]);
		}
		
		// Returns the number of parameters of the specified method
		public override int GetParameterCount (int overload)
		{
			return functions[overload].Parameters.Length;
		}
		public override string GetParameterName (int overload, int paramIndex)
		{
			return "";
		}
		public override bool AllowParameterList (int overload)
		{
			return false;
		}
	}
	
	public class CompletionData : MonoDevelop.Ide.CodeCompletion.CompletionData
	{
		private IconId image;
		private string text;
		private string description;
		private string completion_string;
		
		public CompletionData (LanguageItem item)
		{
			if (item is Class)
				image = Stock.Class;
			else if (item is Structure)
				image = Stock.Struct;
			else if (item is Union)
				image = "md-union";
			else if (item is Enumeration)
				image = Stock.Enum;
			else if (item is Enumerator)
				image = Stock.Literal;
			else if (item is Function)
				image = Stock.Method;
			else if (item is Namespace)
				image = Stock.NameSpace;
			else if (item is Typedef)
				image = Stock.Interface;
			else if (item is Member)
				image = Stock.Field;
			else if (item is Variable)
				image = Stock.Field;
			else if (item is Macro)
				image = Stock.Literal;
			else
				image = Stock.Literal;
			
			this.text = item.Name;
			this.completion_string = item.Name;
			this.description = string.Empty;
		}
		
		public override IconId Icon {
			get { return image; }
		}
		
		public override string DisplayText {
			get { return text; }
		}
		
		public override string Description {
			get { return description; }
		}
		
		public override string CompletionText {
			get { return completion_string; }
		}
	}

}
