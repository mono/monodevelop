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
		ModelRepresentation lastChangedRepresentation;

		internal DocumentModel.DocumentModelData DocumentModelData { get; set; }

		protected SemaphoreSlim WaitHandle { get } = new SemaphoreSlim (1);

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

		public async Task CopyFrom (ModelRepresentation other)
		{
			if (!other.IsLoaded)
				await other.Load ();
			try {
				await WaitHandle.WaitAsync ();
				await OnCopyFrom (other);
				IsLoaded = true;
			} finally {
				WaitHandle.Release ();
			}
		}

		internal async Task Synchronize ()
		{
			try {
				await WaitHandle.WaitAsync ();
				if (lastChangedRepresentation != null && lastChangedRepresentation != this) {
					await OnCopyFrom (lastChangedRepresentation);
					lastChangedRepresentation = null;
				}
			} finally {
				WaitHandle.Release ();
			}
		}

		public void NotifyChanged ()
		{
		}

		protected abstract Task OnLoad ();

		protected abstract Task OnSave ();

		protected abstract Task OnCopyFrom (ModelRepresentation other);

		internal async Task<ModelRepresentation> Clone ()
		{
			var copy = (ModelRepresentation)Activator.CreateInstance (GetType ());
			await copy.CopyFrom (this);
			return copy;
		}
	}
}
