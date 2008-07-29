//  IUsing.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Daniel Grunwald <daniel@danielgrunwald.de>
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
using System.Collections.Generic;

namespace MonoDevelop.Projects.Parser
{
	public interface IUsing
	{
		IRegion Region {
			get;
		}

		IEnumerable<string> Usings { get; }
		
		IEnumerable<string> Aliases { get; }
		
		IReturnType GetAlias (string name);
	}
}
