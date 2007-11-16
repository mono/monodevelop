//  SearchOptions.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Undo;

namespace MonoDevelop.Ide.Gui.Search
{
	public class SearchOptions
	{
		Properties properties;
		
		public bool IgnoreCase {
			get {
				return properties.Get("IgnoreCase", false);
			}
			set {
				properties.Set("IgnoreCase", value);
			}
		}
		
		public bool SearchWholeWordOnly {
			get {
				return properties.Get("SearchWholeWordOnly", false);
			}
			
			set {
				properties.Set("SearchWholeWordOnly", value);
			}
		}
		
		public string SearchPattern {
			get {
				return properties.Get("SearchPattern", String.Empty);
			}
			set {
				properties.Set("SearchPattern", value);
			}
		}
		
		public string ReplacePattern {
			get {
				return properties.Get("ReplacePattern", String.Empty);
			}
			set {
				properties.Set("ReplacePattern", value);
			}
		}
		
		public DocumentIteratorType DocumentIteratorType {
			get {
				return (DocumentIteratorType)properties.Get("DocumentIteratorType", DocumentIteratorType.CurrentDocument);
			}
			set {
				if (DocumentIteratorType != value) {
					properties.Set("DocumentIteratorType", value);
					OnDocumentIteratorTypeChanged(EventArgs.Empty);
				}
			}
		}
		
		public SearchStrategyType SearchStrategyType {
			get {
				return (SearchStrategyType)properties.Get("SearchStrategyType", SearchStrategyType.Normal);
			}
			set {
				if (SearchStrategyType != value) {
					properties.Set("SearchStrategyType", value);
					OnSearchStrategyTypeChanged(EventArgs.Empty);
				}
			}
		}
		
		public string FileMask {
			get {
				return properties.Get("FileMask", String.Empty);
			}
			set {
				properties.Set("FileMask", value);
			}
		}

		public string SearchDirectory {
			get {
				return properties.Get("SearchDirectory", String.Empty);
			}
			set {
				properties.Set("SearchDirectory", value);
			}
		}
		
		public bool SearchSubdirectories {
			get {
				return properties.Get("SearchSubdirectories", true);
			}
			set {
				properties.Set("SearchSubdirectories", value);
			}
		}
		
		/// <remarks>
		/// For unit testing purposes
		/// </remarks>
		public SearchOptions (Properties properties)
		{
			this.properties = properties;
		}
		
		public SearchOptions(string propertyName)
		{
			properties = (Properties)PropertyService.Get (propertyName, new Properties());
		}
		
		protected void OnDocumentIteratorTypeChanged(EventArgs e)
		{
			if (DocumentIteratorTypeChanged != null) {
				DocumentIteratorTypeChanged(this, e);
			}
		}
		
		protected void OnSearchStrategyTypeChanged(EventArgs e)
		{
			if (SearchStrategyTypeChanged != null) {
				SearchStrategyTypeChanged(this, e);
			}
		}
		
		public event EventHandler DocumentIteratorTypeChanged;
		public event EventHandler SearchStrategyTypeChanged;
	}
}
