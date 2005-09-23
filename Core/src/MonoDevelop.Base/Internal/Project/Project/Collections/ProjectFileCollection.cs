// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.IO;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref='.ProjectFile'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.ProjectFileCollection'/>
	[Serializable()]
	public class ProjectFileCollection : CollectionBase {
	
		Project project;
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.ProjectFileCollection'/>.
		///    </para>
		/// </summary>
		public ProjectFileCollection ()
		{
		}
		
		public ProjectFileCollection (Project project)
		{
			this.project = project;
		}
		
		public ProjectFile GetFile (string fileName)
		{
			if (fileName == null) return null;
			foreach (ProjectFile file in this) {
				if (file.Name == fileName)
					return file;
			}
			return null;
		}
		
		public ProjectFile[] GetFilesInPath (string path)
		{
			path = path + Path.DirectorySeparatorChar;
			ArrayList files = new ArrayList ();
			
			foreach (ProjectFile file in List) {
				if ((file.Name + Path.DirectorySeparatorChar).StartsWith (path))
					files.Add (file);
			}
			return (ProjectFile[]) files.ToArray (typeof(ProjectFile));
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.ProjectFile'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public ProjectFile this[int index] {
			get {
				return ((ProjectFile)(List[index]));
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.ProjectFile'/> with the specified value to the
		///    <see cref='.ProjectFileCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectFile'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.ProjectFileCollection.AddRange'/>
		public int Add(ProjectFile value) {
			int i = List.Add(value);
			if (project != null) {
				if (value.Project != null)
					throw new InvalidOperationException ("ProjectFile already belongs to a project");
				value.SetProject (project);
				project.NotifyFileAddedToProject (value);
			}
			return i;
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.ProjectFileCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.ProjectFile'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.ProjectFileCollection.Add'/>
		public void AddRange(ProjectFile[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.ProjectFileCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.ProjectFileCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.ProjectFileCollection.Add'/>
		public void AddRange(ProjectFileCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.ProjectFileCollection'/> contains the specified <see cref='.ProjectFile'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectFile'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.ProjectFile'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.ProjectFileCollection.IndexOf'/>
		public bool Contains(ProjectFile value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.ProjectFileCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.ProjectFileCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.ProjectFileCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(ProjectFile[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.ProjectFile'/> in
		///       the <see cref='.ProjectFileCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectFile'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.ProjectFile'/> of <paramref name='value'/> in the
		/// <see cref='.ProjectFileCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.ProjectFileCollection.Contains'/>
		public int IndexOf(ProjectFile value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.ProjectFile'/> into the <see cref='.ProjectFileCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.ProjectFile'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.ProjectFileCollection.Add'/>
		public void Insert(int index, ProjectFile value) {
			List.Insert(index, value);
			if (project != null) {
				if (value.Project != null)
					throw new InvalidOperationException ("ProjectFile already belongs to a project");
				value.SetProject (project);
				project.NotifyFileAddedToProject (value);
			}
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.ProjectFileCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new ProjectFileEnumerator GetEnumerator() {
			return new ProjectFileEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.ProjectFile'/> from the
		///    <see cref='.ProjectFileCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.ProjectFile'/> to remove from the <see cref='.ProjectFileCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(ProjectFile value) {
			List.Remove (value);
			if (project != null) {
				value.SetProject (null);
				project.NotifyFileRemovedFromProject (value);
			}
		}
		
		public class ProjectFileEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public ProjectFileEnumerator(ProjectFileCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public ProjectFile Current {
				get {
					return ((ProjectFile)(baseEnumerator.Current));
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
