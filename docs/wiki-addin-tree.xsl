<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" indent="no" />

<xsl:template match="/">
= Introduction =
This document shows the complete extension tree available to add-in developers.
= Extension Tree =
<xsl:apply-templates select="ExtensionTree/Tree/Node/*">
	<xsl:sort select="@name" />
</xsl:apply-templates>
= Extension Points =
<xsl:for-each select="ExtensionTree/AddIns/AddIn">
<xsl:sort select="@name" />
== <xsl:value-of select="@name"/> module==
<xsl:variable name="aname" select="@name" />
<xsl:for-each select="/ExtensionTree/Tree/Node//Node[@add-in=$aname]">
<xsl:sort select="@title" />
===<xsl:value-of select="@title"/>===
'''Path''':  <xsl:value-of select="@path"/>
<xsl:if test="@remarks and @remarks!=''">

'''Remarks''': <xsl:value-of select="@remarks"/>
</xsl:if>

'''Child nodes''':
<ul>
<xsl:for-each select="ChildNode"><xsl:sort select="@id" /><xsl:variable name="cid" select="@id"/><li> [[#<xsl:value-of select="@id" />|<xsl:value-of select="@id" />]]: <xsl:value-of select="/ExtensionTree/Codons/Codon[@id=$cid]/@description" /></li>
</xsl:for-each>
</ul>
</xsl:for-each>
</xsl:for-each>
= Extension Elements =
<xsl:apply-templates select="ExtensionTree/Codons/*">
	<xsl:sort select="@id" />
</xsl:apply-templates>

</xsl:template>

<xsl:template match="Codon">
== <xsl:value-of select="@id" /> ==
<xsl:value-of select="@description" />

'''Properties:'''

{| border="1"
|'''Name''' || '''Type''' || '''Description'''
<xsl:apply-templates>
	<xsl:sort select="@name" />
</xsl:apply-templates>
|}

<xsl:if test="ChildNode">
'''Child nodes:'''
<ul>
<xsl:for-each select="ChildNode"><xsl:sort select="@id" /><xsl:variable name="cid" select="@id"/><li>[[#<xsl:value-of select="@id" />|<xsl:value-of select="@id" />]]: <xsl:value-of select="/ExtensionTree/Codons/Codon[@id=$cid]/@description" /></li>
</xsl:for-each>
</ul>
</xsl:if>

'''Can be used in:'''
<ul>
<xsl:variable name="cid" select="@id"/>
<xsl:for-each select="/ExtensionTree/Tree/Node//Node[ChildNode/@id=$cid]"><li>[[#<xsl:value-of select="@title" />|<xsl:value-of select="@title" />]]</li>
</xsl:for-each>
<xsl:for-each select="/ExtensionTree/Codons/Codon[ChildNode/@id=$cid]"><li>[[#<xsl:value-of select="@id" />|<xsl:value-of select="@id" />]] element</li>
</xsl:for-each>
</ul>
</xsl:template>

<xsl:template match="Property">
|-
|<xsl:value-of select="@name" /> || <xsl:value-of select="@type" /> || <xsl:value-of select="@description" />
</xsl:template>

<xsl:template match="Node[@add-in]"><ul><li>'''[[#<xsl:value-of select="@title"/>|<xsl:value-of select="@name"/>]]'''<br/><xsl:value-of select="@title"/></li></ul>
<blockquote>
<xsl:apply-templates><xsl:sort select="@name" /></xsl:apply-templates>
</blockquote>
</xsl:template>

<xsl:template match="Node"><ul><li>'''<xsl:value-of select="@name"/>'''</li></ul>
<blockquote><xsl:apply-templates><xsl:sort select="@name" /></xsl:apply-templates></blockquote>
</xsl:template>

</xsl:stylesheet>
