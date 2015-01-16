// 
// SelectFileDialogHandler.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc. (http://wwww.novell.com)
// Copyright (C) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.IO;
using System.Linq;
using Gtk;
using Microsoft.WindowsAPICodePack.Dialogs;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Platform
{
	class SelectFileDialogHandler : ISelectFileDialogHandler
	{
		public bool Run (SelectFileDialogData data)
		{
			var parent = data.TransientFor ?? MessageService.RootWindow;

			CommonFileDialog dialog;
			if (data.Action == FileChooserAction.Open || data.Action == FileChooserAction.SelectFolder)
				dialog = new CustomCommonOpenFileDialog ();
			else
				dialog = new CommonSaveFileDialog ();

			dialog.SetCommonFormProperties (data);

			if (!GdkWin32.RunModalWin32Dialog (dialog, parent))
				return false;

			dialog.GetCommonFormProperties (data);

			return true;
		}
	}
}
