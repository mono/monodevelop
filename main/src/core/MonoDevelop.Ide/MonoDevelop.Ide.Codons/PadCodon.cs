//
// PadCodon.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Xml;
using Mono.Addins;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode ("Pad", "Registers a pad to be shown in the workbench.")]
	public class PadCodon : ExtensionNode
	{
		[NodeAttribute ("_label", "Display name of the pad.", Localizable = true)]
		public string Label { get; private set; }
		
		[NodeAttribute ("class", "Class name.")]
		public string ClassName { get; private set; }

		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649
		
		[NodeAttribute("icon", "Icon of the pad. It can be a stock icon or a resource icon (use 'res:' as prefix in the last case).")]
		string icon = null;

		[NodeAttribute ("group", "Name of the group, used to group Pads inside Category menus.")]
		string group = null;
		
		[NodeAttribute("defaultPlacement",
		               "Default placement of the pad inside the workbench. " +
		               "It can be: left, right, top, bottom, or a relative position, for example: 'ProjectPad/left'" +
		               "would show the pad at the left side of the project pad. When using " +
		               "relative placements several positions can be provided. If the " +
		               "pad can be placed in the first position, the next one will be " +
		               "tried. For example 'ProjectPad/left; bottom'."
		               )]
		string defaultPlacement = "left";
		
		[NodeAttribute ("defaultStatus", "Default status ofthe pad. It can be 'Dockable', 'Floating', 'AutoHide'.")]
		DockItemStatus defaultStatus = DockItemStatus.Dockable;
		
		[NodeAttribute ("defaultLayout", "Name of the layouts (comma separated list) on which this pad should be visible by default")]
		string[] defaultLayouts;

		#pragma warning restore 649

		bool initializeCalled;
		string id;

		public PadContent PadContent { get; private set; }

		public PadContent InitializePadContent (IPadWindow window)
		{
			if (PadContent == null) {
				PadContent = CreatePad ();
			}
			if (!initializeCalled) {
				PadContent.Init (window);
				ApplyPreferences ();
				initializeCalled = true;
			}
			return PadContent;
		}
			
		public string PadId {
			get => id ?? Id;
			set => id = value;
		}

		public IconId Icon => !string.IsNullOrEmpty (icon) ? icon : "md-generic-pad";
		public string Group => !string.IsNullOrEmpty (group) ? group : "YOther";
		public IList<string> DefaultLayouts => defaultLayouts;

		/// <summary>
		/// Returns the default placement of the pad: left, right, top, bottom.
		/// Relative positions can be used, for example: "ProjectPad/left"
		/// would show the pad at the left of the project pad. When using
		/// relative placements several positions can be provided. If the
		/// pad can be placed in the first position, the next one will be
		/// tried. For example "ProjectPad/left; bottom".
		/// </summary>
		public string DefaultPlacement => defaultPlacement;

		public DockItemStatus DefaultStatus => defaultStatus;

		public bool Initialized => PadContent != null;

		public PadCodon ()
		{
		}
		
		public PadCodon (PadContent content, string id, string label, string defaultPlacement, string icon)
			: this (content, id, label, defaultPlacement, DockItemStatus.Dockable, icon)
		{
		}
		
		public PadCodon (PadContent content, string id, string label, string defaultPlacement, DockItemStatus defaultStatus, string icon)
		{
			this.id               = id;
			this.PadContent          = content;
			this.Label            = label;
			this.defaultPlacement = defaultPlacement;
			this.icon             = icon;
			this.defaultStatus    = defaultStatus;
		}
		
		protected virtual PadContent CreatePad ()
		{
			Counters.PadsLoaded++;
			return (PadContent) Addin.CreateInstance (ClassName, true);
		}
		
		PadUserPrefs preferences = null;

		internal void SetPreferences (PadUserPrefs pi)
		{
			preferences = pi;
			ApplyPreferences ();
		}

		void ApplyPreferences ()
		{
			var memento = PadContent as IMementoCapable;
			if (memento == null || preferences == null)
				return;
			try {
				string xml = preferences.State.OuterXml;
				var innerReader = new XmlTextReader (new StringReader (xml));
				innerReader.MoveToContent ();
				ICustomXmlSerializer cs = (ICustomXmlSerializer)memento.Memento;
				if (cs != null)
					memento.Memento = cs.ReadFrom (innerReader);
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading view memento.", ex);
			}
		}
	}
}
