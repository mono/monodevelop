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
	///       A collection that stores <see cref='.IProperty'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IPropertyCollection'/>
	[Serializable()]
	public class PropertyCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IPropertyCollection'/>.
		///    </para>
		/// </summary>
		public PropertyCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IPropertyCollection'/> based on another <see cref='.IPropertyCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IPropertyCollection'/> from which the contents are copied
		/// </param>
		public PropertyCollection(PropertyCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IPropertyCollection'/> containing any array of <see cref='.IProperty'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IProperty'/> objects with which to intialize the collection
		/// </param>
		public PropertyCollection(IProperty[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IProperty'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IProperty this[int index] {
			get {
				return ((IProperty)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IProperty'/> with the specified value to the
		///    <see cref='.IPropertyCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IProperty'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IPropertyCollection.AddRange'/>
		public int Add(IProperty value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IPropertyCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IProperty'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IPropertyCollection.Add'/>
		public void AddRange(IProperty[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IPropertyCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IPropertyCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IPropertyCollection.Add'/>
		public void AddRange(PropertyCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IPropertyCollection'/> contains the specified <see cref='.IProperty'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IProperty'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IProperty'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IPropertyCollection.IndexOf'/>
		public bool Contains(IProperty value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IPropertyCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IPropertyCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IPropertyCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IProperty[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IProperty'/> in
		///       the <see cref='.IPropertyCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IProperty'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IProperty'/> of <paramref name='value'/> in the
		/// <see cref='.IPropertyCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IPropertyCollection.Contains'/>
		public int IndexOf(IProperty value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IProperty'/> into the <see cref='.IPropertyCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IProperty'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IPropertyCollection.Add'/>
		public void Insert(int index, IProperty value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IPropertyCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IPropertyEnumerator GetEnumerator() {
			return new IPropertyEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IProperty'/> from the
		///    <see cref='.IPropertyCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IProperty'/> to remove from the <see cref='.IPropertyCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IProperty value) {
			List.Remove(value);
		}
		
		public class IPropertyEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IPropertyEnumerator(PropertyCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IProperty Current {
				get {
					return ((IProperty)(baseEnumerator.Current));
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
