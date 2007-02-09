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

using MonoDevelop.Core;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	
	public class DialogFileReplacePolicy : IFileReplacePolicy
	{
		FileReplaceMode persistentMode;
		bool ask;
		
		public DialogFileReplacePolicy (FileReplaceMode mode)
		{
			persistentMode = mode;
		}
		
		public DialogFileReplacePolicy ()
		{
			ask = true;
		}
		
		public FileReplaceMode ReplaceAction {
			get {
				return persistentMode;
			}
		}
		
		public FileReplaceMode GetReplaceAction (string source, DateTime sourceModified, string target, DateTime targetModified)
		{
			if (!ask)
				return persistentMode;

			string[] buttons = new string[] {
				GettextCatalog.GetString ("_Replace file"),
				GettextCatalog.GetString ("Replace all files"),
				GettextCatalog.GetString ("Replace all _older files"),
				GettextCatalog.GetString ("_Skip file"),
				GettextCatalog.GetString ("Skip all files"),
				GettextCatalog.GetString ("_Abort deployment")
			};
			
			string message = GettextCatalog.GetString ("The target file {0} already exists, and was last modified at {1}. The replacement file, {2}, was modified on {3}. What would you like to do?", target, targetModified, source, sourceModified);
			
			int answer = MonoDevelop.Core.Gui.Services.MessageService.ShowCustomDialog (GettextCatalog.GetString ("File already exists"), message, buttons);
			
			switch (answer) {
				case 0: //replace this file
					return FileReplaceMode.Replace;
				
				case 1: //replace all
					persistentMode = FileReplaceMode.Replace;
					return FileReplaceMode.Replace;
				
				case 2: //replace all older
					persistentMode = FileReplaceMode.ReplaceOlder;
					return FileReplaceMode.ReplaceOlder;
				
				case 3: //skip this file
					return FileReplaceMode.Skip;
				
				case 4: //skip all
					persistentMode = FileReplaceMode.Skip;
					return FileReplaceMode.Skip;
				
				default: //case 5, abort
					return FileReplaceMode.Abort;
			}
			
		}
	}
	
}
