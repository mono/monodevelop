// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IMethod"/> that resolves an unresolved method.
	/// </summary>
	public class DefaultResolvedMethod : AbstractResolvedMember, IMethod
	{
		IUnresolvedMethod[] parts;
		
		public DefaultResolvedMethod(DefaultUnresolvedMethod unresolved, ITypeResolveContext parentContext)
			: this(unresolved, parentContext, unresolved.IsExtensionMethod)
		{
		}
		
		public DefaultResolvedMethod(IUnresolvedMethod unresolved, ITypeResolveContext parentContext, bool isExtensionMethod)
			: base(unresolved, parentContext)
		{
			this.Parameters = unresolved.Parameters.CreateResolvedParameters(context);
			this.ReturnTypeAttributes = unresolved.ReturnTypeAttributes.CreateResolvedAttributes(parentContext);
			this.TypeParameters = unresolved.TypeParameters.CreateResolvedTypeParameters(context);
			this.IsExtensionMethod = isExtensionMethod;
		}

		class ListOfLists<T> : IList<T>
		{
			List<IList<T>> lists =new List<IList<T>> ();

			public void AddList(IList<T> list)
			{
				lists.Add (list);
			}

			#region IEnumerable implementation
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
			#endregion

			#region IEnumerable implementation
			public IEnumerator<T> GetEnumerator ()
			{
				for (int i = 0; i < this.Count; i++) {
					yield return this[i];
				}
			}
			#endregion

			#region ICollection implementation
			public void Add (T item)
			{
				throw new NotSupportedException();
			}

			public void Clear ()
			{
				throw new NotSupportedException();
			}

			public bool Contains (T item)
			{
				var comparer = EqualityComparer<T>.Default;
				for (int i = 0; i < this.Count; i++) {
					if (comparer.Equals(this[i], item))
						return true;
				}
				return false;
			}

			public void CopyTo (T[] array, int arrayIndex)
			{
				for (int i = 0; i < Count; i++) {
					array[arrayIndex + i] = this[i];
				}
			}

			public bool Remove (T item)
			{
				throw new NotSupportedException();
			}

			public int Count {
				get {
					return lists.Sum (l => l.Count);
				}
			}

			public bool IsReadOnly {
				get {
					return true;
				}
			}
			#endregion

			#region IList implementation
			public int IndexOf (T item)
			{
				var comparer = EqualityComparer<T>.Default;
				for (int i = 0; i < this.Count; i++) {
					if (comparer.Equals(this[i], item))
						return i;
				}
				return -1;
			}

			public void Insert (int index, T item)
			{
				throw new NotSupportedException();
			}

			public void RemoveAt (int index)
			{
				throw new NotSupportedException();
			}

			public T this[int index] {
				get {
					foreach (var list in lists){
						if (index < list.Count)
							return list[index];
						index -=list.Count;
					}
					throw new IndexOutOfRangeException ();
				}
				set {
					throw new NotSupportedException();
				}
			}
			#endregion
		}

		public static DefaultResolvedMethod CreateFromMultipleParts(IUnresolvedMethod[] parts, ITypeResolveContext[] contexts, bool isExtensionMethod)
		{
			DefaultResolvedMethod method = new DefaultResolvedMethod(parts[0], contexts[0], isExtensionMethod);
			method.parts = parts;
			if (parts.Length > 1) {
				var attrs = new ListOfLists <IAttribute>();
				for (int i = 0; i < parts.Length; i++) {
					attrs.AddList (parts[i].Attributes.CreateResolvedAttributes(contexts[i]));
				}
				method.Attributes = attrs;
			}
			return method;
		}
		
		public IList<IParameter> Parameters { get; private set; }
		public IList<IAttribute> ReturnTypeAttributes { get; private set; }
		public IList<ITypeParameter> TypeParameters { get; private set; }
		
		public bool IsExtensionMethod { get; private set; }
		
		public IList<IUnresolvedMethod> Parts {
			get {
				return parts ?? new IUnresolvedMethod[] { (IUnresolvedMethod)unresolved };
			}
		}
		
		public bool IsConstructor {
			get { return ((IUnresolvedMethod)unresolved).IsConstructor; }
		}
		
		public bool IsDestructor {
			get { return ((IUnresolvedMethod)unresolved).IsDestructor; }
		}
		
		public bool IsOperator {
			get { return ((IUnresolvedMethod)unresolved).IsOperator; }
		}
		
		public override IMemberReference ToMemberReference()
		{
			var declTypeRef = this.DeclaringType.ToTypeReference();
			if (IsExplicitInterfaceImplementation && ImplementedInterfaceMembers.Count == 1) {
				return new ExplicitInterfaceImplementationMemberReference(declTypeRef, ImplementedInterfaceMembers[0].ToMemberReference());
			} else {
				return new DefaultMemberReference(
					this.EntityType, declTypeRef, this.Name, this.TypeParameters.Count,
					this.Parameters.Select(p => p.Type.ToTypeReference()).ToList());
			}
		}
	}
}
