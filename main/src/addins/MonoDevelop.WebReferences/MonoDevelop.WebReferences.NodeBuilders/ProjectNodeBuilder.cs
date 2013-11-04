using System;
using System.Linq;
using MonoDevelop.Projects;
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
			var project = (DotNetProject) dataObject;
			return WebReferencesService.GetWebReferenceItems (project).Any ();
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var project = (DotNetProject) dataObject;
			if (WebReferencesService.GetWebReferenceItems (project).Any ())
				treeBuilder.AddChild (new WebReferenceFolder (project));
		}

		void HandleWebReferencesServiceWebReferencesChanged (object sender, WebReferencesChangedEventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (e.Project);
			if (builder != null)
				builder.UpdateChildren ();
		}
	}
}