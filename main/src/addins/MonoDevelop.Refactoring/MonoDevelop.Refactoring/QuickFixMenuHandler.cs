//
// QuickFixMenuHandler.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using MonoDevelop.Components.Commands;
using MonoDevelop.CodeActions;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring
{
	class QuickFixMenuHandler : CommandHandler
	{
		protected override async Task UpdateAsync (CommandArrayInfo info, CancellationToken cancelToken)
		{
			var editor = IdeApp.Workbench.ActiveDocument?.Editor;
			var ext = editor?.GetContent<CodeActionEditorExtension> ();
			if (ext == null)
				return;
			var quickFixMenu = new CommandInfoSet ();
			quickFixMenu.Text = GettextCatalog.GetString ("Quick Fix");
			quickFixMenu.CommandInfos.Add (new CommandInfo (GettextCatalog.GetString ("Loading..."), false, false), null);
			info.Add (quickFixMenu);
			try {
				var menu = await CodeFixMenuService.CreateFixMenu (editor, ext.GetCurrentFixesAsync (cancelToken).Result, cancelToken);
				quickFixMenu.CommandInfos.Clear ();
				foreach (var item in menu.Items) {
					AddItem (quickFixMenu, item);
				}
				if (menu.Items.Count == 0) {
					quickFixMenu.CommandInfos.Add (new CommandInfo (GettextCatalog.GetString ("No code fixes available"), false, false), null);
				}
			} catch (OperationCanceledException) {
				
			} catch (Exception e) {
				LoggingService.LogError ("Error while creating quick fix menu.", e); 
				quickFixMenu.CommandInfos.Clear ();
				quickFixMenu.CommandInfos.Add (new CommandInfo (GettextCatalog.GetString ("No code fixes available"), false, false), null);
			}
		}

		CommandInfoSet CreateCommandInfoSet (CodeFixMenu menu)
		{
			var cis = new CommandInfoSet ();
			cis.Text = menu.Label;
			foreach (var item in menu.Items) {
				AddItem (cis, item);
			}
			return cis;
		}

		void AddItem (CommandInfoSet cis, CodeFixMenuEntry item)
		{
			if (item == CodeFixMenuEntry.Separator) {
				if (cis.CommandInfos.Count == 0)
					return;
				cis.CommandInfos.AddSeparator ();
			} else if (item is CodeFixMenu)  {
				var menu = (CodeFixMenu)item;
				var submenu = new CommandInfoSet {
					Text = menu.Label
				};
				foreach (var subItem in menu.Items) {
					AddItem (submenu, subItem);
				}
				cis.CommandInfos.Add (submenu, item.Action);
			} else { 
				cis.CommandInfos.Add (new CommandInfo (item.Label), item.Action);
			}
		}

		protected override void Run (object data)
		{
			(data as Action)?.Invoke ();
		}
	}
}
