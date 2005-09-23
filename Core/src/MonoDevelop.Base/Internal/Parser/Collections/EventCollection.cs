// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;

namespace MonoDevelop.Internal.Parser
{
	
	
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='.IEvent'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IEventCollection'/>
	[Serializable()]
	public class EventCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IEventCollection'/>.
		///    </para>
		/// </summary>
		public EventCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IEventCollection'/> based on another <see cref='.IEventCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IEventCollection'/> from which the contents are copied
		/// </param>
		public EventCollection(EventCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IEventCollection'/> containing any array of <see cref='.IEvent'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IEvent'/> objects with which to intialize the collection
		/// </param>
		public EventCollection(IEvent[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IEvent'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IEvent this[int index] {
			get {
				return ((IEvent)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IEvent'/> with the specified value to the
		///    <see cref='.IEventCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IEvent'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IEventCollection.AddRange'/>
		public int Add(IEvent value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IEventCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IEvent'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IEventCollection.Add'/>
		public void AddRange(IEvent[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IEventCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IEventCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IEventCollection.Add'/>
		public void AddRange(EventCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IEventCollection'/> contains the specified <see cref='.IEvent'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IEvent'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IEvent'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IEventCollection.IndexOf'/>
		public bool Contains(IEvent value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IEventCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IEventCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IEventCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IEvent[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IEvent'/> in
		///       the <see cref='.IEventCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IEvent'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IEvent'/> of <paramref name='value'/> in the
		/// <see cref='.IEventCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IEventCollection.Contains'/>
		public int IndexOf(IEvent value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IEvent'/> into the <see cref='.IEventCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IEvent'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IEventCollection.Add'/>
		public void Insert(int index, IEvent value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IEventCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IEventEnumerator GetEnumerator() {
			return new IEventEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IEvent'/> from the
		///    <see cref='.IEventCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IEvent'/> to remove from the <see cref='.IEventCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IEvent value) {
			List.Remove(value);
		}
		
		public class IEventEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IEventEnumerator(EventCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IEvent Current {
				get {
					return ((IEvent)(baseEnumerator.Current));
				}
			}
			
			object IEnumerator.Current {
				get {
					return baseEnumerator.Current;
				}
			}
			
			public bool MoveNext() {
				return baseEnumerator.MoveNext();
			}
			
			bool IEnumerator.MoveNext() {
				return baseEnumerator.MoveNext();
			}
			
			public void Reset() {
				baseEnumerator.Reset();
			}
			
			void IEnumerator.Reset() {
				baseEnumerator.Reset();
			}
		}
	}
}
