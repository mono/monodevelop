//
// ChainedExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public class ChainedExtension: IDisposable
	{
		ChainedExtension nextInChain;
		ExtensionChain chain;

		internal static T CreateChain<T> (ChainedExtension next) where T:ChainedExtension, new()
		{
			var first = new T ();
			first.nextInChain = ChainedExtension.FindNextImplementation<T> (next);
			return first;
		}

		internal protected static T FindNextImplementation<T> (ChainedExtension next) where T:ChainedExtension
		{
			return (T)FindNextImplementation (typeof(T), next);
		}

		internal static ChainedExtension FindNextImplementation (Type type, ChainedExtension next)
		{
			if (next == null || (type.IsInstanceOfType (next) && next.GetType () != type))
				return next;
			return FindNextImplementation (type, next.nextInChain);
		}

		internal void InitChain (ExtensionChain chain, ChainedExtension next)
		{
			this.chain = chain;
			nextInChain = next;
			InitializeChain (next);
		}

		internal protected virtual void InitializeChain (ChainedExtension next)
		{
		}

		internal ChainedExtension Next {
			get { return nextInChain; }
		}

		internal void DisposeChain ()
		{
			Dispose ();
			if (nextInChain != null)
				nextInChain.DisposeChain ();
		}

		public virtual void Dispose ()
		{
			if (chain != null)
				chain.RemoveExtension (this);
		}
	}
}

