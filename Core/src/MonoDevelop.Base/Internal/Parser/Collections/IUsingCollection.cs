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
	///       A collection that stores <see cref='.IUsing'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IUsingCollection'/>
	[Serializable()]
	public class IUsingCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IUsingCollection'/>.
		///    </para>
		/// </summary>
		public IUsingCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IUsingCollection'/> based on another <see cref='.IUsingCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IUsingCollection'/> from which the contents are copied
		/// </param>
		public IUsingCollection(IUsingCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IUsingCollection'/> containing any array of <see cref='.IUsing'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IUsing'/> objects with which to intialize the collection
		/// </param>
		public IUsingCollection(IUsing[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IUsing'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IUsing this[int index] {
			get {
				return ((IUsing)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IUsing'/> with the specified value to the
		///    <see cref='.IUsingCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IUsing'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IUsingCollection.AddRange'/>
		public int Add(IUsing value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IUsingCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IUsing'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IUsingCollection.Add'/>
		public void AddRange(IUsing[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IUsingCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IUsingCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IUsingCollection.Add'/>
		public void AddRange(IUsingCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IUsingCollection'/> contains the specified <see cref='.IUsing'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IUsing'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IUsing'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IUsingCollection.IndexOf'/>
		public bool Contains(IUsing value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IUsingCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IUsingCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IUsingCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IUsing[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IUsing'/> in
		///       the <see cref='.IUsingCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IUsing'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IUsing'/> of <paramref name='value'/> in the
		/// <see cref='.IUsingCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IUsingCollection.Contains'/>
		public int IndexOf(IUsing value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IUsing'/> into the <see cref='.IUsingCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IUsing'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IUsingCollection.Add'/>
		public void Insert(int index, IUsing value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IUsingCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IUsingEnumerator GetEnumerator() {
			return new IUsingEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IUsing'/> from the
		///    <see cref='.IUsingCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IUsing'/> to remove from the <see cref='.IUsingCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IUsing value) {
			List.Remove(value);
		}
		
		public class IUsingEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IUsingEnumerator(IUsingCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IUsing Current {
				get {
					return ((IUsing)(baseEnumerator.Current));
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
