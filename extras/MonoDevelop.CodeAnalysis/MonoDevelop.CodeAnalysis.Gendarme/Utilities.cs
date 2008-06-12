using System;
using System.Reflection;

using GF = Gendarme.Framework;

namespace MonoDevelop.CodeAnalysis.Gendarme {
	
	static class Utilities {
		
		public static bool IsGendarmeRule (Type t)
		{
			return t.GetInterface (typeof (GF.IRule).FullName) != null;
		}
	}
}