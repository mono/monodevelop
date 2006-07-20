/*
 * Copyright (C) 2004 Todd Berman <tberman@off.net>
 * Copyright (C) 2004 Jeroen Zwartepoorte <jeroen@xs4all.nl>
 * Copyright (C) 2005 John Luke <john.luke@gmail.com>
 *
 * based on work by:
 * Copyright (C) 2002 Gustavo Giráldez <gustavo.giraldez@gmx.net>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Collections.Generic;
using System.Xml;
using Gtk;

namespace Gdl
{
	public class DockPlaceholder : DockObject
	{
		private DockObject host;
		private DockObject oldHostParent;
		private bool sticky;
		private Stack<DockPlacement> placementStack;
		int panedPosition;

		protected DockPlaceholder (IntPtr raw) : base (raw) { }
		
		public DockPlaceholder (string name, DockObject obj,
					DockPlacement position, bool sticky)
		{
			this.DockObjectFlags &= ~(DockObjectFlags.Automatic);
			WidgetFlags |= WidgetFlags.NoWindow;
			WidgetFlags &= ~(WidgetFlags.CanFocus);

			Sticky = sticky;
			Name = name;

			if (obj != null) {
				// Store the divider position if the parent is a DockPaned
				DockPaned paned = obj.ParentObject as DockPaned;
				if (paned != null)
					panedPosition = paned.Position;
				
				// Get the relative position of this object, provided by the container
				obj.GetRelativePlacement (out obj, out position); 

				if (position == DockPlacement.None)
					position = DockPlacement.Center;
				
				Attach (obj);

				PushNextPlacement (position);

				//the top placement will be consumed by the toplevel dock, so add a dummy placement
				if (obj is Dock)
					PushNextPlacement (DockPlacement.Center);
			}
		}
		
		public DockPlaceholder (DockObject obj, bool sticky) :
			this (null, obj, DockPlacement.None, sticky) {  }
		
		public DockObject Host {
			get {
				return host;
			}
			set {
				Attach (value);
				EmitPropertyEvent ("Host");
			}
		}
		
		[After]
		[Export]
		DockPlacement NextPlacement {
			get {
				if (placementStack != null && placementStack.Count > 0)
					return placementStack.Peek ();
				return DockPlacement.Center;
			}
		}
		
		DockPlacement PopNextPlacement ()
		{
			if (placementStack != null && placementStack.Count > 0)
				return placementStack.Pop ();
			return DockPlacement.Center;
		}
		
		void PushNextPlacement (DockPlacement value)
		{
			if (placementStack == null)
				placementStack = new Stack<DockPlacement> ();
			placementStack.Push (value);
		}

		public bool Sticky {
			get {
				return sticky;
			}
			set {
				sticky = value;
				EmitPropertyEvent ("Sticky");
			}
		}

		protected override void OnDestroyed ()
		{
			if (host != null)
				OnDetached (false);
			base.OnDestroyed ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			if (!(widget is DockItem))
				return;

			// Try to find the correct parent widget before docking
			DoExcursion ();
			
			Dock ((DockItem)widget, NextPlacement, panedPosition);
		}
		
		public override void OnDetached (bool recursive)
		{
			// disconnect handlers
			DisconnectHost ();

			// free the placement stack
			placementStack = null;

			DockObjectFlags &= ~(DockObjectFlags.Attached);
		}
		
		public override void OnReduce ()
		{
			// placeholders are not reduced
		}
		
		public override void OnDocked (DockObject requestor, DockPlacement position, object data)
		{
			if (host != null && position != DockPlacement.Floating) {
				// we simply act as a placeholder for our host
				host.Dock (requestor, position, data);
			} else {
				if (!IsBound) {
					Console.WriteLine ("Attempt to dock a dock object to an unbound placeholder");
					return;
				}
				// dock the item as a floating of the controller
				Master.Controller.Dock (requestor, DockPlacement.Floating, null);
			}
		}
		
		public override void OnPresent (DockObject child)
		{
			// do nothing
		}
		
		/*
		* Tries to shrink the placement stack by examining the host's
		* children and see if any of them matches the placement which is at
		* the top of the stack.  If this is the case, it tries again with the
		* new host.
		*/
		public void DoExcursion ()
		{
			if (host != null && !Sticky && placementStack != null && placementStack.Count > 1) {
				DockObject newHost = host.GetObjectFromRelativePlacement (NextPlacement);
				if (newHost != null) {
					DisconnectHost ();
					
					PopNextPlacement ();
					
					// connect to the new host
					ConnectHost (newHost);
						
					// recurse ...
					if (!newHost.InReflow)
						DoExcursion ();
				}
			}
		}
		
		public override void Dispose ()
		{
			DisconnectHost ();
			base.Dispose ();
		}
		
		private void DisconnectHost ()
		{
			if (host == null)
				return;

			host.Detached -= new DetachedHandler (OnHostDetached);
			oldHostParent.Docked -= new DockedHandler (OnHostDocked);

			DumpTree (host);
			host = null;
		}
		
		private void ConnectHost (DockObject newHost)
		{
			if (host != null)
				DisconnectHost ();

			host = newHost;

			if (host is Dock)
				oldHostParent = host;
			else
				oldHostParent = host.ParentObject;
			
			host.Detached += new DetachedHandler (OnHostDetached);
			oldHostParent.Docked += new DockedHandler (OnHostDocked);
			
			DumpTree (host);
		}
		
		public void Attach (DockObject objekt)
		{
			if (objekt == null)
				return;
			
			// object binding
			if (!IsBound)
				Bind(objekt.Master);
			
			if (objekt.Master != Master)
				return;
			
			Freeze ();
			
			// detach from previous host first
			if (host != null)
				Detach (false);
			
			ConnectHost (objekt);
			
			DockObjectFlags |= DockObjectFlags.Attached;
			Thaw ();
		}

		void OnHostDetached (object sender, DetachedArgs a)
		{
			// skip sticky objects
			// Ignore the event if the host is in reflow, since it means that
			// it will be added again.
			if (sticky || host.InReflow)
				return;

			DockPlacement pos;
			DockObject newHost;
			
			// get the relative position of the host, and push it in the position stack
			host.GetRelativePlacement (out newHost, out pos);
				
			if (newHost == null) {
				DumpTree (host);
				Console.WriteLine ("Something weird happened while getting the child placement for {0} from parent {1}", host, newHost);
				return;
			}

			PushNextPlacement (pos);
				
			// disconnect host
			DisconnectHost ();

			// the toplevel was detached: we attach ourselves to the
			// controller with an initial placement of floating
			if (newHost == null) {
				newHost = this.Master.Controller;
				PushNextPlacement (DockPlacement.Floating);
			}

			if (newHost != null)
				ConnectHost (newHost);

			PrintPlacementStack ();
		}

		void OnHostDocked (object sender, DockedArgs a)
		{
			DockObject obj = sender as DockObject;
			DumpTree (obj);
			PrintPlacementStack ();
			
			// Try to follow the stack of positions
			DoExcursion ();

			PrintPlacementStack ();
		}

		[System.Diagnostics.Conditional ("DEBUG")]
		void PrintPlacementStack ()
		{
			Console.WriteLine ("-- {0} count {1}", host.Name, placementStack.Count);
			foreach (DockPlacement dp in placementStack)
				Console.WriteLine (dp);
		}
		
		public static void DumpTree (Widget w)
		{
			if (w == null)
				return;
			while (w.Parent != null)
				w = w.Parent;
				
			DumpTreeRec (w, 0);
		}
		
		[System.Diagnostics.Conditional ("DEBUG")]
		static void DumpTreeRec (Widget w, int n)
		{
			if (w is DockObject)
				Console.WriteLine (new string (' ', n*4) + "- " + ((DockObject)w).Name + " " + w.GetType ());
			if (w is Gtk.Container) {
				foreach (Widget c in ((Gtk.Container)w).Children)
					DumpTreeRec (c, n+1);
			}
		}
	}
}
