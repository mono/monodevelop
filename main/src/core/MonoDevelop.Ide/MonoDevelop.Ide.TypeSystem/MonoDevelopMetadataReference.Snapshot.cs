//
// MetadataReferenceCache.Snapshot.cs
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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class MonoDevelopMetadataReference
	{
		/// <summary>
		/// Represents a metadata reference corresponding to a specific version of a file.
		/// If a file changes in future this reference will still refer to the original version.
		/// </summary>
		/// <remarks>
		/// The compiler observes the metadata content a reference refers to by calling <see cref="PortableExecutableReference.GetMetadataImpl()"/>
		/// and the observed metadata is memoized by the compilation. However we drop compilations to decrease memory consumption. 
		/// When the compilation is recreated for a solution the compiler asks for metadata again and we need to provide the original content,
		/// not read the file again. Therefore we need to save the timestamp on the <see cref="Snapshot"/>.
		/// 
		/// When the VS observes a change in a metadata reference file the project version is advanced and a new instance of 
		/// <see cref="Snapshot"/> is created for the corresponding reference.
		/// </remarks>
		[DebuggerDisplay ("{GetDebuggerDisplay(),nq}")]
		internal sealed class Snapshot : PortableExecutableReference, ISupportTemporaryStorage
		{
			readonly MonoDevelopMetadataReferenceManager _provider;
			readonly Lazy<DateTime> _timestamp;
			Exception _error;

			internal Snapshot (MonoDevelopMetadataReferenceManager provider, MetadataReferenceProperties properties, string fullPath)
				: base (properties, fullPath)
			{
				Contract.Requires (Properties.Kind == MetadataImageKind.Assembly);
				_provider = provider;

				_timestamp = new Lazy<DateTime> (() => {
					try {
						return Roslyn.Utilities.FileUtilities.GetFileTimeStamp (this.FilePath);
					} catch (IOException e) {
						// Reading timestamp of a file might fail. 
						// Let's remember the failure and report it to the compiler when it asks for metadata.
						// We could let the Lazy hold onto this (since it knows how to rethrow exceptions), but
						// our support of GetStorages needs to gracefully handle the case where we have no timestamp.
						// If Lazy had a "IsValueFaulted" we could be cleaner here.
						_error = e;
						return DateTime.MinValue;
					}
				}, LazyThreadSafetyMode.PublicationOnly);
			}

			protected override Metadata GetMetadataImpl ()
			{
				// Fetch the timestamp first, so as to populate _error if needed
				var timestamp = _timestamp.Value;

				if (_error != null) {
					throw _error;
				}

				try {
					return _provider.GetMetadata (FilePath, timestamp);
				} catch (Exception e) when (SaveMetadataReadingException (e)) {
					// unreachable
					throw new InvalidOperationException ("Should be unreachable");
				}
			}

			bool SaveMetadataReadingException (Exception e)
			{
				// Save metadata reading failure so that future compilations created 
				// with this reference snapshot fail consistently in the same way.
				if (e is IOException || e is BadImageFormatException) {
					_error = e;
				}

				return false;
			}

			protected override DocumentationProvider CreateDocumentationProvider ()
			{
				DocumentationProvider provider = null;
				try {
					string xmlName = Path.ChangeExtension (FilePath, ".xml");
					if (File.Exists (xmlName)) {
						provider = XmlDocumentationProvider.CreateFromFile (xmlName);
					} else {
						provider = RoslynDocumentationProvider.Instance;
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while creating xml documentation provider for: " + FilePath, e);
				}
				return provider;
			}

			protected override PortableExecutableReference WithPropertiesImpl (MetadataReferenceProperties properties)
			{
				return new Snapshot (_provider, properties, FilePath);
			}

			string GetDebuggerDisplay ()
			{
				return "Metadata File: " + FilePath;
			}

			IEnumerable<ITemporaryStreamStorage> ISupportTemporaryStorage.GetStorages ()
			{
				return _provider.GetStorages (FilePath, _timestamp.Value);
			}

			class RoslynDocumentationProvider : DocumentationProvider
			{
				internal static readonly DocumentationProvider Instance = new RoslynDocumentationProvider ();

				RoslynDocumentationProvider ()
				{
				}

				public override bool Equals (object obj)
				{
					return ReferenceEquals (this, obj);
				}

				public override int GetHashCode ()
				{
					return 42; // singleton
				}

				protected override string GetDocumentationForSymbol (string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default (CancellationToken))
				{
					return MonoDocDocumentationProvider.GetDocumentation (documentationMemberID);
				}
			}
		}
	}
}
