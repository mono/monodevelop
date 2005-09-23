// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='.ProjectReference'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.ProjectReferenceCollection'/>
	[Serializable()]
	public class ProjectReferenceCollection : CollectionBase {
		
		Project project;
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.ProjectReferenceCollection'/>.
		///    </para>
		/// </summary>
		public ProjectReferenceCollection() {
		}
		
		internal void SetProject (Project project)
		{
			this.project = project;
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.ProjectReference'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public ProjectReference this[int index] {
			get {
				return ((ProjectReference)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.ProjectReference'/> with the specified value to the
		///    <see cref='.ProjectReferenceCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectReference'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.ProjectReferenceCollection.AddRange'/>
		public int Add(ProjectReference value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.ProjectReferenceCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.ProjectReference'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.ProjectReferenceCollection.Add'/>
		public void AddRange(ProjectReference[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.ProjectReferenceCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.ProjectReferenceCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.ProjectReferenceCollection.Add'/>
		public void AddRange(ProjectReferenceCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.ProjectReferenceCollection'/> contains the specified <see cref='.ProjectReference'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectReference'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.ProjectReference'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.ProjectReferenceCollection.IndexOf'/>
		public bool Contains(ProjectReference value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.ProjectReferenceCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.ProjectReferenceCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.ProjectReferenceCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(ProjectReference[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.ProjectReference'/> in
		///       the <see cref='.ProjectReferenceCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectReference'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.ProjectReference'/> of <paramref name='value'/> in the
		/// <see cref='.ProjectReferenceCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.ProjectReferenceCollection.Contains'/>
		public int IndexOf(ProjectReference value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.ProjectReference'/> into the <see cref='.ProjectReferenceCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.ProjectReference'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.ProjectReferenceCollection.Add'/>
		public void Insert(int index, ProjectReference value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.ProjectReferenceCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new ProjectReferenceEnumerator GetEnumerator() {
			return new ProjectReferenceEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.ProjectReference'/> from the
		///    <see cref='.ProjectReferenceCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectReference'/> to remove from the <see cref='.ProjectReferenceCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove (ProjectReference value) {
			List.Remove(value);
		}
		
		
		protected override void OnClear()
		{
			if (project != null) {
				ArrayList list = (ArrayList) InnerList.Clone ();
				InnerList.Clear ();
				foreach (ProjectReference pref in list) {
					pref.SetOwnerProject (null);
					project.NotifyReferenceRemovedFromProject (pref);
				}
			}
		}
		
		protected override void OnInsertComplete(int index, object value)
		{
			if (project != null) {
				((ProjectReference) value).SetOwnerProject (project);
				project.NotifyReferenceAddedToProject ((ProjectReference)value);
			}
		}
		
		protected override void OnRemoveComplete(int index, object value)
		{
			if (project != null) {
				((ProjectReference) value).SetOwnerProject (null);
				project.NotifyReferenceRemovedFromProject ((ProjectReference) value);
			}
		}
		
		protected override void OnSet (int index, object oldValue, object newValue)
		{
			if (project != null) {
				((ProjectReference) oldValue).SetOwnerProject (null);
				project.NotifyReferenceRemovedFromProject ((ProjectReference) oldValue);
			}
		}
		
		protected override void OnSetComplete (int index, object oldValue, object newValue)
		{
			if (project != null) {
				((ProjectReference) newValue).SetOwnerProject (project);
				project.NotifyReferenceAddedToProject ((ProjectReference) newValue);
			}
		}
		
		public class ProjectReferenceEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public ProjectReferenceEnumerator(ProjectReferenceCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public ProjectReference Current {
				get {
					return ((ProjectReference)(baseEnumerator.Current));
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
