//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Editor;

    internal class SpaceReservationStack
    {
        #region Private Members
        internal Dictionary<string, int> _orderedManagerDefinitions;

		internal readonly Mono.TextEditor.MonoTextEditor _view;
        internal readonly List<SpaceReservationManager> _managers = new List<SpaceReservationManager>();
        bool _hasAggregateFocus;

        void OnManagerLostFocus(object sender, EventArgs e)
        {
            if (_hasAggregateFocus)
            {
                foreach (var manager in _managers)
                {
                    if (manager.HasAggregateFocus)
                        return;
                }

                _hasAggregateFocus = false;
                _view.QueueAggregateFocusCheck();
            }
        }

        void OnManagerGotFocus(object sender, EventArgs e)
        {
            _hasAggregateFocus = true;
            _view.QueueAggregateFocusCheck();
        }

        #endregion // Private Members

		public SpaceReservationStack(Dictionary<string, int> orderedManagerDefinitions, Mono.TextEditor.MonoTextEditor view)
        {
            _orderedManagerDefinitions = orderedManagerDefinitions;
            _view = view;
        }

        public ISpaceReservationManager GetOrCreateManager(string name)
        {
            foreach (SpaceReservationManager manager in _managers)
            {
                if (manager.Name == name)
                    return manager;
            }

            int rank;
            if (_orderedManagerDefinitions.TryGetValue(name, out rank))
            {
                SpaceReservationManager manager = new SpaceReservationManager(name, rank, _view);

                int position = 0;
                while (position < _managers.Count)
                {
                    SpaceReservationManager existing = _managers[position];
                    if (existing.Rank > rank)
                        break;

                    ++position;
                }

                _managers.Insert(position, manager);
                manager.LostAggregateFocus += OnManagerLostFocus;
                manager.GotAggregateFocus += OnManagerGotFocus;

                return manager;
            }

            return null;
        }

        public void Refresh()
        {
            GeometryGroup reservedGeometry = new GeometryGroup();

            //Make a copy just in case some one in PositionAndDisplay attempts to create a new manager.
            //We don't need to queue a new refresh because adding an empty manager won't affect what is displayed
            //(& adding a agent to a manager will queue a refresh).
            List<SpaceReservationManager> managers = new List<SpaceReservationManager>(_managers);
            foreach (SpaceReservationManager manager in managers)
            {
                manager.PositionAndDisplay(reservedGeometry);
            }
        }

        public bool IsMouseOver
        {
            get
            {
                foreach (var manager in _managers)
                {
                    if (manager.IsMouseOver)
                        return true;
                }

                return false;
            }
        }

        public bool HasAggregateFocus
        {
            get
            {
                //We can't uses _hasAggregateFocus (the got focus event may not have reached the manager yet)
                foreach (var manager in _managers)
                {
                    if (manager.HasAggregateFocus)
                        return true;
                }

                return false;
            }
        }
    }
}
