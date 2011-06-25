// 
// MessageBubbleCommands.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
	
namespace MonoDevelop.SourceEditor
{
	public enum MessageBubbleCommands
	{
		HideIssues,
		ToggleIssues
	}
	
	class HideIssuesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Text = IdeApp.Preferences.DefaultHideMessageBubbles ? GettextCatalog.GetString ("_Show Message Bubbles") : GettextCatalog.GetString ("_Hide Message Bubbles"); 
		}
		
		protected override void Run (object data)
		{
			IdeApp.Preferences.DefaultHideMessageBubbles = !IdeApp.Preferences.DefaultHideMessageBubbles;
		}
	}
	
	class ToggleIssuesHandler : CommandHandler 
	{
		protected override void Run (object data)
		{
			Action action = data as Action;
			if (action != null)
				action ();
		}
		
		protected override void Update (CommandArrayInfo ainfo)
		{
			CommandInfo info = ainfo.Add (GettextCatalog.GetString ("_Errors & Warnings"), new Action (delegate {
				MonoDevelop.Ide.IdeApp.Preferences.ShowMessageBubbles = MonoDevelop.Ide.ShowMessageBubbles.ForErrorsAndWarnings;
			}));
			info.Checked = MonoDevelop.Ide.IdeApp.Preferences.ShowMessageBubbles == MonoDevelop.Ide.ShowMessageBubbles.ForErrorsAndWarnings;
			
			info = ainfo.Add (GettextCatalog.GetString ("E_rrors only"), new Action (delegate {
				MonoDevelop.Ide.IdeApp.Preferences.ShowMessageBubbles = MonoDevelop.Ide.ShowMessageBubbles.ForErrors;
			}));
			info.Checked = MonoDevelop.Ide.IdeApp.Preferences.ShowMessageBubbles == MonoDevelop.Ide.ShowMessageBubbles.ForErrors;
		}
	}
}

