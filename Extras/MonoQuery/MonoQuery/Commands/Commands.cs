//
// AddConnectionCommand.cs
//
// Author:
// Christian Hergert <chris@mosaix.net>
//
// Copyright (C) 2005 Christian Hergert
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
using System.Reflection;

using MonoDevelop.Gui;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;
using MonoDevelop.Core.Services;

using Gtk;

using Mono.Data.Sql;
using MonoQuery;

namespace MonoQuery.Commands
{
	public enum MonoQueryCommands {
		AddConnection,
		RemoveConnection,
		DisconnectConnection,
		RefreshProviderList,
		RefreshConnection,
		QueryCommand,
		EmptyTable,
		DropTable,
		Refresh
	}
	
	public class AddConnection : CommandHandler
	{
		protected override void Run ()
		{
			ConnectionDialog dialog = new ConnectionDialog ();
			int retval = dialog.Run ();
			if (retval == (int) Gtk.ResponseType.Ok) {
				Assembly asm = Assembly.GetAssembly (typeof (Mono.Data.Sql.DbProviderBase));
				DbProviderBase provider = (DbProviderBase) asm.CreateInstance (dialog.ConnectionType.FullName);
				if (provider == null)
					return;
				provider.Name = dialog.ConnectionName;
				provider.ConnectionString = dialog.ConnectionString;
				
				MonoQueryService service = (MonoQueryService)
					ServiceManager.GetService (typeof (MonoQueryService));
				service.Providers.Add ( (DbProviderBase) provider);
			}
			dialog.Destroy ();
		}
	}
}
