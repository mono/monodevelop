// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Internal.Parser
{
	
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='.Tag'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.TagCollection'/>
	
	[Serializable()]
	
	public class TagCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.TagCollection'/>.
		///    </para>
		/// </summary>
		public TagCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.TagCollection'/> based on another <see cref='.TagCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.TagCollection'/> from which the contents are copied
		/// </param>
		public TagCollection(TagCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.TagCollection'/> containing any array of <see cref='.Tag'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.Tag'/> objects with which to intialize the collection
		/// </param>
		public TagCollection(Tag[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.Tag'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public Tag this[int index] {
			get {
				return ((Tag)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.Tag'/> with the specified value to the
		///    <see cref='.TagCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Tag'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.TagCollection.AddRange'/>
		public int Add(Tag value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.TagCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.Tag'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.TagCollection.Add'/>
		public void AddRange(Tag[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.TagCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.TagCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.TagCollection.Add'/>
		public void AddRange(TagCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.TagCollection'/> contains the specified <see cref='.Tag'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Tag'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.Tag'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.TagCollection.IndexOf'/>
		public bool Contains(Tag value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.TagCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.TagCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.TagCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(Tag[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.Tag'/> in
		///       the <see cref='.TagCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Tag'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.Tag'/> of <paramref name='value'/> in the
		/// <see cref='.TagCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.TagCollection.Contains'/>
		public int IndexOf(Tag value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.Tag'/> into the <see cref='.TagCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.Tag'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.TagCollection.Add'/>
		public void Insert(int index, Tag value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.TagCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new TagEnumerator GetEnumerator() {
			return new TagEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.Tag'/> from the
		///    <see cref='.TagCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Tag'/> to remove from the <see cref='.TagCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(Tag value) {
			List.Remove(value);
		}
		
		public class TagEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			private IEnumerable temp;
			
			public TagEnumerator(TagCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public Tag Current {
				get {
					return ((Tag)(baseEnumerator.Current));
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
