// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Ambience
{
	public class AmbienceService : AbstractService
	{
		static readonly string ambienceProperty       = "SharpDevelop.UI.CurrentAmbience";
		static readonly string codeGenerationProperty = "SharpDevelop.UI.CodeGenerationOptions";
		
		Hashtable ambiences = new Hashtable ();

		public Properties CodeGenerationProperties {
			get {
				return PropertyService.Get (codeGenerationProperty, new Properties());
			}
		}
		
		public bool GenerateDocumentComments {
			get {
				return CodeGenerationProperties.Get("GenerateDocumentComments", true);
			}
		}
		
		public bool GenerateAdditionalComments {
			get {
				return CodeGenerationProperties.Get("GenerateAdditionalComments", true);
			}
		}
		
		public bool UseFullyQualifiedNames {
			get {
				return CodeGenerationProperties.Get("UseFullyQualifiedNames", true);
			}
		}
		
		public Ambience GenericAmbience {
			get {
				return AmbienceFromName(".NET");
			}
		}
		
		public string[] AvailableAmbiences {
			get {
				ExtensionNodeList ambiencesNodes = AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/Ambiences");
				string[] availableAmbiences = new string [ambiencesNodes.Count];
				int index = 0;
				foreach (ExtensionNode node in ambiencesNodes)
					availableAmbiences [index++] = node.Id;
				
				return availableAmbiences;
			}
		}
		
		public Ambience AmbienceFromName (string name)
		{
			Ambience amb = (Ambience) ambiences [name];
			
			if (amb == null) {
				TypeExtensionNode node = (TypeExtensionNode) AddinManager.GetExtensionNode ("/MonoDevelop/ProjectModel/Ambiences/" + name);
				if (node != null) {
					amb = (Ambience) node.CreateInstance ();
				} else {
					amb = GenericAmbience;
				}
				ambiences [name] = amb;
			}
			return amb;
		}
		
		public Ambience GetAmbienceForFile (string fileName)
		{
			ILanguageBinding lang = Services.Languages.GetBindingPerFileName (fileName);
			if (lang != null) {
				Ambience a = AmbienceFromName (lang.Language);
				if (a != null)
					return a;
			}
			return GenericAmbience;
		}
		
		public Ambience CurrentAmbience {
			get {
				string language = PropertyService.Get (ambienceProperty, ".NET");
				return AmbienceFromName(language);
			}
		}
		
		/*void PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == ambienceProperty) {
				OnAmbienceChanged(EventArgs.Empty);
			}
		}
		
		public override void InitializeService()
		{
			PropertyService.PropertyChanged += new PropertyEventHandler(PropertyChanged);
		}
		
		public override void UnloadService()
		{
			PropertyService.PropertyChanged -= new PropertyEventHandler(PropertyChanged);
		}
		
		protected virtual void OnAmbienceChanged(EventArgs e)
		{
			if (AmbienceChanged != null) {
				AmbienceChanged(this, e);
			}
		}
		
		public event EventHandler AmbienceChanged;*/
	}
}
