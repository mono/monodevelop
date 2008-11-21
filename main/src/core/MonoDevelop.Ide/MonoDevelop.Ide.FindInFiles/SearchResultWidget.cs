//
// SearchResultWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;
using Gdk;
using Pango;

namespace MonoDevelop.Ide.FindInFiles
{
	[Flags]
	public enum GroupMode
	{
		None              = 0,
		GroupByProject    = 1,
		GroupByDirectory  = 2,
		GroupByFile       = 4
	}
	
	public class SearchResultWidget : Gtk.DrawingArea
	{
		public GroupMode GroupMode { get; set; }
		
		Pango.Layout layout, headerLayout;
		
		public SearchResultWidget ()
		{
			this.Events =  EventMask.ExposureMask | 
				           EventMask.EnterNotifyMask |
				           EventMask.LeaveNotifyMask |
				           EventMask.ButtonPressMask | 
				           EventMask.ButtonReleaseMask | 
				           EventMask.KeyPressMask | 
				           EventMask.PointerMotionMask;
			this.CanFocus = true;
			layout = new Pango.Layout (this.PangoContext);
			headerLayout = new Pango.Layout (this.PangoContext);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			int xpos = spacing - (this.hAdjustement != null ? (int)this.hAdjustement.Value : 0);
			int ypos = spacing - (this.vAdjustement != null ? (int)this.vAdjustement.Value : 0);
			
			Iterate (ref xpos, ref ypos, delegate (Category category, Gdk.Size itemDimension) {
				//TODO: Draw categories
			}, delegate (Category curCategory, SearchResult searchResult, Gdk.Size itemDimension) {
				//TODO: Draw search results
			});
			return true;
		}
		
		// From Mono.TextEditor.FoldMarkerMargin
		void DrawFoldSegment (Gdk.Drawable win, int x, int y, int w, int h, bool isOpen, bool selected)
		{
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x, y, w, h);
			
			win.DrawRectangle (selected ? Style.ForegroundGC (StateType.Normal) : Style.BaseGC (StateType.Normal), true, drawArea);
			win.DrawRectangle (selected ? Style.ForegroundGC (StateType.Normal) : Style.DarkGC (StateType.Normal), false, drawArea);
			
			win.DrawLine (selected ? Style.BaseGC (StateType.Normal) : Style.ForegroundGC (StateType.Normal), 
			              drawArea.Left  + drawArea.Width * 3 / 10,
			              drawArea.Top + drawArea.Height / 2,
			              drawArea.Right - drawArea.Width * 3 / 10,
			              drawArea.Top + drawArea.Height / 2);
			
			if (!isOpen)
				win.DrawLine (selected ? Style.BaseGC (StateType.Normal) : Style.ForegroundGC (StateType.Normal), 
				              drawArea.Left + drawArea.Width / 2,
				              drawArea.Top + drawArea.Height * 3 / 10,
				              drawArea.Left  + drawArea.Width / 2,
				              drawArea.Bottom - drawArea.Height * 3 / 10);
		}
		
		protected override void OnDestroyed ()
		{
			if (this.layout != null) {
				this.layout.Dispose ();
				this.layout = null;
			}
			if (this.headerLayout != null) {
				this.headerLayout.Dispose ();
				this.headerLayout = null;
			}
			base.OnDestroyed ();
		}
		
		List<SearchResult> searchResults = new List<SearchResult> ();
		List<Category> categories = new List<Category> ();
		
		public void Clear ()
		{
			searchResults.Clear ();
			this.QueueDraw ();
		}
		
		public void Add (SearchResult result)
		{
			searchResults.Add (result);
			Category category = SearchCategory (result);
			category.SearchResults.Add (result);
			this.QueueDraw ();
		}
		
		Category SearchCategory (SearchResult searchResult)
		{
			Category result = null;
			if ((GroupMode & GroupMode.GroupByProject) == GroupMode.GroupByProject) 
				result = SearchCategory (searchResult.Project);
			if ((GroupMode & GroupMode.GroupByDirectory) == GroupMode.GroupByDirectory)
				result = SearchCategoryByDirectory (result != null ? result.Categories : categories, System.IO.Path.GetDirectoryName (System.IO.Path.GetFullPath (searchResult.FileName)));
			
			if ((GroupMode & GroupMode.GroupByFile) == GroupMode.GroupByFile)
				result = SearchCategoryByDirectory (result != null ? result.Categories : categories, System.IO.Path.GetDirectoryName (System.IO.Path.GetFullPath (searchResult.FileName)));
			
			if (result  == null) 
				LoggingService.LogError ("Couldn't find category for search result:" + searchResult);
			
			return result;
		}
		
		static Category SearchCategoryByDirectory (List<Category> categories, string directoryName)
		{
			foreach (Category category in categories) {
				if (category.Name == directoryName)
					return category;
			}
			Category result = new Category ();
			result.Name = directoryName;
			categories.Add (result);
			return result;
		}
		
		Category SearchCategory (Project project)
		{
			foreach (Category category in categories) {
				if (category.Tag == project)
					return category;
			}
			Category result = new Category ();
			result.Name = project.Name;
			result.Tag  = project;
			return result;
		}
		
		#region Scrolling
		Adjustment hAdjustement = null;
		Adjustment vAdjustement = null;
				
		public void ScrollToSelectedItem ()
		{
			// TODO
		}
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			this.hAdjustement = hAdjustement;
			this.hAdjustement.ValueChanged += delegate {
				this.QueueDraw ();
			};
			this.vAdjustement = vAdjustement;
			this.vAdjustement.ValueChanged += delegate {
				this.QueueDraw ();
			};
		}
		#endregion

		class Category
		{
			public string Name { get; set; }
			public Gdk.Image Icon { get; set; }
			public bool IsExpanded { get; set; }
			public object Tag { get; set; }
			public List<Category> Categories { get; set; }
			public List<SearchResult> SearchResults { get; set; }
			
			public Category ()
			{
				Categories = new List<Category> ();
				SearchResults = new List<SearchResult> ();
			}
		}
		#region Item & Category iteration
		const int spacing = 4;
		const int categoryHeaderSize = 20;
		Gdk.Size iconSize = new Gdk.Size (24, 24);
		Gdk.Size IconSize {
			get {
				return iconSize;
			}
		}
		
		delegate void CategoryAction (Category category, Gdk.Size categoryDimension);
		delegate void SearchResultAction (Category curCategory, SearchResult searchResult, Gdk.Size itemDimension);
		
		void IterateSearchResults (Category category, ref int xpos, ref int ypos, SearchResultAction action)
		{
			foreach (SearchResult searchResult in category.SearchResults) {
				xpos = spacing;
				if (action != null)
					action (category, searchResult, new Gdk.Size (Allocation.Width - spacing * 2, IconSize.Height));
				ypos += IconSize.Height + spacing;
			}
		}
		
		void Iterate (ref int xpos, ref int ypos, CategoryAction catAction, SearchResultAction action)
		{
			foreach (Category category in this.categories) {
				xpos = spacing;
				catAction (category, new Size (this.Allocation.Width - spacing * 2, categoryHeaderSize));
				if (category.IsExpanded)
					IterateSearchResults (category, ref xpos, ref  ypos, action);
			}
		}
		#endregion
		
	}
}
