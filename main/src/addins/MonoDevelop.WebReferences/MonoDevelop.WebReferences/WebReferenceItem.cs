using MonoDevelop.Projects;
using MonoDevelop.Core;


namespace MonoDevelop.WebReferences
{
	/// <summary>Defines the properties and methods for the WebReferenceItem class.</summary>
	public class WebReferenceItem
	{
		readonly DotNetProject project;
		readonly ProjectFile mapFile;
		readonly WebServiceEngine engine;
		
		public string Name {
			get;
			set;
		}
		
		public ProjectFile MapFile {
			get { return mapFile; }
		}
		
		public FilePath BasePath { get; private set; }
		
		public DotNetProject Project {
			get { return project; }
		}
		
		/// <summary>Initializes a new instance of the WebReferenceItem class.</summary>
		/// <param name="name">A string containing the name for the web reference.</param>
		public WebReferenceItem (WebServiceEngine engine, DotNetProject project, string name, FilePath basePath, ProjectFile mapFile)
		{
			this.engine = engine;
			this.Name = name;
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