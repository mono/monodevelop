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
	///       A collection that stores <see cref='.IParameter'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IParameterCollection'/>
	[Serializable()]
	public class ParameterCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IParameterCollection'/>.
		///    </para>
		/// </summary>
		public ParameterCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IParameterCollection'/> based on another <see cref='.IParameterCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IParameterCollection'/> from which the contents are copied
		/// </param>
		public ParameterCollection(ParameterCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IParameterCollection'/> containing any array of <see cref='.IParameter'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IParameter'/> objects with which to intialize the collection
		/// </param>
		public ParameterCollection(IParameter[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IParameter'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IParameter this[int index] {
			get {
				return ((IParameter)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IParameter'/> with the specified value to the
		///    <see cref='.IParameterCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IParameter'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IParameterCollection.AddRange'/>
		public int Add(IParameter value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IParameterCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IParameter'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IParameterCollection.Add'/>
		public void AddRange(IParameter[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IParameterCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IParameterCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IParameterCollection.Add'/>
		public void AddRange(ParameterCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IParameterCollection'/> contains the specified <see cref='.IParameter'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IParameter'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IParameter'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IParameterCollection.IndexOf'/>
		public bool Contains(IParameter value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IParameterCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IParameterCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IParameterCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IParameter[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IParameter'/> in
		///       the <see cref='.IParameterCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IParameter'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IParameter'/> of <paramref name='value'/> in the
		/// <see cref='.IParameterCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IParameterCollection.Contains'/>
		public int IndexOf(IParameter value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IParameter'/> into the <see cref='.IParameterCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IParameter'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IParameterCollection.Add'/>
		public void Insert(int index, IParameter value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IParameterCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IParameterEnumerator GetEnumerator() {
			return new IParameterEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IParameter'/> from the
		///    <see cref='.IParameterCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IParameter'/> to remove from the <see cref='.IParameterCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IParameter value) {
			List.Remove(value);
		}
		
		public class IParameterEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IParameterEnumerator(ParameterCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IParameter Current {
				get {
					return ((IParameter)(baseEnumerator.Current));
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
