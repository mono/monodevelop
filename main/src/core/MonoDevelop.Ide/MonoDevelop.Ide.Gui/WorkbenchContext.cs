//  IWorkbench.cs
//
// Author:
//   Mike Krüger
//   Lluis Sanchez Gual
//
//  This file was derived from a file from #Develop 2.0
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// 
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
// 
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui
{
	public class WorkbenchContext
	{
		string id;
		static Hashtable contexts = new Hashtable ();
		
		WorkbenchContext (string id)
		{
			this.id = id;
		}
		
		public static WorkbenchContext GetContext (string id)
		{
			WorkbenchContext ctx = (WorkbenchContext) contexts [id];
			if (ctx == null) {
				ctx = new WorkbenchContext (id);
				contexts [id] = ctx;
			}
			return ctx;
		}
		
		public static WorkbenchContext Edit {
			get { return GetContext ("Edit"); }
		}
		
		public static WorkbenchContext Debug {
			get { return GetContext ("Debug"); }
		}
		
		public string Id {
			get { return id; }
		}
	}
}
