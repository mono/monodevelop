using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.CodeAnalysis;
using MonoDevelop.FSW;

namespace MonoDevelop.Ide.TypeSystem
{
	[DebuggerDisplay ("{GetDebuggerDisplay(),nq}")]
	sealed partial class MonoDevelopMetadataReference : IDisposable
	{
		readonly MonoDevelopMetadataReferenceManager _provider;
		readonly MetadataReferenceProperties _properties;
		readonly FileChangeTracker _fileChangeTracker;

		Snapshot _currentSnapshot;

		public event EventHandler UpdatedOnDisk;

		public MonoDevelopMetadataReference (
			MonoDevelopMetadataReferenceManager provider,
			string filePath,
			MetadataReferenceProperties properties)
		{
			Contract.Requires (properties.Kind == MetadataImageKind.Assembly);

			_provider = provider;
			_properties = properties;

			// We don't track changes to netmodules linked to the assembly.
			// Any legitimate change in a linked module will cause the assembly to change as well.
			_fileChangeTracker = new FileChangeTracker (filePath);
			_fileChangeTracker.UpdatedOnDisk += OnUpdatedOnDisk;
		}

		public string FilePath => _fileChangeTracker.FilePath;

		public MetadataReferenceProperties Properties => _properties;

		public PortableExecutableReference CurrentSnapshot {
			get {
				if (_currentSnapshot == null) {
					UpdateSnapshot ();
				}

				return _currentSnapshot;
			}
		}

		void OnUpdatedOnDisk (object sender, EventArgs e) => UpdatedOnDisk?.Invoke (this, EventArgs.Empty);

		public void Dispose ()
		{
			_fileChangeTracker.Dispose ();
			_fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;
		}

		public void UpdateSnapshot ()
		{
			_currentSnapshot = new Snapshot (_provider, Properties, FilePath);
		}

		string GetDebuggerDisplay () => Path.GetFileName (FilePath);
	}
}
