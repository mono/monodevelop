//
// DataProvider.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
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

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;

using MonoDevelop.ValaBinding.Parser;

namespace MonoDevelop.ValaBinding
{
	public class ParameterDataProvider : IParameterDataProvider
	{
		private TextEditor editor;
		private List<Function> functions = new List<Function> ();
		
		public ParameterDataProvider (Document document, ProjectInformation info, string functionName)
		{
			this.editor = document.TextEditor;
			
			foreach (Function f in info.Functions) {
				if (f.Name == functionName) {
					functions.Add (f);
				}
			}
			
			string currentFile = document.FileName;
			
			if (info.IncludedFiles.ContainsKey (currentFile)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFile]) {
					foreach (Function f in fi.Functions) {
						if (f.Name == functionName) {
							functions.Add (f);
						}
					}
				}
			}
		}
		
		// Returns the number of methods
		public int OverloadCount {
			get { return functions.Count; }
		}
		
		// Returns the index of the parameter where the cursor is currently positioned.
		// -1 means the cursor is outside the method parameter list
		// 0 means no parameter entered
		// > 0 is the index of the parameter (1-based)
		public int GetCurrentParameterIndex (ICodeCompletionContext ctx)
		{
			int cursor = editor.CursorPosition;
			int i = ctx.TriggerOffset;
			
			if (editor.GetCharAt (i) == ')')
				return -1;
			
			if (i > cursor)
				return -1;
			else if (i == cursor)
				return 0;
			
			int parameterIndex = 1;
			
			while (i++ < cursor) {
				char ch = editor.GetCharAt (i);
				if (ch == ',')
					parameterIndex++;
				else if (ch == ')')
					return -1;
			}
			
			return parameterIndex;
		}
		
		// Returns the markup to use to represent the specified method overload
		// in the parameter information window.
		public string GetMethodMarkup (int overload, string[] parameterMarkup)
		{
			Function function = functions[overload];
			string paramTxt = string.Join (", ", parameterMarkup);
			
			int len = function.FullName.LastIndexOf (".");
			string prename = null;
			
			if (len > 0)
				prename = function.FullName.Substring (0, len + 2);
			
			string cons = string.Empty;
			
			if (function.IsConst)
				cons = " const";
			
			return prename + "<b>" + function.Name + "</b>" + " (" + paramTxt + ")" + cons;
		}
		
		// Returns the text to use to represent the specified parameter
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			Function function = functions[overload];
			
			return function.Parameters[paramIndex];
		}
		
		// Returns the number of parameters of the specified method
		public int GetParameterCount (int overload)
		{
			return functions[overload].Parameters.Length;
		}
	}
	
	public class CompletionData : ICompletionData
	{
		private string image;
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
		
		public string Icon {
			get { return image; }
		}
		
		public string DisplayText {
			get { return text; }
		}
		
		public string Description {
			get { return description; }
		}

		public string CompletionText {
			get { return completion_string; }
		}
		
		public DisplayFlags DisplayFlags {
			get { return DisplayFlags.None; }
		}
	}

}
