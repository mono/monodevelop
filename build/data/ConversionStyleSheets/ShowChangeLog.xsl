<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- A simple conversion XSL which converts SharpDevelop 1.0 project files to 1.1 project files.
     2002 by Mike Krueger -->
     
<xsl:transform version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="ChangeLog">
		<HTML><HEAD></HEAD><BODY>
			<H1><xsl:value-of select="@project"/> ChangeLog</H1>
			<TABLE class="news">
				<TR>
					<TH align="left" class="copy">Author</TH>
					<TH align="left" class="copy">Date</TH>
					<TH align="left" class="copy">Change</TH>
				</TR>
			
				<xsl:for-each select="Change">
					<TR>
						<TD align="left" class="copy"><xsl:value-of select="@author"/></TD>
						<TD align="left" class="copy"><xsl:value-of select="@date"/></TD>
						<TD align="left" class="copy"><xsl:value-of select="."/></TD>
					</TR>
				</xsl:for-each>
			</TABLE>

		</BODY></HTML>
	</xsl:template>
</xsl:transform>
