//
// SpecializedCollections.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Collections;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static partial class SpecializedCollections
	{
		public static readonly byte[] EmptyBytes = EmptyArray<byte>();
		public static readonly object[] EmptyObjects = EmptyArray<object>();

		public static T[] EmptyArray<T>()
		{
			return Empty.Array<T>.Instance;
		}

		public static IEnumerator<T> EmptyEnumerator<T>()
		{
			return Empty.Enumerator<T>.Instance;
		}

		public static IEnumerable<T> EmptyEnumerable<T>()
		{
			return Empty.List<T>.Instance;
		}

		public static ICollection<T> EmptyCollection<T>()
		{
			return Empty.List<T>.Instance;
		}

		public static IList<T> EmptyList<T>()
		{
			return Empty.List<T>.Instance;
		}

		public static IReadOnlyList<T> EmptyReadOnlyList<T>()
		{
			return Empty.List<T>.Instance;
		}

		public static ISet<T> EmptySet<T>()
		{
			return Empty.Set<T>.Instance;
		}

		public static IDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>()
		{
			return Empty.Dictionary<TKey, TValue>.Instance;
		}

		public static IEnumerable<T> SingletonEnumerable<T>(T value)
		{
			return new Singleton.Collection<T>(value);
		}

		public static ICollection<T> SingletonCollection<T>(T value)
		{
			return new Singleton.Collection<T>(value);
		}

		public static IEnumerator<T> SingletonEnumerator<T>(T value)
		{
			return new Singleton.Enumerator<T>(value);
		}

		public static IEnumerable<T> ReadOnlyEnumerable<T>(IEnumerable<T> values)
		{
			return new ReadOnly.Enumerable<IEnumerable<T>, T>(values);
		}

		public static ICollection<T> ReadOnlyCollection<T>(ICollection<T> collection)
		{
			return collection == null || collection.Count == 0
				? EmptyCollection<T>()
					: new ReadOnly.Collection<ICollection<T>, T>(collection);
		}

		public static ISet<T> ReadOnlySet<T>(ISet<T> set)
		{
			return set == null || set.Count == 0
				? EmptySet<T>()
					: new ReadOnly.Set<ISet<T>, T>(set);
		}

		public static ISet<T> ReadOnlySet<T>(IEnumerable<T> values)
		{
			if (values is ISet<T>)
			{
				return ReadOnlySet((ISet<T>)values);
			}

			HashSet<T> result = null;
			foreach (var item in values)
			{
				result = result ?? new HashSet<T>();
				result.Add(item);
			}

			return ReadOnlySet(result);
		}

		private partial class Empty
		{
			internal class Array<T>
			{
				public static readonly T[] Instance = new T[0];
			}
		
			internal class Collection<T> : Enumerable<T>, ICollection<T>
			{
				public static readonly ICollection<T> Instance = new Collection<T>();

				protected Collection()
				{
				}

				public void Add(T item)
				{
					throw new NotSupportedException();
				}

				public void Clear()
				{
				}

				public bool Contains(T item)
				{
					return false;
				}

				public void CopyTo(T[] array, int arrayIndex)
				{
				}

				public int Count
				{
					get
					{
						return 0;
					}
				}

				public bool IsReadOnly
				{
					get
					{
						return true;
					}
				}

				public bool Remove(T item)
				{
					throw new NotSupportedException();
				}
			}
		
			internal class Dictionary<TKey, TValue> : Collection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
			{
				public static readonly new IDictionary<TKey, TValue> Instance = new Dictionary<TKey, TValue>();

				private Dictionary()
				{
				}

				public void Add(TKey key, TValue value)
				{
					throw new NotSupportedException();
				}

				public bool ContainsKey(TKey key)
				{
					return false;
				}

				public ICollection<TKey> Keys
				{
					get
					{
						return Collection<TKey>.Instance;
					}
				}

				public bool Remove(TKey key)
				{
					throw new NotSupportedException();
				}

				public bool TryGetValue(TKey key, out TValue value)
				{
					value = default(TValue);
					return false;
				}

				public ICollection<TValue> Values
				{
					get
					{
						return Collection<TValue>.Instance;
					}
				}

				public TValue this[TKey key]
				{
					get
					{
						throw new NotSupportedException();
					}

					set
					{
						throw new NotSupportedException();
					}
				}

			}
		
			internal class Enumerable<T> : IEnumerable<T>
			{
				// PERF: cache the instance of enumerator. 
				// accessing a generic static field is kinda slow from here,
				// but since empty enumerables are singletons, there is no harm in having 
				// one extra instance field
				private readonly IEnumerator<T> _enumerator = Enumerator<T>.Instance;

				public IEnumerator<T> GetEnumerator()
				{
					return _enumerator;
				}

				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}
		
			internal class Enumerator : IEnumerator
			{
				public static readonly IEnumerator Instance = new Enumerator();

				protected Enumerator()
				{
				}

				public object Current
				{
					get
					{
						throw new InvalidOperationException();
					}
				}

				public bool MoveNext()
				{
					return false;
				}

				public void Reset()
				{
					throw new InvalidOperationException();
				}
			}

			internal class Enumerator<T> : Enumerator, IEnumerator<T>
			{
				public static new readonly IEnumerator<T> Instance = new Enumerator<T>();

				protected Enumerator()
				{
				}

				public new T Current
				{
					get
					{
						throw new InvalidOperationException();
					}
				}

				public void Dispose()
				{
				}
			}

			internal class List<T> : Collection<T>, IList<T>, IReadOnlyList<T>
			{
				public static readonly new List<T> Instance = new List<T>();

				protected List()
				{
				}

				public int IndexOf(T item)
				{
					return -1;
				}

				public void Insert(int index, T item)
				{
					throw new NotSupportedException();
				}

				public void RemoveAt(int index)
				{
					throw new NotSupportedException();
				}

				public T this[int index]
				{
					get
					{
						throw new ArgumentOutOfRangeException("index");
					}

					set
					{
						throw new NotSupportedException();
					}
				}
			}

			internal class Set<T> : Collection<T>, ISet<T>
			{
				public static readonly new ISet<T> Instance = new Set<T>();

				protected Set()
				{
				}

				public new bool Add(T item)
				{
					throw new NotImplementedException();
				}

				public void ExceptWith(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public void IntersectWith(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public bool IsProperSubsetOf(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public bool IsProperSupersetOf(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public bool IsSubsetOf(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public bool IsSupersetOf(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public bool Overlaps(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public bool SetEquals(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public void SymmetricExceptWith(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public void UnionWith(IEnumerable<T> other)
				{
					throw new NotImplementedException();
				}

				public new System.Collections.IEnumerator GetEnumerator()
				{
					return Set<T>.Instance.GetEnumerator();
				}
			}
		}
	
		private static partial class ReadOnly
		{
			internal class Collection<TUnderlying, T> : Enumerable<TUnderlying, T>, ICollection<T>
				where TUnderlying : ICollection<T>
			{
				public Collection(TUnderlying underlying)
					: base(underlying)
				{
				}

				public void Add(T item)
				{
					throw new NotSupportedException();
				}

				public void Clear()
				{
					throw new NotSupportedException();
				}

				public bool Contains(T item)
				{
					return this.Underlying.Contains(item);
				}

				public void CopyTo(T[] array, int arrayIndex)
				{
					this.Underlying.CopyTo(array, arrayIndex);
				}

				public int Count
				{
					get
					{
						return this.Underlying.Count;
					}
				}

				public bool IsReadOnly
				{
					get
					{
						return true;
					}
				}

				public bool Remove(T item)
				{
					throw new NotSupportedException();
				}
			}
		
			internal class Enumerable<TUnderlying> : IEnumerable
				where TUnderlying : IEnumerable
			{
				protected readonly TUnderlying Underlying;

				public Enumerable(TUnderlying underlying)
				{
					this.Underlying = underlying;
				}

				public IEnumerator GetEnumerator()
				{
					return this.Underlying.GetEnumerator();
				}
			}

			internal class Enumerable<TUnderlying, T> : Enumerable<TUnderlying>, IEnumerable<T>
				where TUnderlying : IEnumerable<T>
			{
				public Enumerable(TUnderlying underlying)
					: base(underlying)
				{
				}

				public new IEnumerator<T> GetEnumerator()
				{
					return this.Underlying.GetEnumerator();
				}
			}

			internal class Set<TUnderlying, T> : Collection<TUnderlying, T>, ISet<T>
				where TUnderlying : ISet<T>
			{
				public Set(TUnderlying underlying)
					: base(underlying)
				{
				}

				public new bool Add(T item)
				{
					throw new NotSupportedException();
				}

				public void ExceptWith(IEnumerable<T> other)
				{
					throw new NotSupportedException();
				}

				public void IntersectWith(IEnumerable<T> other)
				{
					throw new NotSupportedException();
				}

				public bool IsProperSubsetOf(IEnumerable<T> other)
				{
					return Underlying.IsProperSubsetOf(other);
				}

				public bool IsProperSupersetOf(IEnumerable<T> other)
				{
					return Underlying.IsProperSupersetOf(other);
				}

				public bool IsSubsetOf(IEnumerable<T> other)
				{
					return Underlying.IsSubsetOf(other);
				}

				public bool IsSupersetOf(IEnumerable<T> other)
				{
					return Underlying.IsSupersetOf(other);
				}

				public bool Overlaps(IEnumerable<T> other)
				{
					return Underlying.Overlaps(other);
				}

				public bool SetEquals(IEnumerable<T> other)
				{
					return Underlying.SetEquals(other);
				}

				public void SymmetricExceptWith(IEnumerable<T> other)
				{
					throw new NotSupportedException();
				}

				public void UnionWith(IEnumerable<T> other)
				{
					throw new NotSupportedException();
				}
			}


		}

		private static partial class Singleton
		{
			internal sealed class Collection<T> : ICollection<T>, IReadOnlyCollection<T>
			{
				private T _loneValue;

				public Collection(T value)
				{
					_loneValue = value;
				}

				public void Add(T item)
				{
					throw new NotSupportedException();
				}

				public void Clear()
				{
					throw new NotSupportedException();
				}

				public bool Contains(T item)
				{
					return EqualityComparer<T>.Default.Equals(_loneValue, item);
				}

				public void CopyTo(T[] array, int arrayIndex)
				{
					array[arrayIndex] = _loneValue;
				}

				public int Count
				{
					get { return 1; }
				}

				public bool IsReadOnly
				{
					get { return true; }
				}

				public bool Remove(T item)
				{
					throw new NotSupportedException();
				}

				public IEnumerator<T> GetEnumerator()
				{
					return new Enumerator<T>(_loneValue);
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}
			internal class Enumerator<T> : IEnumerator<T>
			{
				private T _loneValue;
				private bool _moveNextCalled;

				public Enumerator(T value)
				{
					_loneValue = value;
					_moveNextCalled = false;
				}

				public T Current
				{
					get
					{
						return _loneValue;
					}
				}

				object IEnumerator.Current
				{
					get
					{
						return _loneValue;
					}
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					if (!_moveNextCalled)
					{
						_moveNextCalled = true;
						return true;
					}

					return false;
				}

				public void Reset()
				{
					_moveNextCalled = false;
				}
			}
		
		}
	
	}
}