﻿//
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
				
			var metadata = new Counters.FixesMenuMetadata ();
			using (var timer = Counters.FixesMenu.BeginTiming ("Quick Fix menu", metadata)) {
				try {
					info.Add (new CommandInfo (GettextCatalog.GetString ("Loading..."), false, false), null);
					var currentFixes = await ext.GetCurrentFixesAsync (cancelToken);
					var menu = CodeFixMenuService.CreateFixMenu (editor, currentFixes, cancelToken);
					info.Clear ();
					foreach (var item in menu.Items) {
						AddItem (info, item);
					}
					if (menu.Items.Count == 0) {
						info.Add (new CommandInfo (GettextCatalog.GetString ("No code fixes available"), false, false), null);
					}
					metadata.SetSuccess ();
					info.NotifyChanged ();
				} catch (OperationCanceledException) {
					metadata.SetUserCancel ();
				} catch (Exception e) {
					metadata.SetFailure ();
					LoggingService.LogError ("Error while creating quick fix menu.", e); 
					info.Clear ();
					info.Add (new CommandInfo (GettextCatalog.GetString ("No code fixes available"), false, false), null);
					info.NotifyChanged ();
				}
			}
		}

		CommandInfoSet CreateCommandInfoSet (CodeFixMenu menu)
		{
			var cis = new CommandInfoSet ();
			cis.Text = menu.Label;
			foreach (var item in menu.Items) {
				AddItem (cis.CommandInfos, item);
			}
			return cis;
		}

		void AddItem (CommandArrayInfo cis, CodeFixMenuEntry item)
		{
			if (item == CodeFixMenuEntry.Separator) {
				if (cis.Count == 0)
					return;
				cis.AddSeparator ();
			} else if (item is CodeFixMenu)  {
				var menu = (CodeFixMenu)item;
				var submenu = new CommandInfoSet {
					Text = menu.Label
				};
				foreach (var subItem in menu.Items) {
					AddItem (submenu.CommandInfos, subItem);
				}
				cis.Add (submenu, item.Action);
			} else {
				var info = new CommandInfo (item.Label);
				info.Enabled = item.Action != null;
				cis.Add (info, item.Action);
			}
		}

		protected override void Run (object data)
		{
			(data as Action)?.Invoke ();
		}
	}
}
