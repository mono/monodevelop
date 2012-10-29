// 
// TemplateParameterDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Resolver;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Completion;

namespace MonoDevelop.CSharp.Completion
{
	public class TemplateParameterDataProvider: IParameterDataProvider
	{
		int startOffset;
		//CSharpCompletionTextEditorExtension ext;
		
		List<IType> types;
		CSharpAmbience ambience = new CSharpAmbience ();

		public int StartOffset {
			get {
				return startOffset;
			}
		}		
		
		public TemplateParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IEnumerable<IType> types)
		{
			this.startOffset = startOffset;
//			this.ext = ext;
			this.types = new List<IType> (types);
		}
		
		static int TypeComparer (IType left, IType right)
		{
			return left.TypeParameterCount - right.TypeParameterCount;
		}
		
		#region IParameterDataProvider implementation
		
		protected virtual string GetPrefix (IMethod method)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			return ambience.GetString (method.ReturnType, flags) + " ";
		}
		
		public string GetHeading (int overload, string[] parameterMarkup, int currentParameter)
		{
			var result = new StringBuilder ();
			result.Append ("<b>");
			result.Append (ambience.GetString (types [overload], OutputFlags.UseFullName | OutputFlags.IncludeMarkup));
			result.Append ("</b>");
			result.Append ("&lt;");
			int parameterCount = 0;
			foreach (string parameter in parameterMarkup) {
				if (parameterCount > 0)
					result.Append (", ");
				result.Append (parameter);
				parameterCount++;
			}
			result.Append ("&gt;");
			
			return result.ToString ();
		}
		
		public string GetDescription (int overload, int currentParameter)
		{
			return "";
		}
		
		public string GetParameterDescription (int overload, int paramIndex)
		{
			var type = types[overload];
			
			if (paramIndex < 0 || paramIndex >= type.TypeParameterCount)
				return "";
			
			return ambience.GetString (type.GetDefinition ().TypeParameters [paramIndex], OutputFlags.AssemblyBrowserDescription | OutputFlags.HideExtensionsParameter | OutputFlags.IncludeGenerics | OutputFlags.IncludeModifiers | OutputFlags.HighlightName);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload >= Count)
				return -1;
			var type = types[overload];
			return type != null ? type.TypeParameterCount : 0;
		}

		public string GetParameterName (int overload, int paramIndex)
		{
			// unused
			return "T";
		}


		public bool AllowParameterList (int overload)
		{
			return false;
		}

		public int Count {
			get {
				return types != null ? types.Count : 0;
			}
		}
		#endregion 
	}
}

