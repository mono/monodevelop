//  IClipboardHandler.cs
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
using MonoDevelop.Ide.Gui.Undo;

namespace MonoDevelop.Ide.Gui.Content
{
	public interface IClipboardHandler
	{
		bool EnableCut {
			get;
		}
		bool EnableCopy {
			get;
		}
		bool EnablePaste {
			get;
		}
		bool EnableDelete {
			get;
		}
		bool EnableSelectAll {
			get;
		}
		
		void Cut(object sender, EventArgs e);
		void Copy(object sender, EventArgs e);
		void Paste(object sender, EventArgs e);
		void Delete(object sender, EventArgs e);
		void SelectAll(object sender, EventArgs e);
	}
}
