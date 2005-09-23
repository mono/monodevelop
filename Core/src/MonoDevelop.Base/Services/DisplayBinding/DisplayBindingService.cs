// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.CodeDom.Compiler;

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	/// <summary>
	/// This class handles the installed display bindings
	/// and provides a simple access point to these bindings.
	/// </summary>
	public class DisplayBindingService : AbstractService
	{
		readonly static string displayBindingPath = "/SharpDevelop/Workbench/DisplayBindings";
		DisplayBindingCodon[] bindings = null;

		public IDisplayBinding LastBinding {
			get {
				return bindings[0].DisplayBinding;
			}
		}
		
		public IDisplayBinding GetBindingPerFileName(string filename)
		{
			DisplayBindingCodon codon = GetCodonPerFileName(filename);
			return codon == null ? null : codon.DisplayBinding;
		}
		
		public IDisplayBinding GetBindingPerLanguageName(string languagename)
		{
			DisplayBindingCodon codon = GetCodonPerLanguageName(languagename);
			return codon == null ? null : codon.DisplayBinding;
		}
		
		public DisplayBindingCodon GetCodonPerFileName(string filename)
		{
			string mimetype = Gnome.Vfs.MimeType.GetMimeTypeForUri (filename);
			if (!filename.StartsWith ("http")) {
				foreach (DisplayBindingCodon binding in bindings) {
					if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForMimeType (mimetype)) {
						return binding;
					}
				}
			}
			Runtime.LoggingService.Info (String.Format (GettextCatalog.GetString ("Didnt match on mimetype: {0}, trying filename"), mimetype));
			foreach (DisplayBindingCodon binding in bindings) {
				if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForFile(filename)) {
					return binding;
				}
			}
			return null;
		}
		
		public DisplayBindingCodon GetCodonPerLanguageName(string languagename)
		{
			foreach (DisplayBindingCodon binding in bindings) {
				if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForLanguage(languagename)) {
					return binding;
				}
			}
			return null;
		}
		
		public void AttachSubWindows(IWorkbenchWindow workbenchWindow)
		{
			foreach (DisplayBindingCodon binding in bindings) {
				if (binding.SecondaryDisplayBinding != null && binding.SecondaryDisplayBinding.CanAttachTo(workbenchWindow.ViewContent)) {
					workbenchWindow.AttachSecondaryViewContent(binding.SecondaryDisplayBinding.CreateSecondaryViewContent(workbenchWindow.ViewContent));
				}
			}
		}
		
		public DisplayBindingService()
		{
			bindings = (DisplayBindingCodon[])(AddInTreeSingleton.AddInTree.GetTreeNode(displayBindingPath).BuildChildItems(this)).ToArray(typeof(DisplayBindingCodon));
		}
	}
}
