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
		ResultsDataSource currentResults;
		
		object matchLock = new object ();
		string matchString = "";
		
		// Thread management
		Thread searchThread;
		AutoResetEvent searchThreadWait;
		bool searchCycleActive;
		bool searchThreadDispose;
		
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
			currentResults = new ResultsDataSource ();
			list.DataSource = currentResults;
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
					SearchResult res = currentResults [sel];
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
		
		void StopActiveSearch ()
		{
			// Tell the thread's search code that it should stop working and 
			// then have the thread wait on the handle until told to resume
			if (searchCycleActive && searchThread != null && searchThreadWait != null) {
				searchCycleActive = false;
				searchThreadWait.Reset ();
			}
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
				list.DataSource = currentResults = new ResultsDataSource ();
				labelResults.LabelProp = GettextCatalog.GetString ("_Results: Enter search term to start.");
				return;
			}
			
			if (!string.IsNullOrEmpty (previousPattern) && toMatch.StartsWith (previousPattern)) {
				list.DataSource = currentResults = new ResultsDataSource ();
			}

			if (searchThread == null) {
				// Create the handle the search thread will wait on when there is nothing to do
				searchThreadWait = new AutoResetEvent (false);
				
				// Only a single thread will be used for searching
				ThreadStart start = new ThreadStart (SearchThread);
				searchThread = new Thread (start);
				searchThread.IsBackground = true;
				searchThread.Name = "Navigate to thread";
				searchThread.Priority = ThreadPriority.Lowest;
				searchThread.Start ();
			}
			
			// Wake the handle up so the search thread can do some work
			searchCycleActive = true;
			searchThreadWait.Set ();
		}
		
		void SearchThread ()
		{
			// The thread will remain active until the dialog goes away
			while (true) {
				searchThreadWait.WaitOne ();
				if (searchThreadDispose) {
					break;
				}
				
				try {
					SearchThreadCycle ();
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in NavigateToDialog", ex);
				}
			}
			
			// Reset all thread state even though this shouldn't be
			// necessary since we destroy and never reuse the dialog
			searchCycleActive = false;
			searchThreadDispose = false;
			
			searchThreadWait.Close ();
			searchThreadWait = null;
			searchThread = null;
		}

		IEnumerable<ProjectFile> files;
		List<IType> types;
		List<IMember> members;
		
		List<ProjectFile> filteredFiles;
		List<IType> filteredTypes;
		List<IMember> filteredMembers;
		
		string previousPattern;
		
		void SearchThreadCycle ()
		{
			// This is the inner thread worker; it actually does the searching
			// Any where we enter loop, a check is added to see if the search
			// should be aborted entirely so we can return to the wait handle

			ResultsDataSource results = new ResultsDataSource ();
			
			foreach (SearchResult result in AllResults ()) {
				if (!searchCycleActive) 
					return;
				results.AddResult (result);
			}
			
			if (!searchCycleActive) 
				return;
			results.Sort (new DataItemComparer ());
			
			Application.Invoke (delegate {
				list.DataSource = results;
				currentResults = results;
				list.SelectedRow = 0;
				list.CenterViewToSelection ();
				labelResults.LabelProp = String.Format (GettextCatalog.GetPluralString ("_Results: {0} match found.", "_Results: {0} matches found.", results.ItemCount), results.ItemCount);
			});
		}
		
		IEnumerable<SearchResult> AllResults ()
		{
			string toMatch = matchString;
			int i = toMatch.IndexOf (':');
			if (i != -1)
				toMatch = toMatch.Substring (0,i);
			
			// Search files
			if ((NavigateToType & NavigateToType.Files) == NavigateToType.Files) {
				WaitForCollectFiles ();
				List<ProjectFile> newFilteredFiles = new List<ProjectFile> ();
				bool startsWithLastFilter = previousPattern != null && toMatch.StartsWith (previousPattern) && filteredFiles != null;
				IEnumerable<ProjectFile> allFiles = startsWithLastFilter ? filteredFiles : files;
				foreach (ProjectFile file in allFiles) {
					if (!searchCycleActive) 
						yield break;
					SearchResult curResult = CheckFile (file, toMatch);
					if (curResult != null) {
						newFilteredFiles.Add (file);
						yield return curResult;
					}
				}
				filteredFiles = newFilteredFiles;
			} else {
				filteredFiles = null;
			}
			
			// Search Types
			if ((NavigateToType & NavigateToType.Types) == NavigateToType.Types) {
				WaitForCollectTypes ();
				List<IType> newFilteredTypes = new List<IType> ();
				bool startsWithLastFilter = previousPattern != null && toMatch.StartsWith (previousPattern) && filteredTypes != null;
				List<IType> allTypes = startsWithLastFilter ? filteredTypes : types;
				foreach (IType type in allTypes) {
					if (!searchCycleActive) 
						yield break;
					SearchResult curResult = CheckType (type, toMatch);
					if (curResult != null) {
						newFilteredTypes.Add (type);
						yield return curResult;
					}
				}
				filteredTypes = newFilteredTypes;
			} else {
				filteredTypes = null;
			}
			
			// Search members
			if ((NavigateToType & NavigateToType.Members) == NavigateToType.Members) {
				WaitForCollectMembers ();
				List<IMember> newFilteredMembers = new List<IMember> ();
				bool startsWithLastFilter = previousPattern != null && toMatch.StartsWith (previousPattern) && filteredMembers != null;
				List<IMember> allMembers = startsWithLastFilter ? filteredMembers : members;
				foreach (IMember member in allMembers) {
					if (!searchCycleActive) 
						yield break;
					SearchResult curResult = CheckMember (member, toMatch);
					if (curResult != null) {
						newFilteredMembers.Add (member);
						yield return curResult;
					}
				}
				filteredMembers = newFilteredMembers;
			} else {
				filteredMembers = null;
			}
			
			previousPattern = toMatch;
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
					return String.CompareOrdinal (o1.PlainText, o2.PlainText);
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
			if (MatchName (System.IO.Path.GetFileName (file.FilePath), toMatch, out rank)) 
				return new FileSearchResult (toMatch, rank, file, true);
			
			if (MatchName (FileSearchResult.GetRelProjectPath (file), toMatch, out rank)) 
				return new FileSearchResult (toMatch, rank, file, false);
			
			return null;
		}
		
		SearchResult CheckType (IType type, string toMatch)
		{
			int rank;
			if (MatchName (type.Name, toMatch, out rank))
				return new TypeSearchResult (toMatch, rank, type, false);
			if (MatchName (type.FullName, toMatch, out rank))
				return new TypeSearchResult (toMatch, rank, type, true);
			return null;
		}
		
		SearchResult CheckMember (IMember member, string toMatch)
		{
			int rank;
			string memberName = (member is IMethod && ((IMethod)member).IsConstructor) ? member.DeclaringType.Name : member.Name;
			if (!MatchName (memberName, toMatch, out rank))
				return null;
			return new MemberSearchResult (toMatch, rank, member);
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
	
	abstract class SearchResult
	{
		protected string match;
		
		public virtual string MarkupText {
			get {
				return HighlightMatch (PlainText, match);
			}
		}
		
		public abstract string PlainText  { get; }
		
		public int Rank { get; private set; }

		public virtual int Row { get { return -1; } }
		public virtual int Column { get { return -1; } }
		
		public abstract string File { get; }
		public abstract Gdk.Pixbuf Icon { get; }
		
		public abstract string Description { get; }
		
		public SearchResult (string match, int rank)
		{
			this.match = match;
			Rank = rank;
		}
		
		protected static string HighlightMatch (string text, string toMatch)
		{
			var lane = !string.IsNullOrEmpty (toMatch) ? NavigateToDialog.MatchString (text, toMatch) : null;
			if (lane != null) {
				StringBuilder result = new StringBuilder ();
				int lastPos = 0;
				for (int n=0; n <= lane.Index; n++) {
					int pos = lane.Positions [n];
					int len = lane.Lengths [n];
					if (pos - lastPos > 0)
						result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, pos - lastPos)));
					result.Append ("<span foreground=\"blue\">");
					result.Append (GLib.Markup.EscapeText (text.Substring (pos, len)));
					result.Append ("</span>");
					lastPos = pos + len;
				}
				if (lastPos < text.Length)
					result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, text.Length - lastPos)));
				return result.ToString ();
			}
			
			return GLib.Markup.EscapeText (text);
		}
	}

	class TypeSearchResult : MemberSearchResult
	{
		bool useFullName;
		
		public override string File {
			get { return ((IType)member).CompilationUnit.FileName; }
		}
		
		protected override OutputFlags Flags {
			get {
				return OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics | (useFullName  ? OutputFlags.UseFullName : OutputFlags.None);
			}
		}
		
		public override string Description {
			get {
				if (useFullName)
					return String.Format (GettextCatalog.GetString ("from Project \"{0}\""), ((IType)member).SourceProject.Name);
				return String.Format (GettextCatalog.GetString ("from Project \"{0} in {1}\""), ((IType)member).SourceProject.Name, ((IType)member).Namespace);
			}
		}
		
		public TypeSearchResult (string match, int rank, IType type, bool useFullName) : base (match, rank, type)
		{
			this.useFullName = useFullName;
		}
	}
	
	class FileSearchResult: SearchResult
	{
		ProjectFile file;
		bool useFileName;
		
		public override string PlainText {
			get {
				if (useFileName)
					return System.IO.Path.GetFileName (file.FilePath);
				return GetRelProjectPath (file);
			}
		}
		 
		public override string File {
			get {
				return file.FilePath;
			}
		}
		
		public override Gdk.Pixbuf Icon {
			get {
				return DesktopService.GetPixbufForFile (file.FilePath, IconSize.Menu);
			}
		}

		public override string Description {
			get {
				if (useFileName)
					return String.Format (GettextCatalog.GetString ("from \"{0}\" in Project \"{1}\""), GetRelProjectPath (file), file.Project.Name);
				return String.Format (GettextCatalog.GetString ("from Project \"{0}\""), file.Project.Name);
			}
		}
		
		public FileSearchResult (string match, int rank, ProjectFile file, bool useFileName) : base (match, rank)
		{
			this.file = file;
			this.useFileName = useFileName;
		}
		
		internal static string GetRelProjectPath (ProjectFile file)
		{
			if (file.Project != null)
				return file.ProjectVirtualPath;
			return file.FilePath;
		}
	}
	
	class MemberSearchResult : SearchResult
	{
		protected IMember member;
		
		protected virtual OutputFlags Flags {
			get {
				return OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics;
			}
		}
		
		public override string MarkupText {
			get {
				
				OutputSettings settings = new OutputSettings (Flags | OutputFlags.IncludeMarkup);
				settings.EmitNameCallback = delegate (INode domVisitable, ref string outString) {
					if (domVisitable == member)
						outString = HighlightMatch (outString, match);
				};
				return Ambience.GetString (member, settings);
			}
		}
		
		public override string PlainText {
			get {
				return Ambience.GetString (member, Flags);
			}
		}
		
		public override string File {
			get { return member.DeclaringType.CompilationUnit.FileName; }
		}

		public override Gdk.Pixbuf Icon {
			get {
				return ImageService.GetPixbuf (member.StockIcon, IconSize.Menu);
			}
		}
		
		public override int Row {
			get { return member.Location.Line; }
		}
		
		public override int Column {
			get { return member.Location.Column; }
		}
		
		public override string Description {
			get {
				return String.Format (GettextCatalog.GetString ("from Type \"{0}\""), member.DeclaringType.Name);
			}
		}
		
		public MemberSearchResult (string match, int rank, IMember member) : base (match, rank)
		{
			this.member= member;
		}
		
		protected Ambience Ambience { 
			get {
				IType type = member is IType ? (IType)member : member.DeclaringType;
				if (type.SourceProject is DotNetProject)
					return ((DotNetProject)type.SourceProject).Ambience;
				return AmbienceService.DefaultAmbience;
			}
		}
	}
	
	class ResultsDataSource: List<SearchResult>, IListViewDataSource
	{
		SearchResult bestResult;
		int bestRank = int.MinValue;
		Dictionary<string,bool> names = new Dictionary<string,bool> ();

		public string GetText (int n)
		{
			string descr = this[n].Description;
			if (string.IsNullOrEmpty (descr))
				return this[n].MarkupText;
			return this[n].MarkupText + " <span foreground=\"darkgray\">[" + descr + "]</span>";
		}
		
		public string GetSelectedText (int n)
		{
			string descr = this[n].Description;
			if (string.IsNullOrEmpty (descr))
				return GLib.Markup.EscapeText (this[n].PlainText);
			return GLib.Markup.EscapeText (this[n].PlainText) + " [" + descr + "]";
		}

		public Pixbuf GetIcon (int n)
		{
			return this[n].Icon;
		}
		
		public bool UseMarkup (int n)
		{
			return true;
		}
				
		public int ItemCount {
			get {
				return Count;
			}
		}

		public SearchResult BestResult {
			get {
				return bestResult;
			}
		}

		public void AddResult (SearchResult res)
		{
			Add (res);
			if (names.ContainsKey (res.PlainText))
				names[res.PlainText] = true;
			else
				names.Add (res.PlainText, false);
			if (res.Rank > bestRank) {
				bestResult = res;
				bestRank = res.Rank;
			}
		}
	}
}

