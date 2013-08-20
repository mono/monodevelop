// 
// MergeCommand.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011, Xamarin Inc.
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

using System.Linq;
using Mono.Addins;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.Views;

namespace MonoDevelop.VersionControl
{
	public class MergeCommand
	{
		internal static readonly string MergeViewHandlers = "/MonoDevelop/VersionControl/MergeViewHandler";
		
		static bool CanShow (VersionControlItem item)
		{
			return !item.IsDirectory
				&& item.VersionInfo.IsVersioned
				&& AddinManager.GetExtensionObjects<IMergeViewHandler> (MergeViewHandlers).Any (h => h.CanHandle (item, null));
		}
		
		public static bool Show (VersionControlItemList items, bool test)
		{
			if (test)
				return items.All (CanShow);
			
			foreach (var item in items) {
				var document = IdeApp.Workbench.OpenDocument (item.Path, OpenDocumentOptions.Default | OpenDocumentOptions.OnlyInternalViewer);
				if (document != null)
					document.Window.SwitchView (document.Window.FindView<IMergeView> ());
			}
			
			return true;
		}
	}
}

