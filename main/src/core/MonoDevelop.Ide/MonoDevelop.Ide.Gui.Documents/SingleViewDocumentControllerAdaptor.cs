//
// SingleViewDocumentControllerAdaptor.cs
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Linq;
using System.Threading;

namespace MonoDevelop.Ide.Gui.Documents
{
	class DocumentControllerViewAdaptorFactory : FileDocumentControllerFactory
	{
		public override Task<DocumentController> CreateController (FileDescriptor file, DocumentControllerDescription controllerDescription)
		{
			var binding = ((BindingDocumentControllerDescription)controllerDescription).Binding;
			var content = binding.CreateContent (file.FilePath, file.MimeType, file.Owner as Project);
			return Task.FromResult< DocumentController> (new SingleViewDocumentControllerAdaptor (binding, content));
		}

		public override IEnumerable<DocumentControllerDescription> GetSupportedControllers (FileDescriptor file)
		{
			foreach (var binding in IdeApp.Services.DisplayBindingService.GetDisplayBindings (file.FilePath, file.MimeType, file.Owner as Project).OfType<IViewDisplayBinding> ())
				yield return new BindingDocumentControllerDescription {
					CanUseAsDefault = binding.CanUseAsDefault,
					Role = DocumentControllerRole.Tool,
					Name = binding.Name,
					Binding = binding
				};
		}
	}

	class BindingDocumentControllerDescription : DocumentControllerDescription
	{
		public IViewDisplayBinding Binding { get; set; }
	}

	class SingleViewDocumentControllerAdaptor : DocumentController
	{
		ViewContent content;
		IViewDisplayBinding binding;
		Control control;

		public ViewContent Content {
			get {
				return content;
			}
		}

		public SingleViewDocumentControllerAdaptor (IViewDisplayBinding binding, ViewContent content)
		{
			this.content = content;
			this.binding = binding;

			content.DirtyChanged += ContentDirtyChanged;

			IsReadOnly = content.IsReadOnly;
		}

		void ContentDirtyChanged (object sender, EventArgs e)
		{
			IsDirty = content.IsDirty;
		}

		protected override bool ControllerIsViewOnly => content.IsViewOnly;

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			return content.Control;
		}

		protected override Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			return base.OnInitialize (modelDescriptor, status);
		}

		protected override async Task<DocumentView> OnInitializeView ()
		{
			var view = await base.OnInitializeView ();
			view.ViewShown += (s,a) => {
				IdeApp.Services.DisplayBindingService.AttachSubWindows (this, binding);
			};
			return view;
		}

		protected override Task<Control> OnGetViewControlAsync (CancellationToken cancellationToken, DocumentViewContent view)
		{
			return Task.FromResult (content.Control);
		}

		internal void InsertViewContent (int v, BaseViewContent subViewContent)
		{
			throw new NotImplementedException ();
		}
	}

	class DocumentControllerExtensionAdaptor : DocumentControllerExtension
	{
		public DocumentControllerExtensionAdaptor (BaseViewContent content)
		{
			Content = content;
		}

		public BaseViewContent Content { get; }
	}
}
