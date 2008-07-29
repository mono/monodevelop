//  ModifierEnum.cs
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

namespace MonoDevelop.Projects.Parser
{
	[Flags]
	public enum ModifierEnum : uint {
		None       = 0,
		
		// Access 
		Private   = 0x0001,
		Internal  = 0x0002,
		Protected = 0x0004,
		Public    = 0x0008,
		
		// Scope
		Abstract  = 0x0010, 
		Virtual   = 0x0020,
		Sealed    = 0x0040,
		Static    = 0x0080,
		Override  = 0x0100,
		Readonly  = 0x0200,
		Const	  = 0X0400,
		New       = 0x0800,
		
		// Special 
		Extern    = 0x1000,
		Volatile  = 0x2000,
		Unsafe    = 0x4000,
		
		ProtectedAndInternal = Internal | Protected,
		ProtectedOrInternal = 0x8000,
//		Literal             = 0x10000, <-- == Const now!!!
		SpecialName         = 0x20000,
		
		Final               = 0x40000,
		VisibilityMask = Private | Internal | Protected | Public,
	}
}

