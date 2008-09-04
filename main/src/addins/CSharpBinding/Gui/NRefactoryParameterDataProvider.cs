// NRefactoryParameterDataProvider.cs
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
	public class NRefactoryParameterDataProvider : IParameterDataProvider
	{
		TextEditor editor;
		List<IMethod> methods = new List<IMethod> ();
		CSharpAmbience ambience = new CSharpAmbience ();
		NRefactoryResolver resolver;
		
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, MethodResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolver = resolver;
			methods.AddRange (resolveResult.Methods);
		}
		
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, ThisResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolver = resolver;
			if (resolveResult.CallingType != null) {
				foreach (IMethod method in resolveResult.CallingType.Methods) {
					if (!method.IsConstructor)
						continue;
					methods.Add (method);
				}
			}
		}
		
		public NRefactoryParameterDataProvider (TextEditor editor, NRefactoryResolver resolver, BaseResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolver = resolver;
			if (resolveResult.CallingType != null) {
				IType baseType = resolver.Dom.GetType (resolveResult.CallingType.BaseType);
				
				if (baseType != null) {
					foreach (IMethod method in baseType.Methods) {
						if (!method.IsConstructor)
							continue;
						methods.Add (method);
					}
				}
			}
		}
		
		#region IParameterDataProvider implementation 
		public int GetCurrentParameterIndex (ICodeCompletionContext ctx)
		{
			int cursor = editor.CursorPosition;
			int i = ctx.TriggerOffset;
			
			if (i > cursor)
				return -1;
			if (i == cursor)
				return 0;
			
			CSharpIndentEngine engine = new CSharpIndentEngine ();
			int index = 1;
			do {
				char c = editor.GetCharAt (i - 1);
				
				engine.Push (c);
				
				if (c == ',' && engine.StackDepth == 1)
					index++;
				
				i++;
			} while (i <= cursor && engine.StackDepth > 0);
			
			return engine.StackDepth == 0 ? -1 : index;
		}
		
		void GeneratePango (StringBuilder sb, XmlNode node)
		{
			if (node == null)
				return;
			if (node is XmlText) {
				sb.Append (node.InnerText);
			} else if (node is XmlElement) {
				XmlElement el = node as XmlElement;
				switch (el.Name) {
					case "see":
					case "seealso":
						sb.Append ("<span foreground=\"blue\" underline=\"single\">");
						sb.Append (el.GetAttribute ("cref"));
						sb.Append ("</span> ");
						break;
				}
			}
			foreach (XmlNode child in node.ChildNodes) {
				GeneratePango (sb, child);
			}
		}
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup)
		{
			return ambience.GetIntellisenseDescription (methods[overload]);
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			if (methods[overload].Parameters == null || paramIndex < 0 || paramIndex >= methods[overload].Parameters.Count)
				return "";
			return ambience.GetIntellisenseDescription (methods[overload].Parameters [paramIndex]);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload < 0 || overload >= OverloadCount)
				return 0;
			return methods[0].Parameters != null ? methods[0].Parameters.Count : 0;
		}
		
		public int OverloadCount {
			get {
				return methods.Count;
			}
		}
		#endregion 
	}
}
