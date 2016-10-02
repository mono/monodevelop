//
// ConditionedPropertyCollection.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ConditionedPropertyCollection
	{
		Dictionary<string, ImmutableList<string>> props = new Dictionary<string, ImmutableList<string>> ();
		Dictionary<KeySet, ImmutableList<ValueSet>> combinedProps = new Dictionary<KeySet, ImmutableList<ValueSet>> (StructEqualityComparer<KeySet>.Instance);

		/// <summary>
		/// A set of strings, which can be compared to other sets ignoring the order.
		/// </summary>
		struct KeySet : IEquatable<KeySet>
		{
			readonly IList<string> keys;

			public KeySet (IList<string> keys)
			{
				this.keys = keys;
			}

			public override bool Equals (object obj)
			{
				if (!(obj is KeySet))
					return false;
				return Equals ((KeySet)obj);
			}

			public bool Equals (KeySet other)
			{
				if (other.keys.Count != keys.Count)
					return false;

				for (int i = 0; i < keys.Count; ++i) {
					if (!other.keys.Contains (keys[i]))
						return false;
				}
				return true;
			}

			public override int GetHashCode ()
			{
				unchecked {
					int r = 0;
					for (int i = 0; i < keys.Count; ++i)
						r ^= keys[i].GetHashCode ();
					return r;
				}
			}
		}

		/// <summary>
		/// A set of key/value pairs
		/// </summary>
		public struct ValueSet : IEquatable<ValueSet>
		{
			internal IList<string> ReferenceKeys;
			readonly internal IList<string> Values;

			/// <summary>
			/// Initializes a new ValueSet
			/// </summary>
			/// <param name="referenceKeys">Array of keys that specifies the order in which values are stored</param>
			/// <param name="names">Array of keys, ordered according to the values parameter</param>
			/// <param name="values">Values of the keys.</param>
			internal ValueSet (IList<string> referenceKeys, IList<string> names, IList<string> values)
			{
				this.ReferenceKeys = referenceKeys;
				this.Values = new string [values.Count];
				if (referenceKeys.Count == 1)
					this.Values [0] = values [0];
				else {
					for (int n = 0; n < names.Count; n++) {
						// Store the values using the order of the reference keys
						int i = referenceKeys.IndexOf (names [n]);
						this.Values [i] = values [n];
					}
				}
			}

			public ValueSet (string [] names, string [] values): this (names, names, values)
			{
			}

			/// <summary>
			/// Gets the value for the given property
			/// </summary>
			public string GetValue (string property)
			{
				if (ReferenceKeys.Count == 1) {
					// Fast path when there is only one property in the set
					if (ReferenceKeys [0] == property)
						return Values [0];
					else
						return null;
				} else {
					int i = ReferenceKeys.IndexOf (property);
					if (i != -1)
						return Values [i];
					else
						return null;
				}
			}

			public override bool Equals (object obj)
			{
				if (!(obj is ValueSet))
					return false;

				return Equals ((ValueSet)obj);
			}

			public bool Equals (ValueSet other)
			{
				if (ReferenceKeys == other.ReferenceKeys) {
					// Fast path, used when both sets are based on the same reference keys
					for (int n = 0; n < Values.Count; n++)
						if (Values [n] != other.Values [n])
							return false;
					return true;
				}

				if (other.ReferenceKeys.Count != ReferenceKeys.Count)
					return false;

				if (other.ReferenceKeys.Count == 1)
					return ReferenceKeys[0] == other.ReferenceKeys[0] && Values [0] == other.Values [0];

				for (int n = 0; n < ReferenceKeys.Count; n++) {
					if (other.GetValue (ReferenceKeys[n]) != Values[n])
						return false;
				}
				return true;
			}

			public override int GetHashCode ()
			{
				unchecked {
					int r = 0;
					for (int i = 0; i < Values.Count; ++i)
						r ^= Values [i].GetHashCode ();
					return r;
				}
			}
		}

		internal void Append (ConditionedPropertyCollection other)
		{
			foreach (var e in other.props) {
				var otherList = e.Value;
				var key = e.Key;
				ImmutableList<string> list;
				if (props.TryGetValue (key, out list)) {
					var lb = list.ToBuilder ();
					foreach (var c in otherList) {
						if (!lb.Contains (c))
							lb.Add (c);
					}
					props [key] = lb.ToImmutableList ();
				} else
					props [key] = otherList;
			}

			foreach (var e in other.combinedProps) {
				var otherList = e.Value;
				var key = e.Key;
				ImmutableList<ValueSet> thisList;
				if (combinedProps.TryGetValue (key, out thisList)) {
					var list = thisList.ToBuilder ();
					foreach (var c in otherList) {
						if (list.IndexOf (c, 0, list.Count, StructEqualityComparer<ValueSet>.Instance) < 0)
							// Create a new ValueSet so that the reference keys of this collection are reused
							list.Add (new ValueSet (list [0].ReferenceKeys, c.ReferenceKeys, c.Values));
					}
					combinedProps [key] = list.ToImmutable ();
				} else
					combinedProps [key] = otherList;
			}
		}

		internal void AddPropertyValues (IList<string> names, IList<string> values)
		{
			var key = new KeySet (names);
			ImmutableList<ValueSet> list;
			ValueSet valueSet;

			// First register the combination of values

			if (!combinedProps.TryGetValue (key, out list)) {
				list = ImmutableList<ValueSet>.Empty;
				valueSet = new ValueSet (names, names, values);
			} else {
				// If there is already a list, there must be at least one item.
				// Use the reference key of that item, so they share the same reference key array
				valueSet = new ValueSet (list [0].ReferenceKeys, names, values);
			}

			if (list.IndexOf (valueSet, StructEqualityComparer<ValueSet>.Instance) < 0)
				combinedProps[key] = list.Add (valueSet);

			// Now register each value individually

			ImmutableList<string> valList;
			for (int n = 0; n < names.Count; n++) {
				var name = names [n];
				var val = values [n];
				if (!props.TryGetValue (name, out valList))
					valList = ImmutableList<string>.Empty;
				if (!valList.Contains (val))
					props[name] = valList.Add (val);
			}
		}

		/// <summary>
		/// Retuns the name of all conditioned properties
		/// </summary>
		/// <returns>The all properties.</returns>
		public IEnumerable<string> GetAllProperties ()
		{
			return props.Keys;
		}

		/// <summary>
		/// Gets the values used in conditions for the given property
		/// </summary>
		public ImmutableList<string> GetAllPropertyValues (string property)
		{
			ImmutableList<string> list;
			if (props.TryGetValue (property, out list))
				return list;
			return ImmutableList<string>.Empty;
		}

		/// <summary>
		/// Gets the values for a combination of properties. It only returns values specified in conditions that
		/// reference only (and only) the given properties. For example, if the properties are Configuration and
		/// Platform, it will return values for those properties specified in conditions that reference both
		/// Configuration and Platform.
		/// </summary>
		public ImmutableList<ValueSet> GetCombinedPropertyValues (params string[] properties)
		{
			ImmutableList<ValueSet> list;
			if (combinedProps.TryGetValue (new KeySet (properties), out list))
				return list;
			return ImmutableList<ValueSet>.Empty;
		}
	}
}

