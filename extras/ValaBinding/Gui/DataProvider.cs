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
using System.Text.RegularExpressions;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;

using MonoDevelop.ValaBinding.Parser;

namespace MonoDevelop.ValaBinding
{
	public class ParameterDataProvider : IParameterDataProvider
	{
		private TextEditor editor;
		private IList<Function> functions;
		private string functionName;
		private string returnType;
		private ProjectInformation info;
		
		private static Regex identifierRegex = new Regex(@"^[^\w\d]*(?<identifier>\w[\w\d\.<>]*)", RegexOptions.Compiled);
		public ParameterDataProvider (Document document, ProjectInformation info, string functionName)
		{
			this.editor = document.TextEditor;
			this.functionName = functionName;
			this.info = info;

			int lastDot = functionName.LastIndexOf (".", StringComparison.OrdinalIgnoreCase);
			string instancename = (0 <= lastDot)? functionName.Substring (0, lastDot): "this";
			
			Match match = identifierRegex.Match (instancename);
			if (match.Success && match.Groups["identifier"].Success) {
				instancename = match.Groups["identifier"].Value;
			}
			
			string typename = info.GetExpressionType (instancename, document.FileName, editor.CursorLine, editor.CursorColumn); // bottleneck
			info.Complete(typename, document.FileName, editor.CursorLine, editor.CursorColumn, null);
			string functionBaseName = (0 <= lastDot)? functionName.Substring (lastDot+1): functionName;
			
			match = identifierRegex.Match (functionBaseName);
			if (match.Success && match.Groups["identifier"].Success) {
				functionBaseName = match.Groups["identifier"].Value;
			}
			IList<Function> myfunctions = info.GetOverloads (functionBaseName); // bottleneck
			
			foreach (Function function in myfunctions) {
				if (string.Format("{0}.{1}", typename, functionBaseName).Equals (function.FullName, StringComparison.Ordinal)) {
					functions = new List<Function>(new Function[]{function});
					break;
				}
			}
			
			if (null == functions){ functions = myfunctions; }
		}// member function constructor
		
		public ParameterDataProvider (Document document, ProjectInformation info, string typename, string constructorOverload)
		{
			this.functionName = constructorOverload;
			this.editor = document.TextEditor;
			this.info = info;
			
			List<Function> myfunctions = info.GetConstructorsForType (typename, document.FileName, editor.CursorLine, editor.CursorColumn, null); // bottleneck
			if (0 < myfunctions.Count) {
				foreach (Function function in myfunctions) {
					if (functionName.Equals (function.Name, StringComparison.Ordinal)) {
						functions = new List<Function>(new Function[]{function});
						return;
					}
				}
			}
			
			functions = myfunctions;
		}// constructor constructor
		
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
			
//			if (editor.GetCharAt (i) == ')')
//				return -1;
			
			if (i > cursor)
				return -1;
			else if (i == cursor)
				return 1;
			
			int parameterIndex = 1;
			
			while (i++ < cursor) {
				char ch = editor.GetCharAt (i-1);
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
			string paramTxt = string.Join (", ", parameterMarkup);
			Function function = functions[overload];
			
			int len = function.FullName.LastIndexOf (".");
			string prename = null;
			
			if (len > 0)
				prename = function.FullName.Substring (0, len + 1);
			
			string cons = string.Empty;
			
//			if (function.IsConst)
//				cons = " const";

			return string.Format ("{2} {3}<b>{0}</b>({1})", GLib.Markup.EscapeText (function.Name), 
			                                                paramTxt, 
			                                                GLib.Markup.EscapeText (function.ReturnType), 
			                                                GLib.Markup.EscapeText (prename));
			// return prename + "<b>" + function.Name + "</b>" + " (" + paramTxt + ")" + cons;
		}
		
		// Returns the text to use to represent the specified parameter
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			Function function = functions[overload];
			
			return GLib.Markup.EscapeText (string.Format ("{1} {0}", function.Parameters[paramIndex].Key, function.Parameters[paramIndex].Value));
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
		
		public CompletionData (CodeNode item)
		{
			this.text = item.Name;
			this.completion_string = item.Name;
			this.description = item.Description;
			this.image = item.Icon;
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

	/// <summary>
	/// Mutable completion data list for asynchronous parsing
	/// </summary>
	public class ValaCompletionDataList: CompletionDataList, IMutableCompletionDataList
	{
		public ValaCompletionDataList (): base ()
		{
		}
		
		#region IMutableCompletionDataList implementation 
		
		public event EventHandler Changed;
		public event EventHandler Changing;
		
		protected virtual void OnChanged (object sender, EventArgs args)
		{
			if (null != Changed){ Changed (sender, args); }
		}// OnChanged
		
		protected virtual void OnChanging (object sender, EventArgs args)
		{
			if (null != Changing){ Changing (sender, args); }
		}// OnChanging
		
		public virtual bool IsChanging
		{
			get{ return isChanging; }
			internal set {
				isChanging = value;
				if (value) {
					OnChanging (this, new EventArgs ());
				} else {
					OnChanged (this, new EventArgs ());
				}
			}
		}
		protected bool isChanging;
		
		public void Dispose ()
		{
		}
		
		#endregion 
		

	}// ValaCompletionDataList
}
