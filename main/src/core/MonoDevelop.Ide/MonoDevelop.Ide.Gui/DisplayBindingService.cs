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

using MonoDevelop.Core.Gui.Codons;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui
{
	public static class DisplayBindingService
	{
		static List<DisplayBindingCodon> displayBindings = new List<DisplayBindingCodon> ();
		
		static DisplayBindingService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/DisplayBindings", delegate(object sender, ExtensionNodeEventArgs args) {
				DisplayBindingCodon displayBindingCodon = (DisplayBindingCodon)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					displayBindings.Add (displayBindingCodon);
					break;
				case ExtensionChange.Remove:
					displayBindings.Remove (displayBindingCodon);
					break;
				}
			});
		}
		
		static IEnumerable<IDisplayBinding> RealDisplayBindings {
			get {
				return from binding in displayBindings where binding.DisplayBinding is IDisplayBinding select (IDisplayBinding)binding.DisplayBinding;
			}
		}
		
		public static IDisplayBinding GetBindingForUri (string uri)
		{
			return GetBinding (uri, null);
		}
		
		public static IDisplayBinding GetBinding (string uri, string mimeType)
		{
			return RealDisplayBindings.FirstOrDefault (binding => binding.CanCreateContentForUri (uri) || binding.CanCreateContentForMimeType (mimeType));
		}
		
		public static IEnumerable<IDisplayBinding> GetBindingsForMimeType (string mimeType)
		{
			return from binding in RealDisplayBindings where binding.CanCreateContentForMimeType (mimeType) select binding;
		}
		
		public static void AttachSubWindows (IWorkbenchWindow workbenchWindow)
		{
			foreach (DisplayBindingCodon codon in displayBindings) {
				IAttachableDisplayBinding binding = codon.DisplayBinding as IAttachableDisplayBinding;
				if (binding != null && binding.CanAttachTo (workbenchWindow.ViewContent)) 
					workbenchWindow.AttachViewContent (binding.CreateViewContent (workbenchWindow.ViewContent));
			}
		}
	}
}
