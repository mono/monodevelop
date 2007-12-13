// SshFuseFileCopyConfigurationEditorWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System;
using MonoDevelop.Deployment.Targets;

namespace MonoDevelop.Deployment.Gui
{
	
	
	internal partial class SshFuseFileCopyConfigurationEditorWidget : Gtk.Bin
	{
		SshFuseFileCopyConfiguration config;
		
		public SshFuseFileCopyConfigurationEditorWidget (SshFuseFileCopyConfiguration config)
		{
			this.Build();
			
			this.config = config;
			
			if (config.Directory != null)
				entryDirectory.Text =  config.Directory;
			if (config.UserName != null)
				entryUserName.Text = config.UserName;
			if (config.HostName != null)
				entryHostName.Text = config.HostName;
		}

		void HostnameChanged (object sender, System.EventArgs e)
		{
			config.HostName = entryHostName.Text;
		}

		void DirectoryChanged (object sender, System.EventArgs e)
		{
			config.Directory = entryDirectory.Text;
		}

		void UserNameChanged (object sender, System.EventArgs e)
		{
			config.UserName = entryUserName.Text;
		}
	}
}
