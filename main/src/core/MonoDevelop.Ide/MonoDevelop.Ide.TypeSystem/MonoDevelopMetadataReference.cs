using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.FSW;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	[DebuggerDisplay ("{GetDebuggerDisplay(),nq}")]
	sealed partial class MonoDevelopMetadataReference : IDisposable
	{
		readonly MonoDevelopMetadataReferenceManager _provider;
		readonly MetadataReferenceProperties _properties;
		readonly FileChangeTracker _fileChangeTracker;

		Snapshot _currentSnapshot;
		public FilePath FilePath { get; }

		public event EventHandler<MetadataReferenceUpdatedEventArgs> SnapshotUpdated;

		public MonoDevelopMetadataReference (
			MonoDevelopMetadataReferenceManager provider,
			string filePath,
			MetadataReferenceProperties properties)
		{
			Contract.Requires (properties.Kind == MetadataImageKind.Assembly);

			FilePath = filePath;
			_provider = provider;
			_properties = properties;
			_fileChangeTracker = new FileChangeTracker (FilePath.ParentDirectory);
			_fileChangeTracker.UpdatedOnDisk += OnUpdatedOnDisk;
		}

		public MetadataReferenceProperties Properties => _properties;

		public PortableExecutableReference CurrentSnapshot {
			get {
				if (_currentSnapshot == null) {
					UpdateSnapshot ();
				}

				return _currentSnapshot;
			}
		}

		void OnUpdatedOnDisk (object sender, FileEventArgs e)
		{
			var args = new MetadataReferenceUpdatedEventArgs (this);
			foreach (var file in e) {
				if (file.FileName == FilePath) {
					SnapshotUpdated?.Invoke (this, args);
					return;
				}
			}
		}

		public void Dispose ()
		{
			_fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;
			_fileChangeTracker.Dispose ();
		}

		internal void UpdateSnapshot () => _currentSnapshot = new Snapshot (_provider, Properties, FilePath, _fileChangeTracker);

		string GetDebuggerDisplay () => Path.GetFileName (FilePath);
	}

	class MetadataReferenceUpdatedEventArgs : EventArgs
	{
		public PortableExecutableReference OldSnapshot { get; }
		public Lazy<PortableExecutableReference> NewSnapshot { get; }

		public MetadataReferenceUpdatedEventArgs (MonoDevelopMetadataReference reference)
		{
			OldSnapshot = reference.CurrentSnapshot;
			NewSnapshot = new Lazy<PortableExecutableReference> (() => {
				reference.UpdateSnapshot ();
				return reference.CurrentSnapshot;
			});
		}
	}
}
