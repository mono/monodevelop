using System;
using System.Linq;
using System.Collections;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.WebReferences.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.WebReferences.NodeBuilders
{
	/// <summary>Defines the properties and methods for the ProjectFolderNodeBuilderExtension class.</summary>
	public class ProjectNodeBuilder: NodeBuilderExtension
	{
		protected override void Initialize ()
		{
			base.Initialize ();
			WebReferencesService.WebReferencesChanged += HandleWebReferencesServiceWebReferencesChanged;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			WebReferencesService.WebReferencesChanged -= HandleWebReferencesServiceWebReferencesChanged;
		}
		
		public override Type CommandHandlerType 
		{
			get { return typeof(WebReferenceCommandHandler); }
		}
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(DotNetProject).IsAssignableFrom (dataType);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			DotNetProject project = (DotNetProject) dataObject;
			return WebReferencesService.GetWebReferenceItems (project).Any ();
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			DotNetProject project = (DotNetProject) dataObject;
			if (WebReferencesService.GetWebReferenceItems (project).Any ())
				builder.AddChild (new WebReferenceFolder (project));
		}

		void HandleWebReferencesServiceWebReferencesChanged (object sender, WebReferencesChangedArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (e.Project);
			if (builder != null)
				builder.UpdateChildren ();
		}
	}
}