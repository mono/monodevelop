// 
// HiddenWorkbenchWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Ide.Gui
{
	public class HiddenWorkbenchWindow : MonoDevelop.Ide.Gui.IWorkbenchWindow
	{
		string title;
		public string Title {
			get {
				return title;
			}
			set {
				title = value;
			}
		}
		
		public string DocumentType {
			get { return ""; }
			set {}
		}
		
		public MonoDevelop.Ide.Gui.Document Document {
			get;
			set;
		}

		public bool ShowNotification {
			get { return false; }
			set {}
		}
		
		MonoDevelop.Ide.Gui.IViewContent viewContent;
		public MonoDevelop.Ide.Gui.IViewContent ViewContent {
			get { return viewContent; }
			set { viewContent = value; }
		}
		
		public IEnumerable<IAttachableViewContent> SubViewContents { get { return new IAttachableViewContent[0]; } }
		
		public MonoDevelop.Ide.Gui.IBaseViewContent ActiveViewContent {
			get { return ViewContent;}
			set {}
		}
		
		public bool CloseWindow (bool force, bool fromMenu, int pageNum)
		{
			return true;
		}
		
		public void SelectWindow ()
		{
		}
		
		public void SwitchView (int viewNumber)
		{
		}
		public void SwitchView (MonoDevelop.Ide.Gui.IAttachableViewContent subViewContent)
		{
		}
		
		public int FindView (Type viewType)
		{
			return -1;
		}
		
		public void AttachViewContent (MonoDevelop.Ide.Gui.IAttachableViewContent subViewContent)
		{
			
		}
		public event EventHandler TitleChanged { add {} remove {} }
		public event EventHandler DocumentChanged;
		public event MonoDevelop.Ide.Gui.WorkbenchWindowEventHandler Closing;
		public event MonoDevelop.Ide.Gui.WorkbenchWindowEventHandler Closed;
		public event MonoDevelop.Ide.Gui.ActiveViewContentEventHandler ActiveViewContentChanged;
	}
}
