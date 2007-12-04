<?xml version="1.0" encoding="utf-8" ?>
<!--
	Modified version of a XSLT with following copyright:

Lutz Roeders's .NET Reflector, October 2000.
Copyright (C) 2000-2002 Lutz Roeder. All rights reserved.
http://www.aisto.com/roeder/dotnet
roeder@aisto.com

Thanks fly out to Lutz Roeder for giving permission to use his XSLT :)
All bugs in this XSLT belong to Mike Krueger mike@icsharpcode.net and are 
protected by international copyright laws
 -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" indent="no" />
	
	<xsl:template match="/">
		<BODY ID="bodyID" CLASS="dtBODY">
			<DIV ID="nstext">
				<xsl:apply-templates select="member"/>
			</DIV>
		</BODY>
	</xsl:template>
	
	<xsl:template match="member">
		<xsl:if test="summary">
			<xsl:apply-templates select="summary"/>
		</xsl:if>
		
		<xsl:if test="param">
			<H4 CLASS="dtH4">Parameters</H4>
			<DL><xsl:apply-templates select="param"/></DL>
		</xsl:if>
		
		<xsl:if test="returns">
			<H4 CLASS="dtH4">Return Value</H4>
			<xsl:apply-templates select="returns"/>
		</xsl:if>
		
		<xsl:if test="value">
			<H4 CLASS="dtH4">Value</H4>
			<xsl:apply-templates select="value"/>
		</xsl:if>
		
		<xsl:if test="exception">
			<H4 CLASS="dtH4">Exceptions</H4>
			<DIV CLASS="tablediv">
				<TABLE CLASS="dtTABLE" CELLSPACING="0">
					<TR VALIGN="top">
						<TH WIDTH="50%">Exception Type</TH>
						<TH WIDTH="50%">Condition</TH>
					</TR>
					<xsl:apply-templates select="exception"/>
				</TABLE>
			</DIV>
		</xsl:if>
		
		<xsl:if test="permission">
			<H4 CLASS="dtH4">Permission</H4>
			<DIV CLASS="tablediv">
				<TABLE CLASS="dtTABLE" CELLSPACING="0">
					<TR VALIGN="top">
						<TH WIDTH="50%">Member</TH>
						<TH WIDTH="50%">Description</TH>
					</TR>
					<xsl:apply-templates select="permission"/>
				</TABLE>
			</DIV>
		</xsl:if>
		
		<xsl:if test="remarks">
			<H4 CLASS="dtH4">Remarks</H4>
			<xsl:apply-templates select="remarks"/>
		</xsl:if>
		
		<xsl:if test="example">
			<H4 CLASS="dtH4">Example</H4>
			<xsl:apply-templates select="example"/>
		</xsl:if>
	
		<xsl:if test="seealso">
			<H4 CLASS="dtH4">See Also</H4>
			<xsl:apply-templates select="seealso"/>
		</xsl:if>
		<BR/><BR/>
	</xsl:template>
	
	<xsl:template match="text()">
		<xsl:value-of select="."/>
	</xsl:template>
	
	<!-- Inner Tags -->
	<xsl:template match="c">
		<pre class="code">
			<xsl:apply-templates/>
		</pre>
	</xsl:template>
	
	<xsl:template match="exception">
		<TR VALIGN="top">
			<TD WIDTH="50%">
				<A>
					<xsl:attribute name="href">
						urn:member:<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:attribute name="title">
						<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:value-of select="@cref"/>
				</A>
			</TD>
			<TD WIDTH="50%">
				<xsl:apply-templates/>
			</TD>
		</TR>
	</xsl:template>
	
	<xsl:template match="list">
		<xsl:if test="@type[.='table']">
			<DIV CLASS="tablediv">
				<TABLE CLASS="dtTABLE" CELLSPACING="0">
					<xsl:for-each select="listheader">
						<TR VALIGN="top">
							<TH WIDTH="50%">
								<xsl:for-each select="term">
									<xsl:apply-templates/>
								</xsl:for-each>
							</TH>
							<TH WIDTH="50%">
								<xsl:for-each select="description">
									<xsl:apply-templates/>
								</xsl:for-each>
							</TH>
						</TR>
					</xsl:for-each>
					<xsl:for-each select="item">
						<TR VALIGN="top">
							<TD WIDTH="50%">
								<xsl:for-each select="term">
									<xsl:apply-templates/>
								</xsl:for-each>
							</TD>
							<TD WIDTH="50%">
								<xsl:for-each select="description">
									<xsl:apply-templates/>
								</xsl:for-each>
							</TD>
						</TR>
					</xsl:for-each>
				</TABLE>
			</DIV>
		</xsl:if>
		<xsl:if test="@type[.='bullet']">
			<UL>
				<xsl:for-each select="item">
					<LI>
						<xsl:for-each select="term">
							<xsl:apply-templates/>
						</xsl:for-each>
					</LI>
				</xsl:for-each>
			</UL>
		</xsl:if>
		<xsl:if test="@type[.='number']">
			<OL type="1">
				<xsl:for-each select="item">
					<LI>
						<xsl:for-each select="term">
							<xsl:apply-templates/>
						</xsl:for-each>
					</LI>
				</xsl:for-each>
			</OL>
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="param">
		<DT><I>
			<xsl:value-of select="@name"/>
		</I></DT>
		<DD>
			<xsl:apply-templates/>
		</DD>
	</xsl:template>
	
	<xsl:template match="paramref">
		<I>
			<xsl:value-of select="@name"/>
		</I>
	</xsl:template>
	
	<xsl:template match="permission">
		<TR VALIGN="top">
			<TD WIDTH="50%">
				<A>
					<xsl:attribute name="href">
						urn:member:<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:attribute name="title">
						<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:value-of select="@cref"/>
				</A>
			</TD>
			<TD WIDTH="50%">
				<xsl:apply-templates/>
			</TD>
		</TR>
	</xsl:template>
	
	<xsl:template match="see">
		<xsl:choose>
			<xsl:when test="@langword">
				<B><xsl:value-of select="@langword"/></B>
			</xsl:when>
			<xsl:when test="@cref">
				<A>
					<xsl:attribute name="href">
						urn:member:<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:attribute name="title">
						<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:value-of select="@cref"/>
				</A>
			</xsl:when>
			<xsl:when test="@internal">
				<U>
					<xsl:value-of select="@internal"/>
				</U>
			</xsl:when>
			<xsl:when test="@topic">
				<U>
					<xsl:value-of select="@topic"/>
				</U>
			</xsl:when>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="seealso">
		<xsl:choose>
			<xsl:when test="@cref">
				<A>
					<xsl:attribute name="href">
						urn:member:<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:attribute name="title">
						<xsl:value-of select="@cref"/>
					</xsl:attribute>
					<xsl:value-of select="@cref"/>
				</A>
			</xsl:when>
			<xsl:when test="@topic">
				<U><xsl:value-of select="@topic"/></U>
			</xsl:when>
		</xsl:choose>
		<xsl:if test="position()!=last()"> | </xsl:if>
	</xsl:template>
	
	<xsl:template match="para">
		<P>
			<xsl:apply-templates/>
		</P>
	</xsl:template>
	
	<xsl:template match="code">
		<pre class="code">
			<xsl:if test="@lang">
				<SPAN CLASS="lang">[<xsl:value-of select="@lang"/>]</SPAN>
				<BR/>
			</xsl:if>
			<xsl:value-of select="."/>
		</pre>
	</xsl:template>
	
</xsl:stylesheet>
