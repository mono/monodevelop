namespace Sharpen
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	internal interface ConcurrentMap<T, U> : IEnumerable, IDictionary<T, U>, IEnumerable<KeyValuePair<T, U>>, ICollection<KeyValuePair<T, U>>
	{
		U PutIfAbsent (T key, U value);
		bool Remove (object key, object value);
		bool Replace (T key, U oldValue, U newValue);
	}
}
