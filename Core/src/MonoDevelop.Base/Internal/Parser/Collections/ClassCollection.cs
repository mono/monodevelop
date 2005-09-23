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
	///       A collection that stores <see cref='.IClass'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IClassCollection'/>
	[Serializable()]
	public class ClassCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IClassCollection'/>.
		///    </para>
		/// </summary>
		public ClassCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IClassCollection'/> based on another <see cref='.IClassCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IClassCollection'/> from which the contents are copied
		/// </param>
		public ClassCollection(ClassCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IClassCollection'/> containing any array of <see cref='.IClass'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IClass'/> objects with which to intialize the collection
		/// </param>
		public ClassCollection(IClass[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IClass'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IClass this[int index] {
			get {
				return ((IClass)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IClass'/> with the specified value to the
		///    <see cref='.IClassCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IClass'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IClassCollection.AddRange'/>
		public int Add(IClass value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IClassCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IClass'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IClassCollection.Add'/>
		public void AddRange(IClass[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IClassCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IClassCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IClassCollection.Add'/>
		public void AddRange(ClassCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IClassCollection'/> contains the specified <see cref='.IClass'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IClass'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IClass'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IClassCollection.IndexOf'/>
		public bool Contains(IClass value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IClassCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IClassCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IClassCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IClass[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IClass'/> in
		///       the <see cref='.IClassCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IClass'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IClass'/> of <paramref name='value'/> in the
		/// <see cref='.IClassCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IClassCollection.Contains'/>
		public int IndexOf(IClass value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IClass'/> into the <see cref='.IClassCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IClass'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IClassCollection.Add'/>
		public void Insert(int index, IClass value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IClassCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IClassEnumerator GetEnumerator() {
			return new IClassEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IClass'/> from the
		///    <see cref='.IClassCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IClass'/> to remove from the <see cref='.IClassCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IClass value) {
			List.Remove(value);
		}
		
		public class IClassEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IClassEnumerator(ClassCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IClass Current {
				get {
					return ((IClass)(baseEnumerator.Current));
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
