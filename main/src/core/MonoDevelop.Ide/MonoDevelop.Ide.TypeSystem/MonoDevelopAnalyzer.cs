//
// MonoDevelopAnalyzer.cs
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
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	internal sealed class MonoDevelopAnalyzer : IDisposable
	{
		private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
		private readonly ProjectId _projectId;
		private readonly Microsoft.CodeAnalysis.Workspace _workspace;
		private readonly IAnalyzerAssemblyLoader _loader;
		private readonly string _language;

		// these 2 are mutable states that must be guarded under the _gate.
		private readonly object _gate = new object ();
		private AnalyzerReference _analyzerReference = null;
		private ImmutableArray<DiagnosticData> _analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;

		public MonoDevelopAnalyzer (FilePath fullPath, HostDiagnosticUpdateSource hostDiagnosticUpdateSource, ProjectId projectId, Microsoft.CodeAnalysis.Workspace workspace, IAnalyzerAssemblyLoader loader, string language)
		{
			FullPath = fullPath;
			_hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
			_projectId = projectId;
			_workspace = workspace;
			_loader = loader;
			_language = language;

			FileService.FileChanged += OnUpdatedOnDisk;
		}

		public event EventHandler UpdatedOnDisk;

		public FilePath FullPath { get; }

		public bool HasLoadErrors {
			get { return !_analyzerLoadErrors.IsEmpty; }
		}

		public AnalyzerReference GetReference ()
		{
			lock (_gate) {
				if (_analyzerReference == null) {
					if (File.Exists (FullPath)) {
						// Pass down a custom loader that will ensure we are watching for file changes once we actually load the assembly.
						var assemblyLoaderForFileTracker = new AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched (this);
						_analyzerReference = new AnalyzerFileReference (FullPath, assemblyLoaderForFileTracker);
						((AnalyzerFileReference)_analyzerReference).AnalyzerLoadFailed += OnAnalyzerLoadError;
					} else {
						_analyzerReference = new VisualStudioUnresolvedAnalyzerReference (FullPath, this);
					}
				}

				return _analyzerReference;
			}
		}

		private void OnAnalyzerLoadError (object sender, AnalyzerLoadFailureEventArgs e)
		{
			var data = AnalyzerHelper.CreateAnalyzerLoadFailureDiagnostic (_workspace, _projectId, _language, FullPath, e);

			lock (_gate) {
				_analyzerLoadErrors = _analyzerLoadErrors.Add (data);
			}

			_hostDiagnosticUpdateSource.UpdateDiagnosticsForProject (_projectId, this, _analyzerLoadErrors);
		}

		public void Dispose ()
		{
			Reset ();

			FileWatcherService.WatchDirectories (this, null);
			FileService.FileChanged -= OnUpdatedOnDisk;
		}

		public void Reset ()
		{
			ResetReferenceAndErrors (out var reference, out var loadErrors);

			if (reference is AnalyzerFileReference fileReference) {
				fileReference.AnalyzerLoadFailed -= OnAnalyzerLoadError;

				if (!loadErrors.IsEmpty) {
					_hostDiagnosticUpdateSource.ClearDiagnosticsForProject (_projectId, this);
				}

				_hostDiagnosticUpdateSource.ClearAnalyzerReferenceDiagnostics (fileReference, _language, _projectId);
			}
		}

		private void ResetReferenceAndErrors (out AnalyzerReference reference, out ImmutableArray<DiagnosticData> loadErrors)
		{
			lock (_gate) {
				loadErrors = _analyzerLoadErrors;
				reference = _analyzerReference;

				_analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;
				_analyzerReference = null;
			}
		}

		private void OnUpdatedOnDisk (object sender, EventArgs e)
		{
			UpdatedOnDisk?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// This custom loader just wraps an existing loader, but ensures that we start listening to the file
		/// for changes once we've actually looked at the file.
		/// </summary>
		private class AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched : IAnalyzerAssemblyLoader
		{
			private readonly MonoDevelopAnalyzer _analyzer;

			public AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched (MonoDevelopAnalyzer analyzer)
			{
				_analyzer = analyzer;
			}

			public void AddDependencyLocation (string fullPath)
			{
				_analyzer._loader.AddDependencyLocation (fullPath);
			}

			public Assembly LoadFromPath (string fullPath)
			{
				FileWatcherService.WatchDirectories (_analyzer, new [] { _analyzer.FullPath.ParentDirectory });
				return _analyzer._loader.LoadFromPath (fullPath);
			}
		}

		/// <summary>
		/// This custom <see cref="AnalyzerReference"/>, just wraps an existing <see cref="UnresolvedAnalyzerReference"/>,
		/// but ensure that we start listening to the file for changes once we've actually observed it, so that if the
		/// file then gets created on disk, we are notified.
		/// </summary>
		private class VisualStudioUnresolvedAnalyzerReference : AnalyzerReference
		{
			private readonly UnresolvedAnalyzerReference _underlying;
			private readonly MonoDevelopAnalyzer _visualStudioAnalyzer;

			public VisualStudioUnresolvedAnalyzerReference (string fullPath, MonoDevelopAnalyzer visualStudioAnalyzer)
			{
				_underlying = new UnresolvedAnalyzerReference (fullPath);
				_visualStudioAnalyzer = visualStudioAnalyzer;
			}

			public override string FullPath
				=> _underlying.FullPath;

			public override object Id
				=> _underlying.Id;

			public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers (string language)
			{
				FileWatcherService.WatchDirectories (_visualStudioAnalyzer, new [] { _visualStudioAnalyzer.FullPath.ParentDirectory });
				return _underlying.GetAnalyzers (language);
			}

			public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages ()
				=> _underlying.GetAnalyzersForAllLanguages ();
		}
	}
}
