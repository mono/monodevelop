// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='ISelection'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='SelectionCollection'/>
	[Serializable()]
	public class SelectionCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='SelectionCollection'/>.
		///    </para>
		/// </summary>
		public SelectionCollection() 
		{
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='SelectionCollection'/> based on another <see cref='SelectionCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='SelectionCollection'/> from which the contents are copied
		/// </param>
		public SelectionCollection(SelectionCollection value) 
		{
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='SelectionCollection'/> containing any array of <see cref='ISelection'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='ISelection'/> objects with which to intialize the collection
		/// </param>
		public SelectionCollection(ISelection[] value) 
		{
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='ISelection'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public ISelection this[int index] 
		{
			get {
				return ((ISelection)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='ISelection'/> with the specified value to the
		///    <see cref='SelectionCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='ISelection'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='SelectionCollection.AddRange'/>
		public int Add(ISelection value) 
		{
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='SelectionCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='ISelection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='SelectionCollection.Add'/>
		public void AddRange(ISelection[] value) 
		{
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='SelectionCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='SelectionCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='SelectionCollection.Add'/>
		public void AddRange(SelectionCollection value) 
		{
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='SelectionCollection'/> contains the specified <see cref='ISelection'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='ISelection'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='ISelection'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='SelectionCollection.IndexOf'/>
		public bool Contains(ISelection value) 
		{
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='SelectionCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='SelectionCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='SelectionCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(ISelection[] array, int index) 
		{
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='ISelection'/> in
		///       the <see cref='SelectionCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='ISelection'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='ISelection'/> of <paramref name='value'/> in the
		/// <see cref='SelectionCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='SelectionCollection.Contains'/>
		public int IndexOf(ISelection value) 
		{
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='ISelection'/> into the <see cref='SelectionCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='ISelection'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='SelectionCollection.Add'/>
		public void Insert(int index, ISelection value) 
		{
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='SelectionCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new ISelectionEnumerator GetEnumerator() 
		{
			return new ISelectionEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='ISelection'/> from the
		///    <see cref='SelectionCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='ISelection'/> to remove from the <see cref='SelectionCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(ISelection value) 
		{
			List.Remove(value);
		}
		
		/// <summary>
		/// used internally
		/// </summary>
		public class ISelectionEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			/// <summary>
			/// Creates a new instance of <see cref="ISelectionEnumerator"/>
			/// </summary>
			public ISelectionEnumerator(SelectionCollection mappings) 
			{
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			/// <remarks>
			/// </remarks>
			public ISelection Current {
				get {
					return ((ISelection)(baseEnumerator.Current));
				}
			}
			
			object IEnumerator.Current {
				get {
					return baseEnumerator.Current;
				}
			}
			
			/// <remarks>
			/// </remarks>
			public bool MoveNext() 
			{
				return baseEnumerator.MoveNext();
			}
			
			bool IEnumerator.MoveNext() 
			{
				return baseEnumerator.MoveNext();
			}
			
			/// <remarks>
			/// </remarks>
			public void Reset() 
			{
				baseEnumerator.Reset();
			}
			
			void IEnumerator.Reset() 
			{
				baseEnumerator.Reset();
			}
		}
	}
}

