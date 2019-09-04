//
// TypeSystemService_WorkspaceRequestRegistration.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class TypeSystemService
	{
		internal sealed class WorkspaceRequestRegistration : IDisposable
		{
			readonly List<TaskCompletionSource<MonoDevelopWorkspace>> requests = new List<TaskCompletionSource<MonoDevelopWorkspace>> ();
			CancellationTokenSource src = new CancellationTokenSource ();

			internal async Task<MonoDevelopWorkspace> GetWorkspaceAsync (CancellationToken token)
			{
				var tcs = new TaskCompletionSource<MonoDevelopWorkspace> ();
				lock (requests) {
					requests.Add (tcs);
				}

				try {
					using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource (src.Token, token);
					using (linkedTokenSource.Token.Register (() => tcs.TrySetCanceled (linkedTokenSource.Token))) {
						var workspace = await tcs.Task;
						return workspace;
					}
				} finally {
					lock (requests) {
						requests.Remove (tcs);
					}
				}
			}

			internal void Complete (MonoDevelopWorkspace workspace)
			{
				if (workspace == null)
					throw new ArgumentNullException (nameof (workspace));

				lock (requests) {
					foreach (var request in requests.ToList ()) {
						// Requests are removed when completed.
						request.TrySetResult (workspace);
					}
				}
			}

			public void Dispose ()
			{
				// Requests are removed when canceled.
				src.Cancel ();
				src.Dispose ();
				src = new CancellationTokenSource ();
			}
		}
	}
}
