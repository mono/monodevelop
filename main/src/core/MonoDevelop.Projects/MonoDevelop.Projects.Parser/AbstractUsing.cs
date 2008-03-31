//  AbstractUsing.cs
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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultUsing : IUsing
	{
		protected IRegion region;
		
		List<string> usings  = new List<string>();
		SortedList<string, IReturnType> aliases;
		
		public IRegion Region {
			get {
				return region;
			}
			set {
				this.region = value;
			}
		}
		
		public List<string> Usings {
			get {
				if (usings == null)
					usings = new List<string> ();
				return usings;
			}
		}
		
		public SortedList<string, IReturnType> Aliases {
			get {
				if (aliases == null)
					aliases = new SortedList<string, IReturnType> ();
				return aliases;
			}
		}
		
		IEnumerable<string> IUsing.Usings {
			get {
				return usings;
			}
		}

		IEnumerable<string> IUsing.Aliases {
			get {
				if (aliases == null)
					return new string[0];
				return aliases.Keys;
			}
		}
		
		public IReturnType GetAlias (string name)
		{
			IReturnType rt;
			if (aliases != null && aliases.TryGetValue (name, out rt))
				return rt;
			else
				return null;
		}
		
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder("[AbstractUsing: using list=");
			foreach (string str in usings) {
				builder.Append(str);
				builder.Append(", ");
			}
			builder.Append("]");
			return builder.ToString();
		}
	}
}
