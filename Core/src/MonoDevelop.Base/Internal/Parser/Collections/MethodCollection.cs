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
	///       A collection that stores <see cref='.IMethod'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='.IMethodCollection'/>
	[Serializable()]
	public class MethodCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IMethodCollection'/>.
		///    </para>
		/// </summary>
		public MethodCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IMethodCollection'/> based on another <see cref='.IMethodCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='.IMethodCollection'/> from which the contents are copied
		/// </param>
		public MethodCollection(MethodCollection value) {
			this.AddRange(value);
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='.IMethodCollection'/> containing any array of <see cref='.IMethod'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='.IMethod'/> objects with which to intialize the collection
		/// </param>
		public MethodCollection(IMethod[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='.IMethod'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public IMethod this[int index] {
			get {
				return ((IMethod)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref='.IMethod'/> with the specified value to the
		///    <see cref='.IMethodCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IMethod'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='.IMethodCollection.AddRange'/>
		public int Add(IMethod value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='.IMethodCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='.IMethod'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IMethodCollection.Add'/>
		public void AddRange(IMethod[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='.IMethodCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='.IMethodCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='.IMethodCollection.Add'/>
		public void AddRange(MethodCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='.IMethodCollection'/> contains the specified <see cref='.IMethod'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IMethod'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='.IMethod'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='.IMethodCollection.IndexOf'/>
		public bool Contains(IMethod value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref='.IMethodCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='.IMethodCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='.IMethodCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(IMethod[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref='.IMethod'/> in
		///       the <see cref='.IMethodCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IMethod'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='.IMethod'/> of <paramref name='value'/> in the
		/// <see cref='.IMethodCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='.IMethodCollection.Contains'/>
		public int IndexOf(IMethod value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref='.IMethod'/> into the <see cref='.IMethodCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='.IMethod'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='.IMethodCollection.Add'/>
		public void Insert(int index, IMethod value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='.IMethodCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new IMethodEnumerator GetEnumerator() {
			return new IMethodEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref='.IMethod'/> from the
		///    <see cref='.IMethodCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='.IMethod'/> to remove from the <see cref='.IMethodCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(IMethod value) {
			List.Remove(value);
		}
		
		public class IMethodEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			public IMethodEnumerator(MethodCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			public IMethod Current {
				get {
					return ((IMethod)(baseEnumerator.Current));
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
