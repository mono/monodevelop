//
// DomType.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Dom
{
	public class DomType : AbstractMember, IType
	{
		protected object sourceProject;
		protected ICompilationUnit compilationUnit;
		protected IReturnType baseType;
		protected List<TypeParameter> typeParameters = new List<TypeParameter> ();
		protected List<IMember> members = new List<IMember> ();
		protected List<IReturnType> implementedInterfaces = new List<IReturnType> ();
		protected ClassType classType = ClassType.Unknown;
		protected string namesp;
		
		public override string FullName {
			get {
				return !String.IsNullOrEmpty (namesp) ? namesp + "." + name : name;
			}
		}
		
		public string Namespace {
			get {
				return namesp;
			}
		}
		
		public object SourceProject {
			get {
				return sourceProject;
			}
		}

		public ICompilationUnit CompilationUnit {
			get {
				return compilationUnit;
			}
		}
		
		public ClassType ClassType {
			get {
				return classType;
			}
		}
		
		public IReturnType BaseType {
			get {
				return baseType;
			}
		}
		
		public IEnumerable<IReturnType> ImplementedInterfaces {
			get {
				return implementedInterfaces;
			}
		}
		
		public IEnumerable<TypeParameter> TypeParameters {
			get {
				return typeParameters;
			}
		}
		
		public virtual IEnumerable<IMember> Members {
			get {
				return members;
			}
		}
		
		public IEnumerable<IType> InnerTypes {
			get {
				foreach (IMember item in Members)
					if (item is IType)
						yield return (IType)item;
			}
		}

		public IEnumerable<IField> Fields {
			get {
				foreach (IMember item in Members)
					if (item is IField)
						yield return (IField)item;
			}
		}

		public IEnumerable<IProperty> Properties {
			get {
				foreach (IMember item in Members)
					if (item is IProperty)
						yield return (IProperty)item;
			}
		}

		public IEnumerable<IMethod> Methods {
			get {
				foreach (IMember item in Members)
					if (item is IMethod)
						yield return (IMethod)item;
			}
		}

		public IEnumerable<IEvent> Events {
			get {
				foreach (IMember item in Members)
					if (item is IEvent)
						yield return (IEvent)item;
			}
		}
		
		static string[,] iconTable = new string[,] {
			{Stock.Error,     Stock.Error,            Stock.Error,              Stock.Error},             // unknown
			{Stock.Class,     Stock.PrivateClass,     Stock.ProtectedClass,     Stock.InternalClass},     // class
			{Stock.Enum,      Stock.PrivateEnum,      Stock.ProtectedEnum,      Stock.InternalEnum},      // enum
			{Stock.Interface, Stock.PrivateInterface, Stock.ProtectedInterface, Stock.InternalInterface}, // interface
			{Stock.Struct,    Stock.PrivateStruct,    Stock.ProtectedStruct,    Stock.InternalStruct},    // struct
			{Stock.Delegate,  Stock.PrivateDelegate,  Stock.ProtectedDelegate,  Stock.InternalDelegate}   // delegate
		};
		
		public override string StockIcon {
			get {
				return iconTable[(int)ClassType, ModifierToOffset (Modifiers)];
			}
		}
		
		protected DomType ()
		{
		}
		
		public DomType (ICompilationUnit compilationUnit, ClassType classType, string name, DomLocation location, string namesp, DomRegion region, List<IMember> members)
		{
			this.compilationUnit = compilationUnit;
			this.classType   = classType;
			this.name        = name;
			this.namesp      = namesp;
			this.bodyRegion  = region;
			this.members     = members;
			this.location    = location;
			
			foreach (IMember member in members) {
				member.DeclaringType = this;
			}
		}
		
		public static DomType CreateDelegate (ICompilationUnit compilationUnit, string name, DomLocation location, IReturnType type, List<IParameter> parameters)
		{
			DomType result = new DomType ();
			result.compilationUnit = compilationUnit;
			result.name = name;
			result.classType = MonoDevelop.Ide.Dom.ClassType.Delegate;
			result.members.Add (new DomMethod ("Invoke", location, type, parameters));
			return result;
		}
		
		public override void JumpToDeclaration ()
		{
			System.Console.WriteLine(Location);
			MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (CompilationUnit.FileName, 
			                                                   Location.Line,
			                                                   Location.Column,
			                                                   true);
		}
		
		public override object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
