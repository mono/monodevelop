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

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A model is the data being shown in a view.
	/// </summary>
	public abstract class DocumentModel: IDisposable
	{
		DocumentModelData data;

		/// <summary>
		/// Identifier of the model
		/// </summary>
		public object Id => data?.Id;

		/// <summary>
		/// Raised when the model is modified
		/// </summary>
		public event EventHandler Changed;

		/// <summary>
		/// Saves the data to disk
		/// </summary>
		/// <returns>The save.</returns>
		public Task Save ()
		{
			return OnSave ();
		}

		public Task Reload ()
		{
			return OnReload ();
		}

		public void Dispose ()
		{
			Data.Unbind (this);
		}

		protected void NotifyChanged ()
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		internal protected DocumentModelData Data {
			get {
				if (data == null)
					data = CreateDataObject ();
				return data;
			}
			set {
				if (value != data) {
					data?.Unbind (this);
					data = value;
					data?.Bind (this);
					OnDataChanged ();
				}
			}
		}

		internal protected abstract DocumentModelData CreateDataObject ();

		protected virtual Task OnSave ()
		{
			return Task.CompletedTask;
		}

		protected virtual void OnDataChanged ()
		{
		}

		protected void Relink (object id)
		{
			if (Data.Registry != null)
				Data.Registry.RelinkModel (this, id);
			else
				Data.Id = id;
		}

		protected void UnlinkFromId ()
		{
			if (Data.Registry != null)
				Data.Registry.RelinkModel (this, null);
			else
				Data.Id = null;
		}

		protected virtual Task OnReload ()
		{
			return Task.CompletedTask;
		}

		public class DocumentModelData : IDisposable
		{
			List<DocumentModel> boundModels = new List<DocumentModel> ();

			internal DocumentModelRegistry Registry { get; set; }

			public object Id { get; internal set; }

			internal void Bind (DocumentModel model)
			{
				boundModels.Add (model);
			}

			internal void Unbind (DocumentModel model)
			{
				boundModels.Remove (model);
				if (boundModels.Count == 0) {
					Registry?.UnregisterModelData (this);
					Dispose ();
				}
			}

			public IEnumerable<DocumentModel> LinkedModels {
				get { return boundModels; }
			}

			internal int LinkedModelCount => boundModels.Count;

			public virtual void CopyFrom (DocumentModelData other)
			{
			}

			public virtual void Dispose ()
			{
			}
		}
	}
}
