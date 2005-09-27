
using System;
using System.Collections;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class DocumentCollection: CollectionBase
	{
		internal void Add (Document doc)
		{
			List.Add (doc);
		}
		
		public Document this [string name] {
			get {
				foreach (Document doc in List)
					if (doc.FileName == name)
						return doc;
				return null;
			}
		}
		
		public Document this [int index] {
			get { return (Document) List [index]; }
		}
		
		public void Insert (int index, Document doc)
		{
			List.Insert (index, doc);
		}
		
		public void Remove (Document doc)
		{
			List.Remove (doc);
		}
		
		public int IndexOf (Document doc)
		{
			return List.IndexOf (doc);
		}
		
		public void CopyTo (Document[] array, int index)
		{
			List.CopyTo (array, index);
		}
	}
}
