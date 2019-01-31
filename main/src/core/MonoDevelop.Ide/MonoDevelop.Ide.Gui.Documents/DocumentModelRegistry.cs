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

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Registry that keeps track of currently active and shared document models
	/// </summary>
	[DefaultServiceImplementation]
	public class DocumentModelRegistry: Service
	{
		Dictionary<object, DocumentModel.DocumentModelData> dataModels = new Dictionary<object, DocumentModel.DocumentModelData> ();

		public FileDocumentModel GetSharedFileModel (FilePath filePath)
		{
			return GetSharedModel<FileDocumentModel> (filePath);
		}

		public T GetSharedModel<T> (object id) where T: DocumentModel, new()
		{
			var t = new T ();
			if (dataModels.TryGetValue (id, out var data))
				t.Data = data;
			else {
				data = t.CreateDataObject ();
				data.Id = id;
				data.Registry = this;
				dataModels[id] = data;
				t.Data = data;
			}
			return t;
		}

		public void RegisterSharedModel (DocumentModel model)
		{
			if (model.Data?.Registry == null)
				RelinkModel (model, model.Id);
		}

		internal void UnregisterModelData (DocumentModel.DocumentModelData data)
		{
			dataModels.Remove (data.Id);
		}

		DocumentModel.DocumentModelData GetModelData (object id)
		{
			dataModels.TryGetValue (id, out var data);
			return data;
		}

		internal void RelinkModel (DocumentModel model, object newId)
		{
			var previousNewIdData = newId != null ? GetModelData (newId) : null;

			// Create a copy of the data object. This copy will be assigned
			// to models with the old id that are already open, since the current data object will
			// be reused for models with the new id (we don't want to replace the data of the
			// model being relinked

			if (model.Data.LinkedModelCount > 1) {
				var oldDataCopy = model.CreateDataObject ();
				oldDataCopy.Id = model.Data.Id;
				oldDataCopy.CopyFrom (model.Data);
				oldDataCopy.Registry = this;
				dataModels [oldDataCopy.Id] = oldDataCopy;

				foreach (var oldLinked in model.Data.LinkedModels) {
					if (oldLinked != model)
						oldLinked.Data = oldDataCopy;
				}
			} else {
				// No other models are linked to this one, just unregister the data,
				// since it will be re-registered with the new id
				dataModels.Remove (model.Data.Id);
			}

			model.Data.Id = newId;

			// If there were models already loaded with the new id, replace their
			// data by the one from the relinked model

			if (previousNewIdData != null) {
				foreach (var linked in previousNewIdData.LinkedModels)
					linked.Data = model.Data;
			}

			// Register the new data object
			if (newId != null)
				dataModels [newId] = model.Data;
		}
	}
}
