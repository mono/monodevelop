// 
// IVersionControlViewHandler.cs
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

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.VersionControl.Views
{
	abstract class VersionControlViewHandler<T>
		where T : IAttachableViewContent
	{
		public virtual bool CanHandle (VersionControlItem item, DocumentView primaryView)
		{
			return (primaryView == null || primaryView.GetContent <ITextFile> () != null)
				&& DesktopService.GetFileIsText (item.Path);
		}

		public abstract T CreateView (VersionControlDocumentInfo info);
	}

	class BlameViewHandler : VersionControlViewHandler<BlameView>
	{
		static BlameViewHandler handler = new BlameViewHandler ();
		public static BlameViewHandler Default {
			get { return handler; }
		}

		public override BlameView CreateView (VersionControlDocumentInfo info)
		{
			return new BlameView (info);
		}
	}

	class DiffViewHandler : VersionControlViewHandler<DiffView>
	{
		static DiffViewHandler handler = new DiffViewHandler ();
		public static DiffViewHandler Default {
			get { return handler; }
		}

		public override DiffView CreateView (VersionControlDocumentInfo info)
		{
			return new DiffView (info);
		}
	}

	class LogViewHandler : VersionControlViewHandler<LogView>
	{
		static LogViewHandler handler = new LogViewHandler ();
		public static LogViewHandler Default {
			get { return handler; }
		}

		public override bool CanHandle (VersionControlItem item, DocumentView primaryView)
		{
			return !item.VersionInfo.HasLocalChange (VersionStatus.ScheduledAdd);
		}

		public override LogView CreateView (VersionControlDocumentInfo info)
		{
			return new LogView (info);
		}
	}

	class MergeViewHandler : VersionControlViewHandler<MergeView>
	{
		static MergeViewHandler handler = new MergeViewHandler ();
		public static MergeViewHandler Default {
			get { return handler; }
		}

		public override MergeView CreateView (VersionControlDocumentInfo info)
		{
			return new MergeView (info);
		}
	}
}
