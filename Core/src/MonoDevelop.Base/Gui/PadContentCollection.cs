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
	///       A collection that stores <see cref='.IPadContent'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.PadContentCollection'/>
	[Serializable()]
	public class PadContentCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.PadContentCollection'/>.
		///    </para>
		/// </summary>
		public PadContentCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.PadContentCollection'/> based on another <see cref='.PadContentCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.PadContentCollection'/> from which the contents are copied
		/// </param>
		public PadContentCollection(PadContentCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.PadContentCollection'/> containing any array of <see cref='.IPadContent'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IPadContent'/> objects with which to intialize the collection
		/// </param>
		public PadContentCollection(IPadContent[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IPadContent'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IPadContent this[int index] {
			get {
				return ((IPadContent)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		public IPadContent this [string id] {
			get {
				foreach (IPadContent padContent in this) {
					if (padContent.Id == id) {
						return padContent;
					}
				}
				return null;
			}
		}
		
		
		
		/// <summary>
		///    <para>Adds a <see cref='.IPadContent'/> with the specified value to the
		///    <see cref='.PadContentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IPadContent'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.PadContentCollection.AddRange'/>
		public int Add(IPadContent value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.PadContentCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IPadContent'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.PadContentCollection.Add'/>
		public void AddRange(IPadContent[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.PadContentCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.PadContentCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.PadContentCollection.Add'/>
		public void AddRange(PadContentCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.PadContentCollection'/> contains the specified <see cref='.IPadContent'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IPadContent'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IPadContent'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.PadContentCollection.IndexOf'/>
		public bool Contains(IPadContent value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.PadContentCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.PadContentCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.PadContentCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IPadContent[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IPadContent'/> in
		///       the <see cref='.PadContentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IPadContent'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IPadContent'/> of <paramref name='value'/> in the
		/// <see cref='.PadContentCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.PadContentCollection.Contains'/>
		public int IndexOf(IPadContent value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IPadContent'/> into the <see cref='.PadContentCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IPadContent'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.PadContentCollection.Add'/>
		public void Insert(int index, IPadContent value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.PadContentCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IPadContentEnumerator GetEnumerator() {
			return new IPadContentEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IPadContent'/> from the
		///    <see cref='.PadContentCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IPadContent'/> to remove from the <see cref='.PadContentCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IPadContent value) {
			List.Remove(value);
		}
		
		public class IPadContentEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IPadContentEnumerator(PadContentCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IPadContent Current {
				get {
					return ((IPadContent)(baseEnumerator.Current));
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
