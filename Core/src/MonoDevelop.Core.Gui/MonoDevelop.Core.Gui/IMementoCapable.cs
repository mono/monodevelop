//  IMementoCapable.cs
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

using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// This interface flags an object beeing "mementocapable". This means that the
	/// state of the object could be saved to an <see cref="Properties"/> object
	/// and set from a object from the same class.
	/// This is used to save and restore the state of GUI objects.
	/// </summary>
	public interface IMementoCapable
	{
		/// <summary>
		/// Creates a new memento from the state.
		/// </summary>
		ICustomXmlSerializer CreateMemento ();
		
		/// <summary>
		/// Sets the state to the given memento.
		/// </summary>
		void SetMemento (ICustomXmlSerializer memento);
	}
}
