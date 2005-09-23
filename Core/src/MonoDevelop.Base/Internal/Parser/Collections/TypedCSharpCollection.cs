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
	///       A collection that stores <see cref='.IIndexer'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IIndexerCollection'/>
	[Serializable()]
	public class IndexerCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IIndexerCollection'/>.
		///    </para>
		/// </summary>
		public IndexerCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IIndexerCollection'/> based on another <see cref='.IIndexerCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IIndexerCollection'/> from which the contents are copied
		/// </param>
		public IndexerCollection(IndexerCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IIndexerCollection'/> containing any array of <see cref='.IIndexer'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IIndexer'/> objects with which to intialize the collection
		/// </param>
		public IndexerCollection(IIndexer[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IIndexer'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IIndexer this[int index] {
			get {
				return ((IIndexer)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IIndexer'/> with the specified value to the
		///    <see cref='.IIndexerCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IIndexer'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IIndexerCollection.AddRange'/>
		public int Add(IIndexer value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IIndexerCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IIndexer'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IIndexerCollection.Add'/>
		public void AddRange(IIndexer[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IIndexerCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IIndexerCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IIndexerCollection.Add'/>
		public void AddRange(IndexerCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IIndexerCollection'/> contains the specified <see cref='.IIndexer'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IIndexer'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IIndexer'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IIndexerCollection.IndexOf'/>
		public bool Contains(IIndexer value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IIndexerCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IIndexerCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IIndexerCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IIndexer[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IIndexer'/> in
		///       the <see cref='.IIndexerCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IIndexer'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IIndexer'/> of <paramref name='value'/> in the
		/// <see cref='.IIndexerCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IIndexerCollection.Contains'/>
		public int IndexOf(IIndexer value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IIndexer'/> into the <see cref='.IIndexerCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IIndexer'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IIndexerCollection.Add'/>
		public void Insert(int index, IIndexer value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IIndexerCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IIndexerEnumerator GetEnumerator() {
			return new IIndexerEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IIndexer'/> from the
		///    <see cref='.IIndexerCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IIndexer'/> to remove from the <see cref='.IIndexerCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IIndexer value) {
			List.Remove(value);
		}
		
		public class IIndexerEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IIndexerEnumerator(IndexerCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IIndexer Current {
				get {
					return ((IIndexer)(baseEnumerator.Current));
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
