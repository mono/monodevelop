using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.CodeAnalysis;

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

		public string FilePath {
			get { return _fileChangeTracker.FilePath; }
		}

		public MetadataReferenceProperties Properties {
			get { return _properties; }
		}

		public PortableExecutableReference CurrentSnapshot {
			get {
				if (_currentSnapshot == null) {
					UpdateSnapshot ();
				}

				return _currentSnapshot;
			}
		}

		void OnUpdatedOnDisk(object sender, EventArgs e)
		{
			UpdatedOnDisk?.Invoke (this, EventArgs.Empty);
		}

		public void Dispose ()
		{
			_fileChangeTracker.Dispose ();
			_fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;
		}

		public void UpdateSnapshot ()
		{
			_currentSnapshot = new Snapshot (_provider, Properties, FilePath);
		}

		string GetDebuggerDisplay ()
		{
			return Path.GetFileName (FilePath);
		}

		/// <summary>
		/// Helper class for working with FileSystemWatcher to observe any change on single file.
		/// </summary>
		internal class FileChangeTracker : IDisposable
		{
			public event EventHandler UpdatedOnDisk;

			FileSystemWatcher watcher;

			public string FilePath { get; }

			public FileChangeTracker (string filePath)
			{
				FilePath = filePath;
				watcher = new FileSystemWatcher (Path.GetDirectoryName (filePath), Path.GetFileName (filePath)) {
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
				};

				watcher.Changed += OnChanged;
				watcher.Created += OnChanged;
				watcher.Deleted += OnChanged;
				watcher.Renamed += OnRenamed;
				watcher.EnableRaisingEvents = true;

				// Currently fails on mono due to https://github.com/mono/mono/issues/8712
				Debug.Assert (watcher.EnableRaisingEvents);
			}


			void OnRenamed (object sender, RenamedEventArgs e)
			{
				UpdatedOnDisk?.Invoke (this, e);
			}

			void OnChanged (object sender, FileSystemEventArgs e)
			{
				UpdatedOnDisk?.Invoke (this, e);
			}

			public void Dispose ()
			{
				if (watcher != null) {
					watcher.Changed -= OnChanged;
					watcher.Created -= OnChanged;
					watcher.Deleted -= OnChanged;
					watcher.Renamed -= OnRenamed;
					watcher.Dispose ();
					watcher = null;
				}
			}
		}
	}
}
