// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
