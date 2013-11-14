using MonoDevelop.Projects;

namespace MonoDevelop.WebReferences
{
	/// <summary>Defines the properties and methods for the WebReferenceFolder class.</summary>
	public class WebReferenceFolder
	{
		#region Properties
		/// <summary>Gets the parent Project for the Web Reference Folder.</summary>
		/// <value>A Project containing the parent project for the current Web Reference Folder.</value>
		public DotNetProject Project
		{
			get { return project; }
		}
		#endregion
		
		#region Member Variables
		readonly DotNetProject project;
		#endregion
		
		/// <summary>Initializes a new instance of the WebReferenceFolder class by specifying the parent project.</summary>
		/// <param name="project">A Project containing the parent project for the WebReferenceFolder.</param>
		public WebReferenceFolder (DotNetProject project)
		{
			this.project = project;
		}
		
		/// <summary>Checks if the specified other object is equal to the current object.</summary>
		/// <param name="obj">An object containing the object that needs to be compared to the current object.</param>
		/// <returns>True of the other object is equal to the current object, otherwise false.</returns>
		public override bool Equals (object obj)
		{
			var folder = obj as WebReferenceFolder;
			return folder != null && project == folder.project;
		}
		
		/// <summary>Get ths Has Code for the current WebReferenceFolder.</summary>
		/// <returns>An int containing the HasCode for the current WebReferenceFolder.</returns>
		public override int GetHashCode ()
		{
			return project.GetHashCode () + 2;
		}
	}
	
}
