<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- A simple conversion XSL which converts SharpDevelop 1.0 project files to 1.1 project files.
     2002 by Mike Krueger -->
     
<xsl:transform version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="HelpWanted">
		<HTML><HEAD></HEAD><BODY>
			<TABLE>
				<TR>
					<TH align="left" class="copy">Topic</TH>
					<TH align="left" class="copy">Issue</TH>
				</TR>
			
				<xsl:for-each select="Topic">
					<TR>
						<TD align="left" class="copy"><xsl:value-of select="@name"/></TD>
						<TD align="left" class="copy"><xsl:value-of select="."/></TD>
					</TR>
				</xsl:for-each>
			</TABLE>

		</BODY></HTML>
	</xsl:template>
</xsl:transform>
