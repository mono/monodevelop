//  DisplayBindingService.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Codons;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This class handles the installed display bindings
	/// and provides a simple access point to these bindings.
	/// </summary>
	public class DisplayBindingService
	{
		readonly static string displayBindingPath = "/MonoDevelop/Ide/DisplayBindings";
		
		public IDisplayBinding LastBinding {
			get {
				return ((DisplayBindingCodon)AddinManager.GetExtensionNodes (displayBindingPath)[0]).DisplayBinding;
			}
		}
		
		public IDisplayBinding GetBindingPerFileName(string filename)
		{
			DisplayBindingCodon codon = GetCodonPerFileName(filename);
			return codon == null ? null : codon.DisplayBinding;
		}
		
		public IDisplayBinding GetBindingForMimeType (string mimeType)
		{
			foreach (DisplayBindingCodon binding in AddinManager.GetExtensionNodes (displayBindingPath)) {
				if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForMimeType (mimeType)) {
					return binding.DisplayBinding;
				}
			}
			return null;
		}
		
		public IDisplayBinding[] GetBindingsForMimeType (string mimeType)
		{
			List<IDisplayBinding> result = new List<IDisplayBinding> ();
			foreach (DisplayBindingCodon binding in AddinManager.GetExtensionNodes (displayBindingPath)) {
				if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForMimeType (mimeType)) 
					result.Add (binding.DisplayBinding);
			}
			return result.ToArray ();
		}
		
		internal DisplayBindingCodon GetCodonPerFileName(string filename)
		{
			string vfsname = filename;
			vfsname = vfsname.Replace ("%", "%25");
			vfsname = vfsname.Replace ("#", "%23");
			vfsname = vfsname.Replace ("?", "%3F");
			string mimetype = IdeApp.Services.PlatformService.GetMimeTypeForUri (vfsname);
			foreach (DisplayBindingCodon binding in AddinManager.GetExtensionNodes (displayBindingPath)) {
				if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForFile(filename)) {
					return binding;
				}
			}
			if (!filename.StartsWith ("http")) {
				foreach (DisplayBindingCodon binding in AddinManager.GetExtensionNodes (displayBindingPath)) {
					if (binding.DisplayBinding != null && binding.DisplayBinding.CanCreateContentForMimeType (mimetype)) {
						return binding;
					}
				}
			}
			return null;
		}
		
		internal void AttachSubWindows(IWorkbenchWindow workbenchWindow)
		{
			foreach (DisplayBindingCodon binding in AddinManager.GetExtensionNodes (displayBindingPath)) {
				if (binding.SecondaryDisplayBinding != null && binding.SecondaryDisplayBinding.CanAttachTo(workbenchWindow.ViewContent)) {
					workbenchWindow.AttachSecondaryViewContent(binding.SecondaryDisplayBinding.CreateSecondaryViewContent(workbenchWindow.ViewContent));
				}
			}
		}
	}
}
