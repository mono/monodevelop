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
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Autotools
{
	public class Set<T>: IEnumerable<T>
	{
		private Dictionary<T,object> hashtable = new Dictionary<T,object>();
		
		public void Add (T o)
		{
			hashtable[o] = this;
		}
		
		public void Remove(T o)
		{
			hashtable.Remove(o);
		}
		
		public bool Contains(T o)
		{
			return hashtable.ContainsKey (o);
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			return hashtable.Keys.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return hashtable.Keys.GetEnumerator();
		}
		
		public bool ContainsSet(Set<T> set)
		{
			foreach(T o in set) {
				if(!Contains(o))
					return false;
			}
			
			return true;
		}
		
		public void Union (IEnumerable<T> set)
		{
			foreach(T o in set) {
				hashtable[o] = this;
			}
		}

		public void Intersect (Set<T> set)
		{
			List<T> toRemove = new List<T> ();
			foreach (T o in hashtable.Keys)
				if (!set.Contains (o))
					toRemove.Add (o);

			foreach (T o in toRemove)
				hashtable.Remove (o);
		}

		public void Without(Set<T> set)
		{
			foreach(T o in set) {
				hashtable.Remove(o);
			}
		}
		
		public bool Empty {
			get {
				return hashtable.Count == 0;
			}
		}

		public int Count {
			get { return hashtable.Keys.Count; }
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("[ ");
			foreach (T o in hashtable.Keys)
				sb.AppendFormat ("{0}, ", o.ToString ());
			sb.Append (" ]");
			return sb.ToString ();
		}
	}
}
