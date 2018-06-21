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

		Snapshot _currentSnapshot;
		public FilePath FilePath { get; }

		public event EventHandler UpdatedOnDisk;

		public MonoDevelopMetadataReference (
			MonoDevelopMetadataReferenceManager provider,
			string filePath,
			MetadataReferenceProperties properties)
		{
			Contract.Requires (properties.Kind == MetadataImageKind.Assembly);

			FilePath = filePath;
			_provider = provider;
			_properties = properties;

			FileWatcherService.WatchDirectories (this, new [] { FilePath.ParentDirectory });
			FileService.FileChanged += OnUpdatedOnDisk;
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
			foreach (var file in e) {
				if (file.FileName == FilePath) {
					UpdatedOnDisk?.Invoke (this, EventArgs.Empty);
					return;
				}
			}
		}

		public void Dispose ()
		{
			FileService.FileChanged -= OnUpdatedOnDisk;
			FileWatcherService.WatchDirectories (this, null);
		}

		public void UpdateSnapshot () => _currentSnapshot = new Snapshot (_provider, Properties, FilePath);

		string GetDebuggerDisplay () => Path.GetFileName (FilePath);
	}
}
