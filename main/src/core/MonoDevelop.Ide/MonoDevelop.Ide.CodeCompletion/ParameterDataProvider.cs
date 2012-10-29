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

namespace MonoDevelop.Ide.CodeCompletion
{
	public abstract class ParameterDataProvider : IParameterDataProvider
	{
		public ParameterDataProvider (int startOffset)
		{
			this.startOffset = startOffset;
		}

		public virtual TooltipInformation CreateTooltipInformation (int overload, int currentParameter, bool smartWrap)
		{
			return new TooltipInformation ();
		}

		#region IParameterDataProvider implementation

		string IParameterDataProvider.GetHeading (int overload, string[] parameterDescription, int currentParameter)
		{
			throw new NotImplementedException ();
		}

		string IParameterDataProvider.GetDescription (int overload, int currentParameter)
		{
			throw new NotImplementedException ();
		}

		string IParameterDataProvider.GetParameterDescription (int overload, int paramIndex)
		{
			throw new NotImplementedException ();
		}

		public abstract int GetParameterCount (int overload);
		public abstract bool AllowParameterList (int overload);
		public abstract string GetParameterName (int overload, int paramIndex);

		public abstract int Count {
			get;
		}

		readonly int startOffset;
		public int StartOffset {
			get {
				return startOffset;
			}
		}

		#endregion
	}
}

