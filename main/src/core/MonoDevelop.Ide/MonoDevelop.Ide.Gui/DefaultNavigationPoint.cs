// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Alpert" email="david@spinthemoose.com"/>
//     <version>$Revision: 1963 $</version>
// </file>

using System;
using System.Globalization;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui {
	/// <summary>
	/// Default implementation for classes that wrap Navigational
	/// information for the <see cref="NavigationService"/>.
	/// </summary>
	public class DefaultNavigationPoint : INavigationPoint {
		string fileName;
		
#region constructor
		public DefaultNavigationPoint () : this (String.Empty)
		{
			
		}
		
		public DefaultNavigationPoint (string fileName)
		{
			this.fileName = fileName == null ? String.Empty : fileName;
		}
#endregion
		
#region overrides
		public override string ToString ()
		{
			return String.Format (CultureInfo.CurrentCulture,
			                      "[{0}: {1}]",
			                      GetType ().Name,
			                      Description);
		}
#endregion
		
#region INavigationPoint implementation
		public virtual string FileName {
			get { return fileName; }
		}
		
		public virtual string Description {
			get { return fileName; }
		}
		
		public virtual string FullDescription {
			get { return Description; }
		}
		
		public virtual string ToolTip {
			get { return Description; }
		}
		
		public virtual void JumpTo ()
		{
			IdeApp.Workbench.OpenDocument (fileName, 1, 1, true);
		}
		
		public void FileNameChanged (string newName)
		{
			fileName = newName == null ? String.Empty : newName;
		}
		
		public virtual void ContentChanging (object sender, EventArgs e)
		{
			//throw new NotImplementedException();
		}
#endregion
		
#region Equality
		
		public override bool Equals (object obj)
		{
			DefaultNavigationPoint b = obj as DefaultNavigationPoint;
			
			if (object.ReferenceEquals (b, null))
				return false;
			
			return FileName == b.FileName;
		}
		
		public override int GetHashCode ()
		{
			return FileName.GetHashCode ();
		}
#endregion
		
#region IComparable
		public virtual int CompareTo (object obj)
		{
			if (obj == null)
				return 1;
			
			if (GetType () != obj.GetType ()) {
				// if of different types, sort the types by name
				return GetType ().Name.CompareTo (obj.GetType ().Name);
			}
			
			DefaultNavigationPoint b = obj as DefaultNavigationPoint;
			
			return FileName.CompareTo (b.FileName);
		}
		
		// Omitting any of the following operator overloads
		// violates rule: OverrideMethodsOnComparableTypes.
		public static bool operator == (DefaultNavigationPoint p1, DefaultNavigationPoint p2)
		{
			return object.Equals (p1, p2); // checks for null and calls p1.Equals(p2)
		}
		
		public static bool operator != (DefaultNavigationPoint p1, DefaultNavigationPoint p2)
		{
			return !(p1 == p2);
		}
		
		public static bool operator < (DefaultNavigationPoint p1, DefaultNavigationPoint p2)
		{
			return p1 == null ? p2 != null : (p1.CompareTo (p2) < 0);
		}
		
		public static bool operator > (DefaultNavigationPoint p1, DefaultNavigationPoint p2)
		{
			return p1 == null ? false : (p1.CompareTo (p2) > 0);
		}
#endregion
	}
}