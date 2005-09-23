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
	///       A collection that stores <see cref='.Comment'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.CommentCollection'/>
	[Serializable()]
	public class CommentCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.CommentCollection'/>.
		///    </para>
		/// </summary>
		public CommentCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.CommentCollection'/> based on another <see cref='.CommentCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.CommentCollection'/> from which the contents are copied
		/// </param>
		public CommentCollection(CommentCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.CommentCollection'/> containing any array of <see cref='.Comment'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.Comment'/> objects with which to intialize the collection
		/// </param>
		public CommentCollection(Comment[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.Comment'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public Comment this[int index] {
			get {
				return ((Comment)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.Comment'/> with the specified value to the
		///    <see cref='.CommentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Comment'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.CommentCollection.AddRange'/>
		public int Add(Comment value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.CommentCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.Comment'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.CommentCollection.Add'/>
		public void AddRange(Comment[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.CommentCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.CommentCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.CommentCollection.Add'/>
		public void AddRange(CommentCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.CommentCollection'/> contains the specified <see cref='.Comment'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Comment'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.Comment'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.CommentCollection.IndexOf'/>
		public bool Contains(Comment value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.CommentCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.CommentCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.CommentCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(Comment[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.Comment'/> in
		///       the <see cref='.CommentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Comment'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.Comment'/> of <paramref name='value'/> in the
		/// <see cref='.CommentCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.CommentCollection.Contains'/>
		public int IndexOf(Comment value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.Comment'/> into the <see cref='.CommentCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.Comment'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.CommentCollection.Add'/>
		public void Insert(int index, Comment value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.CommentCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new CommentEnumerator GetEnumerator() {
			return new CommentEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.Comment'/> from the
		///    <see cref='.CommentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.Comment'/> to remove from the <see cref='.CommentCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(Comment value) {
			List.Remove(value);
		}
		
		public class CommentEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public CommentEnumerator(CommentCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public Comment Current {
				get {
					return ((Comment)(baseEnumerator.Current));
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
