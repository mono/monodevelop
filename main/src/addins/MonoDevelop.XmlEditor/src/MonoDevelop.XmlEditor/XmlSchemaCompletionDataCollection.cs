//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//

using MonoDevelop.Projects.Gui.Completion;
using System;
using System.Collections;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	///   A collection that stores <see cref='XmlSchemaCompletionData'/> objects.
	/// </summary>
	[Serializable()]
	public class XmlSchemaCompletionDataCollection : CollectionBase {
		
		/// <summary>
		///   Initializes a new instance of <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		public XmlSchemaCompletionDataCollection()
		{
		}
		
		/// <summary>
		///   Initializes a new instance of <see cref='XmlSchemaCompletionDataCollection'/> based on another <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		/// <param name='val'>
		///   A <see cref='XmlSchemaCompletionDataCollection'/> from which the contents are copied
		/// </param>
		public XmlSchemaCompletionDataCollection(XmlSchemaCompletionDataCollection val)
		{
			this.AddRange(val);
		}
		
		/// <summary>
		///   Initializes a new instance of <see cref='XmlSchemaCompletionDataCollection'/> containing any array of <see cref='XmlSchemaCompletionData'/> objects.
		/// </summary>
		/// <param name='val'>
		///       A array of <see cref='XmlSchemaCompletionData'/> objects with which to intialize the collection
		/// </param>
		public XmlSchemaCompletionDataCollection(XmlSchemaCompletionData[] val)
		{
			this.AddRange(val);
		}
		
		/// <summary>
		///   Represents the entry at the specified index of the <see cref='XmlSchemaCompletionData'/>.
		/// </summary>
		/// <param name='index'>The zero-based index of the entry to locate in the collection.</param>
		/// <value>The entry at the specified index of the collection.</value>
		/// <exception cref='ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public XmlSchemaCompletionData this[int index] {
			get {
				return ((XmlSchemaCompletionData)(List[index]));
			}
			set {
				List[index] = value;
			}
		}
		
		public ICompletionData[] GetNamespaceCompletionData()
		{
			ArrayList completionItems = new ArrayList();
			
			foreach (XmlSchemaCompletionData schema in this) {
				XmlCompletionData completionData = new XmlCompletionData(schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri);
				completionItems.Add(completionData);
			}
			
			return (ICompletionData[])completionItems.ToArray(typeof(ICompletionData));
		}
		
		/// <summary>
		///   Represents the <see cref='XmlSchemaCompletionData'/> entry with the specified namespace URI.
		/// </summary>
		/// <param name='namespaceUri'>The schema's namespace URI.</param>
		/// <value>The entry with the specified namespace URI.</value>
		public XmlSchemaCompletionData this[string namespaceUri] {
			get {
				return GetItem(namespaceUri);
			}
		}		
		
		/// <summary>
		///   Adds a <see cref='XmlSchemaCompletionData'/> with the specified value to the 
		///   <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		/// <param name='val'>The <see cref='XmlSchemaCompletionData'/> to add.</param>
		/// <returns>The index at which the new element was inserted.</returns>
		/// <seealso cref='XmlSchemaCompletionDataCollection.AddRange'/>
		public int Add(XmlSchemaCompletionData val)
		{
			return List.Add(val);
		}
		
		/// <summary>
		///   Copies the elements of an array to the end of the <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		/// <param name='val'>
		///    An array of type <see cref='XmlSchemaCompletionData'/> containing the objects to add to the collection.
		/// </param>
		/// <seealso cref='XmlSchemaCompletionDataCollection.Add'/>
		public void AddRange(XmlSchemaCompletionData[] val)
		{
			for (int i = 0; i < val.Length; i++) {
				this.Add(val[i]);
			}
		}
		
		/// <summary>
		///   Adds the contents of another <see cref='XmlSchemaCompletionDataCollection'/> to the end of the collection.
		/// </summary>
		/// <param name='val'>
		///    A <see cref='XmlSchemaCompletionDataCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <seealso cref='XmlSchemaCompletionDataCollection.Add'/>
		public void AddRange(XmlSchemaCompletionDataCollection val)
		{
			for (int i = 0; i < val.Count; i++)
			{
				this.Add(val[i]);
			}
		}
		
		/// <summary>
		///   Gets a value indicating whether the 
		///    <see cref='XmlSchemaCompletionDataCollection'/> contains the specified <see cref='XmlSchemaCompletionData'/>.
		/// </summary>
		/// <param name='val'>The <see cref='XmlSchemaCompletionData'/> to locate.</param>
		/// <returns>
		/// <see langword='true'/> if the <see cref='XmlSchemaCompletionData'/> is contained in the collection; 
		///   otherwise, <see langword='false'/>.
		/// </returns>
		/// <seealso cref='XmlSchemaCompletionDataCollection.IndexOf'/>
		public bool Contains(XmlSchemaCompletionData val)
		{
			return List.Contains(val);
		}
		
		/// <summary>
		///   Copies the <see cref='XmlSchemaCompletionDataCollection'/> values to a one-dimensional <see cref='Array'/> instance at the 
		///    specified index.
		/// </summary>
		/// <param name='array'>The one-dimensional <see cref='Array'/> that is the destination of the values copied from <see cref='XmlSchemaCompletionDataCollection'/>.</param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <exception cref='ArgumentException'>
		///   <para><paramref name='array'/> is multidimensional.</para>
		///   <para>-or-</para>
		///   <para>The number of elements in the <see cref='XmlSchemaCompletionDataCollection'/> is greater than
		///         the available space between <paramref name='arrayIndex'/> and the end of
		///         <paramref name='array'/>.</para>
		/// </exception>
		/// <exception cref='ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='Array'/>
		public void CopyTo(XmlSchemaCompletionData[] array, int index)
		{
			List.CopyTo(array, index);
		}
		
		/// <summary>
		///    Returns the index of a <see cref='XmlSchemaCompletionData'/> in 
		///       the <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		/// <param name='val'>The <see cref='XmlSchemaCompletionData'/> to locate.</param>
		/// <returns>
		///   The index of the <see cref='XmlSchemaCompletionData'/> of <paramref name='val'/> in the 
		///   <see cref='XmlSchemaCompletionDataCollection'/>, if found; otherwise, -1.
		/// </returns>
		/// <seealso cref='XmlSchemaCompletionDataCollection.Contains'/>
		public int IndexOf(XmlSchemaCompletionData val)
		{
			return List.IndexOf(val);
		}
		
		/// <summary>
		///   Inserts a <see cref='XmlSchemaCompletionData'/> into the <see cref='XmlSchemaCompletionDataCollection'/> at the specified index.
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='val'/> should be inserted.</param>
		/// <param name='val'>The <see cref='XmlSchemaCompletionData'/> to insert.</param>
		/// <seealso cref='XmlSchemaCompletionDataCollection.Add'/>
		public void Insert(int index, XmlSchemaCompletionData val)
		{
			List.Insert(index, val);
		}
		
		/// <summary>
		///  Returns an enumerator that can iterate through the <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		/// <seealso cref='IEnumerator'/>
		public new XmlSchemaCompletionDataEnumerator GetEnumerator()
		{
			return new XmlSchemaCompletionDataEnumerator(this);
		}
		
		/// <summary>
		///   Removes a specific <see cref='XmlSchemaCompletionData'/> from the <see cref='XmlSchemaCompletionDataCollection'/>.
		/// </summary>
		/// <param name='val'>The <see cref='XmlSchemaCompletionData'/> to remove from the <see cref='XmlSchemaCompletionDataCollection'/>.</param>
		/// <exception cref='ArgumentException'><paramref name='val'/> is not found in the Collection.</exception>
		public void Remove(XmlSchemaCompletionData val)
		{
			List.Remove(val);
		}
		
		/// <summary>
		///   Enumerator that can iterate through a XmlSchemaCompletionDataCollection.
		/// </summary>
		/// <seealso cref='IEnumerator'/>
		/// <seealso cref='XmlSchemaCompletionDataCollection'/>
		/// <seealso cref='XmlSchemaCompletionData'/>
		public class XmlSchemaCompletionDataEnumerator : IEnumerator
		{
			IEnumerator baseEnumerator;
			IEnumerable temp;
			
			/// <summary>
			///   Initializes a new instance of <see cref='XmlSchemaCompletionDataEnumerator'/>.
			/// </summary>
			public XmlSchemaCompletionDataEnumerator(XmlSchemaCompletionDataCollection mappings)
			{
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}
			
			/// <summary>
			///   Gets the current <see cref='XmlSchemaCompletionData'/> in the <seealso cref='XmlSchemaCompletionDataCollection'/>.
			/// </summary>
			public XmlSchemaCompletionData Current {
				get {
					return ((XmlSchemaCompletionData)(baseEnumerator.Current));
				}
			}
			
			object IEnumerator.Current {
				get {
					return baseEnumerator.Current;
				}
			}
			
			/// <summary>
			///   Advances the enumerator to the next <see cref='XmlSchemaCompletionData'/> of the <see cref='XmlSchemaCompletionDataCollection'/>.
			/// </summary>
			public bool MoveNext()
			{
				return baseEnumerator.MoveNext();
			}
			
			/// <summary>
			///   Sets the enumerator to its initial position, which is before the first element in the <see cref='XmlSchemaCompletionDataCollection'/>.
			/// </summary>
			public void Reset()
			{
				baseEnumerator.Reset();
			}
		}
		
		XmlSchemaCompletionData GetItem(string namespaceUri)
		{
			XmlSchemaCompletionData matchedItem = null;
			
			foreach(XmlSchemaCompletionData item in this)
			{
				if (item.NamespaceUri == namespaceUri) {
					matchedItem = item;
					break;
				}
			}
			
			return matchedItem;
		}
	}
}
