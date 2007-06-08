//// <file>
////     <copyright see="prj:///doc/copyright.txt"/>
////     <license see="prj:///doc/license.txt"/>
////     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
////     <version value="$version"/>
//// </file>
//
//using System;
//using MonoDevelop.Projects;
//
//namespace MonoDevelop.Projects
//{
//	public delegate void CombineEntryEventHandler(object sender, CombineEntryEventArgs e);
//	
//	public class CombineEntryEventArgs : EventArgs
//	{
//		CombineEntry entry;
//		
//		public CombineEntry CombineEntry {
//			get {
//				return entry;
//			}
//		}
//		
//		public CombineEntryEventArgs (CombineEntry entry)
//		{
//			this.entry = entry;
//		}
//	}
//	
//	public delegate void CombineEntryChangeEventHandler(object sender, CombineEntryChangeEventArgs e);
//	
//	public class CombineEntryChangeEventArgs: CombineEntryEventArgs
//	{
//		bool reloading;
//		
//		public CombineEntryChangeEventArgs (CombineEntry entry, bool reloading): base (entry)
//		{
//			this.reloading = reloading;
//		}
//		
//		public bool Reloading {
//			get { return reloading; }
//		}
//	}
//}
//