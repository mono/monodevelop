using System;
using System.Collections;
using MonoDevelop.Gui.Widgets;

namespace System {
	[Serializable]
	///<summary>
	///Compares two objects by property.
	///Objects need not be of the same type,
	///they need only possess the same set of properties the used by the comparer
	///and those properties need to implement IComparable
	///</summary>
	public class PropertyComparer : IComparer {
		protected string[] Properties = null;
		
		///<param name="properties">Array of property names to compare</param>
		public PropertyComparer(string [] properties) {
			if(properties != null) {
				Properties = properties;
			}
			else {
				Properties = new string[0];
			}
		}
		
		///<summary>
		///Compare x against y by comparing the list of properties passed in constructor
		///</summary>
		int IComparer.Compare(object x, object y) {
			int cmp;
			Type typex = x.GetType();
			Type typey = y.GetType();
			
			for(int i=0; i < Properties.Length; i++) {
				string p = Properties[i];
				IComparable pvalx = (IComparable)(typex.GetProperty(p).GetValue(x, null));
				object pvaly = typey.GetProperty(p).GetValue(y, null);
				cmp = pvalx.CompareTo(pvaly);
				if (cmp != 0) {
					return cmp;
				}
			}
			return 0;
		}
	}
	
	///<summary>
	///Implements a comparer that inverts the order of comparisons.
	///Intended for inverting sort order on sorters that use IComparer
	///</summary>
	public class ReverseComparer : IComparer {
		
		public static readonly ReverseComparer Default = new ReverseComparer(Comparer.Default);
		
		private IComparer myComparer = null;
		
		protected ReverseComparer():this(Comparer.Default) {}
		
		public ReverseComparer(IComparer comparer) { myComparer = comparer; }
		
		int IComparer.Compare(object a, object b) {
			return myComparer.Compare(b, a);
		}
	}
}


