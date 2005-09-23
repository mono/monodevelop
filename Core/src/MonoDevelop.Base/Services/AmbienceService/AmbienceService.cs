// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Services
{
	public class AmbienceService : AbstractService
	{
		static readonly string ambienceProperty       = "SharpDevelop.UI.CurrentAmbience";
		static readonly string codeGenerationProperty = "SharpDevelop.UI.CodeGenerationOptions";
		
		public IProperties CodeGenerationProperties {
			get {
				return (IProperties) Runtime.Properties.GetProperty (codeGenerationProperty, new DefaultProperties());
			}
		}
		
		public bool GenerateDocumentComments {
			get {
				return CodeGenerationProperties.GetProperty("GenerateDocumentComments", true);
			}
		}
		
		public bool GenerateAdditionalComments {
			get {
				return CodeGenerationProperties.GetProperty("GenerateAdditionalComments", true);
			}
		}
		
		public bool UseFullyQualifiedNames {
			get {
				return CodeGenerationProperties.GetProperty("UseFullyQualifiedNames", true);
			}
		}
		
		public AmbienceReflectionDecorator CurrentAmbience {
			get {
				string language = Runtime.Properties.GetProperty(ambienceProperty, "CSharp");
				return new AmbienceReflectionDecorator((IAmbience)AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/Ambiences").BuildChildItem(language, this));
			}
		}
		
		void PropertyChanged(object sender, PropertyEventArgs e)
		{
			if (e.Key == ambienceProperty) {
				OnAmbienceChanged(EventArgs.Empty);
			}
		}
		
		public override void InitializeService()
		{
			Runtime.Properties.PropertyChanged += new PropertyEventHandler(PropertyChanged);
		}
		
		
		protected virtual void OnAmbienceChanged(EventArgs e)
		{
			if (AmbienceChanged != null) {
				AmbienceChanged(this, e);
			}
		}
		
		public event EventHandler AmbienceChanged;
	}
}
