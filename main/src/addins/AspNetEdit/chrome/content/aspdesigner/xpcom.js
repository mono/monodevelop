 /*
 * xpcom.js - Implementations of some XPCOM interfaces
 * 
 * Authors: 
 *  Blagovest Dachev <blago@dachev.com>
 *  
 * Copyright (C) 2005 Blagovest Dachev
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */




//* ___________________________________________________________________________
// Implementations of some XPCOM interfaces, to observe various editor events
// and actions. Do not remove any of the methods entirely or Mozilla will choke
//_____________________________________________________________________________
// nsIObserver implementation
var gNsIObserverImplementation = {
	// Tel the host command status has changed.
	observe: function (aSubject, aTopic, aData)
	{
		switch (aTopic) {
		case 'cmd_bold':
			break;
		case 'cmd_italics':
			break;
		case 'cmd_underline':
			break;
		case 'cmd_indent':
			break;
		case 'cmd_outdent':
			break;
		}
	}
}

// nsISelectionListener implementation
// TODO: Redo this one, accounting for recursive calls
var gNsISelectionListenerImplementation = {
	notifySelectionChanged: function(doc, sel, reason)
	{
		// Make sure we can't focus a control
		//TODO: make it account for md-can-drop="true" controls, which
		// should be able to recieve focus
		if(sel.isCollapsed && editor && editor.base) {
			var focusNode = sel.focusNode;
			var parentControl =
				editor.base.getElementOrParentByTagName (CONTROL_TAG_NAME,
							focusNode);
			if(parentControl) {
				editor.base.setCaretAfterElement (parentControl);
			}
		}
	}
}

// nsIEditActionListener implementation
var gNsIEditActionListenerImplementation = {
	DidCreateNode: function(tag, node, parent, position, result)
	{
		//alert('did create node');
	},

	// TODO: Check if deleted node contains a control, not only if it is one
	DidDeleteNode: function(child, result)
	{
		if(!editor.inResize && !editor.dragState &&
		   !editor.inUpdate && !editor.inCommandExec) {
			var control = editor.removeLastDeletedControl ();
			if(control) {
				var deletionStr = 'deleteControl(s):';
				deletionStr += ' id=' + control + ',';
				editor.removeFromControlTable (control);
				host.removeControl (control);
				dump (deletionStr +
					' Message source: DidDeleteNode()');
				dump ('There is/are '
					+ editor.controlCount
					+ ' controls left in the page');
			}
		}
	},

	// For each element in to-be-deleted array, remove control from the control
	// table, let the host know we have deleted a control by calling the
	// respective method, and remove from to-be-deleted array.
	DidDeleteSelection: function(selection)
	{
		if(!editor.inResize && !editor.dragState &&
		   !editor.inUpdate && !editor.inCommandExec) {
			var control = editor.removeLastDeletedControl ();
			if(control) {
				var deletionStr = 'Did delete control(s):';
				while(control) {
					deletionStr += ' id=' + control + ',';
					editor.removeFromControlTable (control);
					host.removeControl (control);
					control = editor.removeLastDeletedControl ();
				}
				dump (deletionStr +
					' Message source: DidDeleteSelection()');
				dump ('There is/are ' +
					editor.controlCount +
					' controls left in the page');
			}
		}
	},

	DidDeleteText: function(textNode, offset, length, result)
	{
	
	},

	// Make sure to check node contents for controls too. The node could be
	// a table (or any other element) with a control inside.
	DidInsertNode: function(node, parent, position, result)
	{
		var dumpStr = 'Did insert node ' + node.nodeName;
		dumpStr += (node.nodeType == 1) ?
			', id=' + node.getAttribute(ID) :
			'';
		dump (dumpStr);

		// Check to see if we have inserted a new controls. We need to
		// add'em to the control table. Also update reference to all
		// existing controls
		var controls =
			editor.base.document.getElementsByTagName (CONTROL_TAG_NAME);
		if(controls.length > 0) {
			var i = 0;
			var width, height;
			while(controls [i]) {
				if(editor.getControlTable ().getById (controls [i].getAttribute (ID))) {
					editor.getControlTable ().update (controls [i].getAttribute (ID),
									controls [i]);
					dump ('Did update control(id=' +
						controls [i].getAttribute (ID) +
						') reference in table');
				}
				else {
					editor.insertInControlTable (controls [i].getAttribute (ID),
							controls [i]);

					dump ('New control (id=' +
						controls [i].getAttribute (ID) +
						') inserted');
					dump ('There is/are ' +
						editor.controlCount +
						' controls in the page');
 				}
				editor.setSelectNone (controls [i]);
				width = controls [i].getAttribute(WIDTH);
				height = controls [i].getAttribute(HEIGHT);
				controls [i].style.setProperty (MIN_WIDTH,
							width, '');
				controls [i].style.setProperty (MIN_HEIGHT,
							height, '');

				// Create a NodeIterator to find <table>
				// tags
				var tables =
					editor.base.document.createTreeWalker(controls [i],
								NodeFilter.SHOW_ELEMENT,
								tableFilter,
								false);

				// Use the iterator to loop through all
				// tables
				editor.cancelTableOverrideStyle (tables);

				i++;
			}
		}

		if(editor.dragState) {
			dump ('End drag');
		}

		if(editor.nodeIsControl (node) &&
		   (node.nodeType == 1 || node.nodeType == 3)) {
			//editor.selectControl (node.getAttribute (ID));
		}
		if(editor.dragState)
			editor.dragState = false;
	},

	DidInsertText: function(textNode, offset, string, result)
	{
	
	},

	DidJoinNodes: function(leftNode, rightNode, parent, result)
	{
		//alert('did join nodes');
	},

	DidSplitNode: function(existingRightNode, offset, newLeftNode, result)
	{
		//alert('did split node');
	},

	WillCreateNode: function(tag, parent, position)
	{
		//alert ('Will create node');
	},

	WillDeleteNode: function(child)
	{
		dump ('will delete node-----------------------');
		if(!editor.inResize && !editor.dragState &&
		   !editor.inUpdate && !editor.inCommandExec) {
			var deletionStr = 'Will delete control(s):';
			var i       = 0;

			// is the node itself control?
			if(editor.nodeIsControl (child)) {
					deletionStr += ' id=' +
						child.getAttribute (ID) + ',';
					editor.addLastDeletedControl (child.getAttribute (ID));
			}

			// does the node contain any controls?
			var control = editor.getControlFromTableByIndex (i);
			while(control) {
				if(editor.isControlChildOf (child, control)) {
					deletionStr += ' id=' +
						control.getAttribute (ID) + ',';
					editor.addLastDeletedControl (control.getAttribute (ID));
				}
				i++;
				control = editor.getControlFromTableByIndex (i);
			}
			if(deletionStr != 'Will delete control(s):')
				dump (deletionStr +
					' Message source: WillDeleteNode()');
		}
	},

	// Check if the selection to be deleted contains controls and prepare
	//for deletion. Load all to-be-deleted controls in an array, so we can
	// access them after actual deletion in order to notify the host.
	WillDeleteSelection: function(selection)
	{
		dump ('will delete selection----------context:' +
			'inResize=' + editor.inResize + ', ' +
			'dragState=' + editor.dragState + ', ' +
			'inUpdate=' + editor.inUpdate + ', ' +
			'inCommandExec=' + editor.inCommandExec);
		if(!editor.inResize && !editor.dragState &&
		   !editor.inUpdate && !editor.inCommandExec) {
			var i       = 0;
			var control = editor.getControlFromTableByIndex (i);
			var deletionStr = 'Will delete control(s):';
			if(control) {
				while(control) {
					if(selection.containsNode (control, true)) {
						deletionStr += ' id=' +
							control.getAttribute (ID) + ',';
						editor.addLastDeletedControl (control.getAttribute (ID));
					}
					i++;
					control = editor.getControlFromTableByIndex (i);
				}
				if(deletionStr != 'Will delete control(s):')
					dump (deletionStr +
						' Message source: WillDeleteSelection()');
			}
		}
	},

	WillDeleteText: function(textNode, offset, length)
	{
	
	},

	WillInsertNode: function(node, parent, position)
	{

	},

	WillInsertText: function(textNode, offset, string)
	{
	
	},

	WillJoinNodes: function(leftNode, rightNode, parent)
	{
		//alert ('Will join node');
	},

	WillSplitNode: function(existingRightNode, offset)
	{
		//alert ('Will split node');
	}
}

// nsIHTMLObjectResizeListener implementation
var gNsIHTMLObjectResizeListenerImplementation = {
	onEndResizing: function(element, oldWidth, oldHeight, newWidth, newHeight)
	{
		if(editor.nodeIsControl (element)) {
			var id = element.getAttribute (ID);
			host.resizeControl (id, newWidth, newHeight);
		}
		editor.inResize = false;
		dump ('End resize.');
	},

	onStartResizing: function(element)
	{
		if(editor.nodeIsControl (element)) {
			editor.beginBatch ();
		}
		editor.inResize = true;
		dump ('Begin resize.');
	}
}

// nsIContentFilter implementation
// In future we should use this one to manage all insertion coming from
// paste, drag&drop, insertHTML(), and page load. Currently not working.
// Mozilla throws an INVALID_POINTER exception
var gNsIContentFilterImplementation = {
	notifyOfInsertion: function(mimeType,
				    contentSourceURL,
				    sourceDocument,
				    willDeleteSelection,
				    docFragment,
				    contentStartNode,
				    contentStartOffset,
				    contentEndNode,
				    contentEndOffset,
				    insertionPointNode,
				    insertionPointOffset,
				    continueWithInsertion)
	{
		
	}
}
