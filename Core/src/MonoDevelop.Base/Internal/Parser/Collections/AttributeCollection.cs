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
	///       A collection that stores <see cref='.IAttribute'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IAttributeCollection'/>
	[Serializable()]
	public class AttributeCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IAttributeCollection'/>.
		///    </para>
		/// </summary>
		public AttributeCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IAttributeCollection'/> based on another <see cref='.IAttributeCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IAttributeCollection'/> from which the contents are copied
		/// </param>
		public AttributeCollection(AttributeCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IAttributeCollection'/> containing any array of <see cref='.IAttribute'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IAttribute'/> objects with which to intialize the collection
		/// </param>
		public AttributeCollection(IAttribute[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IAttribute'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IAttribute this[int index] {
			get {
				return ((IAttribute)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IAttribute'/> with the specified value to the
		///    <see cref='.IAttributeCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IAttribute'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IAttributeCollection.AddRange'/>
		public int Add(IAttribute value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IAttributeCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IAttribute'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IAttributeCollection.Add'/>
		public void AddRange(IAttribute[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IAttributeCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IAttributeCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IAttributeCollection.Add'/>
		public void AddRange(AttributeCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IAttributeCollection'/> contains the specified <see cref='.IAttribute'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IAttribute'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IAttribute'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IAttributeCollection.IndexOf'/>
		public bool Contains(IAttribute value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IAttributeCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IAttributeCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IAttributeCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IAttribute[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IAttribute'/> in
		///       the <see cref='.IAttributeCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IAttribute'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IAttribute'/> of <paramref name='value'/> in the
		/// <see cref='.IAttributeCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IAttributeCollection.Contains'/>
		public int IndexOf(IAttribute value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IAttribute'/> into the <see cref='.IAttributeCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IAttribute'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IAttributeCollection.Add'/>
		public void Insert(int index, IAttribute value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IAttributeCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IAttributeEnumerator GetEnumerator() {
			return new IAttributeEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IAttribute'/> from the
		///    <see cref='.IAttributeCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IAttribute'/> to remove from the <see cref='.IAttributeCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IAttribute value) {
			List.Remove(value);
		}
		
		public class IAttributeEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IAttributeEnumerator(AttributeCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IAttribute Current {
				get {
					return ((IAttribute)(baseEnumerator.Current));
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
