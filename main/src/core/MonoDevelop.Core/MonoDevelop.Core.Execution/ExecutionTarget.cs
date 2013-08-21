//
// IExecutionTarget.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// A target that can execute a command. For example, a specific device when doing mobile development
	/// </summary>
	public abstract class ExecutionTarget
	{
		/// <summary>
		/// Display name of the device
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// The display name of the item when it is selected
		/// </summary>
		public virtual string FullName { get { return Name; } }

		/// <summary>
		/// Unique identifier of the target
		/// </summary>
		public abstract string Id { get; }

		public override bool Equals (object obj)
		{
			var t = obj as ExecutionTarget;
			return t != null && t.Id == Id;
		}

		public override int GetHashCode ()
		{
			return Id.GetHashCode ();
		}

		public override string ToString ()
		{
			return string.Format ("[ExecutionTarget: Name={0}, FullName={1}, Id={2}]", Name, FullName, Id);
		}
	}

	public class ExecutionTargetGroup : ExecutionTarget, IList<ExecutionTarget>
	{
		List<ExecutionTarget> targets;
		string name, id;

		public ExecutionTargetGroup (string name, string id)
		{
			targets = new List<ExecutionTarget> ();
			this.name = name;
			this.id = id;
		}

		public override string Name {
			get { return name; }
		}

		public override string Id {
			get { return id; }
		}

		#region IList implementation

		public int IndexOf (ExecutionTarget target)
		{
			return targets.IndexOf (target);
		}

		public void Insert (int index, ExecutionTarget target)
		{
			targets.Insert (index, target);
		}

		public void RemoveAt (int index)
		{
			targets.RemoveAt (index);
		}

		public ExecutionTarget this [int index] {
			get { return targets[index]; }
			set { targets[index] = value; }
		}

		#endregion

		#region ICollection implementation

		public void Add (ExecutionTarget target)
		{
			targets.Add (target);
		}

		public void Clear ()
		{
			targets.Clear ();
		}

		public bool Contains (ExecutionTarget target)
		{
			return targets.Contains (target);
		}

		public void CopyTo (ExecutionTarget[] array, int arrayIndex)
		{
			targets.CopyTo (array, arrayIndex);
		}

		public bool Remove (ExecutionTarget target)
		{
			return targets.Remove (target);
		}

		public int Count {
			get { return targets.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<ExecutionTarget> GetEnumerator ()
		{
			return targets.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return targets.GetEnumerator ();
		}

		#endregion
	}
}
