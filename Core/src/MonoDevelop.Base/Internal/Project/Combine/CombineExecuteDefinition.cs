// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using MonoDevelop.Internal.Serialization;

namespace MonoDevelop.Internal.Project
{
	public enum EntryExecuteType {
		None,
		Execute
	}
	
	public class CombineExecuteDefinition
	{
		Combine combine;
		CombineEntry combineEntry;
		
		[ItemProperty ("type")]
		EntryExecuteType type = EntryExecuteType.None;

		string entryName;
		
		[ItemProperty ("entry")]
		internal string EntryName {
			get { return Entry != null ? Entry.Name : entryName; }
			set { entryName = value; }
		}
		
		public CombineExecuteDefinition()
		{
		}
		
		public CombineExecuteDefinition (CombineEntry entry, EntryExecuteType type)
		{
			Entry = entry;
			this.type  = type;
		}
		
		internal void SetCombine (Combine cmb)
		{
			combine = cmb;
		}
		
		public CombineEntry Entry {
			get {
				if (combineEntry == null)
					combineEntry = combine.Entries [entryName];
				return combineEntry;
			}
			set {
				combineEntry = value; 
				entryName = value != null ? value.Name : null;
			}
		}
		
		public EntryExecuteType Type {
			get { return type; }
			set { type = value; }
		}
	}
}
