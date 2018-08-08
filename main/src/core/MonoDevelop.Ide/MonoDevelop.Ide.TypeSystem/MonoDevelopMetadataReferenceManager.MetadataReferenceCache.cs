//
// MetadataReferenceCache.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class MonoDevelopMetadataReferenceManager
	{
		internal class MetadataReferenceCache
		{
			readonly Dictionary<string, MonoDevelopMetadataReference> cacheAssemblyProperties = new Dictionary<string, MonoDevelopMetadataReference> ();
			readonly Dictionary<string, Dictionary<MetadataReferenceProperties, MonoDevelopMetadataReference>> cacheWithCustomProperties = new Dictionary<string, Dictionary<MetadataReferenceProperties, MonoDevelopMetadataReference>> ();
			public MonoDevelopMetadataReference GetOrCreate (MonoDevelopMetadataReferenceManager provider, string path, MetadataReferenceProperties properties)
			{
				if (properties == MetadataReferenceProperties.Assembly) {
					// fast path for no custom properties
					return GetOrCreate (cacheAssemblyProperties, path, provider, path, properties);
				}

				Dictionary<MetadataReferenceProperties, MonoDevelopMetadataReference> dict;
				lock (cacheWithCustomProperties) {
					if (!cacheWithCustomProperties.TryGetValue (path, out dict)) {
						cacheWithCustomProperties [path] = dict = new Dictionary<MetadataReferenceProperties, MonoDevelopMetadataReference> ();
					}
				}
				return GetOrCreate (dict, properties, provider, path, properties);
			}

			static MonoDevelopMetadataReference GetOrCreate<TKey> (Dictionary<TKey, MonoDevelopMetadataReference> cache, TKey key, MonoDevelopMetadataReferenceManager provider, string path, MetadataReferenceProperties properties)
			{
				lock (cache) {
					if (!cache.TryGetValue (key, out var result)) {
						cache [key] = result = new MonoDevelopMetadataReference (provider, path, properties);
					}
					return result;
				}
			}

			public void ClearCache ()
			{
				ClearCache (cacheAssemblyProperties);

				lock (cacheWithCustomProperties) {
					foreach (var kvp in cacheWithCustomProperties) {
						var dict = kvp.Value;
						ClearCache (dict);
					}
					cacheWithCustomProperties.Clear ();
				}
			}

			static void ClearCache<TKey> (Dictionary<TKey, MonoDevelopMetadataReference> dict)
			{
				lock (dict) {
					foreach (var kvp in dict) {
						var reference = kvp.Value;
						reference.Dispose ();
					}
					dict.Clear ();
				}
			}


		}
	}
}
