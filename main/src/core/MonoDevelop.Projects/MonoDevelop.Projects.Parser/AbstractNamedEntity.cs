//  AbstractNamedEntity.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 http://www.icsharpcode.net/ <#Develop>
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

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractNamedEntity : AbstractDecoration
	{
		public static Hashtable fullyQualifiedNames = new Hashtable();
		string name;
		
		public override string Name {
			get { return name; }
			set { name = value; }
//			set { name = GetSharedString (value); }
		}
		
		static int req;
		public static string GetSharedString (string value)
		{
			req++;
			if (value == null)
				return null;
			else {
				string sharedVal = fullyQualifiedNames[value] as string;
				if (sharedVal != null)
					return sharedVal;
				else {
					fullyQualifiedNames[value] = value;
					return value;
				}
			}
		}

		protected virtual bool CanBeSubclass {
			get {
				return false;
			}
		}
	}
}
