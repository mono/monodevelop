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

namespace MonoDevelop.Ide.Gui
{
	public static class DisplayBindingService
	{
		public static IEnumerable<T> GetBindings<T> ()
		{
			return AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/DisplayBindings")
				.OfType<T> ();
		}
		
		internal static IEnumerable<IDisplayBinding> GetDisplayBindings (FilePath filePath, string mimeType, Project ownerProject)
		{
			if (mimeType == null && !filePath.IsNullOrEmpty)
				mimeType = DesktopService.GetMimeTypeForUri (filePath);
			
			foreach (var b in GetBindings<IDisplayBinding> ()) {
				if (b.CanHandle (filePath, mimeType, ownerProject))
					yield return b;
			}
		}
		
		public static IViewDisplayBinding GetDefaultViewBinding (FilePath filePath, string mimeType, Project ownerProject)
		{
			return GetDisplayBindings (filePath, mimeType, ownerProject).OfType<IViewDisplayBinding> ()
				.FirstOrDefault (d => d.CanUseAsDefault);
		}
		
		public static IDisplayBinding GetDefaultBinding (FilePath filePath, string mimeType, Project ownerProject)
		{
			return GetDisplayBindings (filePath, mimeType, ownerProject).FirstOrDefault (d => d.CanUseAsDefault);
		}
		
		internal static void AttachSubWindows (IWorkbenchWindow workbenchWindow, IViewDisplayBinding binding)
		{
			int index = 0;

			foreach (var o in GetBindings<object> ()) {
				if (o == binding) {
					index++;
					continue;
				}

				var attachable = o as IAttachableDisplayBinding;
				if (attachable == null)
					continue;

				if (attachable.CanAttachTo (workbenchWindow.ViewContent))
					workbenchWindow.InsertViewContent (index++, attachable.CreateViewContent (workbenchWindow.ViewContent));
			}
		}

		public static IEnumerable<FileViewer> GetFileViewers (FilePath filePath, Project ownerProject)
		{
			string mimeType = DesktopService.GetMimeTypeForUri (filePath);
			var viewerIds = new HashSet<string> ();
			
			foreach (var b in GetDisplayBindings (filePath, mimeType, ownerProject)) {
				var vb = b as IViewDisplayBinding;
				if (vb != null) {
					yield return new FileViewer (vb);
				} else {
					var eb = (IExternalDisplayBinding) b;
					var app = eb.GetApplication (filePath, mimeType, ownerProject);
					if (viewerIds.Add (app.Id))
						yield return new FileViewer (app);
				}
			}

			foreach (var app in DesktopService.GetApplications (filePath))
				if (viewerIds.Add (app.Id))
					yield return new FileViewer (app);
		}
	}
	
	//dummy binding, anchor point for extension tree
	class DefaultDisplayBinding : IViewDisplayBinding
	{
		public IViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			throw new InvalidOperationException ();
		}

		public string Name {
			get { return null; }
		}

		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			return false;
		}

		public bool CanUseAsDefault {
			get { return false; }
		}
	}
}