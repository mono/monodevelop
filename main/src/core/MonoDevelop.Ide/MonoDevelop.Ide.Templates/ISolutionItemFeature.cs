// ISolutionItemFeature.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Ide.Templates
{
	public enum FeatureSupportLevel
	{
		/// <summary>
		/// The feature is not supported
		/// </summary>
		NotSupported,
		
		/// <summary>
		/// The feature is supported and it is currently enabled for the provided solution item
		/// </summary>
		Enabled,
		
		/// <summary>
		/// The feature is supported
		/// </summary>
		Supported,
		
		/// <summary>
		/// The feature is supported and it should be shown in the default list of features for the solution item
		/// </summary>
		SupportedByDefault
	}
	
	public interface ISolutionItemFeature
	{
		/// <summary>
		/// Gets the support level of this feature for the provided project
		/// </summary>
		/// <returns>
		/// The support level.
		/// </returns>
		/// <param name='parentFolder'>
		/// The parent folder of the solution item. It may be null.
		/// </param>
		/// <param name='item'>
		/// The project being checked for the feature
		/// </param>
		/// <remarks>
		/// The provided item, parent folder and parent solution may or may not have a file name, and even if they
		/// have, they may not be saved to disk. parentFolder can be null.
		/// </remarks>
		FeatureSupportLevel GetSupportLevel (SolutionFolder parentFolder, SolutionFolderItem item);
		
		/// <summary>
		/// Short title of the feature
		/// </summary>
		string Title { get; }
		
		/// <summary>
		/// Description of the feature (one or two sentences)
		/// </summary>
		string Description { get; }
		
		/// <summary>
		/// Creates a widget for editing the feature configuration
		/// </summary>
		/// <returns>
		/// The feature editor.
		/// </returns>
		/// <param name='parentFolder'>
		/// The parent folder of the solution item.
		/// </param>
		/// <param name='entry'>
		/// The project being checked for the feature
		/// </param>
		/// <remarks>
		/// The provided item, parent folder and parent solution may or may not have a file name, and even if they
		/// have, they may not be saved to disk.
		/// </remarks>
		Gtk.Widget CreateFeatureEditor (SolutionFolder parentFolder, SolutionFolderItem entry);
		
		/// <summary>
		/// Validates the configuration of the feature
		/// </summary>
		/// <returns>
		/// <c>null</c> if the configuration is correct, or an error message if there is some
		/// error in the configuration parameters specified in the editor
		/// </returns>
		/// <param name='parentFolder'>
		/// The parent folder of the solution item.
		/// </param>
		/// <param name='entry'>
		/// The project being checked for the feature
		/// </param>
		/// <param name='editor'>
		/// The feature editor.
		/// </param>
		/// <remarks>
		/// This method is always called before calling ApplyFeature
		/// </remarks>
		/// <remarks>
		/// The provided item, parent folder and parent solution may or may not have a file name, and even if they
		/// have, they may not be saved to disk.
		/// </remarks>
		string Validate (SolutionFolder parentFolder, SolutionFolderItem entry, Gtk.Widget editor);
		
		/// <summary>
		/// Applies the feature to a project
		/// </summary>
		/// <param name='parentFolder'>
		/// The parent folder of the solution item.
		/// </param>
		/// <param name='entry'>
		/// The project being checked for the feature
		/// </param>
		/// <param name='editor'>
		/// The feature editor.
		/// </param>
		/// <remarks>
		/// The provided item, parent folder and parent solution may or may not have a file name, and even if they
		/// have, they may not be saved to disk.
		/// </remarks>
		void ApplyFeature (SolutionFolder parentFolder, SolutionFolderItem entry, Gtk.Widget editor);
	}
	
	internal class SolutionItemFeatures
	{
		public static ISolutionItemFeature[] GetFeatures (SolutionFolder parentCombine, SolutionFolderItem entry)
		{
			List<ISolutionItemFeature> list = new List<ISolutionItemFeature> ();
			foreach (ISolutionItemFeature e in AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectFeatures", typeof(ISolutionItemFeature), true)) {
				FeatureSupportLevel level = e.GetSupportLevel (parentCombine, entry);
				if (level == FeatureSupportLevel.Enabled || level == FeatureSupportLevel.SupportedByDefault)
					list.Add (e);
			}
			return list.ToArray ();
		}
	}
}
