// 
// MacSelectFileDialogHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Components.Extensions;
using OSXIntegration.Framework;
using MonoDevelop.Ide.Extensions;
using Gtk;

namespace MonoDevelop.Platform.Mac
{
	class MacSelectFileDialogHandler : ISelectFileDialogHandler
	{
		public bool Run (SelectFileDialogData data)
		{
			var options = NavDialogCreationOptions.NewFromDefaults ();
			NavDialog dialog = null;
			
			try {
				options.Modality = WindowModality.AppModal;
				
				if (!string.IsNullOrEmpty (data.Title))
					options.WindowTitle = data.Title;
				
				options.OptionFlags |= NavDialogOptionFlags.DontAddTranslateItems
					& NavDialogOptionFlags.DontAutoTranslate & NavDialogOptionFlags.DontConfirmReplacement;
				
				if (data.SelectMultiple)
					options.OptionFlags |= NavDialogOptionFlags.AllowMultipleFiles;
				else
					options.OptionFlags ^= NavDialogOptionFlags.AllowMultipleFiles;
				
				//data.SelectedFiles
				
				switch (data.Action) {
				case FileChooserAction.CreateFolder:
					dialog = NavDialog.CreateNewFolderDialog (options);
					break;
				case FileChooserAction.Save:
					options.SaveFileName = data.InitialFileName;
					dialog = NavDialog.CreatePutFileDialog (options);
					break;
				case FileChooserAction.Open:
					dialog = NavDialog.CreateChooseFileDialog (options);
					break;
				case FileChooserAction.SelectFolder:
					dialog = NavDialog.CreateChooseFolderDialog (options);
					break;
				default:
					throw new InvalidOperationException ("Unknown action " + data.Action.ToString ());
				}
				
				if (!string.IsNullOrEmpty (data.CurrentFolder))
					dialog.SetLocation (data.CurrentFolder);
				
				var action = dialog.Run ();
				if (action == NavUserAction.Cancel || action == NavUserAction.None)
					return false;
				using (var reply = dialog.GetReply ()) {
				}
			} finally {
				if (dialog != null)
					dialog.Dispose ();
				if (options != null)
					options.Dispose ();
			}
			return true;
		}
	}
	
	class MacAddFileDialogHandler : IAddFileDialogHandler
	{
		public bool Run (AddFileDialogData data)
		{
			throw new NotImplementedException ();
		}
	}
	
	class MacOpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			throw new NotImplementedException ();
		}
	}
}

