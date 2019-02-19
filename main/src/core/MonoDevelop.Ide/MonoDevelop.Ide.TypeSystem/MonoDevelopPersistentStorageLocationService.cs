//
// MonoDevelopPersistentStorageLocationService.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 Microsoft Inc.
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
using System.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SQLite;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.SolutionSize;
using MonoDevelop.Core;
using System.Diagnostics.Contracts;
using System.Diagnostics;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceService (typeof (IPersistentStorageLocationService), ServiceLayer.Host), Shared]
	class MonoDevelopPersistentStorageLocationService : IPersistentStorageLocationService
	{
		private readonly object _gate = new object ();
		private WorkspaceId primaryWorkspace = WorkspaceId.Empty;
		private SolutionId _currentSolutionId = null;
		private string _currentWorkingFolderPath = null;

		public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;

		[ImportingConstructor]
		[Obsolete (MefConstruction.ImportingConstructorMessage, error: true)]
		public MonoDevelopPersistentStorageLocationService ()
		{
		}

		public IDisposable RegisterPrimaryWorkspace (WorkspaceId id)
		{
			if (primaryWorkspace.Equals (WorkspaceId.Empty)) {
				primaryWorkspace = id;
				return new WorkspaceRegistration (this);
			}
			return null;
		}

		class WorkspaceRegistration : IDisposable
		{
			readonly MonoDevelopPersistentStorageLocationService service;
			bool disposed;

			public WorkspaceRegistration (MonoDevelopPersistentStorageLocationService service) => this.service = service;

			public void Dispose ()
			{
				if (!disposed) {
					service.DisconnectCurrentStorage ();
					disposed = true;
				}
			}
		}

		public bool IsSupported (Workspace workspace) => workspace is MonoDevelopWorkspace;

		public string TryGetStorageLocation (SolutionId solutionId)
		{
			lock (_gate) {
				if (solutionId == _currentSolutionId) {
					return _currentWorkingFolderPath;
				}
			}

			return null;
		}

		internal void SetupSolution (MonoDevelopWorkspace visualStudioWorkspace)
		{
			lock (_gate) {
				// Don't trigger events for workspaces other than those we want to inspect.
				if (!primaryWorkspace.Equals (visualStudioWorkspace.Id))
					return;

				if (visualStudioWorkspace.CurrentSolution.Id == _currentSolutionId && _currentWorkingFolderPath != null) {
					return;
				}

				var solution = visualStudioWorkspace.MonoDevelopSolution;
				solution.Modified += OnSolutionModified;
				if (string.IsNullOrWhiteSpace (solution.BaseDirectory))
					return;

				var workingFolderPath = solution.GetPreferencesDirectory ();

				try {
					if (!string.IsNullOrWhiteSpace (workingFolderPath)) {
						OnWorkingFolderChanging_NoLock (
							new PersistentStorageLocationChangingEventArgs (
								visualStudioWorkspace.CurrentSolution.Id,
								workingFolderPath,
								mustUseNewStorageLocationImmediately: false));
					}
				} catch {
					// don't crash just because solution having problem getting working folder information
				}
			}
		}

		async void OnSolutionModified (object sender, MonoDevelop.Projects.WorkspaceItemEventArgs args)
		{
			var sol = (MonoDevelop.Projects.Solution)args.Item;
			var workspace = await TypeSystemService.GetWorkspaceAsync (sol, CancellationToken.None);
			if (workspace.Id.Equals (primaryWorkspace)) {
				DisconnectCurrentStorage ();
			}
		}

		private void OnWorkingFolderChanging_NoLock (PersistentStorageLocationChangingEventArgs eventArgs)
		{
			StorageLocationChanging?.Invoke (this, eventArgs);

			_currentSolutionId = eventArgs.SolutionId;
			_currentWorkingFolderPath = eventArgs.NewStorageLocation;
		}

		void DisconnectCurrentStorage ()
		{
			lock (_gate) {
				var workspace = TypeSystemService.GetWorkspace (primaryWorkspace);
				var solution = workspace.MonoDevelopSolution;
				if (solution != null)
					solution.Modified -= OnSolutionModified;

				// We want to make sure everybody synchronously detaches
				OnWorkingFolderChanging_NoLock (
					new PersistentStorageLocationChangingEventArgs (
						_currentSolutionId,
						newStorageLocation: null,
						mustUseNewStorageLocationImmediately: true));
				primaryWorkspace = WorkspaceId.Empty;
			}
		}
	}
}
