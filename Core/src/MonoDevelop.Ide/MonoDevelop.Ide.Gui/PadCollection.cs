
using System;
using System.Collections;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class PadCollection: CollectionBase
	{
		internal void Add (Pad pad)
		{
			List.Add (pad);
		}
		
		public Pad this [Type type] {
			get {
				foreach (Pad pad in List)
					if (type.IsInstanceOfType (pad.Content))
						return pad;
				return null;
			}
		}

/*		public Pad this [string name] {
			get {
				foreach (Pad pad in List)
					if (pad.FileName == name)
						return pad;
				return null;
			}
		}
*/
		public Pad this [int index] {
			get { return (Pad) List [index]; }
		}
		
		internal void Remove (Pad pad)
		{
			List.Remove (pad);
		}
	}
}
