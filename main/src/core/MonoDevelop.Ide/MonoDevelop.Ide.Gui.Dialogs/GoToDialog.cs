//
// GoToDialog.cs
//
// Author:
//   Zach Lute (zach.lute@gmail.com)
//   Aaron Bockover (abockover@novell.com)
//   Jacob Ilsø Christensen
//   Lluis Sanchez
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2007 Zach Lute
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class GoToDialog : Gtk.Dialog
	{
		ListView list;
		ResultsDataSource currentResults;
		Dictionary<string, Pixbuf> icons = new Dictionary<string, Pixbuf> ();
		
		object matchLock;
		string matchString;
		
		// Thread management
		Thread searchThread;
		AutoResetEvent searchThreadWait;
		bool searchCycleActive;
		bool searchThreadDispose;
		
		bool searchFiles;
		bool updating;
		bool userSelecting;
		
		string filename;
		int fileLine;
		int fileCol;
		
		protected string Filename {
			get { return filename; }
			set { filename = value; }
		}
		
		protected int FileLine {
			get { return fileLine; }
		}
		
		protected int FileColumn {
			get { return fileCol; }
		}
		
		protected bool SearchFiles {
			get { return searchFiles; }
			set {
				if (searchFiles == value) {
					return;
				}
				
				searchFiles = value;
				Title = searchFiles 
					? GettextCatalog.GetString ("Go to File")
					: GettextCatalog.GetString ("Go to Type");
				
				UpdateList ();
			}
		}
		
		protected GoToDialog (bool searchFiles)
		{	
			this.searchFiles = searchFiles;
			
			matchLock = new object ();
			matchString = String.Empty;
			
			Build ();
			SetupTreeView ();
			matchEntry.GrabFocus ();
			
			SearchFiles = searchFiles;
		}
		
	    public static void Run (bool searchFiles)
		{
			GoToDialog dialog = new GoToDialog (searchFiles);
			try {
				if ((ResponseType)dialog.Run () == ResponseType.Ok) {
					IdeApp.Workbench.OpenDocument (dialog.Filename, dialog.FileLine, dialog.FileColumn, true);
				}
			} finally {
				dialog.Destroy ();
			}
	    }
		
		private void SetupTreeView ()
		{
			list = new ListView ();
			currentResults = new ResultsDataSource ();
			list.DataSource = currentResults;
			list.Show ();
			list.ItemActivated += HandleOpen;
			scrolledwindow.Add (list);
		}
		
		protected void HandleShown (object sender, System.EventArgs e)
		{
			// Perform the search over in case things have changed.
			PerformSearch ();
			
			// Highlight the text so they can quickly type over it.
			matchEntry.SelectRegion (0, matchEntry.Text.Length);
			matchEntry.GrabFocus ();
		}

		protected virtual void HandleOpen (object o, EventArgs args)
		{
			OpenFile ();
		}
		
		protected virtual void HandleEntryActivate (object o, EventArgs args)
		{
			OpenFile ();
		}
		
		private void OpenFile ()
		{
			if (list.Selection != -1) {
				SearchResult res = currentResults [list.Selection];
				Filename = res.File;
				fileLine = res.Row;
				fileCol = res.Column;
				Respond (ResponseType.Ok);
			} else {
				Filename = String.Empty;
				Respond (ResponseType.Cancel);
			}
		}

		protected virtual void HandleEntryChanged (object sender, System.EventArgs e)
		{
			// Find the matching files and display them in the tree.
			PerformSearch ();
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
			UpdateList ();
		}
		
		public override void Destroy ()
		{
			// Set the thread into a dispose state and wake it up so it can exit
			if (searchCycleActive && searchThread != null && searchThreadWait != null) {
				searchCycleActive = false;
				searchThreadDispose = true;
				searchThreadWait.Set ();
			}
			
			base.Destroy ();
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
			userSelecting = false;
			
			StopActiveSearch ();
			
			string toMatch = matchEntry.Text.ToLower ();
				
			lock (matchLock) {
				matchString = toMatch;
			}

			if (searchThread == null) {
				// Create the handle the search thread will wait on when there is nothing to do
				searchThreadWait = new AutoResetEvent (false);
				
				// Only a single thread will be used for searching
				ThreadStart start = new ThreadStart (SearchThread);
				searchThread = new Thread (start);
				searchThread.IsBackground = true;
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
					LoggingService.LogError ("Exception in GoToDialog", ex);
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

		List<string> files;
		List<IType> types;
		List<string> filteredFiles;
		List<IType> filteredTypes;
		string previousPattern;
		
		void SearchThreadCycle ()
		{
			// This is the inner thread worker; it actually does the searching
			// Any where we enter loop, a check is added to see if the search
			// should be aborted entirely so we can return to the wait handle

			ResultsDataSource results = new ResultsDataSource ();
			string toMatch = matchString;

			if (searchFiles) {
				// Get the list of files. If the parttern is a refinement of the previous
				// one, use the list filtered in the previous search.
				List<string> allFiles;
				if (previousPattern != null && toMatch.StartsWith (previousPattern) && filteredFiles != null)
					allFiles = filteredFiles;
				else if (files == null)
					allFiles = files = GetFiles ();
				else
					allFiles = files;

				List<string> newFilteredFiles = new List<string> ();
				foreach (string file in allFiles) {
					if (!searchCycleActive) return;
					if (CheckFile (results, file, toMatch))
						newFilteredFiles.Add (file);
				}
				previousPattern = toMatch;
				filteredFiles = newFilteredFiles;
			}
			else {
				// Get the list of types. If the parttern is a refinement of the previous
				// one, use the list filtered in the previous search.
				List<IType> allTypes;
				if (previousPattern != null && toMatch.StartsWith (previousPattern) && filteredTypes != null)
					allTypes = filteredTypes;
				else if (types == null)
					allTypes = types = GetTypes ();
				else
					allTypes = types;
				
				List<IType> newFilteredTypes = new List<IType> ();
				foreach (IType type in types) {
					if (!searchCycleActive) return;
					if (CheckType (results, type, toMatch))
						newFilteredTypes.Add (type);
				}
				previousPattern = toMatch;
				filteredTypes = newFilteredTypes;
			}

			results.FixDuplicateNames ();
			
			results.Sort (delegate (SearchResult o1, SearchResult o2) {
				return o1.PlainText.CompareTo (o2.PlainText);
			});
			
			int best = results.IndexOf (results.BestResult);
			if (best == -1)
				best = 0;

			Application.Invoke (delegate {
				list.DataSource = results;
				currentResults = results;
				list.Selection = best;
				list.CenterViewToSelection ();
			});
		}

		List<string> GetFiles ()
		{
			List<string> list = new List<string> ();
			foreach (Document doc in IdeApp.Workbench.Documents) {
				// We only want to check it here if it's not part
				// of the open combine.  Otherwise, it will get
				// checked down below.
				if (doc.Project == null && doc.IsFile)
					list.Add (doc.Name);
			}
			
			ReadOnlyCollection<Project> projects = IdeApp.Workspace.GetAllProjects ();

			foreach (Project p in projects) {
				foreach (ProjectFile file in p.Files) {
					list.Add (file.FilePath);
				}
			}
			return list;
		}
		
		List<IType> GetTypes ()
		{
			List<IType> list = new List<IType> ();
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
				foreach (IType c in ProjectDomService.GetProjectDom (p).Types) {
					list.Add (c);
				}
			}
			return list;
		}

		void AddType (IType c, List<IType> list)
		{
			list.Add (c);
			foreach (IType ct in c.InnerTypes)
				AddType (ct, list);
		}
		
		bool CheckFile (ResultsDataSource results, string path, string toMatch)
		{
			string result;
			int rank;
			string fullName = System.IO.Path.GetFileName (path);
			if (!MatchName (fullName, toMatch, out result, out rank)) {
				fullName = path;
				if (!MatchName (path, toMatch, out result, out rank))
					return false;
			}

			string dirName = System.IO.Path.GetFileName (System.IO.Path.GetDirectoryName (path));
			results.AddResult (new FileSearchResult (result, fullName, dirName, path, rank));
			return true;
		}
		
		bool CheckType (ResultsDataSource results, IType c, string toMatch)
		{
			string result;
			int rank;
			string fullName = c.Name;
			if (!MatchName (fullName, toMatch, out result, out rank)) {
				fullName = c.FullName;
				if (!MatchName (fullName, toMatch, out result, out rank))
					return false;
			}

			results.AddResult (new TypeSearchResult (c, result, fullName, c.FullName, rank));
			return true;
		}

		bool MatchName (string name, string toMatch, out string matchedString, out int matchRank)
		{
			if (toMatch.Length == 0) {
				matchedString = GLib.Markup.EscapeText (name);
				matchRank = int.MinValue;
				return true;
			}
			MatchLane lane = MatchString (name, toMatch);
			if (lane != null) {
				StringBuilder sb = new StringBuilder ();
				int lastPos = 0;
				for (int n=0; n <= lane.Index; n++) {
					int pos = lane.Positions [n];
					int len = lane.Lengths [n];
					if (pos - lastPos > 0)
						sb.Append (GLib.Markup.EscapeText (name.Substring (lastPos, pos - lastPos)));
					sb.Append ("<b>");
					sb.Append (GLib.Markup.EscapeText (name.Substring (pos, len)));
					sb.Append ("</b>");
					lastPos = pos + len;
				}
				if (lastPos < name.Length)
					sb.Append (GLib.Markup.EscapeText (name.Substring (lastPos, name.Length - lastPos)));
				matchedString = sb.ToString ();
				matchRank = -(lane.Positions [0] + (name.Length - toMatch.Length));
				return true;
			}
			
			matchedString = null;
			matchRank = int.MinValue;
			return false;
		}

		MatchLane MatchString (string text, string toMatch)
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
						matchLanes.Add (new MatchLane (MatchMode.Acronim, tn, text.Length - tn));
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
							newLane.MatchMode = MatchMode.Acronim;
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
							newLane.MatchMode = MatchMode.Acronim;
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Lengths [lane.Index]++;
							lane.MatchIndex++;
						} else {
							if (lane.Lengths [lane.Index] > 1)
								lane.MatchMode = MatchMode.Acronim;
							else
								matchLanes [n] = null; // Kill the lane
						}
					}
					else if (lane.MatchMode == MatchMode.Acronim) {
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

		enum MatchMode
		{
			Substring,
			Acronim
		}

		class MatchLane
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

		public Pixbuf GetIcon (string id)
		{
			Gdk.Pixbuf icon;
			if (!icons.TryGetValue (id, out icon)) {
				icon = IdeApp.Services.Resources.GetBitmap (id, IconSize.Menu);
				icons.Add (id, icon);
			}
			return icon;
		}
		
		protected virtual void HandleKeyPress (object o, 
		                                       KeyPressEventArgs args)
		{
			// Up and down move the tree selection up and down
			// for rapid selection changes.
			Gdk.EventKey key = args.Event;
			switch (key.Key) {
			case Gdk.Key.Page_Down:
				list.Selection += list.VisibleRows;
				args.RetVal = true;
				break;
			case Gdk.Key.Page_Up:
				list.Selection -= list.VisibleRows;
				args.RetVal = true;
				break;
			case Gdk.Key.Up:
				list.Selection--;
				args.RetVal = true;
				break;
			case Gdk.Key.Down:
				list.Selection++;
				args.RetVal = true;
				break;
			}
		}
		
		
		void UpdateList ()
		{
			updating = true;
			toggleFiles.Active = searchFiles;
			toggleTypes.Active = !searchFiles;
			updating = false;
			if (Visible)
				PerformSearch ();
		}

		protected virtual void OnToggleFilesClicked(object sender, System.EventArgs e)
		{
			if (updating)
				return;
			this.SearchFiles = true;
			matchEntry.GrabFocus ();
		}

		protected virtual void OnToggleTypesClicked(object sender, System.EventArgs e)
		{
			if (updating)
				return;
			this.SearchFiles = false;
			matchEntry.GrabFocus ();
		}
	}

	abstract class SearchResult
	{
		public SearchResult (string name, string plainText, string fullName, int rank)
		{
			Text = name;
			FullName = fullName;
			Rank = rank;
			PlainText = plainText;
		}
		
		public string Text;
		public string PlainText;
		public string FullName;
		public int Rank;
		
		public virtual int Row {
			get { return -1; }
		}
		
		public virtual int Column {
			get { return -1; }
		}
		
		public abstract string File { get; }
		public abstract Gdk.Pixbuf Icon { get; }
	}

	class TypeSearchResult: SearchResult
	{
		string icon;
		IType type;
		
		public TypeSearchResult (IType type, string name, string plainText, string fullName, int rank)
			: base (name, plainText, fullName, rank)
		{
			this.icon = type.StockIcon;
			this.type = type;
		}

		public override string File {
			get { return type.CompilationUnit.FileName; }
		}

		public override Gdk.Pixbuf Icon {
			get {
				return IdeApp.Services.Resources.GetBitmap (icon, IconSize.Menu);
			}
		}
		
		public override int Row {
			get { return type.BodyRegion.Start.Line; }
		}
		
		public override int Column {
			get { return type.BodyRegion.Start.Column; }
		}

	}

	class FileSearchResult: SearchResult
	{
		string file;
		
		public FileSearchResult (string name, string plainText, string fullName, string file, int rank)
			: base (name, plainText, fullName, rank)
		{
			this.file = file;
		}

		public override string File {
			get {
				return file;
			}
		}


		public override Gdk.Pixbuf Icon {
			get {
				return IdeApp.Services.PlatformService.GetPixbufForFile (file, IconSize.Menu);
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
			return this [n].Text;
		}

		public Pixbuf GetIcon (int n)
		{
			return this [n].Icon;
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
			if (names.ContainsKey (res.Text))
				names [res.Text] = true;
			else
				names.Add (res.Text, false);
			if (res.Rank > bestRank) {
				bestResult = res;
				bestRank = res.Rank;
			}
		}

		public void FixDuplicateNames ()
		{
			foreach (SearchResult res in this) {
				bool dup = names [res.Text];
				if (dup)
					res.Text += " [" + res.FullName + "]";
			}
		}
	}
}
