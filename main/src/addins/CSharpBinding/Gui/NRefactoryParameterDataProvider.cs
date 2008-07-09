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
		MethodResolveResult resolveResult;
		
		public NRefactoryParameterDataProvider (TextEditor editor, ProjectDom dom, MethodResolveResult resolveResult)
		{
			this.editor = editor;
			this.resolveResult = resolveResult;
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
		
		public string GetMethodMarkup (int overload, string[] parameterMarkup)
		{
			return AmbienceService.Default.GetIntellisenseDescription (resolveResult.Methods[overload]);
		}
		
		public string GetParameterMarkup (int overload, int paramIndex)
		{
			return AmbienceService.Default.GetIntellisenseDescription (resolveResult.Methods[overload].Parameters [paramIndex]);
		}
		
		public int GetParameterCount (int overload)
		{
			if (overload < 0 || overload >= OverloadCount)
				return 0;
			return resolveResult.Methods[0].Parameters != null ? resolveResult.Methods[0].Parameters.Count : 0;
		}
		
		public int OverloadCount {
			get {
				return resolveResult.Methods.Count;
			}
		}
		#endregion 
	}
}
