// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref="AddIn"/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref="AddInCollection"/>
	[Serializable()]
	public class AddInCollection : CollectionBase 
	{
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="AddInCollection"/>.
		///    </para>
		/// </summary>
		public AddInCollection() 
		{
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref="AddIn"/>.</para>
		/// </summary>
		/// <param name="index"><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is outside the valid range of indexes for the collection.</exception>
		public AddIn this[int index] 
		{
			get {
				return ((AddIn)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		public AddIn this [string name] 
		{
			get {
				foreach (AddIn addin in List)
					if (addin.Name == name)
						return addin;
				return null;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref="AddIn"/> with the specified value to the
		///    <see cref="AddInCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="AddIn"/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		public int Add(AddIn value) 
		{
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref="AddInCollection"/> contains the specified <see cref="AddIn"/>.</para>
		/// </summary>
		/// <param name="value">The <see cref="AddIn"/> to locate.</param>
		/// <returns>
		/// <para><see langword="true"/> if the <see cref="AddIn"/> is contained in the collection;
		///   otherwise, <see langword="false"/>.</para>
		/// </returns>
		/// <seealso cref="AddInCollection.IndexOf"/>
		public bool Contains(AddIn value) 
		{
			return List.Contains(value);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref="AddIn"/> in
		///       the <see cref="AddInCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="AddIn"/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref="AddIn"/> of <paramref name="value"/> in the
		/// <see cref="AddInCollection"/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref="AddInCollection.Contains"/>
		public int IndexOf(AddIn value) 
		{
			return List.IndexOf(value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref="AddInCollection"/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref="System.Collections.IEnumerator"/>
		public new AddInEnumerator GetEnumerator() 
		{
			return new AddInEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref="AddIn"/> from the
		///    <see cref="AddInCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="AddIn"/> to remove from the <see cref="AddInCollection"/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref="System.ArgumentException"><paramref name="value"/> is not found in the Collection. </exception>
		public void Remove(AddIn value) 
		{
			List.Remove(value);
		}
		
		/// <summary>
		/// Default enumerator.
		/// </summary>
		public class AddInEnumerator : object, IEnumerator 
		{
			IEnumerator baseEnumerator;
			IEnumerable temp;
			
			/// <summary>
			/// Creates a new instance.
			/// </summary>
			public AddInEnumerator(AddInCollection mappings) 
			{
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			/// <summary>
			/// Returns the current object.
			/// </summary>
			public AddIn Current {
				get {
					return ((AddIn)(baseEnumerator.Current));
				}
			}
			
			object IEnumerator.Current {
				get {
					return baseEnumerator.Current;
				}
			}
			
			/// <summary>
			/// Moves to the next object.
			/// </summary>
			public bool MoveNext() 
			{
				return baseEnumerator.MoveNext();
			}
			
			bool IEnumerator.MoveNext() 
			{
				return baseEnumerator.MoveNext();
			}
			
			/// <summary>
			/// Resets this enumerator.
			/// </summary>
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
