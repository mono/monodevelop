//
// ExtensionChain.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public class ExtensionChain
	{
		ChainedExtension first;
		Dictionary<Type,ChainedExtension> chains = new Dictionary<Type, ChainedExtension> ();

		public static ExtensionChain Create<T> (T[] extensions) where T:ChainedExtension
		{
			var c = new ExtensionChain ();

			for (int n = extensions.Length - 2; n >= 0; n--)
				extensions [n].InitChain (c, extensions [n + 1]);

			c.first = extensions [0];
			return c;
		}

		public T GetExtension<T> () where T:ChainedExtension, new()
		{
			ChainedExtension e;
			if (!chains.TryGetValue (typeof(T), out e)) {
				e = new T ();
				e.InitChain (this, ChainedExtension.FindNextImplementation<T> (first));
				chains [typeof(T)] = e;
			}
			return (T)e;
		}

		public IEnumerable<ChainedExtension> GetAllExtensions ()
		{
			var e = first;
			while (e != null) {
				yield return e;
				e = e.Next;
			}
		}

		internal void DisposeExtension (ChainedExtension ext)
		{
			var e = first;
			var extensions = new List<ChainedExtension> ();
			while (e != null) {
				if (e != ext)
					extensions.Add (e);
				e = e.Next;
			}
			for (int n = extensions.Count - 2; n >= 0; n--)
				extensions [n].InitChain (this, extensions [n + 1]);
			first = extensions [0];
			foreach (var fex in chains)
				fex.Value.InitChain (this, ChainedExtension.FindNextImplementation (fex.Key, first));
		}

		public void Dispose ()
		{
			first.DisposeChain ();
		}
	}
}

