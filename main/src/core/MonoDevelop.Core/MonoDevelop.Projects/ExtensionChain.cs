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
		Dictionary<Type,ChainedExtensionSentinel> chains = new Dictionary<Type, ChainedExtensionSentinel> ();
		// Maybe an array is not the best solution here, given chains grow and decrease, a list might be better.
		ChainedExtension [] extensions;
		ChainedExtension defaultInsertBefore;
		BatchModifier batchModifier;

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
			if (!chains.TryGetValue (typeof(T), out ChainedExtensionSentinel e)) {
				e = new ChainedExtensionSentinel (new T());
				e.Update (this, typeof (T), -1);
				chains [typeof(T)] = e;
			}
			return (T)e.Extension;
		}

		internal void SetDefaultInsertionPosition (ChainedExtension insertBefore)
		{
			defaultInsertBefore = insertBefore;
		}

		public IEnumerable<ChainedExtension> GetAllExtensions ()
		{
			return extensions;
		}

		internal IDisposable BatchModify () => batchModifier = new BatchModifier (this);

		internal void AddExtension (ChainedExtension ext, ChainedExtension insertAfter = null, ChainedExtension insertBefore = null)
		{
			int index = -1;
			if (insertBefore != null) {
				index = Array.IndexOf (extensions, insertBefore);
			} else if (insertAfter != null) {
				index = Array.IndexOf (extensions, insertAfter);
				if (index != -1)
					index++;
			} else if (defaultInsertBefore != null) {
				index = Array.IndexOf (extensions, defaultInsertBefore);
			}

			if (index == -1) {
				index = extensions.Length;
			}

			Array.Resize (ref extensions, extensions.Length + 1);
			for (int n = extensions.Length - 1; n > index; n--)
				extensions [n] = extensions [n - 1];
			extensions [index] = ext;

			Rechain (index);
		}

		internal void RemoveExtension (ChainedExtension ext)
		{
			if (extensions == null)
				return;

			int index = extensions.Length;
			extensions = extensions.Where ((e, eindex) => {
				bool shouldRemove = e == ext;
				if (shouldRemove)
					index = eindex;
				return !shouldRemove;
			}).ToArray ();

			Rechain (index);
		}

		void Rechain (int firstChangeIndex)
		{
			// If we are in a batch update, only update where to start rechaining.
			if (batchModifier != null) {
				batchModifier.UpdateFirstIndex (firstChangeIndex);
				return;
			}

			// Re-chain every extension
			for (int n = extensions.Length - 2; n >= 0; n--)
				extensions [n].InitChain (this, extensions [n + 1]);

			// The first extension object in type-specific chains is a placeholder extension used only to hold
			// a reference to the real first extension.
			foreach (var kvp in chains) {
				ChainedExtensionSentinel fex = kvp.Value;
				fex.Update (this, kvp.Key, firstChangeIndex);
			}
		}

		public void Dispose ()
		{
			var first = extensions [0];
			extensions = null;
			first.DisposeChain ();

			foreach (var kvp in chains) {
				// Dispose the placeholder extension just in case the extension itself registers something
				// in InitializeChain that it cleans up in Dispose.
				var extension = kvp.Value;
				extension.Dispose ();
			}
		}

		class ChainedExtensionSentinel
		{
			public ChainedExtension Extension { get; }
			int extensionIndex = -1;

			public ChainedExtensionSentinel (ChainedExtension extension)
			{
				Extension = extension;
			}

			public void Update (ExtensionChain chain, Type type, int firstChainChangeIndex)
			{
				// We only want to update an extension if we insert somewhere before the extension we found.
				if (extensionIndex < firstChainChangeIndex)
					return;

				// Maybe it would be useful to skip extensions until min(indices), as they've already been scanned
				// in a previous check
				var impl = ChainedExtension.FindNextImplementation (type, chain.extensions[0], out extensionIndex);
				Extension.InitChain (chain, impl);
			}

			public void Dispose () => Extension.Dispose ();
		}

		class BatchModifier : IDisposable
		{
			readonly ExtensionChain chain;
			int minChangedIndex;

			public BatchModifier (ExtensionChain chain)
			{
				this.chain = chain;
				minChangedIndex = chain.extensions.Length;
			}

			public void UpdateFirstIndex (int firstChainChangeIndex)
			{
				// If we added a node at firstChainChangeIndex then removed that one
				// it might help not to rechain in that case and reset the index.
				// Maybe we can keep track of that and handle it.
				// Regardless, the code is simpler this way and it should not be a bottleneck
				minChangedIndex = Math.Min (firstChainChangeIndex, minChangedIndex);
			}

			public void Dispose ()
			{
				chain.batchModifier = null;
				chain.Rechain (minChangedIndex);
			}
		}
	}
}

