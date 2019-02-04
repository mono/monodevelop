//
// DocumentModel.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.Gui.Documents
{
	public abstract class ModelRepresentation
	{
		bool doingInternalChange;

		internal int CurrentVersion { get; set; }

		internal DocumentModel.DocumentModelData DocumentModelData { get; set; }

		internal protected SemaphoreSlim WaitHandle { get; } = new SemaphoreSlim (1);

		public object Id { get; set; }

		public bool IsLoaded { get; internal set; }

		public void SetLoaded ()
		{
			IsLoaded = true;
		}

		public async Task Load ()
		{
			try {
				await WaitHandle.WaitAsync ();
				if (IsLoaded)
					return;
				if (DocumentModelData.IsLinked)
					await OnLoad ();
				else
					OnLoadNew ();
				IsLoaded = true;
			} finally {
				WaitHandle.Release ();
			}
		}

		public async Task Reload ()
		{
			try {
				await WaitHandle.WaitAsync ();
				await OnLoad ();
				IsLoaded = true;
			} finally {
				WaitHandle.Release ();
			}
		}

		public async Task Save ()
		{
			try {
				await WaitHandle.WaitAsync ();
				await OnSave ();
			} finally {
				WaitHandle.Release ();
			}
		}

		internal async Task Synchronize ()
		{
			await DocumentModelData?.Synchronize (this);
		}

		public void NotifyChanged ()
		{
			if (!doingInternalChange)
				DocumentModelData?.NotifyChanged (this);
		}

		protected abstract void OnLoadNew ();

		protected abstract Task OnLoad ();

		protected abstract Task OnSave ();

		internal async Task InternalCopyFrom (ModelRepresentation other)
		{
			try {
				doingInternalChange = true;

				// Capture the copied version now. If the copied object changes during the copy, the version
				// will increase, so a subsequent synchronize call will bring the additional changes.
				CurrentVersion = other.CurrentVersion;
				IsLoaded = true;

				await OnCopyFrom (other);
			} finally {
				doingInternalChange = false;
			}
		}

		protected abstract Task OnCopyFrom (ModelRepresentation other);

		internal protected virtual Task OnDispose ()
		{
			return Task.CompletedTask;
		}
	}
}
