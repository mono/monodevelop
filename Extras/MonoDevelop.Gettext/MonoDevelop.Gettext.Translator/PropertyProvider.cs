////
//// PropertyProvider.cs
////
//// Author:
////   David Makovský <yakeen@sannyas-on.net>
////
//// Copyright (C) 2007 David Makovský
////
//// Permission is hereby granted, free of charge, to any person obtaining
//// a copy of this software and associated documentation files (the
//// "Software"), to deal in the Software without restriction, including
//// without limitation the rights to use, copy, modify, merge, publish,
//// distribute, sublicense, and/or sell copies of the Software, and to
//// permit persons to whom the Software is furnished to do so, subject to
//// the following conditions:
//// 
//// The above copyright notice and this permission notice shall be
//// included in all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////
//
//using System;
//using System.ComponentModel;
//using MonoDevelop.Projects;
//using MonoDevelop.DesignerSupport;
//
//namespace MonoDevelop.Gettext.Translator
//{
//	class PropertyProvider : IPropertyProvider
//	{
//		public bool SupportsObject (object obj)
//		{
//			return
//				obj is ProjectFile &&
//				! (((ProjectFile)obj).Project is Translator.TranslationProject) &&
//				TranslationProject.HasTranslationFiles (((ProjectFile)obj).Project);
//		}
//
//		public object CreateProvider (object obj)
//		{
//			return new ProjectFileTranslationProperty ((ProjectFile) obj);
//		}
//	}
//
//	class ProjectFileTranslationProperty
//	{
//		TranslationProjectInfo info;
//		ProjectFile file;
//		
//		public ProjectFileTranslationProperty (ProjectFile file)
//		{
//			this.file = file;
//			info = file.Project.ExtendedProperties ["MonoDevelop.Gettext.TranslationInfo"] as Translator.TranslationProjectInfo;
//		}
//		
//		[Category ("Translation")]
//		[Description ("Set to 'true' if the you want to include this file in translation resources.")]
//		public bool IncludeInTranslation
//		{
//			get
//			{
//				if (info != null)
//				{
//					if (! info.IsFileExcluded (file.FilePath))
//						return true;
//				}
//				return false;
//			}
//			set
//			{
//				if (info != null)
//				{
//					info.SetFileExcluded (file.FilePath, ! value);
//				}
//			}
//		}
//	}
//}
