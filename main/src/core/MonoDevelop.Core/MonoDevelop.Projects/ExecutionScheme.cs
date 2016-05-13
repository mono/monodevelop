//
// ExecutionScheme.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Projects
{
	public class ExecutionScheme
	{
		public ExecutionScheme (string name)
		{
			Name = name;
		}

		public SolutionItem ParentItem {
			get;
			internal set;
		}

		public string Name { get; private set; }

		/// <summary>
		/// Copies the data of an execution scheme into this execution scheme
		/// </summary>
		/// <param name="scheme">Scheme from which to get the data.</param>
		/// <param name="isRename">If true, it means that the copy is being made as a result of a rename or clone operation. In this case,
		/// the overriden method may change the value of some properties that depend on the scheme name.</param>
		public void CopyFrom (ExecutionScheme scheme, bool isRename = false)
		{
			OnCopyFrom (scheme, isRename);
		}

		protected virtual void OnCopyFrom (ExecutionScheme scheme, bool isRename)
		{
		}

		public override string ToString ()
		{
			return Name;
		}
	}
}

