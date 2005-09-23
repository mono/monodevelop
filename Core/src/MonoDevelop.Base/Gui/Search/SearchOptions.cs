// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Undo;

namespace MonoDevelop.Gui.Search
{
	public class SearchOptions
	{
		static PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
		IProperties properties;
		
		public bool IgnoreCase {
			get {
				return properties.GetProperty("IgnoreCase", false);
			}
			set {
				properties.SetProperty("IgnoreCase", value);
			}
		}
		
		public bool SearchWholeWordOnly {
			get {
				return properties.GetProperty("SearchWholeWordOnly", false);
			}
			
			set {
				properties.SetProperty("SearchWholeWordOnly", value);
			}
		}
		
		public string SearchPattern {
			get {
				return properties.GetProperty("SearchPattern", String.Empty);
			}
			set {
				properties.SetProperty("SearchPattern", value);
			}
		}
		
		public string ReplacePattern {
			get {
				return properties.GetProperty("ReplacePattern", String.Empty);
			}
			set {
				properties.SetProperty("ReplacePattern", value);
			}
		}
		
		public DocumentIteratorType DocumentIteratorType {
			get {
				return (DocumentIteratorType)properties.GetProperty("DocumentIteratorType", DocumentIteratorType.CurrentDocument);
			}
			set {
				if (DocumentIteratorType != value) {
					properties.SetProperty("DocumentIteratorType", value);
					OnDocumentIteratorTypeChanged(EventArgs.Empty);
				}
			}
		}
		
		public SearchStrategyType SearchStrategyType {
			get {
				return (SearchStrategyType)properties.GetProperty("SearchStrategyType", SearchStrategyType.Normal);
			}
			set {
				if (SearchStrategyType != value) {
					properties.SetProperty("SearchStrategyType", value);
					OnSearchStrategyTypeChanged(EventArgs.Empty);
				}
			}
		}
		
		public string FileMask {
			get {
				return properties.GetProperty("FileMask", String.Empty);
			}
			set {
				properties.SetProperty("FileMask", value);
			}
		}

		public string SearchDirectory {
			get {
				return properties.GetProperty("SearchDirectory", String.Empty);
			}
			set {
				properties.SetProperty("SearchDirectory", value);
			}
		}
		
		public bool SearchSubdirectories {
			get {
				return properties.GetProperty("SearchSubdirectories", true);
			}
			set {
				properties.SetProperty("SearchSubdirectories", value);
			}
		}
		
		/// <remarks>
		/// For unit testing purposes
		/// </remarks>
		public SearchOptions(IProperties properties)
		{
			this.properties = properties;
		}
		
		public SearchOptions(string propertyName)
		{
			properties = (IProperties)propertyService.GetProperty(propertyName, new DefaultProperties());
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
