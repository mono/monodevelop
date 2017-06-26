// 
// CustomExecutionModeWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Execution
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class CustomExecutionModeWidget : Gtk.Bin, IExecutionConfigurationEditor
	{
		public CustomExecutionModeWidget ()
		{
			this.Build ();

			folderEntry.EntryAccessible.SetCommonAttributes ("CustomExecutionMode.WorkingDirectory", null, 
			                                                 GettextCatalog.GetString ("Select the working directory for execution"));
			folderEntry.EntryAccessible.SetTitleUIElement (label2.Accessible);
			label2.Accessible.SetTitleFor (folderEntry.EntryAccessible);

			entryArgs.SetCommonAccessibilityAttributes ("CustomExecutionMode.Arguments", label4,
			                                            GettextCatalog.GetString ("Enter any custom arguments to be passed to the executable"));

			envVarList.SetCommonAccessibilityAttributes ("CustomExecutionMode.Variables", label3,
			                                             GettextCatalog.GetString ("Enter any custom environment variables"));
		}
		
		#region IExecutionModeEditor implementation
		public Gtk.Widget Load (CommandExecutionContext ctx, object data)
		{
			if (data != null) {
				CustomArgsExecutionModeData cdata = (CustomArgsExecutionModeData) data;
				entryArgs.Text = cdata.Arguments;
				folderEntry.Path = cdata.WorkingDirectory;
				envVarList.LoadValues (cdata.EnvironmentVariables);
			}
			return this;
		}
		
		public object Save ()
		{
			CustomArgsExecutionModeData cdata = new CustomArgsExecutionModeData ();
			cdata.Arguments = entryArgs.Text;
			cdata.WorkingDirectory = folderEntry.Path;
			envVarList.StoreValues (cdata.EnvironmentVariables);
			return cdata;
		}
		#endregion
	}
}
