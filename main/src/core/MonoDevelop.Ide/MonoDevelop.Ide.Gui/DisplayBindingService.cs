// DisplayBindingService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;

using Mono.Addins;

using MonoDevelop.Ide.Codons;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Gui
{
	[DefaultServiceImplementation]
	public class DisplayBindingService: Service
	{
		const string extensionPath = "/MonoDevelop/Ide/DisplayBindings";

		DesktopService desktopService;

		protected override async Task OnInitialize (ServiceProvider serviceProvider)
		{
			AddinManager.ExtensionChanged += OnExtensionChanged;
			UpdateExtensionObjects ();
			desktopService = await serviceProvider.GetService<DesktopService> ();
		}

		DisplayBindingService ()
		{
			AddinManager.ExtensionChanged += OnExtensionChanged;
			UpdateExtensionObjects ();
		}

		void OnExtensionChanged (object sender, ExtensionEventArgs args)
		{
			if (args.PathChanged (extensionPath))
				UpdateExtensionObjects ();
		}

		void UpdateExtensionObjects ()
		{
			registeredObjects = AddinManager.GetExtensionObjects (extensionPath);
		}

		object [] registeredObjects;
		private List<IDisplayBinding> runtimeBindings = new List<IDisplayBinding>();

		public IEnumerable<T> GetBindings<T> ()
		{
			return runtimeBindings.OfType<T> ().Concat (registeredObjects.OfType<T> ());
		}

		public void RegisterRuntimeDisplayBinding(IDisplayBinding binding)
		{
			runtimeBindings.Add(binding);
		}

		public void DeregisterRuntimeDisplayBinding(IDisplayBinding binding)
		{
			runtimeBindings.Remove(binding);
		}

		internal IEnumerable<IDisplayBinding> GetDisplayBindings (FilePath filePath, string mimeType, Project ownerProject)
		{
			if (mimeType == null && !filePath.IsNullOrEmpty)
				mimeType = desktopService.GetMimeTypeForUri (filePath);
			
			foreach (var b in GetBindings<IDisplayBinding> ()) {
				bool canHandle = false;
				try {
					canHandle = b.CanHandle (filePath, mimeType, ownerProject);
				} catch (Exception ex) {
					LoggingService.LogError ("Error while getting display bindings", ex);
				}
				if (canHandle)
					yield return b;
			}
		}
		
		public IDisplayBinding GetDefaultBinding (FilePath filePath, string mimeType, Project ownerProject)
		{
			return GetDisplayBindings (filePath, mimeType, ownerProject).FirstOrDefault (d => d.CanUseAsDefault);
		}

		public async Task<IEnumerable<FileViewer>> GetFileViewers (FilePath filePath, Project ownerProject)
		{
			var result = new List<FileViewer> ();

			string mimeType = desktopService.GetMimeTypeForUri (filePath);
			var viewerIds = new HashSet<string> ();
			var fileDescriptor = new FileDescriptor (filePath, mimeType, ownerProject);

			foreach (var b in await IdeServices.DocumentControllerService.GetSupportedControllers (fileDescriptor)) {
				result.Add (new FileViewer (b));
			}

			foreach (var eb in GetDisplayBindings (filePath, mimeType, ownerProject).OfType< IExternalDisplayBinding> ()) {
				var app = eb.GetApplication (filePath, mimeType, ownerProject);
				if (viewerIds.Add (app.Id))
					result.Add (new FileViewer (app));
			}

			foreach (var app in desktopService.GetApplications (filePath))
				if (viewerIds.Add (app.Id))
					result.Add (new FileViewer (app));
			return result;
		}
	}
}