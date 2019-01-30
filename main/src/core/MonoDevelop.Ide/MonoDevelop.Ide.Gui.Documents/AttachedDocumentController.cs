//
// AttachedDocumentController.cs
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
namespace MonoDevelop.Ide.Gui
{
	public class AttachedDocumentController
	{
		/// <summary>
		/// Initializes the controller
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="status">Status of the controller/view, returned by a GetDocumentStatus() call from a previous session</param>
		public virtual Task Initialize (DocumentStatus status)
		{
		}

		/// <summary>
		/// Saves the document. If the controller has a model, the default implementation will save the model.
		/// </summary>
		public virtual Task OnSave ()
		{
			if (Model != null)
				return Model.Save ();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Attaches a controller to this controller. 
		/// </summary>
		public void AttachController (AttachedDocumentController controller)
		{
			attachedControllers.Add (controller);
		}

		/// <summary>
		/// List of controllers attached to this one
		/// </summary>
		/// <value>The attached controllers.</value>
		public IEnumerable<AttachedDocumentController> AttachedControllers => attachedControllers;

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		public virtual ProjectReloadCapability ProjectReloadCapability { get; }

		/// <summary>
		/// Title shown in the document tab
		/// </summary>
		public virtual string DocumentTitle { get; }

		/// <summary>
		/// Icon of the document tab
		/// </summary>
		/// <value>The stock icon identifier.</value>
		public virtual string DocumentIconId { get; }

		/// <summary>
		/// Returns the current editing status of the controller.
		/// </summary>
		public virtual DocumentStatus GetDocumentStatus ();

		/// <summary>
		/// Return true if the document has been modified
		/// </summary>
		public bool IsDirty { get; set; }
	}
}
