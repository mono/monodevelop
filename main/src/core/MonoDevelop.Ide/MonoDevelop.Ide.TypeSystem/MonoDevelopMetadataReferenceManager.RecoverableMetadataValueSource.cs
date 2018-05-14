//
// MetadataReferenceCache.RecoverableMetadataValueSource.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class MonoDevelopMetadataReferenceManager
	{
		class RecoverableMetadataValueSource : ValueSource<AssemblyMetadata>
		{
			readonly WeakReference<AssemblyMetadata> _weakValue;
			readonly List<ITemporaryStreamStorage> _storages;
			readonly ConditionalWeakTable<Metadata, object> _lifetimeMap;

			public RecoverableMetadataValueSource (AssemblyMetadata value, List<ITemporaryStreamStorage> storages, ConditionalWeakTable<Metadata, object> lifetimeMap)
			{
				Contract.ThrowIfFalse (storages.Count > 0);

				_weakValue = new WeakReference<AssemblyMetadata> (value);
				_storages = storages;
				_lifetimeMap = lifetimeMap;
			}

			public IEnumerable<ITemporaryStreamStorage> GetStorages ()
			{
				return _storages;
			}

			public override AssemblyMetadata GetValue (CancellationToken cancellationToken)
			{
				if (_weakValue.TryGetTarget (out var value)) {
					return value;
				}

				return RecoverMetadata ();
			}

			AssemblyMetadata RecoverMetadata ()
			{
				var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance (_storages.Count);

				foreach (var storage in _storages) {
					moduleBuilder.Add (GetModuleMetadata (storage));
				}

				var metadata = AssemblyMetadata.Create (moduleBuilder.ToImmutableAndFree ());
				_weakValue.SetTarget (metadata);

				return metadata;
			}

			ModuleMetadata GetModuleMetadata (ITemporaryStreamStorage storage)
			{
				var stream = storage.ReadStream (CancellationToken.None);

				// under VS host, direct access should be supported
				var directAccess = (ISupportDirectMemoryAccess)stream;
				var pImage = directAccess.GetPointer ();

				var metadata = ModuleMetadata.CreateFromMetadata (pImage, (int)stream.Length);

				// memory management.
				_lifetimeMap.Add (metadata, stream);
				return metadata;
			}

			public override bool TryGetValue (out AssemblyMetadata value)
			{
				return _weakValue.TryGetTarget (out value);
			}

			public override Task<AssemblyMetadata> GetValueAsync (CancellationToken cancellationToken)
			{
				return Task.FromResult (GetValue (cancellationToken));
			}
		}
	}
}
