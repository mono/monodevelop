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
using System.Threading;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.ValaBinding.Parser;
using MonoDevelop.ValaBinding.Parser.Afrodite;

namespace MonoDevelop.ValaBinding
{
	public class ParameterDataProvider : IParameterDataProvider
	{
		Document document;
		private IList<Symbol> functions;
		private string functionName;
		
		public ParameterDataProvider (Document document, ProjectInformation info, string functionName)
		{
			this.document = document;
			this.functionName = functionName;

			functions = new List<Symbol> ();
			Symbol function = info.GetFunction (functionName, document.FileName, document.Editor.Caret.Line + 1, document.Editor.Caret.Column + 1);
			if (null != function){ functions.Add (function); }
		}// member function constructor
		
		/// <summary>
		/// Create a ParameterDataProvider for a constructor
		/// </summary>
		/// <param name="constructorOverload">
		/// A <see cref="System.String"/>: The named of the pertinent constructor overload
		/// </param>
		public ParameterDataProvider (Document document, ProjectInformation info, string typename, string constructorOverload)
		{
			this.functionName = constructorOverload;
			this.document = document;
			
			List<Symbol> myfunctions = info.GetConstructorsForType (typename, document.FileName, document.Editor.Caret.Line + 1, document.Editor.Caret.Column + 1, null); // bottleneck
			if (1 < myfunctions.Count) {
				foreach (Symbol function in myfunctions) {
					if (functionName.Equals (function.Name, StringComparison.Ordinal)) {
						functions = new List<Symbol> () {function};
						return;
					}
				}
			}
			
			functions = myfunctions;
		}// constructor constructor
		
		/// <summary>
		/// The number of overloads for this method
		/// </summary>
		public int OverloadCount {
			get { return functions.Count; }
		}
		
		/// <summary>
		/// Get the index of the parameter where the cursor is currently positioned.
		/// </summary>
		/// <param name="ctx">
		/// A <see cref="CodeCompletionContext"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>: The index of the parameter, 
		/// 0 for no parameter entered, 
		/// -1 for outside the list
		/// </returns>
		public int GetCurrentParameterIndex (ICompletionWidget widget, CodeCompletionContext ctx)
		{
			int cursor = document.Editor.Caret.Offset;
			int i = ctx.TriggerOffset;
			
			if (i > cursor)
				return -1;
			else if (i == cursor)
				return 1;
			
			int parameterIndex = 1;
			
			while (i++ < cursor) {
				char ch = document.Editor.GetCharAt (i-1);
				if (ch == ',')
					parameterIndex++;
				else if (ch == ')')
					return -1;
			}
			
			return parameterIndex;
		}
		
		/// <summary>
		/// Get the markup to use to represent the specified method overload
		/// in the parameter information window.
		/// </summary>
		public string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter)
		{
			string paramTxt = string.Join (", ", parameterMarkup);
			Symbol function = functions[overload];
			
			int len = function.FullyQualifiedName.LastIndexOf (".");
			string prename = null;
			
			if (len > 0)
				prename = function.FullyQualifiedName.Substring (0, len + 1);
			
//			string cons = string.Empty;
			
//			if (function.IsConst)
//				cons = " const";

			return string.Format ("{2} {3}<b>{0}</b>({1})", GLib.Markup.EscapeText (function.Name), 
			                                                paramTxt, 
			                                                GLib.Markup.EscapeText (function.ReturnType.TypeName), 
			                                                GLib.Markup.EscapeText (prename));
			// return prename + "<b>" + function.Name + "</b>" + " (" + paramTxt + ")" + cons;
		}
		
		/// <summary>
		/// Get the text to use to represent the specified parameter
		/// </summary>
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			Symbol function = functions[overload];
			
			if (null != function && null != function.Parameters[paramIndex]) {
				string name = function.Parameters[paramIndex].Name;
				string type = function.Parameters[paramIndex].TypeName;
				return GLib.Markup.EscapeText (string.Format ("{1} {0}", name, type));
			}
			
			return string.Empty;
		}
		
		/// <summary>
		/// Get the number of parameters of the specified method
		/// </summary>
		public int GetParameterCount (int overload)
		{
			if (null != functions && null != functions[overload] && null != functions[overload].Parameters) {
				return functions[overload].Parameters.Count;
			}
			return 0;
		}
	}
	
	/// <summary>
	/// Data for Vala completion
	/// </summary>
	internal class CompletionData : MonoDevelop.Ide.CodeCompletion.CompletionData
	{
		private string image;
		private string text;
		private string description;
		private string completion_string;
		
		public CompletionData (Symbol item)
		{
			this.text = item.Name;
			this.completion_string = item.Name;
			this.description = item.DisplayText;
			this.image = item.Icon;
			DisplayFlags = DisplayFlags.None;
			CompletionCategory = new ValaCompletionCategory (text, image);
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

	internal class ValaCompletionDataList: CompletionDataList, IMutableCompletionDataList
	{
		public ValaCompletionDataList (): base ()
		{
			IsChanging = true;
			Add (string.Empty);
		}
		
		internal virtual void AddRange (IEnumerable<CompletionData> vals)
		{
			foreach (CompletionData item in vals) {
				Add (item);
			}
		}
		
		#region IMutableCompletionDataList implementation
		
		public event EventHandler Changed;
		public event EventHandler Changing;
		
		
		public bool IsChanging {
			get { return isChanging; }
			set {
				isChanging = value;
				if (value){ OnChanging (this, null); }
				else{ OnChanged (this, null); }
			}
		}
		private bool isChanging;
		
		#endregion
		
		protected virtual void OnChanging (object sender, EventArgs args)
		{
			if (null != Changing) {
				Changing (sender, args);
			}
		}
		
		protected virtual void OnChanged (object sender, EventArgs args)
		{
			if (null != Changed) {
				Changed (sender, args);
			}
		}
		
		public void Dispose ()
		{
		}
	}// ValaCompletionDataList
	
	internal class ValaCompletionCategory: CompletionCategory
	{
		public ValaCompletionCategory (string text, string image): base (text, image)
		{
		}
		
		public override int CompareTo (CompletionCategory other)
		{
			return DisplayText.CompareTo (other.DisplayText);
		}
	}
}
