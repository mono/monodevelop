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
using MonoDevelop.Projects.Policies;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.Ide.TypeSystem
{
	public class OutputSettings
	{
		public OutputFlags OutputFlags {
			get;
			set;
		}
		
		public PolicyContainer PolicyParent {
			get;
			set;
		}
		
		public OutputSettings (OutputFlags outputFlags)
		{
			this.OutputFlags = outputFlags;
		}
		
		public string Markup (string text)
		{
			if (MarkupCallback != null)
				return MarkupCallback (text);
			return IncludeMarkup ? PangoFormat (text) : text;
		}

		public string EmitName (object domVisitable, string text)
		{
			if (EmitNameCallback != null)
				return EmitNameCallback (domVisitable, text);
			return text;
		}
		
		public string EmitModifiers (string text)
		{
			if (!IncludeModifiers)
				return string.Empty;
			if (EmitModifiersCallback != null)
				return EmitModifiersCallback (text) + " ";
			if (IncludeMarkup)
				return "<b>" + PangoFormat (text) + "</b> ";
			return text + " ";
		}
		
		public string EmitKeyword (string text)
		{
			if (EmitKeywordCallback != null)
				return EmitKeywordCallback (text) + " ";
			if (!IncludeKeywords)
				return "";
			if (IncludeMarkup)
				return "<b>" + PangoFormat (text) + "</b> ";
			return text + " ";
		}
		
		public string Highlight (string text)
		{
			if (HighlightCallback != null)
				return HighlightCallback (text);
			if (IncludeMarkup)
				return "<b>" + PangoFormat (text) + "</b>";
			return text;
		}
		
		public string PostProcess (object domVisitable, string outString)
		{
			if (PostProcessCallback != null)
				return PostProcessCallback (domVisitable, outString);
			return outString;
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
			
		public bool IncludeMarkup {
			get {
				return (OutputFlags & OutputFlags.IncludeMarkup) == OutputFlags.IncludeMarkup;
			}
		}
		
		public bool IncludeKeywords  {
			get {
				return (OutputFlags & OutputFlags.IncludeKeywords) == OutputFlags.IncludeKeywords;
			}
		}
		
		public bool IncludeModifiers {
			get {
				return (OutputFlags & OutputFlags.IncludeModifiers) == OutputFlags.IncludeModifiers;
			}
		}

		public bool UseFullName {
			get {
				return (OutputFlags & OutputFlags.UseFullName) == OutputFlags.UseFullName;
			}
		}
		
		public bool UseFullInnerTypeName {
			get {
				return (OutputFlags & OutputFlags.UseFullInnerTypeName) == OutputFlags.UseFullInnerTypeName;
			}
		}
		
		public bool IncludeParameters {
			get {
				return (OutputFlags & OutputFlags.IncludeParameters) == OutputFlags.IncludeParameters;
			}
		}

		public bool IncludeReturnType {
			get {
				return (OutputFlags & OutputFlags.IncludeReturnType) == OutputFlags.IncludeReturnType;
			}
		}
		
		public bool IncludeParameterName {
			get {
				return (OutputFlags & OutputFlags.IncludeParameterName) == OutputFlags.IncludeParameterName;
			}
		}
		
		public bool IncludeBaseTypes {
			get {
				return (OutputFlags & OutputFlags.IncludeBaseTypes) == OutputFlags.IncludeBaseTypes;
			}
		}
		
		public bool IncludeGenerics {
			get {
				return (OutputFlags & OutputFlags.IncludeGenerics) == OutputFlags.IncludeGenerics;
			}
		}
		
		public bool HideArrayBrackets {
			get {
				return (OutputFlags & OutputFlags.HideArrayBrackets) == OutputFlags.HideArrayBrackets;
			}
		}
		
		public bool HighlightName {
			get {
				return (OutputFlags & OutputFlags.HighlightName) == OutputFlags.HighlightName;
			}
		}
		
		public bool HideExtensionsParameter {
			get {
				return (OutputFlags & OutputFlags.HideExtensionsParameter) == OutputFlags.HideExtensionsParameter;
			}
		}
		
		public bool HideGenericParameterNames {
			get {
				return (OutputFlags & OutputFlags.HideGenericParameterNames) != 0;
			}
		}
		
		public bool GeneralizeGenerics {
			get {
				return (OutputFlags & OutputFlags.GeneralizeGenerics) != 0;
			}
		}
		
		public bool UseNETTypeNames {
			get {
				return (OutputFlags & OutputFlags.UseNETTypeNames) != 0;
			}
		}
		
		public bool ReformatDelegates {
			get {
				return (OutputFlags & OutputFlags.ReformatDelegates) != 0;
			}
		}
		
		public bool StaticUsage {
			get {
				return (OutputFlags & OutputFlags.StaticUsage) != 0;
			}
		}
		
		public bool IncludeConstraints {
			get {
				return (OutputFlags & OutputFlags.IncludeConstraints) != 0;
			}
		}
		
		public bool CompletionListFomat {
			get {
				return (OutputFlags & OutputFlags.CompletionListFomat) != 0;
			}
		}
		
		public bool ReturnTypesLast {
			get {
				return (OutputFlags & OutputFlags.ReturnTypesLast) != 0;
			}
		}
		
		public bool IncludeAccessor {
			get {
				return (OutputFlags & OutputFlags.IncludeAccessor) == OutputFlags.IncludeAccessor;
			}
		}

		public MarkupText EmitModifiersCallback;
		public MarkupText EmitKeywordCallback;
		public MarkupText MarkupCallback;
		public MarkupText HighlightCallback;
		public Func<object, string, string> EmitNameCallback;
		
		public delegate string MarkupText (string text);
		
		public Func<object, string, string> PostProcessCallback;
	}
}
