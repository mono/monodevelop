//
// CombineTestGroup.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;

using NUnit.Core;

namespace MonoDevelop.NUnit
{
	public class CombineTestGroup: UnitTestGroup
	{
		Combine combine;
		
		public CombineTestGroup (Combine c): base (c.Name, c)
		{
			string storeId = Path.GetFileName (c.FileName);
			string resultsPath = Path.Combine (c.BaseDirectory, "test-results");
			ResultsStore = new XmlResultsStore (resultsPath, storeId);
			
			combine = c;
			combine.EntryAdded += new CombineEntryEventHandler (OnEntryChanged);
			combine.EntryRemoved += new CombineEntryEventHandler (OnEntryChanged);
			combine.NameChanged += new CombineEntryRenamedEventHandler (OnCombineRenamed);
		}
		
		public static CombineTestGroup CreateTest (Combine c)
		{
			return new CombineTestGroup (c);
		}
		
		public override void Dispose ()
		{
			combine.EntryAdded -= new CombineEntryEventHandler (OnEntryChanged);
			combine.EntryRemoved -= new CombineEntryEventHandler (OnEntryChanged);
			combine.NameChanged -= new CombineEntryRenamedEventHandler (OnCombineRenamed);
			base.Dispose ();
		}
		
		void OnEntryChanged (object sender, CombineEntryEventArgs e)
		{
			UpdateTests ();
		}
		
		void OnCombineRenamed (object sender, CombineEntryRenamedEventArgs e)
		{
			UnitTestGroup parent = Parent as UnitTestGroup;
			if (parent != null)
				parent.UpdateTests ();
		}
		
		protected override void OnCreateTests ()
		{
			NUnitService testService = (NUnitService) ServiceManager.GetService (typeof(NUnitService));
			foreach (CombineEntry e in combine.Entries) {
				UnitTest t = testService.BuildTest (e);
				if (t != null)
					Tests.Add (t);
			}
		}
	}
}

