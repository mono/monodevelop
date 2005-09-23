// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	///     <para>
	///       A collection that stores <see cref="ICondition"/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref="ConditionCollection"/>
	[Serializable()]
	public class ConditionCollection : CollectionBase {
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="ConditionCollection"/>.
		///    </para>
		/// </summary>
		public ConditionCollection() {
		}
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="ConditionCollection"/> based on another <see cref="ConditionCollection"/>.
		///    </para>
		/// </summary>
		/// <param name="value">
		///       A <see cref="ConditionCollection"/> from which the contents are copied
		/// </param>
		public ConditionCollection(ConditionCollection value) {
			this.AddRange(value);
		}
		
		public ConditionFailedAction GetCurrentConditionFailedAction(object caller)
		{
			ConditionFailedAction action = ConditionFailedAction.Nothing;
			foreach (ICondition condition in this) {
				if (!condition.IsValid(caller)) {
					if (condition.Action == ConditionFailedAction.Disable) {
						action = ConditionFailedAction.Disable;
					} else {
						return action = ConditionFailedAction.Exclude;
					}
				}
			}
			return action;
		}
		
		
		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref="ConditionCollection"/> containing any array of <see cref="ICondition"/> objects.
		///    </para>
		/// </summary>
		/// <param name="value">
		///       A array of <see cref="ICondition"/> objects with which to intialize the collection
		/// </param>
		public ConditionCollection(ICondition[] value) {
			this.AddRange(value);
		}
		
		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref="ICondition"/>.</para>
		/// </summary>
		/// <param name="index"><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is outside the valid range of indexes for the collection.</exception>
		public ICondition this[int index] {
			get {
				return ((ICondition)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		/// <summary>
		///    <para>Adds a <see cref="ICondition"/> with the specified value to the
		///    <see cref="ConditionCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="ICondition"/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref="ConditionCollection.AddRange"/>
		public int Add(ICondition value) {
			return List.Add(value);
		}
		
		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref="ConditionCollection"/>.</para>
		/// </summary>
		/// <param name="value">
		///    An array of type <see cref="ICondition"/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref="ConditionCollection.Add"/>
		public void AddRange(ICondition[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref="ConditionCollection"/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name="value">
		///    A <see cref="ConditionCollection"/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref="ConditionCollection.Add"/>
		public void AddRange(ConditionCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}
		
		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref="ConditionCollection"/> contains the specified <see cref="ICondition"/>.</para>
		/// </summary>
		/// <param name="value">The <see cref="ICondition"/> to locate.</param>
		/// <returns>
		/// <para><see langword="true"/> if the <see cref="ICondition"/> is contained in the collection;
		///   otherwise, <see langword="false"/>.</para>
		/// </returns>
		/// <seealso cref="ConditionCollection.IndexOf"/>
		public bool Contains(ICondition value) {
			return List.Contains(value);
		}
		
		/// <summary>
		/// <para>Copies the <see cref="ConditionCollection"/> values to a one-dimensional <see cref="System.Array"/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name="array"><para>The one-dimensional <see cref="System.Array"/> that is the destination of the values copied from <see cref="ConditionCollection"/> .</para></param>
		/// <param name="index">The index in <paramref name="array"/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref="System.ArgumentException"><para><paramref name="array"/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref="ConditionCollection"/> is greater than the available space between <paramref name="arrayIndex"/> and the end of <paramref name="array"/>.</para></exception>
		/// <exception cref="System.ArgumentNullException"><paramref name="array"/> is <see langword="null"/>. </exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than <paramref name="array"/>"s lowbound. </exception>
		/// <seealso cref="System.Array"/>
		public void CopyTo(ICondition[] array, int index) {
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    <para>Returns the index of a <see cref="ICondition"/> in
		///       the <see cref="ConditionCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="ICondition"/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref="ICondition"/> of <paramref name="value"/> in the
		/// <see cref="ConditionCollection"/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref="ConditionCollection.Contains"/>
		public int IndexOf(ICondition value) {
			return List.IndexOf(value);
		}
		
		/// <summary>
		/// <para>Inserts a <see cref="ICondition"/> into the <see cref="ConditionCollection"/> at the specified index.</para>
		/// </summary>
		/// <param name="index">The zero-based index where <paramref name="value"/> should be inserted.</param>
		/// <param name=" value">The <see cref="ICondition"/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref="ConditionCollection.Add"/>
		public void Insert(int index, ICondition value) {
			List.Insert(index, value);
		}
		
		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref="ConditionCollection"/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref="System.Collections.IEnumerator"/>
		public new IConditionEnumerator GetEnumerator() {
			return new IConditionEnumerator(this);
		}
		
		/// <summary>
		///    <para> Removes a specific <see cref="ICondition"/> from the
		///    <see cref="ConditionCollection"/> .</para>
		/// </summary>
		/// <param name="value">The <see cref="ICondition"/> to remove from the <see cref="ConditionCollection"/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref="System.ArgumentException"><paramref name="value"/> is not found in the Collection. </exception>
		public void Remove(ICondition value) {
			List.Remove(value);
		}
		
		/// <summary>
		/// Default enumerator.
		/// </summary>
		public class IConditionEnumerator : object, IEnumerator {
			
			private IEnumerator baseEnumerator;
			
			private IEnumerable temp;
			
			/// <summary>
			/// Creates a new instance.
			/// </summary>
			public IConditionEnumerator(ConditionCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			/// <summary>
			/// Returns the current object.
			/// </summary>
			public ICondition Current {
				get {
					return ((ICondition)(baseEnumerator.Current));
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
