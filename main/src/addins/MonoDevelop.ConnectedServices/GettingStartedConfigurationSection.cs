using System;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Basic implementation of a ConfigurationSection that represents a getting started section that display code snippets to the user
	/// </summary>
	public abstract class GettingStartedConfigurationSection : ConfigurationSection
	{
		protected GettingStartedConfigurationSection (IConnectedService service, int snippetCount) : base (service, ConnectedServices.GettingStartedSectionDisplayName, false)
		{
			this.SnippetCount = snippetCount;
		}

		public int SnippetCount { get; private set; }

		public override Control GetSectionWidget ()
		{
			// TODO: create a wiget that contains the sections of code snippets that have been set in tabs
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Gets the code snippet title to show for the given snippet index
		/// </summary>
		protected virtual string GetSnippetTitle (int snippet)
		{
			return GettextCatalog.GetString ("Snippet {0}", snippet);
		}

		/// <summary>
		/// Gets the code snippet to show for the given snippet index
		/// </summary>
		protected virtual string GetSnippet(int snippet)
		{
			return string.Empty;
		}
	}
}