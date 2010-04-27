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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.CodeCompletion;
using System.ComponentModel;

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
		
		object matchLock = new object ();
		string matchString = "";
		
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
			
			int width  = PropertyService.Get ("NavigateToDialog.Width", 400);
			int height = PropertyService.Get ("NavigateToDialog.Height", 300);
			this.Resize (width, height);
			this.SizeAllocated += delegate(object o, SizeAllocatedArgs args) {
				PropertyService.Set ("NavigateToDialog.Width", args.Allocation.Width);
				PropertyService.Set ("NavigateToDialog.Height", args.Allocation.Height);
			};
		}
		
		Thread collectFiles, collectTypes, collectMembers;
		void StartCollectThreads ()
		{
			StartCollectFiles ();
			StartCollectTypes ();
		}
		
		static TimerCounter getMembersTimer = InstrumentationService.CreateTimerCounter ("Time to get all members", "NavigateToDialog");

		void StartCollectMembers ()
		{
			collectMembers = new Thread (new ThreadStart (delegate {
				Console.WriteLine ("start");
				DateTime t = DateTime.Now;
				getMembersTimer.BeginTiming ();
				try {
					members = new List<IMember> ();
					foreach (IType type in types) {
						foreach (IMember m in type.Members) {
							if (m is IType)
								continue;
							members.Add (m);
						}
					}
				} finally {
					getMembersTimer.EndTiming ();
					Console.WriteLine ("done" + (DateTime.Now - t).TotalMilliseconds);
				}
			}));
			collectMembers.IsBackground = true;
			collectMembers.Name = "Navigate to: Collect Members";
			collectMembers.Priority = ThreadPriority.Lowest;
			collectMembers.Start ();
		}
		
		void StartCollectTypes ()
		{
			collectTypes = new Thread (new ThreadStart (delegate {
				types = GetTypes ();
				if (isAbleToSearchMembers)
					StartCollectMembers ();
			}));
			collectTypes.IsBackground = true;
			collectTypes.Name = "Navigate to: Collect Types";
			collectTypes.Priority = ThreadPriority.Lowest;
			collectTypes.Start ();
		}
		
		void StartCollectFiles ()
		{
			collectFiles= new Thread (new ThreadStart (delegate {
				files = GetFiles ();
			}));
			collectFiles.IsBackground = true;
			collectFiles.Name = "Navigate to: Collect Files";
			collectFiles.Priority = ThreadPriority.Lowest;
			collectFiles.Start ();
		}
		
		void SetupTreeView ()
		{
			list = new ListView ();
			list.AllowMultipleSelection = true;
			list.DataSource = new ResultsDataSource ();
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
			
			string toMatch = matchEntry.Query.ToLower ();
			lock (matchLock) {
				matchString = toMatch;
				savedMatches.Clear ();
			}
			
			if (string.IsNullOrEmpty (toMatch)) {
				list.DataSource = new ResultsDataSource ();
				labelResults.LabelProp = GettextCatalog.GetString ("_Results: Enter search term to start.");
				return;
			}
			
			if (!string.IsNullOrEmpty (lastResult.pattern) && toMatch.StartsWith (lastResult.pattern))
				list.DataSource = new ResultsDataSource ();
			
			searchWorker = new System.ComponentModel.BackgroundWorker  ();
			searchWorker.WorkerSupportsCancellation = true;
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
			
			searchWorker.RunWorkerAsync (new KeyValuePair<string, WorkerResult> (matchString, lastResult));
		}
		
		class WorkerResult 
		{
			public List<ProjectFile> filteredFiles = null;
			public List<IType> filteredTypes = null;
			public List<IMember> filteredMembers  = null;
			
			public string pattern = null;
			public ResultsDataSource results = new ResultsDataSource ();
		}
		
		IEnumerable<ProjectFile> files;
		List<IType> types;
		List<IMember> members;
		
		WorkerResult lastResult = new WorkerResult ();
		
		void SearchWorker (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;
			var arg = (KeyValuePair<string, WorkerResult>)e.Argument;
			
			WorkerResult lastResult = arg.Value;
			
			WorkerResult newResult = new WorkerResult ();
			newResult.pattern = arg.Key;
			
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
			string toMatch = newResult.pattern;
			int i = toMatch.IndexOf (':');
			if (i != -1)
				toMatch = toMatch.Substring (0,i);
			
			// Search files
			if ((NavigateToType & NavigateToType.Files) == NavigateToType.Files) {
				WaitForCollectFiles ();
				newResult.filteredFiles = new List<ProjectFile> ();
				bool startsWithLastFilter = lastResult.pattern != null && toMatch.StartsWith (lastResult.pattern) && lastResult.filteredFiles != null;
				IEnumerable<ProjectFile> allFiles = startsWithLastFilter ? lastResult.filteredFiles : files;
				foreach (ProjectFile file in allFiles) {
					if (worker.CancellationPending) 
						yield break;
					SearchResult curResult = CheckFile (file, toMatch);
					if (curResult != null) {
						newResult.filteredFiles.Add (file);
						yield return curResult;
					}
				}
			}
			
			// Search Types
			if ((NavigateToType & NavigateToType.Types) == NavigateToType.Types) {
				WaitForCollectTypes ();
				newResult.filteredTypes = new List<IType> ();
				bool startsWithLastFilter = lastResult.pattern != null && toMatch.StartsWith (lastResult.pattern) && lastResult.filteredTypes != null;
				List<IType> allTypes = startsWithLastFilter ? lastResult.filteredTypes : types;
				foreach (IType type in allTypes) {
					if (worker.CancellationPending)
						yield break;
					SearchResult curResult = CheckType (type, toMatch);
					if (curResult != null) {
						newResult.filteredTypes.Add (type);
						yield return curResult;
					}
				}
			}
			
			// Search members
			if ((NavigateToType & NavigateToType.Members) == NavigateToType.Members) {
				WaitForCollectMembers ();
				newResult.filteredMembers = new List<IMember> ();
				bool startsWithLastFilter = lastResult.pattern != null && toMatch.StartsWith (lastResult.pattern) && lastResult.filteredMembers != null;
				List<IMember> allMembers = startsWithLastFilter ? lastResult.filteredMembers : members;
				foreach (IMember member in allMembers) {
					if (worker.CancellationPending)
						yield break;
					SearchResult curResult = CheckMember (member, toMatch);
					if (curResult != null) {
						newResult.filteredMembers.Add (member);
						yield return curResult;
					}
				}
			}
			
		}
		
		void WaitForCollectMembers ()
		{
			WaitForCollectTypes ();
			if (collectMembers != null) {
				collectMembers.Join ();
				collectMembers = null;
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
		
		List<IType> GetTypes ()
		{
			List<IType> list = new List<IType> ();
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
								list.Add (c);
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
						AddType (c, list);
				}
			} finally {
				getTypesTimer.EndTiming ();
			}
			return list;
		}

		void AddType (IType c, List<IType> list)
		{
			list.Add (c);
			foreach (IType ct in c.InnerTypes)
				AddType (ct, list);
		}
		
		SearchResult CheckFile (ProjectFile file, string toMatch)
		{
			int rank;
			string matchString = System.IO.Path.GetFileName (file.FilePath);
			if (MatchName (matchString, toMatch, out rank)) 
				return new FileSearchResult (toMatch, matchString, rank, file, true);
			
			matchString = FileSearchResult.GetRelProjectPath (file);
			if (MatchName (FileSearchResult.GetRelProjectPath (file), toMatch, out rank)) 
				return new FileSearchResult (toMatch, matchString, rank, file, false);
			
			return null;
		}
		
		SearchResult CheckType (IType type, string toMatch)
		{
			int rank;
			if (MatchName (type.Name, toMatch, out rank))
				return new TypeSearchResult (toMatch, type.Name, rank, type, false);
			if (MatchName (type.FullName, toMatch, out rank))
				return new TypeSearchResult (toMatch, type.FullName, rank, type, true);
			return null;
		}
		
		SearchResult CheckMember (IMember member, string toMatch)
		{
			int rank;
			string memberName = (member is IMethod && ((IMethod)member).IsConstructor) ? member.DeclaringType.Name : member.Name;
			if (!MatchName (memberName, toMatch, out rank))
				return null;
			return new MemberSearchResult (toMatch, memberName, rank, member);
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
		
		Dictionary<string, MatchResult> savedMatches = new Dictionary<string, MatchResult> ();
		
		bool MatchName (string name, string toMatch, out int matchRank)
		{
			MatchResult savedMatch;
			if (!savedMatches.TryGetValue (name, out savedMatch)) {
				if (MonoDevelop.Ide.CodeCompletion.ListWidget.Matches (toMatch, name)) {
					CalcMatchRank (name, toMatch, out matchRank);
					savedMatch = new MatchResult (true, matchRank);
				} else {
					savedMatch = new MatchResult (false, int.MinValue);
				}
				savedMatches[name] = savedMatch;
			}
			
			matchRank = savedMatch.Rank;
			return savedMatch.Match;
		}
		
		static bool CalcMatchRank (string name, string toMatch, out int matchRank)
		{
			if (toMatch.Length == 0) {
				matchRank = int.MinValue;
				return true;
			}
			MatchLane lane = MatchString (name, toMatch);
			if (lane != null) {
				matchRank = -(lane.Positions [0] + (name.Length - toMatch.Length));
				return true;
			}
			matchRank = int.MinValue;
			return false;
		}
		
		internal static MatchLane MatchString (string text, string toMatch)
		{
			if (text.Length < toMatch.Length)
				return null;
			
			List<MatchLane> matchLanes = null;
			bool lastWasSeparator = false;
			int tn = 0;
			
			while (tn < text.Length) {
				char ct = text [tn];
				
				// Keep the lane count in a var because new lanes don't have to be updated
				// until the next iteration
				int laneCount = matchLanes != null ? matchLanes.Count : 0;
				
				char cm = toMatch [0]; 
				if (char.ToLower (ct) == char.ToLower (cm)) {
					if (matchLanes == null)
						matchLanes = new List<MatchLane> ();
					matchLanes.Add (new MatchLane (MatchMode.Substring, tn, text.Length - tn));
					if (toMatch.Length == 1)
						return matchLanes[0];
					if (char.IsUpper (ct) || lastWasSeparator)
						matchLanes.Add (new MatchLane (MatchMode.Acronym, tn, text.Length - tn));
				}
					
				for (int n=0; n<laneCount; n++) {
					MatchLane lane = matchLanes [n];
					if (lane == null)
						continue;
					cm = toMatch [lane.MatchIndex]; 
					bool match = char.ToLower (ct) == char.ToLower (cm);
					bool wordStartMatch = match && (tn == 0 || char.IsUpper (ct) || lastWasSeparator);
	
					if (lane.MatchMode == MatchMode.Substring) {
						if (wordStartMatch) {
							// Possible acronym match after a substring. Start a new lane.
							MatchLane newLane = lane.Clone ();
							newLane.MatchMode = MatchMode.Acronym;
							newLane.Index++;
							newLane.Positions [newLane.Index] = tn;
							newLane.Lengths [newLane.Index] = 1;
							newLane.MatchIndex++;
							matchLanes.Add (newLane);
						}
						if (match) {
							// Maybe it is a false substring start, so add a new lane to keep
							// track of the old lane
							MatchLane newLane = lane.Clone ();
							newLane.MatchMode = MatchMode.Acronym;
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Lengths [lane.Index]++;
							lane.MatchIndex++;
						} else {
							if (lane.Lengths [lane.Index] > 1)
								lane.MatchMode = MatchMode.Acronym;
							else
								matchLanes [n] = null; // Kill the lane
						}
					}
					else if (lane.MatchMode == MatchMode.Acronym) {
						if (match && lane.Positions [lane.Index] == tn - 1) {
							// Possible substring match after an acronim. Start a new lane.
							MatchLane newLane = lane.Clone ();
							newLane.MatchMode = MatchMode.Substring;
							newLane.Index++;
							newLane.Positions [newLane.Index] = tn;
							newLane.Lengths [newLane.Index] = 1;
							newLane.MatchIndex++;
							matchLanes.Add (newLane);
							if (newLane.MatchIndex == toMatch.Length)
								return newLane;
						}
						if (wordStartMatch || (match && char.IsPunctuation (cm))) {
							// Maybe it is a false acronym start, so add a new lane to keep
							// track of the old lane
							MatchLane newLane = lane.Clone ();
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Index++;
							lane.Positions [lane.Index] = tn;
							lane.Lengths [lane.Index] = 1;
							lane.MatchIndex++;
						}
					}
					if (lane.MatchIndex == toMatch.Length)
						return lane;
				}
				lastWasSeparator = (ct == '.' || ct == '_' || ct == '-' || ct == ' ' || ct == '/' || ct == '\\');
				tn++;
			}
			return null;
		}

		internal enum MatchMode
		{
			Substring,
			Acronym
		}

		internal class MatchLane
		{
			public int[] Positions;
			public int[] Lengths;
			public MatchMode MatchMode;
			public int Index;
			public int MatchIndex;
	
			public MatchLane ()
			{
			}
	
			public MatchLane (MatchMode mode, int pos, int len)
			{
				MatchMode = mode;
				Positions = new int [len];
				Lengths = new int [len];
				Positions [0] = pos;
				Lengths [0] = 1;
				Index = 0;
				MatchIndex = 1;
			}
	
			public MatchLane Clone ()
			{
				MatchLane lane = new MatchLane ();
				lane.Positions = (int[]) Positions.Clone ();
				lane.Lengths = (int[]) Lengths.Clone ();
				lane.MatchMode = MatchMode;
				lane.MatchIndex = MatchIndex;
				lane.Index = Index;
				return lane;
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
