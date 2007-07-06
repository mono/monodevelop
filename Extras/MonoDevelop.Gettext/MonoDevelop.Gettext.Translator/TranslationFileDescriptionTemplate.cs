//
// TranslationFileDescriptionTemplate.cs
//
// Author:
//   Rafael 'Monoman' Teixeira
//
// Copyright (C) 2006 Rafael 'Monoman' Teixeira
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Gettext.Translator
{
	internal class TranslationFileDescriptionTemplate : FileDescriptionTemplate
	{
		SingleFileDescriptionTemplate fileTemplate;
		XmlElement translationTemplate;

		public override string Name
		{
			get { return "Translation"; }
		}

		public override void Load (XmlElement filenode)
		{
			foreach (XmlNode node in filenode.ChildNodes)
			{
				XmlElement elem = node as XmlElement;
				if (elem == null)
					continue;

				if (elem.Name == "TranslationTemplate")
				{
					if (translationTemplate != null)
						throw new InvalidOperationException ("Translation templates can't contain more than one TranslationTemplate element");
					translationTemplate = elem;
				} else if (fileTemplate == null)
				{
					fileTemplate = FileDescriptionTemplate.CreateTemplate (elem) as SingleFileDescriptionTemplate;
					if (fileTemplate == null)
						throw new InvalidOperationException ("Translation templates can only contain single-file and translator templates.");
				}
			}
			if (fileTemplate == null)
				throw new InvalidOperationException ("File template not found in widget template.");
			if (translationTemplate == null)
				throw new InvalidOperationException ("Translator template not found in translation template.");
		}

		public override void AddToProject (Project project, string language, string directory, string name)
		{
			TranslatorInfo info = TranslatorCoreService.GetTranslatorInfo (project);
			if (info == null)
				info = TranslatorCoreService.EnableTranslatorSupport (project);

//			TranslationProject gproject = info.TranslationProject;

			string fileName = fileTemplate.GetFileName (project, language, directory, name);
			fileTemplate.AddToProject (project, language, directory, name);

			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, fileName, null);
		}

		public override void Show ()
		{
			fileTemplate.Show ();
		}
	}
}
