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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using MonoDevelop.Core;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A model is the data being shown in a view.
	/// </summary>
	public abstract class DocumentModel : IDisposable
	{
		bool disposed;
		DocumentModelData data;
		ModelRepresentation modelRepresentation;
		int notifiedChangeVersion;

		internal DocumentModelData Data {
			get {
				if (data == null) {
					data = new DocumentModelData ();
					data.LinkNewNoLock (this);
				}
				return data;
			}
			set {
				data = value;
			}
		}

		protected ModelRepresentation ModelRepresentation {
			get {
				if (modelRepresentation == null) {
					if (Registry != null)
						throw new InvalidOperationException ("The model is not yet loaded");

					// Not shared, so this should always run sync
					modelRepresentation = GetRepresentationAsync ().Result;
				}
				return modelRepresentation;
			}
		}

		/// <summary>
		/// Identifier of the model
		/// </summary>
		public object Id => data?.Id;

		/// <summary>
		/// Returns true if this model is currently being shared with another view
		/// </summary>
		/// <value><c>true</c> if is shared; otherwise, <c>false</c>.</value>
		public bool IsShared => Data.IsShared;

		/// <summary>
		/// Raised when the model is modified
		/// </summary>
		public event EventHandler Changed;

		/// <summary>
		/// Saves the data to disk
		/// </summary>
		/// <returns>The save.</returns>
		public async Task Save ()
		{
			await ModelRepresentation.Save ();
		}

		public async Task Reload ()
		{
			var rep = await GetRepresentationAsync ();
			await rep.Reload ();
		}

		public async Task Load ()
		{
			modelRepresentation = await GetRepresentationAsync ();
			await modelRepresentation.Load ();
		}

		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			OnDispose ();
			if (Registry != null)
				Registry.DisposeModel (this).Ignore ();
			else
				Data.Unlink (this).Ignore ();
		}

		protected virtual void OnDispose ()
		{
		}

		public async Task Synchronize ()
		{
			var rep = await GetRepresentationAsync ();
			await rep.Synchronize ();
		}

		protected Task Relink (object id)
		{
			if (Registry != null)
				return Registry.RelinkModel (this, id);
			else {
				Data.Id = id;
				return Task.CompletedTask;
			}
		}

		protected Task UnlinkFromId ()
		{
			if (Registry != null)
				return Registry.RelinkModel (this, null);
			else {
				Data.Id = null;
				return Task.CompletedTask;
			}
		}

		internal DocumentModelRegistry Registry { get; set; }

		internal protected abstract Type RepresentationType { get; }


		protected T GetRepresentation<T> () where T : ModelRepresentation
		{
			return (T)ModelRepresentation;
		}

		async Task<T> GetRepresentationAsync<T> () where T : ModelRepresentation
		{
			return (T) await Data.GetRepresentation (RepresentationType);
		}

		Task<ModelRepresentation> GetRepresentationAsync ()
		{
			return Data.GetRepresentation (RepresentationType);
		}

		internal protected virtual void OnRepresentationChanged ()
		{
		}

		internal async Task RaiseRepresentationChangeEvent ()
		{
			if (modelRepresentation == null)
				return;

			var newRep = await GetRepresentationAsync ();
			if (newRep != modelRepresentation) {
				modelRepresentation = newRep;
				try {
					OnRepresentationChanged ();
				} catch (Exception ex) {
					LoggingService.LogError ("OnRepresentationChanged failed", ex);
				}
				RaiseChangeEvent ();
			}
		}

		internal void RaiseChangeEvent ()
		{
			if (modelRepresentation != null && notifiedChangeVersion != modelRepresentation.CurrentVersion) {
				try {
					notifiedChangeVersion = modelRepresentation.CurrentVersion;
					Changed?.Invoke (this, EventArgs.Empty);
				} catch (Exception ex) {
					LoggingService.LogError ("RaiseChanged failed", ex);
				}
			}
		}

		internal class DocumentModelData
		{
			ImmutableList<DocumentModel> linkedModels = ImmutableList<DocumentModel>.Empty;
			Dictionary <Type, ModelRepresentation> representations = new Dictionary<Type, ModelRepresentation> ();
			SemaphoreSlim dataLock = new SemaphoreSlim (1);

			ModelRepresentation lastChangedRepresentation;
			int changeVersion;
			object changeLock = new object ();

			public bool IsShared => linkedModels.Count > 1;

			public bool IsLinked => Id != null;

			public object Id { get; set; }

			public async Task LinkNew (DocumentModel model)
			{
				await dataLock.WaitAsync ();
				try {
					LinkNewNoLock (model);
				} finally {
					dataLock.Release ();
				}
			}

			public void LinkNewNoLock (DocumentModel model)
			{
				linkedModels = linkedModels.Add (model);
				model.Data = this;
			}

			internal async Task Relink (DocumentModel model, DocumentModelData newData)
			{
				bool newDataLocked = false, representationLocked = false;
				ModelRepresentation representation = null;
				var repType = model.RepresentationType;
				ModelRepresentation modelRepresentationToDispose = null;

				var modelsToNotify = new List<DocumentModel> ();

				await dataLock.WaitAsync ();
				try {
					representations.TryGetValue (repType, out representation);
					await representation.WaitHandle.WaitAsync ();
					representationLocked = true;

					linkedModels = linkedModels.Remove (model);

					var modelsForRep = linkedModels.Where (m => m.RepresentationType == repType).ToList ();
					if (modelsForRep.Count > 0) {
						// If there is more than one model using this representation then the representation
						// has to be cloned, since it can't be shared anymore
						var representationCopy = (ModelRepresentation)Activator.CreateInstance (representation.GetType ());
						await representationCopy.InternalCopyFrom (representation);
						representationCopy.DocumentModelData = this;
						representations [repType] = representationCopy;
						foreach (var m in modelsForRep)
							modelsToNotify.Add (m);
					} else {
						await RemoveRepresentation (repType, representation);
					}

					if (newData != null) {
						await newData.dataLock.WaitAsync ();
						newDataLocked = true;

						// If there are other models using the same representation, we'll have to notify them that
						// the representation has changed
						if (newData.representations.TryGetValue (repType, out modelRepresentationToDispose))
							modelsToNotify.AddRange (newData.linkedModels.Where (m => m.RepresentationType == repType));

						newData.linkedModels = newData.linkedModels.Add (model);
						newData.representations [repType] = representation;
						representation.DocumentModelData = newData;

						// Register that this new representation is the latest version.
						// If there are other representations, they will get the new data after a Synchronize call.
						newData.NotifyChanged (representation);
					}
				} finally {
					dataLock.Release ();
					if (representationLocked)
						representation.WaitHandle.Release ();
					if (newDataLocked)
						newData.dataLock.Release ();
				}

				foreach (var m in modelsToNotify)
					m.RaiseRepresentationChangeEvent ().Ignore ();

				if (modelRepresentationToDispose != null)
					modelRepresentationToDispose.OnDispose ().Ignore ();
			}

			async Task RemoveRepresentation (Type repType, ModelRepresentation representation)
			{
				if (lastChangedRepresentation == representation) {
					// Make sure all other representations have the latest data
					foreach (var otherRep in representations.Values) {
						if (otherRep != representation) {
							await Synchronize (otherRep, true);
							lock (changeLock) {
								if (lastChangedRepresentation == representation || changeVersion < otherRep.CurrentVersion) {
									lastChangedRepresentation = otherRep;
									changeVersion = otherRep.CurrentVersion;
								}
							}
						}
					}
				}
				representations.Remove (repType);
			}

			public async Task<ModelRepresentation> GetRepresentation (Type type)
			{
				ModelRepresentation existingRep = null;
				ModelRepresentation representation;

				try {
					await dataLock.WaitAsync ();
					if (!representations.TryGetValue (type, out representation)) {
						existingRep = representations.Values.FirstOrDefault ();
						representations [type] = representation = (ModelRepresentation)Activator.CreateInstance (type);
						representation.DocumentModelData = this;
						if (existingRep != null) {
							// The requested representation is new and there are other representations around which may contain
							// changes. Get existing data from them.
							await existingRep.WaitHandle.WaitAsync ();
							await representation.InternalCopyFrom (existingRep);
						}
					}
				} finally {
					if (existingRep != null)
						existingRep.WaitHandle.Release ();
					dataLock.Release ();
				}

				return representation;
			}

			public async Task<bool> Unlink (DocumentModel model)
			{
				await dataLock.WaitAsync ();
				try {
					linkedModels = linkedModels.Remove (model);
					if (!linkedModels.Any (m => m.RepresentationType == model.RepresentationType)) {
						// Last model referencing its representation. Get rid of it.
						if (representations.TryGetValue (model.RepresentationType, out var rep)) {
							await RemoveRepresentation (model.RepresentationType, rep);
							rep.OnDispose ().Ignore ();
						}
					}
					return linkedModels.Count == 0;
				} finally {
					dataLock.Release ();
				}
			}

			public void NotifyChanged (ModelRepresentation representation)
			{
				lock (changeLock) {
					lastChangedRepresentation = representation;
					representation.CurrentVersion = ++changeVersion;
				}
				RaiseChangedEvent (representation.GetType ());
			}

			public Task Synchronize (ModelRepresentation targetRep)
			{
				return Synchronize (targetRep, false);
			}

			async Task Synchronize (ModelRepresentation targetRep, bool dataLocked)
			{
				ModelRepresentation sourceModel = null;
				int currentChangeVersion;

				lock (changeLock) {
					sourceModel = lastChangedRepresentation;
					currentChangeVersion = changeVersion;
				}

				if (targetRep.CurrentVersion == currentChangeVersion || sourceModel == null || sourceModel == targetRep)
					return; // Not changed

				try {
					// Any operation that requires the lock of several representation must take
					// the model lock first, to avoid deadlocks
					if (!dataLocked)
						await dataLock.WaitAsync ();

					await targetRep.WaitHandle.WaitAsync ();
					await sourceModel.WaitHandle.WaitAsync ();

					await targetRep.InternalCopyFrom (sourceModel);
				} finally {
					if (!dataLocked)
						dataLock.Release ();
					targetRep.WaitHandle.Release ();
					sourceModel.WaitHandle.Release ();
				}
				RaiseChangedEvent (targetRep.GetType ());
			}

			void RaiseChangedEvent (Type repType)
			{
				foreach (var m in linkedModels.Where (m => m.RepresentationType == repType))
					m.RaiseChangeEvent ();
			}
		}
	}
}
