//
// CodeBinder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.CodeDom;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui;

using MonoDevelop.GtkCore.Dialogs;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	/// This class provides several methods for managing the relation
	/// between an object (e.g. a window) and the source code that will implement the
	/// code for that object.
	///
	/// Once created, a CodeBinder object will keep track of the class bound to the
	/// object. If the class is renamed, it will properly update the object name.

	public class CodeBinder
	{
		ITextFileProvider textFileProvider;
		object targetObject;
		Project project;
		string className;
		string classFile;
		
		public CodeBinder (Project project, ITextFileProvider textFileProvider, object targetObject)
		{
			this.project = project;
			this.textFileProvider = textFileProvider;
			
			TargetObject = targetObject;
		}
		
		public object TargetObject {
			get { return targetObject; }
			set {
				this.targetObject = value;
				IClass cls = GetClass (GetClassName (targetObject)); 
				className = cls.FullyQualifiedName;
				classFile = cls.Region.FileName;
			}
		}
		
		/// Synchronizes the bindings between the object and the source code
		public void UpdateBindings (string fileName)
		{
			IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, fileName, null);
			classFile = fileName;
			UpdateBindings (targetObject, GetClass ());
		}
		
		void UpdateBindings (object obj, IClass cls)
		{
			// Remove signals for which there isn't a handler in the class
			
			Stetic.SignalCollection objectSignals = GetSignals (obj);
			if (objectSignals != null) {
				Stetic.Signal[] signals = new Stetic.Signal [objectSignals.Count];
				objectSignals.CopyTo (signals, 0);
				
				foreach (Stetic.Signal signal in signals) {
					if (FindSignalHandler (cls, signal) == null) {
						objectSignals.Remove (signal);
					}
				}
			}

			// Update children
			
			IEnumerable children = GetChildren (obj);
			if (children != null) {
				foreach (object ob in children)
					UpdateBindings (ob, cls);
			}
		}
		
		IMethod FindSignalHandler (IClass cls, Stetic.Signal signal)
		{
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					return met;
				}
			}
			return null;
		}

		public void UpdateField (Stetic.ObjectWrapper obj, string oldName)
		{
			CodeRefactorer cr = GetCodeGenerator ();
			
			IClass cls;
			
			if (obj == targetObject)
				return;	// The root widget name can only be changed internally.
			else
				cls = GetClass ();
			
			string newName = GetObjectName (obj);
			if (newName.Length == 0)
				return;
			
			if (cls != null) {
				IField f = ClassUtils.FindWidgetField (cls, oldName);
				if (f != null) {
					// Rename the field
					cr.RenameMember (new NullProgressMonitor (), cls, f, newName, RefactoryScope.File);
				}
			}
		}
		
		/// Adds a signal handler to the class
		public void BindSignal (Stetic.Signal signal)
		{
			IClass cls = GetClass ();
			if (cls == null)
				return;
			
			if (FindSignalHandler (cls, signal) != null)
				return;

			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = signal.Handler;
			met.Attributes = MemberAttributes.Family;
			met.ReturnType = new CodeTypeReference (signal.SignalDescriptor.HandlerReturnTypeName);
			
			foreach (Stetic.ParameterDescriptor pinfo in signal.SignalDescriptor.HandlerParameters)
				met.Parameters.Add (new CodeParameterDeclarationExpression (pinfo.TypeName, pinfo.Name));
			
			CodeRefactorer gen = GetCodeGenerator ();
			gen.AddMember (cls, met);
		}
		
		public void UpdateSignal (Stetic.Signal oldSignal, Stetic.Signal newSignal)
		{
			if (oldSignal.Handler == newSignal.Handler)
				return;

			IClass cls = GetClass ();
			if (cls == null) return;

			IMethod met = FindSignalHandler (cls, oldSignal);
			if (met == null) return;
			
			CodeRefactorer gen = GetCodeGenerator ();
			gen.RenameMember (new NullProgressMonitor (), cls, met, newSignal.Handler, RefactoryScope.File);
		}

		/// Adds a field to the class
		public void BindToField (Stetic.ObjectWrapper obj)
		{
			string name = GetMemberName (obj);
			IClass cls = GetClass ();
			
			if (FindField (cls, name) != null)
				return;

			Document doc = IdeApp.Workbench.OpenDocument (cls.Region.FileName, true);
			
			IEditableTextFile editor = doc.Content as IEditableTextFile;
			if (editor != null) {
				CodeRefactorer gen = GetCodeGenerator ();
				gen.AddMember (cls, GetFieldCode (obj, name));
			}
		}
		
		CodeMemberField GetFieldCode (Stetic.ObjectWrapper obj, string name)
		{
			string type = obj.ClassDescriptor.WrappedTypeName;
			CodeMemberField field = new CodeMemberField (type, name);
			field.Attributes = MemberAttributes.Family;
			return field;
		}
		
		IField FindField (IClass cls, string name)
		{
			foreach (IField field in cls.Fields)
				if (field.Name == name)
					return field;
			return null;
		}
		
		CodeRefactorer GetCodeGenerator ()
		{
			CodeRefactorer cr = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
			cr.TextFileProvider = textFileProvider;
			return cr;
		}
		
		public IClass GetClass ()
		{
			IClass cls = GetClass (className);
			if (cls != null)
				return cls;
				
			// The class name may have changed. Try to guess the new name.
			
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
			IParseInformation pi = ctx.ParseFile (classFile);
			
			ArrayList matches = new ArrayList ();
			ClassCollection classes = ((ICompilationUnit)pi.BestCompilationUnit).Classes;
			foreach (IClass fcls in classes) {
				if (IsValidClass (ctx, fcls, targetObject))
					matches.Add (fcls);
			}
			
			// If found the class, just return it
			if (matches.Count == 1) {
				cls = (IClass) matches [0];
				className = cls.FullyQualifiedName;
				SetObjectName (targetObject, className);
				return cls;
			}
			
			// If not found, warn the user.
			
			if (classes.Count > 0) {
				using (SelectRenamedClassDialog dialog = new SelectRenamedClassDialog (classes)) {
					if (dialog.Run ()) {
						className = dialog.SelectedClass;
						if (className == null)
							return null;
						else {
							SetObjectName (targetObject, className);
							return GetClass (project, className);
						}
					}
				}
			} else {
				IdeApp.Services.MessageService.ShowError (GettextCatalog.GetString ("The class bound to the component '{0}' could not be found. This may be due to syntax errors in the source code file.", GetObjectName(targetObject)));
			}
			
			return null;
		}
		
		public static IClass GetClass (Project project, object obj)
		{
			string name = GetClassName (obj);
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
			IClass[] classes = ctx.GetProjectContents ();
			foreach (IClass cls in classes) {
				if (cls.FullyQualifiedName == name)
					return cls;
			}
			return null;
		}
		
		public static string GetSourceCodeFile (Project project, object obj)
		{
			IClass cls = GetClass (project, obj);
			if (cls != null) return cls.Region.FileName;
			else return null;
		}
		
		internal IClass GetClass (string name)
		{
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
			IClass[] classes = ctx.GetProjectContents ();
			foreach (IClass cls in classes) {
				if (cls.FullyQualifiedName == name)
					return cls;
			}
			return null;
		}
		
		static bool IsValidClass (IParserContext ctx, IClass cls, object obj)
		{
			if (cls.BaseTypes != null) {
				string typeName = GetObjectTypeName (obj);
				foreach (string bt in cls.BaseTypes) {
					if (bt == typeName)
						return true;
					
					IClass baseCls = ctx.GetClass (bt, true, true);
					if (baseCls != null && IsValidClass (ctx, baseCls, obj))
						return true;
				}
			}
			return false;
		}
		
		public static string GetClassName (object obj)
		{
			return GetObjectName (obj);
		}
		
		public static string GetMemberName (object obj)
		{
			Stetic.Wrapper.Widget w = GetWrapper (obj) as Stetic.Wrapper.Widget;
			if (w != null)
				return w.MemberName;
			else
				return GetObjectName (obj);
		}
		
		public static string GetObjectName (object obj)
		{
			Stetic.Wrapper.Widget w = GetWrapper (obj) as Stetic.Wrapper.Widget;
			if (w != null) {
				return w.Wrapped.Name;
			}
			else if (obj is Stetic.Wrapper.Action) {
				return ((Stetic.Wrapper.Action)obj).Name;
			}
			else if (obj is Stetic.Wrapper.ActionGroup) {
				return ((Stetic.Wrapper.ActionGroup)obj).Name;
			}
			else {
				return null;
			}
		}
		
		public static void SetMemberName (object obj, string name)
		{
			Stetic.Wrapper.Widget w = GetWrapper (obj) as Stetic.Wrapper.Widget;
			if (w != null) {
				w.Wrapped.Name = name;
			} else
				SetObjectName (obj, name);
		}
		
		public static void SetObjectName (object obj, string name)
		{
			Stetic.Wrapper.Widget w = GetWrapper (obj) as Stetic.Wrapper.Widget;
			if (w != null) {
				w.Wrapped.Name = name;
			}
			else if (obj is Stetic.Wrapper.Action) {
				((Stetic.Wrapper.Action)obj).Name = name;
			}
			else if (obj is Stetic.Wrapper.ActionGroup) {
				((Stetic.Wrapper.ActionGroup)obj).Name = name;
			}
			
		}
		
		public static string GetObjectTypeName (object obj)
		{
			Stetic.ObjectWrapper w = GetWrapper (obj);
			if (w != null)
				return w.ClassDescriptor.WrappedTypeName;
			if (obj is Stetic.Wrapper.ActionGroup)
				return "Gtk.ActionGroup";
			else
				return obj.GetType().FullName;
		}
		
		static Stetic.SignalCollection GetSignals (object obj)
		{
			Stetic.ObjectWrapper w = GetWrapper (obj);
			if (w != null) return w.Signals;
			else return null;
		}
		
		static IEnumerable GetChildren (object obj)
		{
			Stetic.Wrapper.Container w = GetWrapper (obj) as Stetic.Wrapper.Container;
			if (w != null) return w.RealChildren;
			
			Stetic.Wrapper.ActionGroup grp = obj as Stetic.Wrapper.ActionGroup;
			if (grp != null) return grp.Actions;
			
			return null;
		}
		
		static Stetic.ObjectWrapper GetWrapper (object obj)
		{
			if (obj is Stetic.ObjectWrapper)
				return (Stetic.ObjectWrapper) obj;
			return Stetic.ObjectWrapper.Lookup (obj);
		}
	}
}
