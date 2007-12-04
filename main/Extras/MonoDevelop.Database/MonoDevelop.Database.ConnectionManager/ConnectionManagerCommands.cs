//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.ConnectionManager
{
	public enum ConnectionManagerCommands
	{
		AddConnection,
		EditConnection,
		RemoveConnection,
		ConnectConnection,
		DisconnectConnection,

		Query,
		Refresh,
		SelectAll,
		SelectColumns,
		EmptyTable,
		Rename,
		RenameDatabase,
		
		CreateDatabase,
		CreateTable,
		CreateView,
		CreateProcedure,
		CreateConstraint,
		CreateUser,
		CreateTrigger,
		
		AlterDatabase,
		AlterTable,
		AlterView,
		AlterProcedure,
		AlterConstraint,
		AlterUser,
		AlterTrigger,
		
		DropDatabase,
		DropTable,
		DropView,
		DropProcedure,
		DropConstraint,
		DropUser,
		DropTrigger
	}
}