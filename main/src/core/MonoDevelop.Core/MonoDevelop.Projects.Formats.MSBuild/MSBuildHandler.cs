// MSBuildHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildHandler: ISolutionItemHandler
	{
		SolutionItem item;
		string typeGuid;
		string id;
		string[] slnProjectContent;
		DataItem customSlnData;
		
		internal List<string> UnresolvedProjectDependencies { get; set; }

		internal protected MSBuildHandler ()
		{
		}
		
		public MSBuildHandler (string typeGuid, string itemId)
		{
			Initialize (typeGuid, itemId);
		}
		
		internal void Initialize (string typeGuid, string itemId)
		{
			this.typeGuid = typeGuid;
			this.id = itemId;
		}
		
		// When set, it means this item is saved as part of a global solution save operation
		internal bool SavingSolution { get; set; }
		
		internal protected SolutionItem Item {
			get { return item; }
			set { item = value; }
		}
		
		public virtual bool SyncFileName {
			get { return true; }
		}

		public string TypeGuid {
			get {
				return typeGuid;
			}
		}
		
		internal string[] SlnProjectContent {
			get {
				return slnProjectContent;
			}
			set {
				slnProjectContent = value;
			}
		}
		
		public string ItemId {
			get {
				if (id == null)
					id = String.Format ("{{{0}}}", System.Guid.NewGuid ().ToString ().ToUpper ());
				return id; 
			}
			set { id = value; }
		}

		internal MSBuildFileFormat SolutionFormat { get; private set; }

		internal virtual void SetSolutionFormat (MSBuildFileFormat format, bool converting)
		{
			SolutionFormat = format;
		}

		public virtual BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			throw new NotSupportedException ();
		}
		
		public void Save (IProgressMonitor monitor)
		{
			if (HasSlnData && !SavingSolution && Item.ParentSolution != null) {
				// The project has data that has to be saved in the solution, but the solution is not being saved. Do it now.
				monitor.BeginTask (null, 2);
				SaveItem (monitor);
				monitor.Step (1);
				Solution sol = Item.ParentSolution;
				SolutionFormat.SlnFileFormat.WriteFile (sol.FileName, sol, SolutionFormat, false, monitor);
				sol.NeedsReload = false;
				monitor.EndTask ();
			} else
				SaveItem (monitor);
		}
		
		protected virtual void SaveItem (MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void OnModified (string hint)
		{
		}

		public virtual void Dispose ()
		{
		}
		
		public virtual bool HasSlnData {
			get { return false; }
		}

		public virtual DataItem WriteSlnData ()
		{
			return customSlnData;
		}
		
		public virtual void ReadSlnData (DataItem item)
		{
			customSlnData = item;
		}
	}
}
