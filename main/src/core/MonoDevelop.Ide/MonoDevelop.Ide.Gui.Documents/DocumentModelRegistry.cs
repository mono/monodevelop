//
// DocumentModelRegistry.cs
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
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Registry that keeps track of currently active and shared document models
	/// </summary>
	[DefaultServiceImplementation]
	public class DocumentModelRegistry: Service
	{
		SemaphoreSlim dataLock = new SemaphoreSlim (1, 1);
		Dictionary<object, DocumentModel.DocumentModelData> dataModels = new Dictionary<object, DocumentModel.DocumentModelData> ();

		public Task<T> GetSharedFileModel<T> (FilePath filePath) where T:FileModel, new()
		{
			return GetSharedModel<T> (filePath);
		}

		public async Task<T> GetSharedModel<T> (object id) where T : DocumentModel, new()
		{
			return (T) await GetSharedModel (typeof (T), id);
		}

		public async Task<DocumentModel> GetSharedModel (Type type, object id)
		{
			var model = (DocumentModel) Activator.CreateInstance (type);

			await dataLock.WaitAsync ();
			try {
				if (dataModels.TryGetValue (id, out var data))
					await data.LinkNew (model);
				else {
					dataModels [id] = model.Data;
					model.Data.Id = id;
				}
			} finally {
				dataLock.Release ();
			}
			model.Registry = this;
			return model;
		}

		public Task ShareModel (DocumentModel model)
		{
			if (model.Registry != null && model.Registry != this)
				throw new InvalidOperationException ("Model does not belong to this registry");

			if (!model.Data.IsLinked)
				throw new InvalidOperationException ("Model is not linked to an id");

			model.Registry = this;
			return RelinkModel (model, model.Id);
		}

		internal async Task DisposeModel (DocumentModel model)
		{
			await dataLock.WaitAsync ();
			try {
				if (await model.Data.Unlink (model)) {
					dataModels.Remove (model.Id);
					model.Data.Id = null;
					model.Registry = null;
				}
			} finally {
				dataLock.Release ();
			}
		}

		internal async Task RelinkModel (DocumentModel model, object newId)
		{
			await dataLock.WaitAsync ();
			try {
				var oldData = model.Data;
				if (newId != null) {
					if (!dataModels.TryGetValue (newId, out var newData)) {
						if (!oldData.IsShared) {
							// Model not shared being assigned a new id with no models registered
							if (model.Id != null)
								dataModels.Remove (model.Id);
							dataModels [newId] = oldData;
							oldData.Id = newId;
							return;
						} else {
							newData = new DocumentModel.DocumentModelData ();
							dataModels [newId] = newData;
							newData.Id = newId;
						}
					}
					await oldData.Relink (model, newData);
				} else {
					var newData = new DocumentModel.DocumentModelData ();
					await oldData.Relink (model, newData);
					model.Registry = null;
				}
			} finally {
				dataLock.Release ();
			}
		}
	}
}
