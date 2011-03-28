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

namespace MonoDevelop.Ide.Gui
{
	public static class DisplayBindingService
	{
		static IEnumerable<T> GetBindings<T> ()
		{
			return AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/DisplayBindings")
				.OfType<T> ();
		}
		
		static IEnumerable<IDisplayBinding> GetDisplayBindings (string filename, string mimeType)
		{
			if (mimeType == null && filename != null)
				mimeType = DesktopService.GetMimeTypeForUri (filename);
			
			foreach (var b in GetBindings<IDisplayBinding> ()) {
				if ((filename != null && b.CanHandleFile (filename)) || (mimeType != null && b.CanHandleMimeType (mimeType)))
					yield return b;
			}
		}
		
		public static IViewDisplayBinding GetDefaultViewBinding (string filename, string mimeType)
		{
			return GetDisplayBindings (filename, mimeType).OfType<IViewDisplayBinding> ()
				.FirstOrDefault (d => d.CanUseAsDefault);
		}
		
		static IDisplayBinding GetDefaultBinding (string filename, string mimeType)
		{
			return GetDisplayBindings (filename, mimeType).FirstOrDefault ();
		}
		
		static IEnumerable<IDisplayBinding> GetBindingsForMimeType (string mimeType)
		{
			return GetBindings<IDisplayBinding> ().Where (b => b.CanHandleMimeType (mimeType));
		}
		
		public static void AttachSubWindows (IWorkbenchWindow workbenchWindow)
		{
			foreach (var b in GetBindings<IAttachableDisplayBinding> ()) {
				if (b.CanAttachTo (workbenchWindow.ViewContent)) 
					workbenchWindow.AttachViewContent (b.CreateViewContent (workbenchWindow.ViewContent));
			}
		}

		public static IEnumerable<FileViewer> GetFileViewers (FilePath fileName)
		{
			string mimeType = DesktopService.GetMimeTypeForUri (fileName);
			var viewerIds = new HashSet<string> ();
			
			foreach (var b in GetBindings<IDisplayBinding> ()) {
				if (b.CanHandleMimeType (mimeType)) {
					var vb = b as IViewDisplayBinding;
					if (vb != null) {
						yield return new FileViewer (vb);
					} else {
						var eb = (IExternalDisplayBinding) b;
						var app = eb.GetApplicationForMimeType (mimeType);
						if (viewerIds.Add (app.Id))
							yield return new FileViewer (app);
					}
				} else if (b.CanHandleFile (fileName)) {
					var vb = b as IViewDisplayBinding;
					if (vb != null) {
						yield return new FileViewer (vb);
					} else {
						var eb = (IExternalDisplayBinding) b;
						var app = eb.GetApplicationForFile (mimeType);
						if (viewerIds.Add (app.Id))
							yield return new FileViewer (app);
					}
				}
			}

			foreach (var app in DesktopService.GetApplications (fileName))
				if (viewerIds.Add (app.Id))
					yield return new FileViewer (app);
		}
	}
}