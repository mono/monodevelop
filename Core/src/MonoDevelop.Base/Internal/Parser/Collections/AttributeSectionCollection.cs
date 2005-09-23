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
	///       A collection that stores <see cref='.AttributeSection'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.AttributeSectionCollection'/>
	[Serializable()]
	public class AttributeSectionCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.AttributeSectionCollection'/>.
		///    </para>
		/// </summary>
		public AttributeSectionCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.AttributeSectionCollection'/> based on another <see cref='.AttributeSectionCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.AttributeSectionCollection'/> from which the contents are copied
		/// </param>
		public AttributeSectionCollection(AttributeSectionCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.AttributeSectionCollection'/> containing any array of <see cref='.AttributeSection'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.AttributeSection'/> objects with which to intialize the collection
		/// </param>
		public AttributeSectionCollection(IAttributeSection[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.AttributeSection'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IAttributeSection this[int index] {
			get {
				return ((IAttributeSection)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.AttributeSection'/> with the specified value to the
		///    <see cref='.AttributeSectionCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.AttributeSection'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.AttributeSectionCollection.AddRange'/>
		public int Add(IAttributeSection value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.AttributeSectionCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.AttributeSection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.AttributeSectionCollection.Add'/>
		public void AddRange(IAttributeSection[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.AttributeSectionCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.AttributeSectionCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.AttributeSectionCollection.Add'/>
		public void AddRange(AttributeSectionCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.AttributeSectionCollection'/> contains the specified <see cref='.AttributeSection'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.AttributeSection'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.AttributeSection'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.AttributeSectionCollection.IndexOf'/>
		public bool Contains(IAttributeSection value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.AttributeSectionCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.AttributeSectionCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.AttributeSectionCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IAttributeSection[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.AttributeSection'/> in
		///       the <see cref='.AttributeSectionCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.AttributeSection'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.AttributeSection'/> of <paramref name='value'/> in the
		/// <see cref='.AttributeSectionCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.AttributeSectionCollection.Contains'/>
		public int IndexOf(IAttributeSection value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.AttributeSection'/> into the <see cref='.AttributeSectionCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.AttributeSection'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.AttributeSectionCollection.Add'/>
		public void Insert(int index, IAttributeSection value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.AttributeSectionCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new AttributeSectionEnumerator GetEnumerator() {
			return new AttributeSectionEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.AttributeSection'/> from the
		///    <see cref='.AttributeSectionCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.AttributeSection'/> to remove from the <see cref='.AttributeSectionCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IAttributeSection value) {
			List.Remove(value);
		}
		
		public class AttributeSectionEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public AttributeSectionEnumerator(AttributeSectionCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IAttributeSection Current {
				get {
					return ((IAttributeSection)(baseEnumerator.Current));
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
