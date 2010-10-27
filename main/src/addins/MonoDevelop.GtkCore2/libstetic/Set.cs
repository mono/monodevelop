using System;
using System.Collections;

namespace Stetic {

	public class Set : IEnumerable, IEnumerator {

		Hashtable hash = new Hashtable ();

		public bool this [object obj] {
			get {
				return hash[obj] != null;
			}
			set {
				if (value)
					hash[obj] = obj;
				else
					hash.Remove (obj);
			}
		}

		public void Clear ()
		{
			hash.Clear ();
		}

		public IEnumerator GetEnumerator ()
		{
			return this;
		}

		IDictionaryEnumerator hashEnum;

		public void Reset ()
		{
			hashEnum = hash.GetEnumerator ();
			hashEnum.Reset ();
		}

		public bool MoveNext ()
		{
			return hashEnum.MoveNext ();
		}
 
		public object Current {
			get {
				return hashEnum.Key;
			}
		}
	}
}
