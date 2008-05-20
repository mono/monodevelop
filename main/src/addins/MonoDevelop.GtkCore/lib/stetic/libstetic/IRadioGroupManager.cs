
using System;
using System.Collections;

namespace Stetic
{
	public delegate void GroupsChangedDelegate ();
	
	public interface IRadioGroupManagerProvider
	{
		IRadioGroupManager GetGroupManager ();
	}
	
	public interface IRadioGroupManager
	{
		event GroupsChangedDelegate GroupsChanged;
		IEnumerable GroupNames { get; }
		void Rename (string oldName, string newName);
		void Add (string group);
	}
}
