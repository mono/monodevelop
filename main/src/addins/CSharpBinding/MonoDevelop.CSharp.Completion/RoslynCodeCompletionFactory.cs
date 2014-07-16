// 
// CSharpCompletionTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCodeCompletionFactory : ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory
	{
		readonly CSharpCompletionTextEditorExtension ext;

		public RoslynCodeCompletionFactory (CSharpCompletionTextEditorExtension ext)
		{
			if (ext == null)
				throw new ArgumentNullException ("ext");
			this.ext = ext;
		}

		#region ICompletionDataFactory implementation

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateGenericData (string data, ICSharpCode.NRefactory6.CSharp.Completion.GenericDataType genericDataType)
		{
			return new RoslynCompletionData {
				CompletionText = data,
				DisplayText = data,
				Icon = "md-keyword"
			};
		}
		
		ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateEnumMemberCompletionData (Microsoft.CodeAnalysis.IFieldSymbol field)
		{
			return new RoslynSymbolCompletionData (ext, field, field.Type.Name + "." + field.Name);
		}
		
		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateFormatItemCompletionData (string format, string description, object example)
		{
			throw new NotImplementedException ();
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateXmlDocCompletionData (string tag, string description, string tagInsertionText)
		{
			throw new NotImplementedException ();
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateSymbolCompletionData (Microsoft.CodeAnalysis.ISymbol symbol)
		{
			return new RoslynSymbolCompletionData (ext, symbol);
		}
		
		ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateSymbolCompletionData (Microsoft.CodeAnalysis.ISymbol symbol, string text)
		{
			return new RoslynSymbolCompletionData (ext, symbol, text);
		}
		#endregion
	}
}
