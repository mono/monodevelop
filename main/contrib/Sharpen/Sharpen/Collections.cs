namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	
	internal static class Collections<T>
	{
		static readonly IList<T> empty = new T [0];
		public static IList<T> EMPTY_SET {
			get { return empty; }
		}
		
	}
	
	internal static class Collections
	{
		public static bool AddAll<T> (ICollection<T> list, IEnumerable toAdd)
		{
			foreach (T t in toAdd)
				list.Add (t);
			return true;
		}

		public static V Remove<K, V> (IDictionary<K, V> map, K toRemove) where K : class
		{
			V local;
			if (map.TryGetValue (toRemove, out local)) {
				map.Remove (toRemove);
				return local;
			}
			return default(V);
		}

		public static object[] ToArray (ArrayList list)
		{
			return list.ToArray ();
		}

		public static T[] ToArray<T> (ICollection<T> list)
		{
			T[] array = new T[list.Count];
			list.CopyTo (array, 0);
			return array;
		}

		public static U[] ToArray<T,U> (ICollection<T> list, U[] res) where T:U
		{
			if (res.Length < list.Count)
				res = new U [list.Count];
			
			int n = 0;
			foreach (T t in list)
				res [n++] = t;
			
			if (res.Length > list.Count)
				res [list.Count] = default (T);
			return res;
		}
		
		public static IDictionary<K,V> EmptyMap<K,V> ()
		{
			return new Dictionary<K,V> ();
		}

		public static IList<T> EmptyList<T> ()
		{
			return Collections<T>.EMPTY_SET;
		}

		public static ICollection<T> EmptySet<T> ()
		{
			return Collections<T>.EMPTY_SET;
		}

		public static IList<T> NCopies<T> (int n, T elem)
		{
			List<T> list = new List<T> (n);
			while (n-- > 0) {
				list.Add (elem);
			}
			return list;
		}

		public static void Reverse<T> (IList<T> list)
		{
			int end = list.Count - 1;
			int index = 0;
			while (index < end) {
				T tmp = list [index];
				list [index] = list [end];
				list [end] = tmp;
				++index;
				--end;
			}
		}

		public static ICollection<T> Singleton<T> (T item)
		{
			List<T> list = new List<T> (1);
			list.Add (item);
			return list;
		}

		public static IList<T> SingletonList<T> (T item)
		{
			List<T> list = new List<T> (1);
			list.Add (item);
			return list;
		}

		public static IList<T> SynchronizedList<T> (IList<T> list)
		{
			return new Sharpen.SynchronizedList<T> (list);
		}

		public static ICollection<T> UnmodifiableCollection<T> (ICollection<T> list)
		{
			return list;
		}

		public static IList<T> UnmodifiableList<T> (IList<T> list)
		{
			return new ReadOnlyCollection<T> (list);
		}

		public static ICollection<T> UnmodifiableSet<T> (ICollection<T> list)
		{
			return list;
		}
		
		public static IDictionary<K,V> UnmodifiableMap<K,V> (IDictionary<K,V> dict)
		{
			return dict;
		}
	}
}
