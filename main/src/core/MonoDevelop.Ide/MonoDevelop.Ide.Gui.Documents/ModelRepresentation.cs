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
		int changeEventFreeze;
		bool changeEventRaised;
		bool hasUnsavedChangesRaised;
		bool hasUnsavedChanges;

		internal int CurrentVersion { get; set; }

		internal DocumentModel.DocumentModelData DocumentModelData { get; set; }

		internal protected SemaphoreSlim WaitHandle { get; } = new SemaphoreSlim (1, 1);

		public object Id => DocumentModelData.Id;

		public bool IsLoaded { get; internal set; }

		/// <summary>
		/// Returs true if the data has been modified and the changes are not yet saved
		/// </summary>
		public bool HasUnsavedChanges {
			get { return hasUnsavedChanges; }
			set {
				if (hasUnsavedChanges != value) {
					hasUnsavedChanges = value;
					NotifyHasUnsavedChanges ();
				}
			}
		}

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
					OnCreateNew ();
				IsLoaded = true;
			} finally {
				WaitHandle.Release ();
			}
		}

		internal void CreateNew ()
		{
			FreezeChangeEvent ();
			try {
				OnCreateNew ();
				HasUnsavedChanges = true;
			} finally {
				ThawChangeEvent (false);
			}
			IsLoaded = true;
		}

		public async Task Reload ()
		{
			try {
				await WaitHandle.WaitAsync ();
				await OnLoad ();
				IsLoaded = true;
				HasUnsavedChanges = false;
			} finally {
				WaitHandle.Release ();
			}
		}

		public async Task Save ()
		{
			try {
				await WaitHandle.WaitAsync ();
				await OnSave ();
				HasUnsavedChanges = false;
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
			if (changeEventFreeze == 0)
				DocumentModelData?.NotifyChanged (this);
			else
				changeEventRaised = true;
		}

		void NotifyHasUnsavedChanges ()
		{
			if (changeEventFreeze == 0)
				DocumentModelData?.NotifyHasUnsavedChanges (this);
			else
				hasUnsavedChangesRaised = true;
		}

		protected abstract void OnCreateNew ();

		protected abstract Task OnLoad ();

		protected abstract Task OnSave ();

		internal async Task InternalCopyFrom (ModelRepresentation other)
		{
			try {
				FreezeChangeEvent ();

				// Capture the copied version now. If the copied object changes during the copy, the version
				// will increase, so a subsequent synchronize call will bring the additional changes.
				CurrentVersion = other.CurrentVersion;
				IsLoaded = true;

				await OnCopyFrom (other);

				HasUnsavedChanges = other.HasUnsavedChanges;
			} finally {
				ThawChangeEvent (false);
			}
		}

		protected abstract Task OnCopyFrom (ModelRepresentation other);

		internal protected virtual Task OnDispose ()
		{
			return Task.CompletedTask;
		}

		protected void FreezeChangeEvent ()
		{
			changeEventFreeze++;
		}

		protected void ThawChangeEvent (bool notifyPendingEvents = true)
		{
			if (--changeEventFreeze == 0) {
				var changeEventRaisedTemp = changeEventRaised;
				var hasUnsavedChangesRaisedTemp = hasUnsavedChangesRaised;
				changeEventRaised = false;
				hasUnsavedChangesRaised = false;
				if (notifyPendingEvents) {
					if (changeEventRaisedTemp)
						NotifyChanged ();
					if (hasUnsavedChangesRaisedTemp)
						NotifyHasUnsavedChanges ();
				}
			}
		}
	}
}
