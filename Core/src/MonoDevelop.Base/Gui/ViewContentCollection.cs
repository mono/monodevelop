// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;

namespace MonoDevelop.Gui
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='.IViewContent'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.ViewContentCollection'/>
	[Serializable()]
	public class ViewContentCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.ViewContentCollection'/>.
		///    </para>
		/// </summary>
		public ViewContentCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.ViewContentCollection'/> based on another <see cref='.ViewContentCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.ViewContentCollection'/> from which the contents are copied
		/// </param>
		public ViewContentCollection(ViewContentCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.ViewContentCollection'/> containing any array of <see cref='.IViewContent'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IViewContent'/> objects with which to intialize the collection
		/// </param>
		public ViewContentCollection(IViewContent[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IViewContent'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IViewContent this[int index] {
			get {
				return ((IViewContent)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IViewContent'/> with the specified value to the
		///    <see cref='.ViewContentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IViewContent'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.ViewContentCollection.AddRange'/>
		public int Add(IViewContent value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.ViewContentCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IViewContent'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.ViewContentCollection.Add'/>
		public void AddRange(IViewContent[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.ViewContentCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.ViewContentCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.ViewContentCollection.Add'/>
		public void AddRange(ViewContentCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.ViewContentCollection'/> contains the specified <see cref='.IViewContent'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IViewContent'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IViewContent'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.ViewContentCollection.IndexOf'/>
		public bool Contains(IViewContent value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.ViewContentCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.ViewContentCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.ViewContentCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IViewContent[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IViewContent'/> in
		///       the <see cref='.ViewContentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IViewContent'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IViewContent'/> of <paramref name='value'/> in the
		/// <see cref='.ViewContentCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.ViewContentCollection.Contains'/>
		public int IndexOf(IViewContent value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IViewContent'/> into the <see cref='.ViewContentCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IViewContent'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.ViewContentCollection.Add'/>
		public void Insert(int index, IViewContent value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.ViewContentCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IViewContentEnumerator GetEnumerator() {
			return new IViewContentEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IViewContent'/> from the
		///    <see cref='.ViewContentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IViewContent'/> to remove from the <see cref='.ViewContentCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IViewContent value) {
			List.Remove(value);
		}
		
		public class IViewContentEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IViewContentEnumerator(ViewContentCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IViewContent Current {
				get {
					return ((IViewContent)(baseEnumerator.Current));
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
