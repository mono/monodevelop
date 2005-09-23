//
// SolutionPadCodon.cs
//
// Author:
//   Lluis Sanchez Gual
//

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
using System.Collections;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Pads;

namespace MonoDevelop.Core.AddIns.Codons
{
	[CodonNameAttribute ("SolutionPad")]
	public class SolutionPadCodon : PadCodon
	{
		NodeBuilder[] builders;
		TreePadOption[] options;
		
		[XmlMemberAttribute("_label", IsRequired=true)]
		string label = null;
		
		[XmlMemberAttribute("icon")]
		string icon = null;

		[XmlMemberAttribute("defaultPlacement")]
		string placement = null;

		public NodeBuilder[] NodeBuilders {
			get { return builders; }
		}
		
		public string Label {
			get { return label; }
		}
		
		public string Icon {
			get { return icon; }
		}
		
		public string DefaultPlacement {
			get { return placement; }
		}
		
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			ArrayList bs = new ArrayList ();
			ArrayList ops = new ArrayList ();
			
			foreach (object ob in subItems) {
				NodeBuilderCodon nbc = ob as NodeBuilderCodon;
				if (nbc != null)
					bs.Add (nbc.NodeBuilder);
				else {
					PadOptionCodon poc = ob as PadOptionCodon;
					if (poc != null)
						ops.Add (poc.Option);
				}
			}
			builders = (NodeBuilder[]) bs.ToArray (typeof(NodeBuilder));
			options = (TreePadOption[]) ops.ToArray (typeof(TreePadOption));
			return base.BuildItem (owner, subItems, conditions);
		}
		
		protected override IPadContent CreatePad ()
		{
			TreeViewPad pad;
			if (Class != null) {
				object ob = AddIn.CreateObject (Class);
				if (!(ob is TreeViewPad))
					throw new InvalidOperationException ("'" + Class + "' is not a subclass of TreeViewPad.");
				pad = (TreeViewPad) ob;
			} else
				pad = new SolutionPad ();

			pad.Initialize (label, icon, builders, options);
			pad.DefaultPlacement = placement;
			pad.Id = ID;
			return pad;
		}
	}
}
