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
using System.Linq;

namespace MonoDevelop.Projects
{
	public class ExtensionChain
	{
		Dictionary<Type,ChainedExtension> chains = new Dictionary<Type, ChainedExtension> ();
		ChainedExtension[] extensions;
		ChainedExtension defaultInsertBefore;

		public static ExtensionChain Create<T> (T[] extensions) where T:ChainedExtension
		{
			var c = new ExtensionChain ();

			for (int n = extensions.Length - 2; n >= 0; n--)
				extensions [n].InitChain (c, extensions [n + 1]);

			c.extensions = extensions;
			return c;
		}

		public T GetExtension<T> () where T:ChainedExtension, new()
		{
			ChainedExtension e;
			if (!chains.TryGetValue (typeof(T), out e)) {
				e = new T ();
				e.InitChain (this, ChainedExtension.FindNextImplementation<T> (extensions[0]));
				chains [typeof(T)] = e;
			}
			return (T)e;
		}

		internal void SetDefaultInsertionPosition (ChainedExtension insertBefore)
		{
			defaultInsertBefore = insertBefore;
		}

		public IEnumerable<ChainedExtension> GetAllExtensions ()
		{
			return extensions;
		}

		internal void AddExtension (ChainedExtension ext, ChainedExtension insertAfter = null, ChainedExtension insertBefore = null, bool rechain = true)
		{
			int index;
			if (insertBefore != null) {
				index = Array.IndexOf (extensions, insertBefore);
			} else if (insertAfter != null) {
				index = Array.IndexOf (extensions, insertAfter);
				if (index != -1)
					index++;
			} else if (defaultInsertBefore != null) {
				index = Array.IndexOf (extensions, defaultInsertBefore);
			} else
				index = extensions.Length;
			
			Array.Resize (ref extensions, extensions.Length + 1);
			for (int n = extensions.Length - 1; n > index; n--)
				extensions [n] = extensions [n - 1];
			extensions [index] = ext;
			if (rechain)
				Rechain ();
		}

		internal void RemoveExtension (ChainedExtension ext)
		{
			if (extensions == null)
				return;

			extensions = extensions.Where (e => e != ext).ToArray ();
			Rechain ();
		}

		internal void Rechain ()
		{
			// Re-chain every extension
			for (int n = extensions.Length - 2; n >= 0; n--)
				extensions [n].InitChain (this, extensions [n + 1]);
			
			// The first extension object in type-specific chains is a placeholder extension used only to hold
			// a reference to the real first extension.
			foreach (var fex in chains)
				fex.Value.InitChain (this, ChainedExtension.FindNextImplementation (fex.Key, extensions[0]));
		}

		public void Dispose ()
		{
			var first = extensions [0];
			extensions = null;
			first.DisposeChain ();
		}
	}
}

