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
using MonoDevelop.Projects;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// A target that can execute a command. For example, a specific device when doing mobile development
	/// </summary>
	public abstract class ExecutionTarget
	{
		protected ExecutionTarget ()
		{
			this.Enabled = true;	
		}

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

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Core.Execution.ExecutionTarget"/> is enabled.
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Core.Execution.ExecutionTarget"/> is notable.
		/// </summary>
		/// <remarks>
		/// This is introduced to be able to highlight execution targets for whatever reason makes sense for the project. 
		/// For example, the android add-in uses this to indicate which emulators are currently running but other addins can use this
		/// for their own purposes
		/// </remarks>
		public bool Notable { get; set; }

		/// <summary>
		/// Gets or sets the image name that should be used for this <see cref="MonoDevelop.Core.Execution.ExecutionTarget"/>.
		/// </summary>
		/// <value>The name of the image.</value>
		public string Image { get; set; }

		/// <summary>
		/// Gets or sets the tooltip that should be used for this <see cref="MonoDevelop.Core.Execution.ExecutionTarget"/>.
		/// </summary>
		/// <value>The text to be shown in the tooltip.</value>
		public string Tooltip { get; set; }

		/// <summary>
		/// Target group on which this target is included
		/// </summary>
		public ExecutionTargetGroup ParentGroup { get; internal set; }

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
			target.ParentGroup = this;
			targets.Insert (index, target);
		}

		public void RemoveAt (int index)
		{
			var t = targets [index];
			t.ParentGroup = null;
			targets.RemoveAt (index);
		}

		public ExecutionTarget this [int index] {
			get { return targets[index]; }
			set {
				var t = targets [index];
				t.ParentGroup = null;
				targets[index] = value;
				value.ParentGroup = this;
			}
		}

		#endregion

		#region ICollection implementation

		public void Add (ExecutionTarget target)
		{
			target.ParentGroup = this;
			targets.Add (target);
		}

		public void Clear ()
		{
			foreach (var t in targets)
				t.ParentGroup = null;
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
			target.ParentGroup = null;
			return targets.Remove (target);
		}

		public int Count {
			get { return targets.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		#endregion

		public List<ExecutionTarget>.Enumerator GetEnumerator ()
		{
			return targets.GetEnumerator ();
		}

		#region IEnumerable implementation

		IEnumerator<ExecutionTarget> IEnumerable<ExecutionTarget>.GetEnumerator ()
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

	public class MultiProjectExecutionTarget : ExecutionTarget
	{
		//string id;

		public override string Id {
			get {
				return "multiple-projects-5ce70911-0a07-4da6-ad3d-cc7a293f6656";
			}
		}

		public override string Name { get; } = GettextCatalog.GetString ("Multiple");

		Dictionary<SolutionItem, ExecutionTarget> list = new Dictionary<SolutionItem, ExecutionTarget> ();

		public void SetExecutionTarget (SolutionItem project, ExecutionTarget target)
		{
			if (target == null)
				list.Remove (project);
			else
				list [project] = target;
			//id = string.Join ("/", list.Select (p => p.Value.Id).OrderBy (id => id));
		}

		public ExecutionTarget GetTarget(SolutionItem project)
		{
			ExecutionTarget target;
			if (list.TryGetValue (project, out target))
				return target;
			return null;
		}
	}
}
