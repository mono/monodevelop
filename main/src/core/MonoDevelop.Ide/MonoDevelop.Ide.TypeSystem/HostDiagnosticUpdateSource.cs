//
// HostDiagnosticUpdateSource.cs
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
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace MonoDevelop.Ide.TypeSystem
{
	// exporting both Abstract and HostDiagnosticUpdateSource is just to make testing easier.
	// use HostDiagnosticUpdateSource when abstract one is not needed for testing purpose

	// FIXME: Not sure we can use this until roslyn supports multi-workspace or we go single workspace
	//[Export (typeof (AbstractHostDiagnosticUpdateSource))]
	//[Export (typeof (HostDiagnosticUpdateSource))]
	internal sealed class HostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource
	{
		private readonly MonoDevelopWorkspace _workspace;
		private readonly object _gate = new object ();
		private readonly Dictionary<ProjectId, HashSet<object>> _diagnosticMap = new Dictionary<ProjectId, HashSet<object>> ();

		public HostDiagnosticUpdateSource (MonoDevelopWorkspace workspace, IDiagnosticUpdateSourceRegistrationService registrationService)
		{
			_workspace = workspace;

			registrationService.Register (this);
		}

		public override Microsoft.CodeAnalysis.Workspace Workspace {
			get {
				return _workspace;
			}
		}

		private void RaiseDiagnosticsCreatedForProject (ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
		{
			var args = DiagnosticsUpdatedArgs.DiagnosticsCreated (
				CreateId (projectId, key),
				_workspace,
				solution: null,
				projectId: projectId,
				documentId: null,
				diagnostics: items.AsImmutableOrEmpty ());

			RaiseDiagnosticsUpdated (args);
		}

		private void RaiseDiagnosticsRemovedForProject (ProjectId projectId, object key)
		{
			var args = DiagnosticsUpdatedArgs.DiagnosticsRemoved (
				CreateId (projectId, key),
				_workspace,
				solution: null,
				projectId: projectId,
				documentId: null);

			RaiseDiagnosticsUpdated (args);
		}

		private object CreateId (ProjectId projectId, object key) => Tuple.Create (this, projectId, key);

		public void UpdateDiagnosticsForProject (ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
		{
			Contract.ThrowIfNull (projectId);
			Contract.ThrowIfNull (key);
			Contract.ThrowIfNull (items);

			lock (_gate) {
				_diagnosticMap.GetOrAdd (projectId, id => new HashSet<object> ()).Add (key);
			}

			RaiseDiagnosticsCreatedForProject (projectId, key, items);
		}

		public void ClearAllDiagnosticsForProject (ProjectId projectId)
		{
			Contract.ThrowIfNull (projectId);

			HashSet<object> projectDiagnosticKeys;
			lock (_gate) {
				if (_diagnosticMap.TryGetValue (projectId, out projectDiagnosticKeys)) {
					_diagnosticMap.Remove (projectId);
				}
			}

			if (projectDiagnosticKeys != null) {
				foreach (var key in projectDiagnosticKeys) {
					RaiseDiagnosticsRemovedForProject (projectId, key);
				}
			}

			ClearAnalyzerDiagnostics (projectId);
		}

		public void ClearDiagnosticsForProject (ProjectId projectId, object key)
		{
			Contract.ThrowIfNull (projectId);
			Contract.ThrowIfNull (key);

			var raiseEvent = false;
			lock (_gate) {
				if (_diagnosticMap.TryGetValue (projectId, out var projectDiagnosticKeys)) {
					raiseEvent = projectDiagnosticKeys.Remove (key);
				}
			}

			if (raiseEvent) {
				RaiseDiagnosticsRemovedForProject (projectId, key);
			}
		}
	}
}
