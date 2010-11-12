//
// DialogFileReplacePolicy.cs
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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

using MonoDevelop.Deployment;
using MonoDevelop.Ide;

namespace MonoDevelop.Deployment.Gui
{
	
	public class DialogFileReplacePolicy : IFileReplacePolicy
	{
		FileReplaceMode persistentMode = FileReplaceMode.NotSet;
		FileReplaceDialog.ReplaceResponse response = FileReplaceDialog.ReplaceResponse.ReplaceOlder;
		
		public DialogFileReplacePolicy ()
		{
		}
		
		public FileReplaceMode ReplaceAction {
			get {
				return persistentMode;
			}
		}
		
		delegate int DialogDelegate ();
		
		public FileReplaceMode GetReplaceAction (string source, DateTime sourceModified, string target, DateTime targetModified)
		{
			if (persistentMode != FileReplaceMode.NotSet)
				return persistentMode;
			
			//IFileReplacePolicy is not likely to be running in the GUI thread
			//so use some DispatchService magic to synchronously call the dialog in the GUI thread
			DispatchService.GuiSyncDispatch (delegate {
				var dialog = new FileReplaceDialog (response, source, sourceModified.ToString (), target, targetModified.ToString ());
				response = (FileReplaceDialog.ReplaceResponse) MessageService.ShowCustomDialog (dialog);
			});
			
			switch (response) {
			case FileReplaceDialog.ReplaceResponse.Replace:
				return FileReplaceMode.Replace;
			
			case FileReplaceDialog.ReplaceResponse.ReplaceAll:
				persistentMode = FileReplaceMode.Replace;
				return FileReplaceMode.Replace;
			
			case FileReplaceDialog.ReplaceResponse.ReplaceOlder:
				return FileReplaceMode.ReplaceOlder;
			
			case FileReplaceDialog.ReplaceResponse.ReplaceOlderAll:	
				persistentMode = FileReplaceMode.ReplaceOlder;
				return FileReplaceMode.ReplaceOlder;
			
			case FileReplaceDialog.ReplaceResponse.Skip:
				return FileReplaceMode.Skip;
			
			case FileReplaceDialog.ReplaceResponse.SkipAll:
				persistentMode = FileReplaceMode.Skip;
				return FileReplaceMode.Skip;
			
			case FileReplaceDialog.ReplaceResponse.Abort:			
				return FileReplaceMode.Abort;
			
			default:
				throw new Exception ("Unexpected ReplaceResponse value");
			}
			
		}
	}
	
}
