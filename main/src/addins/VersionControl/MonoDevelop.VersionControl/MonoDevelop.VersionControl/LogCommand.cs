// 
// LogCommand.cs
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
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.Views;

namespace MonoDevelop.VersionControl
{
	public class LogCommand
	{
		internal static readonly string LogViewHandlers = "/MonoDevelop/VersionControl/LogViewHandler";
		
		static bool CanShow (VersionControlItem item)
		{
			// We want directories to be able to view the log for an entire directory
			// by selecting it from the solution pane
			return item.VersionInfo.IsVersioned && 
				       AddinManager.GetExtensionObjects<IVersionControlViewHandler> (LogViewHandlers).Any (h => h.CanHandle (item, null));
		}
		
		public static async Task<bool> Show (VersionControlItemList items, bool test)
		{
			if (test)
				return items.All (CanShow);
			
			foreach (var item in items) {
				if (!item.IsDirectory) {
					Document document = await IdeApp.Workbench.OpenDocument (item.Path, item.ContainerProject, OpenDocumentOptions.Default | OpenDocumentOptions.OnlyInternalViewer);
					document?.GetContent<VersionControlDocumentController> ()?.ShowLogView ();
					continue;
				}

				var info = new VersionControlDocumentInfo (null, null, item, item.Repository);
				var logView = new LogView (info);
				info.Document = await IdeApp.Workbench.OpenDocument (logView, true);
				logView.Init ();
			}
			
			return true;
		}
	}
}

