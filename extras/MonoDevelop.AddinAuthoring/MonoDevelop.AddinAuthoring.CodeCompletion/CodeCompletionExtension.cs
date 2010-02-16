// CodeCompletionExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.IO;
using System.Collections.Generic;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.XmlEditor;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.XmlEditor.Gui;
using Gdk;

namespace MonoDevelop.AddinAuthoring.CodeCompletion
{
	public class CodeCompletionExtension: BaseXmlEditorExtension
	{
		AddinDescription adesc;
		AddinRegistry registry;
		ToplevelCompletionContext topCtx = new ToplevelCompletionContext ();
		
		public override bool ExtendsEditor(Document doc, IEditableTextBuffer editor)
		{
			return (doc.Project is DotNetProject) && (editor.Name.ToString ().EndsWith (".addin") || editor.Name.ToString ().EndsWith (".addin.xml"));
		}

		protected override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, bool forced, ref int triggerWordLength)
		{
			if ((Tracker.Engine.CurrentState is XmlDoubleQuotedAttributeValueState
			    || Tracker.Engine.CurrentState is XmlSingleQuotedAttributeValueState))
			{
				// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
				int currentPosition = Editor.CursorPosition;
				if (currentPosition > 0) {
					string s = Editor.GetText (currentPosition - Tracker.Engine.CurrentStateLength, currentPosition);
					if (s.EndsWith ("/"))
						return GetPathCompletion (s);
				}
			}
			return base.HandleCodeCompletion (completionContext, forced, ref triggerWordLength);
		}

		CompletionDataList GetPathCompletion (string subPath)
		{
			CompletionContext ctx = GetCompletionContext (1);
			if (!(ctx is ExtensionCompletionContext))
				return null;
			ModuleCompletionContext mc = (ModuleCompletionContext) ctx.GetParentContext (typeof(ModuleCompletionContext));
			
			Set<string> paths = new Set<string> ();
			CompletionDataList cp = new CompletionDataList ();
			foreach (AddinDependency adep in mc.Module.Dependencies) {
				Addin addin = registry.GetAddin (adep.FullAddinId);
				if (addin != null && addin.Description != null) {
					foreach (ExtensionPoint ep in addin.Description.ExtensionPoints) {
						if (ep.Path.StartsWith (subPath)) {
							string spath = ep.Path.Substring (subPath.Length);
							int i = spath.IndexOf ('/');
							if (i != -1)
								spath = spath.Substring (0, i);
							if (paths.Add (spath)) {
								if (i == -1) // Full match. Add the documentation
									cp.Add (spath, "md-extension-point", ep.Name + "\n" + ep.Description);
								else
									cp.Add (spath, "md-literal");
							}
						}
					}
				}
			}
			return cp;
		}
		
		
		public override bool KeyPress(Key key, char keyChar, ModifierType modifier)
		{
			UpdateAddinDescription ();
			return base.KeyPress (key, keyChar, modifier);
		}

		protected override void GetElementCompletions(CompletionDataList list)
		{
			CompletionContext ctx = GetCompletionContext (0);
			if (ctx != null) {
				ctx.SetCompletionAction (CompletionAction.ElementStart, null);
				ctx.AddCompletionData (list);
			}
		}

		protected override CompletionDataList GetAttributeCompletions(IAttributedXObject attributedOb, Dictionary<string, string> existingAtts)
		{
			CompletionDataList list = new CompletionDataList ();
			CompletionContext ctx = GetCompletionContext (1);
			if (ctx != null) {
				ctx.SetCompletionAction (CompletionAction.AttributeStart, null);
				ctx.AddCompletionData (list);
			}
			return list;
		}

		protected override CompletionDataList GetAttributeValueCompletions(IAttributedXObject attributedOb, XAttribute att)
		{
			CompletionDataList list = new CompletionDataList ();
			CompletionContext ctx = GetCompletionContext (1);
			if (ctx != null) {
				ctx.SetCompletionAction (CompletionAction.AttributeValue, att.Name.Name);
				ctx.AddCompletionData (list);
			}
			return list;
		}


		void UpdateAddinDescription ()
		{
			try {
				AddinData adata = AddinData.GetAddinData ((DotNetProject) this.Document.Project);
				adesc = adata.AddinRegistry.ReadAddinManifestFile (new StringReader (Editor.Text), Document.FileName);
				registry = adata.AddinRegistry;
			} catch {
			}
		}
		
		CompletionContext GetCompletionContext (int numParent)
		{
			XElement element = GetParentElement (numParent);
			if (element == null)
				return topCtx;
			
			CompletionContext parentContext;
			parentContext = GetCompletionContext (numParent + 1);
			
			if (parentContext == null)
				return null;
			
			if (!element.IsNamed)
				return parentContext;
			ItemData data = parentContext.GetElementData (element.Name.Name);
			if (data == null)
				return null;
			if (data.ChildContextType == null)
				return null;
			if (adesc == null)
				return null;
			
			CompletionContext ctx = (CompletionContext) Activator.CreateInstance (data.ChildContextType);
			ctx.Init (registry, adesc, parentContext, element);
			return ctx;
		}
	}
	
	
	enum CompletionAction
	{
		ElementStart,
		AttributeStart,
		AttributeValue
	}

	class ItemData
	{
		public string Name;
		public string Description;
		public Type ChildContextType;
		public string CompletionString;
	}

	class CompletionContext
	{
		List<ItemData> elementData;
		List<ItemData> attributeData;
		List<ItemData> currentCollection;
		string attNameForValue;
		CompletionAction action;
		CompletionContext parentContext;
		AddinDescription adesc;
		AddinRegistry registry;
		
		public List<ItemData> ElementData {
			get {
				if (elementData == null) {
					elementData = currentCollection = new List<ItemData> ();
					OnAddChildElements ();
				}
				return elementData;
			}
		}
		
		public List<ItemData> AttributeData {
			get {
				if (attributeData == null) {
					attributeData = currentCollection = new List<ItemData> ();
					OnAddAttributes ();
				}
				return attributeData;
			}
		}

		public ItemData GetElementData (string name)
		{
			foreach (ItemData data in ElementData)
				if (data.Name == name)
					return data;
			return null;
		}
		
		public CompletionContext ParentContext {
			get {
				return parentContext;
			}
			set {
				parentContext = value;
			}
		}

		public AddinDescription AddinDescription {
			get {
				return adesc;
			}
		}

		public AddinRegistry AddinRegistry {
			get {
				return registry;
			}
		}

		public void Init (AddinRegistry registry, AddinDescription adesc, CompletionContext parentContext, XElement elem)
		{
			this.registry = registry;
			this.adesc = adesc;
			this.parentContext = parentContext;
			Initialize (elem);
		}

		public virtual void Initialize (XElement elem)
		{
		}

		public void SetCompletionAction (CompletionAction action, string attNameForValue)
		{
			this.action = action;
			this.attNameForValue = attNameForValue;
		}
		
		public virtual void AddCompletionData (CompletionDataList provider)
		{
			if (action == CompletionAction.AttributeValue) {
				currentCollection = new List<ItemData> ();
				OnAddAttributeValues (attNameForValue);
			}
			else if (action == CompletionAction.ElementStart)
				currentCollection = ElementData;
			else
				currentCollection = AttributeData;

			foreach (ItemData data in currentCollection) {
				CompletionData cd = new CompletionData (data.Name, "md-literal", data.Description);
				if (data.CompletionString != null)
					cd.CompletionText = data.CompletionString;
				provider.Add (cd);
			}
		}

		public void AddTrueFalse ()
		{
			Add ("true", "");
			Add ("false", "");
		}

		public virtual void OnAddChildElements ()
		{
		}

		public virtual void OnAddAttributes ()
		{
		}

		public virtual void OnAddAttributeValues (string attName)
		{
		}

		public void Add (string name, string desc)
		{
			Add (name, desc, null, null);
		}

		public void Add (string name, string desc, string completionString)
		{
			Add (name, desc, completionString, null);
		}

		public void Add (string name, string desc, Type type)
		{
			Add (name, desc, null, type);
		}
		
		public void Add (string name, string desc, string completionString, Type type)
		{
			ItemData data = new ItemData ();
			data.Name = name;
			data.Description = name + "\n" + desc;
			data.CompletionString = completionString;
			data.ChildContextType = type;
			currentCollection.Add (data);
		}

		public CompletionContext GetParentContext (Type type)
		{
			if (parentContext != null) {
				if (type.IsInstanceOfType (parentContext))
					return parentContext;
				else
					return parentContext.GetParentContext (type);
			} else
				return null;
		}
	}

	class ToplevelCompletionContext: CompletionContext
	{
		public override void OnAddChildElements()
		{
			Add ("Addin", GettextCatalog.GetString ("Declaration of an add-in."), typeof(HeaderCompletionContext));
		}
	}
	
	class ModuleCompletionContext: CompletionContext
	{
		ModuleDescription module;

		public ModuleDescription Module {
			get { return module; }
		}

		public override void Initialize(XElement elem)
		{
			if (elem.Name.Name == "Addin")
				module = this.AddinDescription.MainModule;
			XElement pe = elem.Parent as XElement;
			if (pe != null) {
				int n = 0;
				XNode ob = pe.FirstChild;
				while (ob != null) {
					if (ob == elem) {
						module = this.AddinDescription.OptionalModules [n];
						break;
					}
					ob = ob.NextSibling;
				}
			}
		}
		
		public override void OnAddChildElements()
		{
			if (module != null) {
				Add ("Extension", GettextCatalog.GetString ("Extension\nA collection of extension nodes. An extension node is the definition of an object that extends an application."), typeof(ExtensionCompletionContext));
				Add ("Runtime", GettextCatalog.GetString ("Declaration of files to be loaded at run-time."), typeof(RuntimeCompletionContext));
				Add ("Dependencies", GettextCatalog.GetString ("Declaration of dependencies of the add-in."), typeof(DependenciesCompletionContext));
			}
		}
	}
	
	class RuntimeCompletionContext: CompletionContext
	{
		public override void OnAddChildElements()
		{
			Add ("Import", GettextCatalog.GetString ("File import."), typeof(ImportCompletionContext));
		}
	}
	
	class ImportCompletionContext: CompletionContext
	{
		public override void OnAddAttributes ()
		{
			Add ("assembly", GettextCatalog.GetString ("Name of an assembly that belongs to the add-in."));
			Add ("file", GettextCatalog.GetString ("Name of a file that belongs to the add-in."));
		}
	}

	class DependenciesCompletionContext: CompletionContext
	{
		public override void OnAddChildElements()
		{
			Add ("Addin", GettextCatalog.GetString ("Declares an add-in dependency."), typeof(AddinDependencyCompletionContext));
		}
	}

	class AddinDependencyCompletionContext: CompletionContext
	{
		string id;
		
		public override void Initialize(XElement elem)
		{
			XAttribute attr = elem.Attributes [new XName ("id")];
			if (attr != null)
				id = attr.Value;
		}
		
		public override void OnAddAttributes ()
		{
			Add ("id", GettextCatalog.GetString ("Identifier of the extended add-in."));
			Add ("version", GettextCatalog.GetString ("Version of the extended add-in."));
		}

		public override void OnAddAttributeValues(string attName)
		{
			if (attName == "id") {
				foreach (Addin a in this.AddinRegistry.GetAddins ())
					AddAddin (a);
				foreach (Addin a in this.AddinRegistry.GetAddinRoots ())
					AddAddin (a);
			}
			else if (attName == "version" && id != null) {
				Addin a = this.AddinRegistry.GetAddin (Addin.GetFullId (this.AddinDescription.Namespace, id, null));
				if (a != null)
					Add (a.Version, "");
			}
		}

		void AddAddin (Addin a)
		{
			string id = a.Namespace == this.AddinDescription.Namespace ? a.LocalId : Addin.GetIdName (a.Id);
			Add (id, a.Name + ". " + a.Description, id + "\" version=\"" + a.Version + "\"");
		}
	}
	
	class HeaderCompletionContext: ModuleCompletionContext
	{
		public override void OnAddAttributes ()
		{
			Add ("id", GettextCatalog.GetString ("The identifier of the add-in. It is mandatory for add-in roots and for add-ins that can be extended, optional for other add-ins."));
			Add ("namespace", GettextCatalog.GetString ("Namespace of the add-in. The full ID of an add-in is composed by 'namespace.name'."));
			Add ("version", GettextCatalog.GetString ("The version of the add-in. It is mandatory for add-in roots and for add-ins that can be extended."));
			Add ("compatVersion", GettextCatalog.GetString ("Version of the add-in with which this add-in is backwards compatible (optional)."));
			Add ("name", GettextCatalog.GetString ("Display name of the add-in."));
			Add ("description", GettextCatalog.GetString ("Description of the add-in."));
			Add ("author", GettextCatalog.GetString ("Author of the add-in."));
			Add ("url", GettextCatalog.GetString ("Url of a web page with more information about the add-in."));
			Add ("defaultEnabled", GettextCatalog.GetString ("When set to 'false', the add-in won't be enabled until it is explicitly enabled by the user. The default is 'true'."));
			Add ("isroot", GettextCatalog.GetString ("Must be true if this manifest belongs to an add-in root."));
		}

		public override void OnAddChildElements()
		{
			base.OnAddChildElements ();
			Add ("ExtensionPoint", GettextCatalog.GetString ("An extension point. A placeholder where add-ins can register extension nodes to provide extra functionality. Extension points are identified using extension paths."), typeof(ExtensionPointCompletionContext));
			Add ("ExtensionNodeSet", GettextCatalog.GetString ("Node sets allows grouping a set of extension node declarations and give an identifier to that group (the node set). Once a node set is declared, it can be referenced from several extension points which use the same extension node structure. Extension node sets also allow declaring recursive extension nodes, that is, extension nodes with a tree structure."), typeof(ExtensionNodeSetCompletionContext));
			Add ("Module", GettextCatalog.GetString ("An optional Module. By using optional modules, and add-in can declare extensions which will be registered only if some specified add-in dependencies can be satisfied."), typeof(ModuleCompletionContext));
			Add ("ConditionType", GettextCatalog.GetString ("Definition of a Condition Type. Add-ins may use conditions to register nodes in an extension point which are only visible under some contexts."), typeof(ConditionTypeCompletionContext));
			Add ("Localizer", GettextCatalog.GetString ("Definition of a Localizer. Enables localization support."), typeof(LocalizerCompletionContext));
		}
		
		public override void OnAddAttributeValues(string attName)
		{
			switch (attName) {
				case "isroot":
				case "defaultEnabled":
					AddTrueFalse ();
					break;

				case "namespace": {
					Set<string> nss = new Set<string> ();
					foreach (Addin a in this.AddinRegistry.GetAddins ())
						nss.Add (a.Namespace);
					foreach (Addin a in this.AddinRegistry.GetAddinRoots ())
						nss.Add (a.Namespace);
					foreach (string s in nss) {
						if (!string.IsNullOrEmpty (s))
							Add (s, "");
					}
					break;
				}
			}
		}
	}

	class BaseExtensionNodeSetCompletionContext: CompletionContext
	{
		public override void OnAddChildElements()
		{
			Add ("Description", GettextCatalog.GetString ("Long description of the extension point or node set."));
			Add ("ExtensionNode", GettextCatalog.GetString ("Declares a type of node allowed in this extension point or node set."), typeof(ExtensionNodeTypeCompletionContext));
			Add ("ExtensionNodeSet", GettextCatalog.GetString ("A node set reference. Node sets allows grouping a set of extension node declarations and give an identifier to that group (the node set). Once a node set is declared, it can be referenced from several extension points which use the same extension node structure."), typeof(ExtensionNodeSetRefCompletionContext));
		}
	}
	
	class ExtensionNodeSetCompletionContext: BaseExtensionNodeSetCompletionContext
	{
		public override void OnAddAttributes ()
		{
			base.OnAddAttributes ();
			Add ("id", GettextCatalog.GetString ("The identifier of the Node Set."));
		}
	}
	
	class ConditionTypeCompletionContext: CompletionContext
	{
		public override void OnAddAttributes ()
		{
			Add ("id", GettextCatalog.GetString ("The identifier of the condition."));
			Add ("type", GettextCatalog.GetString ("The type that implements the condition."));
		}
	}
	
	class LocalizerCompletionContext: CompletionContext
	{
		string type;
		
		public override void Initialize(XElement elem)
		{
			XAttribute attr = elem.Attributes [new XName ("type")];
			if (attr != null)
				type = attr.Value;
		}

		public override void OnAddAttributes ()
		{
			if (type == null)
				Add ("type", GettextCatalog.GetString ("The type of localizer. It can be 'Gettext', 'StringResource', 'StringTable' or the name of a class that implements Mono.Addins.IAddinLocalizerFactory."));
			if (type == "Gettext") {
				Add ("catalog", GettextCatalog.GetString ("Name of the catalog which contains the strings (the add-in id by default)."));
				Add ("location", GettextCatalog.GetString ("Relative path to the location of the catalog ('./locale' by default). This path must be relative to the add-in location."));
			}
		}

		public override void OnAddAttributeValues(string attName)
		{
			if (attName == "type") {
				Add ("Gettext", GettextCatalog.GetString ("The Gettext localizer type can be used to localize an add-in with 'gettext'."));
				Add ("StringResource", GettextCatalog.GetString ("The StringResource localizer type can be used to localize an add-in using string resources defined in satellite assemblies."));
				Add ("StringTable", GettextCatalog.GetString ("The StringTable localizer type can be used for add-ins with very basic localization needs. Translated strings are specified in a table embedded in the add-in manifest."));
			}
		}
	}
	
	class ExtensionPointCompletionContext: BaseExtensionNodeSetCompletionContext
	{
		public override void OnAddAttributes()
		{
			Add ("path", GettextCatalog.GetString ("Path of the extension point."));
			Add ("name", GettextCatalog.GetString ("Display name of the extension point (to be shown in documentation)."));
		}
	}

	class ExtensionNodeSetRefCompletionContext: CompletionContext
	{
		public override void OnAddAttributes()
		{
			Add ("id", GettextCatalog.GetString ("Identifier of the Node Set."));
		}

		public override void OnAddAttributeValues (string attName)
		{
			if (attName == "id") {
				ModuleCompletionContext mc = (ModuleCompletionContext) GetParentContext (typeof(ModuleCompletionContext));
				foreach (AddinDependency adep in mc.Module.Dependencies) {
					Addin addin = AddinRegistry.GetAddin (adep.FullAddinId);
					if (addin != null && addin.Description != null) {
						foreach (ExtensionNodeSet ns in addin.Description.ExtensionNodeSets)
							Add (ns.Id, "");
					}
				}
			}
		}
	}
	
	class ExtensionNodeTypeCompletionContext: BaseExtensionNodeSetCompletionContext
	{
		public override void Initialize (XElement elem)
		{
		}
		
		public override void OnAddAttributes()
		{
			Add ("name", GettextCatalog.GetString ("Name of the node type. When an element is added to an extension point, its name must match one of the declared node types."));
			Add ("type", GettextCatalog.GetString ("CLR type that implements this extension node type. It must be a subclass of Mono.Addins.ExtensionNode. If not specified, by default it is Mono.Addins.TypeExtensionNode."));
		}
	}
	
	abstract class BaseExtensionCompletionContext: CompletionContext
	{
		public abstract ExtensionNodeTypeCollection GetAllowedNodeTypes ();

		public abstract string GetPath ();
		
		public override void OnAddChildElements()
		{
			foreach (ExtensionNodeType nt in GetAllowedNodeTypes ())
				Add (nt.NodeName, nt.Description, typeof(ExtensionNodeCompletionContext));
		}
	}
	
	class ExtensionCompletionContext: BaseExtensionCompletionContext
	{
		Extension extension;

		public override void Initialize (XElement elem)
		{
			XAttribute attr = elem.Attributes [new XName ("path")];
			if (attr != null) {
				ModuleCompletionContext ctx = ParentContext as ModuleCompletionContext;
				if (ctx != null && ctx.Module != null)
					extension = ctx.Module.GetExtension (attr.Value);
			}
		}
		
		public override string GetPath ()
		{
			if (extension != null)
				return extension.Path;
			else
				return null;
		}
		
		public override ExtensionNodeTypeCollection GetAllowedNodeTypes ()
		{
			if (extension == null)
				return new ExtensionNodeTypeCollection ();
			else
				return extension.GetAllowedNodeTypes ();
		}
		
		public override void OnAddAttributes()
		{
			Add ("path", GettextCatalog.GetString ("Path of the extension point where the nodes will be registered."));
		}
	}

	class ExtensionNodeCompletionContext: BaseExtensionCompletionContext
	{
		ExtensionNodeType nodeType;
		string id;

		public string Id {
			get { return id; }
		}
		
		public override void Initialize (XElement elem)
		{
			XAttribute att = elem.Attributes [new XName ("id")];
			if (att != null)
				id = att.Value;
			string nodeName = elem.Name.Name;
			BaseExtensionCompletionContext ctx = ParentContext as BaseExtensionCompletionContext;
			foreach (ExtensionNodeType nt in ctx.GetAllowedNodeTypes ()) {
				if (nt.NodeName == nodeName) {
					nodeType = nt;
					break;
				}
			}
		}

		public override ExtensionNodeTypeCollection GetAllowedNodeTypes ()
		{
			return nodeType.GetAllowedNodeTypes ();
		}
		
		public override void OnAddAttributes()
		{
			if (nodeType != null) {
				foreach (NodeTypeAttribute att in nodeType.Attributes)
					Add (att.Name, att.Description);
			}
			Add ("id", GettextCatalog.GetString ("Identifier of the node. It's optional, but needed if the node will be referenced from other nodes."));
			Add ("insertafter", GettextCatalog.GetString ("Identifier of the node after which this node has to be placed."));
			Add ("insertbefore", GettextCatalog.GetString ("Identifier of the node before which this node has to be placed."));
		}

		public override void OnAddAttributeValues (string attName)
		{
			if (attName == "insertafter" || attName == "insertbefore") {
				string parentPath = ((BaseExtensionCompletionContext)ParentContext).GetPath ();
				if (parentPath != null) {
					foreach (ExtensionNodeDescription en in AddinData.GetExtensionNodes (AddinRegistry, AddinDescription, parentPath)) {
						if (!string.IsNullOrEmpty (en.Id)) {
							ExtensionNodeType nt = en.GetNodeType ();
							string desc = nt != null ? nt.Description : "";
							Add (en.Id, desc);
						}
					}
				}
			}
		}
		
		public override string GetPath ()
		{
			if (id != null)
				return ((BaseExtensionCompletionContext)ParentContext).GetPath () + "/" + id;
			else
				return null;
		}
	}
}
