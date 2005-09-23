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
	///       A collection that stores <see cref="IWorkspaceWindow"/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref="WorkspaceWindowCollection"/>
	[Serializable()]
	public class WorkbenchWindowCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="WorkspaceWindowCollection"/>.
		///    </para>
		/// </summary>
		public WorkbenchWindowCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="WorkspaceWindowCollection"/> based on another <see cref="WorkspaceWindowCollection"/>.
		///    </para>
		/// </summary>
		/// <param name="value">
		///       A <see cref="WorkspaceWindowCollection"/> from which the contents are copied
		/// </param>
		public WorkbenchWindowCollection(WorkbenchWindowCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="WorkspaceWindowCollection"/> containing any array of <see cref="IWorkspaceWindow"/> objects.
		///    </para>
		/// </summary>
		/// <param name="value">
		///       A array of <see cref="IWorkspaceWindow"/> objects with which to intialize the collection
		/// </param>
		public WorkbenchWindowCollection(IWorkbenchWindow[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref="IWorkspaceWindow"/>.</para>
		/// </summary>
		/// <param name="index"><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is outside the valid range of indexes for the collection.</exception>
		public IWorkbenchWindow this[int index] {
			get {
				return ((IWorkbenchWindow)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref="IWorkspaceWindow"/> with the specified value to the
		///    <see cref="WorkspaceWindowCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="IWorkspaceWindow"/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref="WorkspaceWindowCollection.AddRange"/>
		public int Add(IWorkbenchWindow value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref="WorkspaceWindowCollection"/>.</para>
		/// </summary>
		/// <param name="value">
		///    An array of type <see cref="IWorkspaceWindow"/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref="WorkspaceWindowCollection.Add"/>
		public void AddRange(IWorkbenchWindow[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref="WorkspaceWindowCollection"/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name="value">
		///    A <see cref="WorkspaceWindowCollection"/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref="WorkspaceWindowCollection.Add"/>
		public void AddRange(WorkbenchWindowCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref="WorkspaceWindowCollection"/> contains the specified <see cref="IWorkspaceWindow"/>.</para>
		/// </summary>
		/// <param name="value">The <see cref="IWorkspaceWindow"/> to locate.</param>
		/// <returns>
		/// <para><see langword="true"/> if the <see cref="IWorkspaceWindow"/> is contained in the collection;
		///   otherwise, <see langword="false"/>.</para>
		/// </returns>
		/// <seealso cref="WorkspaceWindowCollection.IndexOf"/>
		public bool Contains(IWorkbenchWindow value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref="WorkspaceWindowCollection"/> values to a one-dimensional <see cref="System.Array"/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name="array"><para>The one-dimensional <see cref="System.Array"/> that is the destination of the values copied from <see cref="WorkspaceWindowCollection"/> .</para></param>
		/// <param name="index">The index in <paramref name="array"/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref="System.ArgumentException"><para><paramref name="array"/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref="WorkspaceWindowCollection"/> is greater than the available space between <paramref name="arrayIndex"/> and the end of <paramref name="array"/>.</para></exception>
		/// <exception cref="System.ArgumentNullException"><paramref name="array"/> is <see langword="null"/>. </exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than <paramref name="array"/>"s lowbound. </exception>
		/// <seealso cref="System.Array"/>
		public void CopyTo(IWorkbenchWindow[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref="IWorkspaceWindow"/> in
		///       the <see cref="WorkspaceWindowCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="IWorkspaceWindow"/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref="IWorkspaceWindow"/> of <paramref name="value"/> in the
		/// <see cref="WorkspaceWindowCollection"/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref="WorkspaceWindowCollection.Contains"/>
		public int IndexOf(IWorkbenchWindow value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref="IWorkspaceWindow"/> into the <see cref="WorkspaceWindowCollection"/> at the specified index.</para>
		/// </summary>
		/// <param name="index">The zero-based index where <paramref name="value"/> should be inserted.</param>
		/// <param name=" value">The <see cref="IWorkspaceWindow"/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref="WorkspaceWindowCollection.Add"/>
		public void Insert(int index, IWorkbenchWindow value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref="WorkspaceWindowCollection"/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref="System.Collections.IEnumerator"/>
		public new IWorkspaceWindowEnumerator GetEnumerator() {
			return new IWorkspaceWindowEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref="IWorkspaceWindow"/> from the
		///    <see cref="WorkspaceWindowCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="IWorkspaceWindow"/> to remove from the <see cref="WorkspaceWindowCollection"/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref="System.ArgumentException"><paramref name="value"/> is not found in the Collection. </exception>
		public void Remove(IWorkbenchWindow value) {
			List.Remove(value);
		}
		
		/// <summary>
		/// Default enumerator.
		/// </summary>
		public class IWorkspaceWindowEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			/// <summary>
			/// Creates a new instance.
			/// </summary>
			public IWorkspaceWindowEnumerator(WorkbenchWindowCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			/// <summary>
			/// Returns the current object.
			/// </summary>
			public IWorkbenchWindow Current {
				get {
					return ((IWorkbenchWindow)(baseEnumerator.Current));
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
			public bool MoveNext() {
				return baseEnumerator.MoveNext();
			}
			
			bool IEnumerator.MoveNext() {
				return baseEnumerator.MoveNext();
			}
			
			/// <summary>
			/// Resets this enumerator.
			/// </summary>
			public void Reset() {
				baseEnumerator.Reset();
			}
			
			void IEnumerator.Reset() {
				baseEnumerator.Reset();
			}
		}
	}
}
