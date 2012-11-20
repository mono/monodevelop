using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Net;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core;
using System.Collections.Generic;


namespace MonoDevelop.WebReferences
{
	/// <summary>Defines the properties and methods for the WebReferenceItem class.</summary>
	public class WebReferenceItem
	{
		DotNetProject project;
		string name;
		ProjectFile mapFile;
		WebServiceEngine engine;
		
		public string Name
		{
			get { return name; }
			set { name = value; }
		}
		
		public ProjectFile MapFile {
			get { return this.mapFile; }
		}
		
		public FilePath BasePath { get; private set; }
		
		public DotNetProject Project {
			get { return this.project; }
		}
		
		/// <summary>Initializes a new instance of the WebReferenceItem class.</summary>
		/// <param name="name">A string containing the name for the web reference.</param>
		public WebReferenceItem (WebServiceEngine engine, DotNetProject project, string name, FilePath basePath, ProjectFile mapFile)
		{
			this.engine = engine;
			this.name = name;
			this.project = project;
			this.mapFile = mapFile;
			BasePath = basePath.CanonicalPath;
		}
		
		/// <summary>Update the web reference item by using the map file.</summary>
		public void Update()
		{
			WebServiceDiscoveryResult service = engine.Load (this);
			service.Update ();
		}
		
		/// <summary>Delete the web reference from the project.</summary>
		public void Delete()
		{
			engine.Delete (this);
			WebReferencesService.NotifyWebReferencesChanged (project);
		}

		public WebServiceDiscoveryResult Load ()
		{
			return engine.Load (this);
		}
	}	
}