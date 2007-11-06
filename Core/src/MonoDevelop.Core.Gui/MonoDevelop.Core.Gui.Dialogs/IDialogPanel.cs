//  IDialogPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.CodeDom.Compiler;
using Gtk;

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public enum DialogMessage {
		OK,
		Cancel,
		Help,
		Next,
		Prev,
		Finish,
		Activated
	}
	
	public interface IDialogPanel
	{
		/// <summary>
		/// Some panels do get an object which they can customize, like
		/// Wizard Dialogs. Check the dialog description for more details
		/// about this.
		/// </summary>
		object CustomizationObject {
			get;
			set;
		}
		
		Widget Control {
			get;
		}
		
		bool WasActivated {
			get;
		}
				
		bool EnableFinish {
			get;
		}

		Gtk.Image Icon {
			get;
		}
		
		/// <returns>
		/// true, if the DialogMessage could be executed.
		/// </returns>
		bool ReceiveDialogMessage(DialogMessage message);
		
		event EventHandler EnableFinishChanged;
	}
}
