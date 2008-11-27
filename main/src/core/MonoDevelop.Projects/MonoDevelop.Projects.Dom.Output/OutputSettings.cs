// OutputSettings.cs
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
using System.Text;

namespace MonoDevelop.Projects.Dom.Output
{
	public class OutputSettings
	{
		public OutputFlags OutputFlags {
			get;
			set;
		}
		
		public OutputSettings (OutputFlags outputFlags)
		{
			this.OutputFlags = outputFlags;
			PostProcess = delegate (OutputSettings settings, IDomVisitable domVisitable, ref string outString) {
				
			};
			
			EmitModifiers = delegate (string text) {
				if (!IncludeModifiers)
					return "";
				if (IncludeMarkup)
					return "<b>" + PangoFormat (text) + "</b> ";
				return text + " ";
			};
			
			EmitKeyword = delegate (string text) {
				if (!IncludeKeywords)
					return "";
				if (IncludeMarkup)
					return "<b>" + PangoFormat (text) + "</b> ";
				return text + " ";
			};
			
			Highlight = delegate (string text) {
				if (IncludeMarkup)
					return "<b>" + PangoFormat (text) + "</b> ";
				return text;
			};
			
			Markup = delegate (string text) {
				return IncludeMarkup ? PangoFormat (text) : text;
			};
		}
		
		static string PangoFormat (string input)
		{
			StringBuilder result = new StringBuilder ();
			foreach (char ch in input) {
				switch (ch) {
					case '<':
						result.Append ("&lt;");
						break;
					case '>':
						result.Append ("&gt;");
						break;
					case '&':
						result.Append ("&amp;");
						break;
					default:
						result.Append (ch);
						break;
				}
			}
			return result.ToString ();
		}
			
		protected bool IncludeMarkup {
			get {
				return (OutputFlags & OutputFlags.EmitMarkup) == OutputFlags.EmitMarkup;
			}
		}
		
		protected bool IncludeKeywords  {
			get {
				return (OutputFlags & OutputFlags.EmitKeywords) == OutputFlags.EmitKeywords;
			}
		}
		
		protected bool IncludeModifiers {
			get {
				return (OutputFlags & OutputFlags.IncludeModifiers) == OutputFlags.IncludeModifiers;
			}
		}
		
		public MarkupText EmitModifiers;
		public MarkupText EmitKeyword;
		public MarkupText Markup;
		public MarkupText Highlight;
		
		public delegate string MarkupText (string text);
		
		public ProcessString PostProcess;
		public delegate void ProcessString (OutputSettings settings, IDomVisitable domVisitable, ref string outString);
	}
}
