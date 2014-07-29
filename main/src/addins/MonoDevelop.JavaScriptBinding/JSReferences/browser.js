/*******************************************************************************
 * Copyright (c) 2008, 2011 IBM Corporation and others.
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *     IBM Corporation - initial API and implementation
 *******************************************************************************/
/**
  * Object DOMException()
  * @super Object
  * @type  constructor
  * @memberOf DOMException
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
function DOMException(){};
DOMException.prototype = new Object();
/**
  * Constant DOMException.INDEX_SIZE_ERR=1
  * @type Number
  * @memberOf DOMException
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see DOMException()  
  * @see     http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.INDEX_SIZE_ERR=1;
/**
  * Constant DOMException.DOMSTRING_SIZE_ERR=2
  * @type Number
  * @memberOf DOMException
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.   
  * @see DOMException()  
  * @see     http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.DOMSTRING_SIZE_ERR=2;
/**
  * Constant DOMException.HIERARCHY_REQUEST_ERR=3
  * @type Mi,ber
  * @memberOf DOMException
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition. 
  * @see DOMException() 
  * @see     http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.HIERARCHY_REQUEST_ERR=3;
/**
  * Constant DOMException.WRONG_DOCUMENT_ERR=4
  * @type Number
  * @see DOMException() 
  * @memberOf DOMException
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
   
  * @see     http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.WRONG_DOCUMENT_ERR=4;
/**
  * Constant DOMException.INVALID_CHARACTER_ERR=5
  * @memberOf DOMException
  * @type Number
  * @see DOMException()
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
  
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.INVALID_CHARACTER_ERR=5;
/**
  * Constant DOMException.NO_DATA_ALLOWED_ER=6
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  
  * @since Level 2 Document Object Model Core Definition. 
  * @since Standard ECMA-262 3rd. Edition

  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.NO_DATA_ALLOWED_ER=6;
/**
  * Constant DOMException.NO_MODIFICATION_ALLOWED_ERR=7
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.

  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.NO_MODIFICATION_ALLOWED_ERR=7;
/**
  * Constant DOMException.NOT_FOUND_ERR=8
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.

  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.NOT_FOUND_ERR=8;
/**
  * Constant DOMException.NOT_SUPPORTED_ERR=9
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.

  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.NOT_SUPPORTED_ERR=9;
/**
  * Constant DOMException.INUSE_ATTRIBUTE_ERR=10
  * @type Number
  * @memberOf DOMException
  * @see DOMException()
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
  
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.INUSE_ATTRIBUTE_ERR=10;
/**
  * Constant DOMException.INVALID_STATE_ERR=11
  * @type Number
  * @memberOf DOMException
  * @see DOMException()
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
  
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.INVALID_STATE_ERR=11;
/**
  * Constant DOMException.SYNTAX_ERR=12
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.

  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.SYNTAX_ERR=12;
/**
  * Constant DOMException.INVALID_MODIFICATION_ER=13
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  

  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.INVALID_MODIFICATION_ER=13;
/**
  * Constant DOMException.NAMESPACE_ERR=14
  * @type Number
  * @memberOf DOMException
  * @see DOMException()  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.

  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.NAMESPACE_ERR=14;
/**
  * Constant DOMException.NVALID_ACCESS_ERR=15
  * @type Number
  * @memberOf DOMException
  * @see DOMException() 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.INVALID_ACCESS_ERR=15;
/**
  * Property code
  * @type Number
  * @memberOf DOMException
  * @see DOMException() 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DOMException.prototype.code=new Number();


/**
  * Object DOMImplementation()
  * @super Object
  * @type  constructor
  * @memberOf DOMImplementation
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
function DOMImplementation();
DOMImplementation.prototype = new Object();

/**
  * function hasFeature()
  * @type  method
  * @memberOf DOMImplementation
  * @param {String} feature
  * @param {String} version 
  * @returns {boolean}
  * @see DOMImplementation
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.

  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
DOMImplementation.prototype.hasFeature = new function(feature, version){};

/**
  * function createDocumentType()
  * @type  method
  * @memberOf DOMImplementation
  * @param {String} namespaceURI
  * @param {String} qualifiedName 
  * @param {DocumentType} doctype 
  * @returns {Document}
  * @throws DOMException
  * @see DOMImplementation
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.

  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
DOMImplementation.prototype.createDocument = function(namespaceURI, qualifiedName, doctype){};
/**
  * function createDocumentType()
  * @type  method
  * @memberOf DOMImplementation

  * @param {String} qualifiedName
  * @param {String} publicId
  * @param {String} systemId
  
  * @returns {DocumentType}
  * @throws DOMException
  * @see DOMImplementation
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.

  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
DOMImplementation.prototype.createDocumentType = function(qualifiedName, publicId, systemId){}; 
/**
  * Object Node()
  * @super Object
  * @type  constructor
  * @memberOf Node
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
function Node(){};
Node.prototype=new Object(); 
/**
  * Constant Node.ELEMENT_NODE=1
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.ELEMENT_NODE=1; 
/**
  * Constant Node.ATTRIBUTE_NODE=2
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.ATTRIBUTE_NODE=2;
/**
  * Constant Node.TEXT_NODE=3
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.TEXT_NODE=3;
/**
  * Constant Node.CDATA_SECTION_NODE=4
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.CDATA_SECTION_NODE=4; 
/**
  * Constant Node.ENTITY_REFERENCE_NODE=5
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.ENTITY_REFERENCE_NODE=5; 
/**
  * Constant Node.ENTITY_NODE=6
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.ENTITY_NODE=6;
/**
  * Constant Node.PROCESSING_INSTRUCTION_NODE=7
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.PROCESSING_INSTRUCTION_NODE=7; 
/**
  * Constant Node.COMMENT_NODE=8
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.COMMENT_NODE=8;
/**
  * Constant Node.DOCUMENT_NODE=9
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.DOCUMENT_NODE=9;
/**
  * Constant Node.DOCUMENT_TYPE_NODE=10
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.DOCUMENT_TYPE_NODE=10; 
/**
  * Constant Node.DOCUMENT_FRAGMENT_NODE=11
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.DOCUMENT_FRAGMENT_NODE=11; 
/**
  * Constant Node.NOTATION_NODE=12
  * @type Number
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.NOTATION_NODE=12;

/**
  * Property nodeName
  * @type String
  * @memberOf Node
  * @see Node 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.nodeName = new String(); 
/**
  * Property nodeName
  * @type String
  * @memberOf Node
  * @see Node 
  * @throws DOMException when setting or getting the value.
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.nodeValue = new String(); 
/**
  * Property nodeType
  * @type Number
  * @memberOf Node
  * @see Node 
 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.nodeType = new Number(); 
/**
  * Property parentNode 
  * @type Node
  * @memberOf Node
  * @see Node 
 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.parentNode=new Node(); 
/**
  * Property childNodes  
  * @type NodeList
  * @memberOf Node
  * @see Node
  * @see NodeList 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.childNodes=new NodeList(); 
/**
  * Property firstChild 
  * @type Node
  * @memberOf Node
  * @see Node 
 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.firstChild=new Node(); 
/**
  * Property lastChild 
  * @type Node
  * @memberOf Node
  * @see Node 
 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.lastChild=new Node();  
/**
  * Property previousSibling 
  * @type Node
  * @memberOf Node
  * @see Node 
 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.previousSibling=new Node(); 
/**
  * Property nextSibling  
  * @type Node
  * @memberOf Node
  * @see Node 
 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.nextSibling=new Node(); 
/**
  * Property attributes  
  * @type NamedNodeMap
  * @memberOf Node
  * @see Node
  * @see NamedNodeMap 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.attributes=new NamedNodeMap();
/**
  * Property ownerDocument   
  * @type Document
  * @memberOf Node
  * @see Node
  * @see Document
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.ownerDocument = new Document(); 
/**
  * Property namespaceURI   
  * @type String
  * @memberOf Node
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.namespaceURI=new String(); 
/**
  * Property prefix   
  * @type String
  * @memberOf Node
  * @see Node
  * @throws DOMException on setting.
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.prefix = new String(); 
/**
  * Property localName   
  * @type String
  * @memberOf Node
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.localName= new String();
/**
  * function insertBefore(newChild, refChild)   
  * @type Method
  * @memberOf Node
  * @param {Node} newChilds
  * @param {Node} refChild
  * @returns {Node}
  * @throws DOMException
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.insertBefore = new function(newChild, refChild){}; 
/**
  * function replaceChild(newChild, oldChild) 
  * @type Method
  * @memberOf Node
  * @param {Node} newChilds
  * @param {Node} oldChild
  * @returns {Node}
  * @throws DOMException
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.replaceChild = function(newChild, oldChild){}; 
 /**
  * function removeChild(oldChild) 
  * @type Method
  * @memberOf Node
  * @param {Node} oldChild
  * @returns {Node}
  * @throws DOMException
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.removeChild = function(oldChild){}; 
 /**
  * function appendChild(newChild) 
  * @type Method
  * @memberOf Node
  * @param {Node} newChild
  * @returns {Node}
  * @throws DOMException
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Node.prototype.appendChild = function(newChild){}; 
 /**
  * function hasChildNodes() 
  * @type Method
  * @memberOf Node
  * @returns {Boolean}
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.prototype.hasChildNodes=function(){}; 
 /**
  * function hasChildNodes() 
  * @type Method
  * @memberOf Node
  * @param {Boolean} deep
  * @returns {Node}
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.prototype.cloneNode=function(deep){}; 
 /**
  * function normalize() 
  * @type Method
  * @memberOf Node
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Node.prototype.normalize = function(){}; 
 /**
  * function isSupported(feature, version)  
  * @type Method
  * @memberOf Node
  * @param {String} feature
  * @param {String} version
  * @returns {Boolean}
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.isSupported=function(feature, version){}; 
 /**
  * function hasAttributes()   
  * @type Method
  * @memberOf Node
  * @returns {Boolean}
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Node.prototype.hasAttributes=function(){};
 /**
  * Object NodeList   
  * @type constructor
  * @memberOf NodeList
  * @see NodeList
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function NodeList(){};
NodeList.prototype = new Object();
 /**
  * property length   
  * @type Number
  * @memberOf NodeList
  * @see NodeList
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 

NodeList.prototype.length=new Number(); 
 /**
  * function item(index) 
  *     Note: This object can also be dereferenced using square bracket notation (e.g. obj[1]). Dereferencing with an integer index is equivalent to invoking the item method with that index 
  * @type Method
  * @memberOf NodeList
  * @param {Number} index
  * @returns {Node}
  * @see NodeList
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
NodeList.prototype.item = function(index){}; 
/**
  * Object DocumentFragment()
  * DocumentFragment inherits all of the methods and properties from Document and Node.
  * @super Document
  * @type  constructor
  * @see Document
  * @memberOf  DocumentFragment
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
function DocumentFragment(){};
DocumentFragment.prototype=new Document(); 
/**
  * Object Document()
  * Document inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor
  * @see Node
  * @memberOf  Document
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function Document(){};
Document.prototype = new Node();
/**
  * property doctype
  * @type  DocumentType
  * @see Document
  * @see DocumentType
  * @memberOf  Document
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.doctype = new DocumentType(); 
/**
  * property implementation
  * @type   DOMImplementation
  * @see Document
  * @see DOMImplementation
  * @memberOf  Document
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.implementation = new DOMImplementation();
/**
  * property documentElement 
  * @type   Element
  * @see Document
  * @see Element
  * @memberOf  Document
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.documentElement= new Element(); 
 /**
  * function createElement(tagName)  
  * @type Method
  * @memberOf Document
  * @param {String} tagName
  * @returns {Element}
  * @throws DOMException
  * @see Document
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  

Document.prototype.createElement=function(tagName){}; 
 /**
  * function createDocumentFragment()  
  * @type Method
  * @memberOf Document
  * @returns {DocumentFragment}
  * @see Document
  * @see DocumentFragment
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.createDocumentFragment=function(){}; 
 /**
  * function createTextNode(data)  
  * @type Method
  * @memberOf Document
  * @param {String} data
  * @returns {Text}
  * @see Document
  * @see Text
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.createTextNode=function(data){}; 
 /**
  * function createComment(data)  
  * @type Method
  * @memberOf Document
  * @param {String} data
  * @returns {Comment}
  * @see Document
  * @see Comment
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.createComment=function(data){}; 
 /**
  * function createCDATASection(data)  
  * @type Method
  * @memberOf Document
  * @param {String} data
  * @returns {CDATASection}
  * @throws DOMException
  * @see Document
  * @see CDATASection
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.createCDATASection = function(data){};
 /**
  * function createProcessingInstruction(target, data) 
  * @type Method
  * @memberOf Document
  * @param {String} target
  * @param {String} data
  * @returns {ProcessingInstruction}
  * @throws DOMException
  * @see Document
  * @see ProcessingInstruction
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.createProcessingInstruction=function(target, data){}; 
 /**
  * function createAttribute(name)  
  * @type Method
  * @memberOf Document
  * @param {String} name
  * @returns {Attr}
  * @throws DOMException
  * @see Document
  * @see Attr
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.createAttribute=function(name){}; 
 /**
  * function createEntityReference(name)  
  * @type Method
  * @memberOf Document
  * @param {String} name
  * @returns {EntityReference}
  * @throws DOMException
  * @see Document
  * @see EntityReference
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.createEntityReference=function(name){}; 
 /**
  * function getElementsByTagName(tagname)  
  * @type Method
  * @memberOf Document
  * @param {String} tagname
  * @returns {NodeList}
  * @see Document
  * @see NodeList
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.getElementsByTagName=function(tagname){}; 
 /**
  * function importNode(importedNode, deep)  
  * @type Method
  * @memberOf Document
  * @param {Node} importedNode
  * @param {Boolean} deep
  * @returns {Node}
  * @throws DOMException
  * @see Document
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.importNode=function(importedNode, deep){}; 
 /**
  * function createElementNS(namespaceURI, qualifiedName) 
  * @type Method
  * @memberOf Document
  * @param {String} namespaceURI
  * @param {String} qualifiedName
  * @returns {Element}
  * @throws DOMException
  * @see Document
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.createElementNS=function(namespaceURI, qualifiedName){}; 
 /**
  * function createAttributeNS(namespaceURI, qualifiedName)
  * @type Method
  * @memberOf Document
  * @param {String} namespaceURI
  * @param {String} qualifiedName
  * @returns {Attr}
  * @throws DOMException
  * @see Document
  * @see Attr
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.createAttributeNS=function(namespaceURI, qualifiedName){}; 
/**
  * function getElementsByTagNameNS(namespaceURI, localName)
  * @type Method
  * @memberOf Document
  * @param {String} namespaceURI
  * @param {String} qualifiedName
  * @returns {NodeList}
  * @see Document
  * @see NodeList
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Document.prototype.getElementsByTagNameNS=function(namespaceURI, localName){}; 
/**
  * function getElementById(elementId)
  * @type Method
  * @memberOf Document
  * @param {String} elementId
  * @returns {Element}
  * @see Document
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Document.prototype.getElementById=function(elementId){}; 
/**
  * Object NamedNodeMap()
  * @super Object
  * @type  constructor
  * @memberOf NamedNodeMap
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
function NamedNodeMap(){};
NamedNodeMap.prototype = new Object();
/**
  * property length 
  * @type   Number
  * @memberOf  NamedNodeMap;
  * @see NamedNodeMap

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.length=new Number(); 
/**
  * function getNamedItem(name) 
  * @type Method
  * @memberOf NamedNodeMap
  * @param {String} Name
  * @returns {Node}
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
NamedNodeMap.prototype.getNamedItem=function(name){}; 
/**
  * function setNamedItem(arg) 
  * @type Method
  * @memberOf NamedNodeMap
  * @param {Node} arg
  * @returns {Node}
  * @throws DOMException
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.setNamedItem=function(arg){}; 
/**
  * function removeNamedItem(name)  
  * @type Method
  * @memberOf NamedNodeMap
  * @param {String} name
  * @returns {Node}
  * @throws DOMException
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.removeNamedItem=function(name){}; 
/**
  * function item(index)
  * Note: This object can also be dereferenced using square bracket notation (e.g. obj[1]). Dereferencing with an integer index is equivalent to invoking the item method with that index.
  * @type Method
  * @memberOf NamedNodeMap
  * @param {Number} index
  * @returns {Node}
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.item=function(index){}; 
/**
  * function getNamedItemNS(namespaceURI, localName) 
  * @type Method
  * @memberOf NamedNodeMap
  * @param {String} namespaceURI
  * @param {String} localName
  * @returns {Node}
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.getNamedItemNS=function(namespaceURI, localName){}; 
/**
  * function setNamedItemNS(arg) 
  * @type Method
  * @memberOf NamedNodeMap
  * @param {Node} arg
  * @param {String} localName
  * @returns {Node}
  * @throws DOMException
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.setNamedItemNS=function(arg){}; 
/**
  * function removeNamedItemNS(namespaceURI, localName)  
  * @type Method
  * @memberOf NamedNodeMap
  * @param {String} namespaceURI
  * @param {String} localName
  * @returns {Node}
  * @throws DOMException
  * @see NamedNodeMap
  * @see Node
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
NamedNodeMap.prototype.removeNamedItemNS=function(namespaceURI, localName){}; 
/**
  * Object CharacterData()
  * CharacterData inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor
  * @see Node
  * @memberOf  CharacterData
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
function CharacterData(){};
CharacterData.prototype=new Node();
/**
  * property data
  * @type   String
  * @memberOf  CharacterData
  * @throws DOMException on setting and can raise a DOMException object on retrieval.
  * @see CharacterData

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
CharacterData.prototype.data=new String(); 
/**
  * property length
  * @type   Number
  * @memberOf  CharacterData
  * @see CharacterData

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
CharacterData.prototype.length=new Number(); 
/**
  * function substringData(offset, count)   
  * @type Method
  * @memberOf CharacterData
  * @param {Number} parameter
  * @param {Number} count
  * @returns {String}
  * @throws DOMException
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
CharacterData.prototype.substringData=function(offset, count){}; 
/**
  * function appendData(arg)    
  * @type Method
  * @memberOf CharacterData
  * @param {String} arg
  * @returns {String}
  * @throws DOMException
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
CharacterData.prototype.appendData=function(arg){}; 
/**
  * function insertData(offset, arg)  
  * @type Method
  * @memberOf CharacterData
  * @param {Number} offset
  * @param {String} arg
  * @throws DOMException
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
CharacterData.prototype.insertData=function(offset, arg){};  
/**
  * function deleteData(offset, count)  
  * @type Method
  * @memberOf CharacterData
  * @param {Number} offset
  * @param {Number} count
  * @throws DOMException
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
CharacterData.prototype.deleteData=function(offset, count){}; 
/**
  * function replaceData(offset, count, arg)
  * @type Method
  * @memberOf CharacterData
  * @param {Number} offset
  * @param {Number} count
  * @param {String} arg
  * @throws DOMException
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
CharacterData.prototype.replaceData=function(offset, count, arg){}; 
/**
  * Object Attr()
  * Attr inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor
  * @see Node
  * @memberOf Attr
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function Attr(){};
Attr.prototype=new Node();
/**
  * property name
  * @type   String
  * @memberOf  Attr
 
  * @see Attr

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Attr.prototype.name=new String(); 
/**
  * property specified
  * @type   Boolean
  * @memberOf  Attr
  
  * @see Attr

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Attr.prototype.specified=new Boolean(); 
/**
  * property value 
  * @type   Boolean
  * @memberOf  Attr
  * @throws DOMException on setting and can raise a DOMException object on retrieval.
  * @see Attr

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Attr.prototype.value=new String();
/**
  * property ownerElement 
  * @type   Element
  * @memberOf  Attr
  * @throws DOMException on setting and can raise a DOMException object on retrieval.
  * @see Attr
  * @see Element

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Attr.prototype.ownerElement=new Element(); 
/**
  * Object Element()
  * Element inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor
  * @see Node
  * @memberOf Attr
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function Element(){};
Element.prototype=new Node(); 
/**
  * property tagName  
  * @type   String
  * @memberOf  Element
  * @throws DOMException on setting and can raise a DOMException object on retrieval.
  * @see Attr
  * @see Element

  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Element.prototype.tagName=new String();
/**
  * function getAttribute(name) 
  * @type Method
  * @memberOf Element
  * @param {String} name
  * @returns {String}
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Element.prototype.getAttribute=function(name){}; 
/**
  * function setAttribute(name, value) 
  * @type Method
  * @memberOf Element
  * @param {String} name
  * @param {String} value
  * @throws DOMException
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Element.prototype.setAttribute=function(name, value){}; 
/**
  * function removeAttribute(name)
  * @type Method
  * @memberOf Element
  * @param {String} name
  * @throws DOMException
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Element.prototype.removeAttribute=function(name){}; 
/**
  * function getAttributeNode(name)
  * @type Method
  * @memberOf Element
  * @param {String} name
  * @returns {Attr}
  * @see Element
  * @see Attr
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.getAttributeNode=function(name){}; 
/**
  * function setAttributeNode(newAttr)
  * @type Method
  * @memberOf Element
  * @param {Attr} newAttr
  * @returns {Attr}
  * @throws DOMException
  * @see Element
  * @see Attr
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.setAttributeNode=function(newAttr){}; 
/**
  * function removeAttributeNode(oldAttr) 
  * @type Method
  * @memberOf Element
  * @param {Attr} oldAttr
  * @returns {Attr}
  * @throws DOMException
  * @see Element
  * @see Attr;
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Element.prototype.removeAttributeNode=function(oldAttr){}; 
/**
  * function getElementsByTagName(name)
  * @type Method
  * @memberOf Element
  * @param {String} name
  * @returns {NodeList}
  * @see NodeList
  * @see Element;
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Element.prototype.getElementsByTagName=function(name){}; 
/**
  * function getAttributeNS(namespaceURI, localName) 
  * @type Method
  * @memberOf Element
  * @param {String} namespaceURI
  * @param {String} localName
  * @returns {String}
  * @see Element
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.getAttributeNS=function(namespaceURI, localName){}; 
 /**
  * function setAttributeNS(namespaceURI, qualifiedName, value)  
  * @type Method
  * @memberOf Element
  * @param {String} namespaceURI
  * @param {String} qualifiedName
  * @param {String} value
  * @throws DOMException
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.setAttributeNS=function(namespaceURI, qualifiedName, value){}; 
 /**
  * function removeAttributeNS(namespaceURI, localName)  
  * @type Method
  * @memberOf Element
  * @param {String} namespaceURI
  * @param {String} localName
  * @throws DOMException
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.removeAttributeNS=function(namespaceURI, localName){}; 
 /**
  * function getAttributeNodeNS(namespaceURI, localName)   
  * @type Method
  * @memberOf Element
  * @param {String} namespaceURI
  * @param {String} localName
  * @returns {Attr}
  * @throws DOMException
  * @see Attr
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.getAttributeNodeNS=function(namespaceURI, localName){}; 
 /**
  * function setAttributeNodeNS(newAttr)    
  * @type Method
  * @memberOf Element
  * @param {Attr} newAttr

  * @returns {Attr}
  * @throws DOMException
  * @see Attr
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.setAttributeNodeNS=function(newAttr){}; 
 /**
  * function getElementsByTagNameNS(namespaceURI, localName)   
  * @type Method
  * @memberOf Element
  * @param {String} namespaceURI
  * @param {String} localName

  * @returns {NodeList}
 
  * @see NodeList
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.getElementsByTagNameNS=function(namespaceURI, localName){}; 
 /**
  * function hasAttribute(name)   
  * @type Method
  * @memberOf Element
  
  * @param {String} name

  * @returns {Boolean}
 
  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Element.prototype.hasAttribute=function(name){}; 
 /**
  * function hasAttributeNS(namespaceURI, localName)    
  * @type Method
  * @memberOf Element
  * @param {String} namespaceURI
  * @param {String} localName

  * @returns {Boolean}
 
  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Element.prototype.hasAttributeNS=function(namespaceURI, localName){}; 
/**
  * Object Text()
  * Text inherits all of the methods and properties from CharacterData.
  * @super CharacterData
  * @type  constructor

  * @memberOf Text
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
function Text(){};
Text.prototype = new CharacterData();
 /**
  * function splitText(offset)     
  * @type Method
  * @memberOf Text
  * @param {Number} offset
 

  * @returns {Text}
  * @throws DOMException
  * @see Text
  
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Text.prototype.splitText=function(offset){}; 
/**
  * Object Comment()
  * Comment inherits all of the methods and properties from CharacterData.
  * @super CharacterData
  * @type  constructor

  * @memberOf Comment
  * @see CharacterData
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
function Comment(){};
Comment.prototype = new CharacterData();
/**
  * Object CDATASection()
  * Comment inherits all of the methods and properties from Text.
  * @super Text
  * @type  constructor

  * @memberOf CDATASection
  * @see Text
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
function CDATASection(){};
CDATASection.prototype = new Text();
/**
  * Object DocumentType()
  * DocumentType inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor

  * @memberOf DocumenType
  * @see Node
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function DocumentType(){};
DocumentType.prototype = new Node(); 
/**
  * read-only Property name
  * @type String
  * @memberOf DocumentType
  * @see DocumentType(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DocumentType.prototype.name=new String(); 
/**
  * read-only Property entities
  * @type NamedNodeMap
  * @memberOf DocumentType
  * @see DocumentType(); 
  * @see NamedNodeMap
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DocumentType.prototype.entities = new NamedNodeMap();
/**
  * Read-Only Property notations 
  * @type NamedNodeMap
  * @memberOf DocumentType
  * @see DocumentType(); 
  * @see NamedNodeMap
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DocumentType.prototype.notations=new NamedNodeMap(); 
/**
  * Read-Only Property publicId 
  * @type String
  * @memberOf DocumentType
  * @see DocumentType(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
DocumentType.prototype.publicId=new String(); 
/**
  * Read-Only Property systemId  
  * @type String
  * @memberOf DocumentType
  * @see DocumentType(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
DocumentType.prototype.systemId=new String(); 
/**
  * Read-Only Property internalSubset 
  * @type String
  * @memberOf DocumentType
  * @see DocumentType(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
DocumentType.prototype.internalSubset=new String();
/**
  * Object Notation()
  * Notation inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor

  * @memberOf Notation
  * @see Node
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function Notation(){};
Notation.prototype=new Node(); 
/**
  * Read-Only Property publicId 
  * @type String
  * @memberOf Notation
  * @see Notation(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
Notation.prototype.publicId=new String(); 
/**
  * Read-Only Property systemId 
  * @type String
  * @memberOf Notation
  * @see Notation(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Notation.prototype.systemId=new String();
/**
  * Object Entity()
  * Entity inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor

  * @memberOf Entity
  * @see Node
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */  
function Entity(){}; 
Entity.prototype=new Node();
/**
  * Read-Only Property publicId 
  * @type String
  * @memberOf Entity
  * @see Entity(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Entity.prototype.publicId=new String();
 /**
  * Read-Only Property systemId 
  * @type String
  * @memberOf Entity
  * @see Entity(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
Entity.prototype.systemId=new String(); 
 /**
  * Read-Only Property notationName 
  * @type String
  * @memberOf Entity
  * @see Entity(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
Entity.prototype.notationName=new String(); 
/**
  * Object EntityReference()
  * EntityReference inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor

  * @memberOf EntityReference
  * @see Node
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */ 
function EntityReference(){};
EntityReference.prototype=new Node();
 /**
  * Object ProcessingInstruction()
  * ProcessingInstruction inherits all of the methods and properties from Node.
  * @super Node
  * @type  constructor

  * @memberOf ProcessingInstruction
  * @see Node
  * @since Standard ECMA-262 3rd. Edition
  * @since Level 2 Document Object Model Core Definition.
  * @see   http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
 
function ProcessingInstruction(){}; 
ProcessingInstruction.prototype=new Node();
 /**
  * Read-Only Property target  
  * @type String
  * @memberOf ProcessingInstruction
  * @see ProcessingInstruction(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
ProcessingInstruction.prototype.target=new String();
 /**
  * Read-Only Property target  
  * @type String
  * @memberOf ProcessingInstruction
  * @throws DOMException on setting.
  * @see ProcessingInstruction(); 
  * @since Standard ECMA-262 3rd. Edition 
  * @since Level 2 Document Object Model Core Definition.
 
  * @see    http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/ecma-script-binding.html     
 */
ProcessingInstruction.prototype.data=new String(); 



