/*
Copyright (C) 2006  Matthias Braun <matze@braunis.de>
 
This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 59 Temple Place - Suite 330,
Boston, MA 02111-1307, USA.
*/

using System;
using System.Collections;

namespace MonoDevelop.Autotools
{
	public class Set : IEnumerable
	{
		private Hashtable hashtable = new Hashtable();
		
		public Set()
		{
		}
		
		public void Add(object o)
		{
			hashtable[o] = true;
		}
		
		public void Remove(object o)
		{
			hashtable.Remove(o);
		}
		
		public bool Contains(object o)
		{
			return hashtable.Contains(o);
		}
		
		public IEnumerator GetEnumerator()
		{
			return hashtable.Keys.GetEnumerator();
		}
		
		public bool ContainsSet(Set set)
		{
			foreach(object o in set) {
				if(!Contains(o))
					return false;
			}
			
			return true;
		}
		
		public void Union(Set set)
		{
			foreach(object o in set) {
				hashtable[o] = true;
			}
		}
		
		public void Without(Set set)
		{
			foreach(object o in set) {
				hashtable.Remove(o);
			}
		}
		
		public bool Empty {
			get {
				return hashtable.Count == 0;
			}
		}		
	}
}
