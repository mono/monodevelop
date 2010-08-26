using System.Collections.Generic;

namespace GitSharp.Core.Util
{
    public static class IListUtil
    {
        public static bool isEmpty<T>(this ICollection<T> l)
        {
			if (l == null)
				throw new System.ArgumentNullException ("l");
            return l.Count == 0;
        }

        public static bool isEmpty<TK, TV>(this IDictionary<TK, TV> d)
        {
			if (d == null)
				throw new System.ArgumentNullException ("d");
            return (d.Count == 0);
        }
    }
}