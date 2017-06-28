//
// ParameterDataProvider.cs
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
using ICSharpCode.NRefactory.Completion;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.CodeCompletion
{
	public abstract class ParameterHintingData
	{
		public abstract int ParameterCount {
			get;
		}

		public abstract bool IsParameterListAllowed {
			get;
		}

		public ParameterHintingData ()
		{

		}

		[Obsolete("Obsolete use parameterless constructor.")]
		public ParameterHintingData (ISymbol symbol)
		{

		}

		public abstract string GetParameterName (int parameter);

		public virtual Task<TooltipInformation> CreateTooltipInformation (TextEditor editor, DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken cancelToken)
		{
			return Task.FromResult (new TooltipInformation ());
		}
	}
}
