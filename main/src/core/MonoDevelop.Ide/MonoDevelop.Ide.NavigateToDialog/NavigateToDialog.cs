//
// NavigateToDialog.cs
//  
// Author:
//   Zach Lute (zach.lute@gmail.com)
//   Aaron Bockover (abockover@novell.com)
//   Jacob Ilsø Christensen
//   Lluis Sanchez
//   Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Ide.NavigateToDialog
{
	[Flags]
	public enum NavigateToType {
		Files   = 1,
		Types   = 2,
		Members = 4,
		All     = Files | Types | Members,
		NonMembers = Files | Types
	}
	
	partial class NavigateToDialog : Gtk.Dialog
	{
		ListView list;
		
		public NavigateToType NavigateToType {
			get;
			set;
		}
		
		public struct OpenLocation
		{
			public string Filename;
			public int Line;
			public int Column;
			
			public OpenLocation (string filename, int line, int column)
			{
				this.Filename = filename;
				this.Line = line;
				this.Column = column;
			}
		}
		
		List<OpenLocation> locations = new List<OpenLocation> ();
		public IEnumerable<OpenLocation> Locations {
			get {
				return locations.ToArray ();
			}
		}
		
		bool useFullSearch;
		bool isAbleToSearchMembers;
		public NavigateToDialog (NavigateToType navigateTo, bool isAbleToSearchMembers)
		{
			this.NavigateToType = navigateTo;
			this.isAbleToSearchMembers = isAbleToSearchMembers;
			this.Build ();
			this.label1.MnemonicWidget = matchEntry.Entry;
			this.matchEntry.Ready = true;
			this.matchEntry.Visible = true;
			this.matchEntry.IsCheckMenu = true;
			lastResult = new WorkerResult (this);
			HasSeparator = false;
			useFullSearch = PropertyService.Get ("UseFullSearchMatch", true);
			
			CheckMenuItem includeFilesItem = this.matchEntry.AddFilterOption (0, GettextCatalog.GetString ("Include _Files"));
			includeFilesItem.DrawAsRadio = false;
			includeFilesItem.Active = (navigateTo & NavigateToType.Files) == NavigateToType.Files;
			includeFilesItem.Toggled += delegate {
				if (includeFilesItem.Active) {
					this.NavigateToType |= NavigateToType.Files;
				} else {
					this.NavigateToType &= ~NavigateToType.Files;
				}
				PerformSearch ();
			};
			
			CheckMenuItem includeTypes = this.matchEntry.AddFilterOption (1, GettextCatalog.GetString ("Include _Types"));
			includeTypes.DrawAsRadio = false;
			includeTypes.Active = (navigateTo & NavigateToType.Files) == NavigateToType.Files;
			includeTypes.Toggled += delegate {
				if (includeTypes.Active) {
					this.NavigateToType |= NavigateToType.Types;
				} else {
					this.NavigateToType &= ~NavigateToType.Types;
				}
				PerformSearch ();
			};
			
			if (this.isAbleToSearchMembers) {
				CheckMenuItem includeMembers = this.matchEntry.AddFilterOption (2, GettextCatalog.GetString ("Include _Members"));
				includeMembers.DrawAsRadio = false;
				includeMembers.Active = (navigateTo & NavigateToType.Members) == NavigateToType.Members;
				includeMembers.Toggled += delegate {
					if (includeMembers.Active) {
						this.NavigateToType |= NavigateToType.Members;
					} else {
						this.NavigateToType &= ~NavigateToType.Members;
					}
					PerformSearch ();
				};
			}
			
			CheckMenuItem useComplexMatching = this.matchEntry.AddFilterOption (3, GettextCatalog.GetString ("Use complex matching"));
			useComplexMatching.DrawAsRadio = false;
			useComplexMatching.Active = useFullSearch;
			useComplexMatching.Toggled += delegate {
				useFullSearch = useComplexMatching.Active;
				PropertyService.Set ("UseFullSearchMatch", useFullSearch);
				PerformSearch ();
			};
			
			this.matchEntry.Changed += delegate {
				PerformSearch ();
			};
			SetupTreeView ();
			this.labelResults.MnemonicWidget = list;
			
			StartCollectThreads ();
			this.matchEntry.Entry.KeyPressEvent += HandleKeyPress;
			this.matchEntry.Activated += delegate {
				OpenFile ();
			};
			this.buttonOpen.Clicked += delegate {
				OpenFile ();
			};
			
			this.matchEntry.Entry.GrabFocus ();
			
			DefaultWidth  = PropertyService.Get ("NavigateToDialog.DialogWidth", 620);
			DefaultHeight = PropertyService.Get ("NavigateToDialog.DialogHeight", 440);
			
			this.SizeAllocated += delegate(object o, SizeAllocatedArgs args) {
				PropertyService.Set ("NavigateToDialog.DialogWidth", args.Allocation.Width);
				PropertyService.Set ("NavigateToDialog.DialogHeight", args.Allocation.Height);
			};
		}
		
		Thread collectFiles, collectTypes;
		void StartCollectThreads ()
		{
			members = new List<IMember> ();
			types = new List<IType> ();
			
			StartCollectFiles ();
			StartCollectTypes ();
		}
		
		static TimerCounter getMembersTimer = InstrumentationService.CreateTimerCounter ("Time to get all members", "NavigateToDialog");
		
		void StartCollectTypes ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				CollectTypes ();
				
				if (isAbleToSearchMembers) {
					getMembersTimer.BeginTiming ();
					try {
						lock (members) {
							foreach (IType type in types) {
								foreach (IMember m in type.Members) {
									if (m is IType)
										continue;
									members.Add (m);
								}
							}
						}
					} finally {
						getMembersTimer.EndTiming ();
					}
				}
			});
		}
		
		void StartCollectFiles ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				files = GetFiles ();
			});
		}
		
		void SetupTreeView ()
		{
			list = new ListView ();
			list.AllowMultipleSelection = true;
			list.DataSource = new ResultsDataSource (this);
			list.Show ();
			list.ItemActivated += delegate { 
				OpenFile ();
			};
			scrolledwindow1.Add (list);
		}
		
		void OpenFile ()
		{
			locations.Clear ();
			if (list.SelectedRows.Count != 0) {
				foreach (int sel in list.SelectedRows) {
					SearchResult res = lastResult.results [sel];
					OpenLocation loc = new OpenLocation (res.File, res.Row, res.Column);
					if (loc.Line == -1) {
						int i = matchEntry.Query.LastIndexOf (':');
						if (i != -1) {
							if (!int.TryParse (matchEntry.Query.Substring (i+1), out loc.Line))
								loc.Line = -1;
						}
					}
					locations.Add (loc);
				}
				Respond (ResponseType.Ok);
			} else {
				Respond (ResponseType.Cancel);
			}
		}
		
		protected override void OnDestroyed ()
		{
			StopActiveSearch ();
			
			base.OnDestroyed ();
		}
		 
		System.ComponentModel.BackgroundWorker searchWorker = null;

		void StopActiveSearch ()
		{
			if (searchWorker != null) 
				searchWorker.CancelAsync ();
			searchWorker = null;
		}
		
		void PerformSearch ()
		{
			StopActiveSearch ();
			
			WaitForCollectFiles ();
			WaitForCollectTypes ();
			
			string toMatch = matchEntry.Query;
			
			if (string.IsNullOrEmpty (toMatch)) {
				list.DataSource = new ResultsDataSource (this);
				labelResults.LabelProp = GettextCatalog.GetString ("_Results: Enter search term to start.");
				return;
			} else {
				labelResults.LabelProp = GettextCatalog.GetString ("_Results: Searching...");
			}
			
			if (!string.IsNullOrEmpty (lastResult.pattern) && toMatch.StartsWith (lastResult.pattern))
				list.DataSource = new ResultsDataSource (this);
			
			searchWorker = new System.ComponentModel.BackgroundWorker  ();
			searchWorker.WorkerSupportsCancellation = true;
			searchWorker.WorkerReportsProgress = false;
			searchWorker.DoWork += SearchWorker;
			
			searchWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
				if (e.Cancelled)
					return;
				Application.Invoke (delegate {
					lastResult = e.Result as WorkerResult;
					list.DataSource = lastResult.results;
					list.SelectedRow = 0;
					list.CenterViewToSelection ();
					labelResults.LabelProp = String.Format (GettextCatalog.GetPluralString ("_Results: {0} match found.", "_Results: {0} matches found.", lastResult.results.ItemCount), lastResult.results.ItemCount);
				});
			};
			
			searchWorker.RunWorkerAsync (new KeyValuePair<string, WorkerResult> (toMatch, lastResult));
		}
		
		class WorkerResult 
		{
			public List<ProjectFile> filteredFiles = null;
			public List<IType> filteredTypes = null;
			public List<IMember> filteredMembers  = null;
			
			public string pattern = null;
			public bool isGotoFilePattern;
			public ResultsDataSource results;
			
			public bool FullSearch;
			
			public bool IncludeFiles, IncludeTypes, IncludeMembers;
			
			public StringMatcher matcher = null;
			
			public WorkerResult (Widget widget)
			{
				results = new ResultsDataSource (widget);
			}
			
			internal SearchResult CheckFile (ProjectFile file)
			{
				int rank;
				string matchString = System.IO.Path.GetFileName (file.FilePath);
				if (MatchName (matchString, out rank)) 
					return new FileSearchResult (pattern, matchString, rank, file, true);
				
				if (!FullSearch)
					return null;
				matchString = FileSearchResult.GetRelProjectPath (file);
				if (MatchName (FileSearchResult.GetRelProjectPath (file), out rank)) 
					return new FileSearchResult (pattern, matchString, rank, file, false);
				
				return null;
			}
			
			internal SearchResult CheckType (IType type)
			{
				int rank;
				if (MatchName (type.Name, out rank))
					return new TypeSearchResult (pattern, type.Name, rank, type, false);
				if (!FullSearch)
					return null;
				if (MatchName (type.FullName, out rank))
					return new TypeSearchResult (pattern, type.FullName, rank, type, true);
				return null;
			}
			
			internal SearchResult CheckMember (IMember member)
			{
				int rank;
				bool useDeclaringTypeName = member is IMethod && (((IMethod)member).IsConstructor || ((IMethod)member).IsFinalizer);
				string memberName = useDeclaringTypeName ? member.DeclaringType.Name : member.Name;
				if (MatchName (memberName, out rank))
					return new MemberSearchResult (pattern, memberName, rank, member, false);
				if (!FullSearch)
					return null;
				memberName = useDeclaringTypeName ? member.DeclaringType.FullName : member.FullName;
				if (MatchName (memberName, out rank))
					return new MemberSearchResult (pattern, memberName, rank, member, true);
				return null;
			}
			
			Dictionary<string, MatchResult> savedMatches = new Dictionary<string, MatchResult> ();
			bool MatchName (string name, out int matchRank)
			{
				MatchResult savedMatch;
				if (!savedMatches.TryGetValue (name, out savedMatch)) {
					bool doesMatch = matcher.CalcMatchRank (name, out matchRank);
					savedMatches[name] = savedMatch = new MatchResult (doesMatch, matchRank);
				}
				
				matchRank = savedMatch.Rank;
				return savedMatch.Match;
			}
		}
		
		IEnumerable<ProjectFile> files;
		List<IType> types;
		List<IMember> members;
		
		WorkerResult lastResult;
		
		void SearchWorker (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;
			var arg = (KeyValuePair<string, WorkerResult>)e.Argument;
			
			WorkerResult lastResult = arg.Value;
			
			WorkerResult newResult = new WorkerResult (this);
			newResult.pattern = arg.Key;
			newResult.IncludeFiles = (NavigateToType & NavigateToType.Files) == NavigateToType.Files;
			newResult.IncludeTypes = (NavigateToType & NavigateToType.Types) == NavigateToType.Types;
			newResult.IncludeMembers = (NavigateToType & NavigateToType.Members) == NavigateToType.Members;
			
			string toMatch = arg.Key;
			int i = toMatch.IndexOf (':');
			if (i != -1) {
				toMatch = toMatch.Substring (0,i);
				newResult.isGotoFilePattern = true;
			}
			newResult.matcher = StringMatcher.GetMatcher (toMatch, false);
			newResult.FullSearch = useFullSearch;
			
			foreach (SearchResult result in AllResults (worker, lastResult, newResult)) {
				if (worker.CancellationPending)
					break;
				newResult.results.AddResult (result);
			}
			
			if (worker.CancellationPending) {
				e.Cancel = true;
				return;
			}
			newResult.results.Sort (new DataItemComparer ());
			
			e.Result = newResult;
		}
		
		IEnumerable<SearchResult> AllResults (BackgroundWorker worker, WorkerResult lastResult, WorkerResult newResult)
		{
			// Search files
			if (newResult.IncludeFiles) {
				newResult.filteredFiles = new List<ProjectFile> ();
				bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredFiles != null;
				IEnumerable<ProjectFile> allFiles = startsWithLastFilter ? lastResult.filteredFiles : files;
				foreach (ProjectFile file in allFiles) {
					if (worker.CancellationPending) 
						yield break;
					SearchResult curResult = newResult.CheckFile (file);
					if (curResult != null) {
						newResult.filteredFiles.Add (file);
						yield return curResult;
					}
				}
			}
			if (newResult.isGotoFilePattern)
				yield break;
			
			// Search Types
			if (newResult.IncludeTypes) {
				newResult.filteredTypes = new List<IType> ();
				lock (types) {
					bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredTypes != null;
					List<IType> allTypes = startsWithLastFilter ? lastResult.filteredTypes : types;
					foreach (IType type in allTypes) {
						if (worker.CancellationPending)
							yield break;
						SearchResult curResult = newResult.CheckType (type);
						if (curResult != null) {
							newResult.filteredTypes.Add (type);
							yield return curResult;
						}
					}
				}
			}
			
			// Search members
			if (newResult.IncludeMembers) {
				newResult.filteredMembers = new List<IMember> ();
				lock (members) {
					bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredMembers != null;
					List<IMember> allMembers = startsWithLastFilter ? lastResult.filteredMembers : members;
					foreach (IMember member in allMembers) {
						if (worker.CancellationPending)
							yield break;
						SearchResult curResult = newResult.CheckMember (member);
						if (curResult != null) {
							newResult.filteredMembers.Add (member);
							yield return curResult;
						}
					}
				}
			}
		}
		
		
		void WaitForCollectTypes ()
		{
			if (collectTypes != null) {
				collectTypes.Join ();
				collectTypes= null;
			}
		}
		
		void WaitForCollectFiles ()
		{
			if (collectFiles != null) {
				collectFiles.Join ();
				collectFiles = null;
			}
		}
		
		class DataItemComparer : IComparer<SearchResult>
		{
			public int Compare (SearchResult o1, SearchResult o2)
			{
				var r = o2.Rank.CompareTo (o1.Rank);
				if (r == 0)
					return String.CompareOrdinal (o1.MatchedString, o2.MatchedString);
				return r;
			}
		}
		
		IEnumerable<ProjectFile> GetFiles ()
		{
			HashSet<ProjectFile> list = new HashSet<ProjectFile> ();
			foreach (Document doc in IdeApp.Workbench.Documents) {
				// We only want to check it here if it's not part
				// of the open combine.  Otherwise, it will get
				// checked down below.
				if (doc.Project == null && doc.IsFile)
					list.Add (new ProjectFile (doc.Name));
			}
			
			ReadOnlyCollection<Project> projects = IdeApp.Workspace.GetAllProjects ();

			foreach (Project p in projects) {
				foreach (ProjectFile file in p.Files) {
					if (file.Subtype != Subtype.Directory)
						list.Add (file);
				}
			}
			return list;
		}
		
		static TimerCounter getTypesTimer = InstrumentationService.CreateTimerCounter ("Time to get all types", "NavigateToDialog");
		
		void CollectTypes ()
		{
			lock (types) {
				getTypesTimer.BeginTiming ();
				try {
					foreach (Document doc in IdeApp.Workbench.Documents) {
						// We only want to check it here if it's not part
						// of the open combine.  Otherwise, it will get
						// checked down below.
						if (doc.Project == null && doc.IsFile) {
							ICompilationUnit info = doc.CompilationUnit;
							if (info != null) {
								foreach (IType c in info.Types) {
									types.Add (c);
								}
							}
						}
					}
					
					ReadOnlyCollection<Project> projects = IdeApp.Workspace.GetAllProjects ();
		
					foreach (Project p in projects) {
						ProjectDom dom = ProjectDomService.GetProjectDom (p);
						if (dom == null)
							continue;
						foreach (IType c in dom.Types)
							AddType (c, types);
					}
				} finally {
					getTypesTimer.EndTiming ();
				}
			}
		}

		void AddType (IType c, List<IType> list)
		{
			list.Add (c);
			foreach (IType ct in c.InnerTypes)
				AddType (ct, list);
		}
		
		struct MatchResult 
		{
			public bool Match;
			public int Rank;
			
			public MatchResult (bool match, int rank)
			{
				this.Match = match;
				this.Rank = rank;
			}
		}
		
		protected virtual void HandleKeyPress (object o, KeyPressEventArgs args)
		{
			// Up and down move the tree selection up and down
			// for rapid selection changes.
			Gdk.EventKey key = args.Event;
			switch (key.Key) {
			case Gdk.Key.Page_Down:
				list.ModifySelection (false, true, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Page_Up:
				list.ModifySelection (true, true, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Up:
				list.ModifySelection (true, false, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Down:
				list.ModifySelection (false, false, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Escape:
				Destroy ();
				args.RetVal = true;
				break;
			}
		}
	}
}
