<?xml version="1.0" encoding="ISO-8859-1"?>

<!-- A simple conversion XSL which converts SharpDevelop 1.0 project files to 1.1 project files.
     2002 by Mike Krueger -->
     
<xsl:transform version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="Project">
		<Project version="1.1" 
		         name ="{@name}" 
		         projecttype="{@projecttype}" 
		         description="{@description}" 
		         enableviewstate="{@enableviewstate}">
		         
		        <!-- Convert Contents -->
			<Contents>
				<!-- copy the file nodes, they're equal -->
				<xsl:apply-templates select="Contents/File"/>
				
				<!-- Directory nodes must be converted to file nodes -->
				<xsl:for-each select="Contents/Directory">
					<File name        = "{@relativepath}"
					      subtype     = "Directory"
					      buildaction = "Nothing" />
				</xsl:for-each>
			</Contents>
			
			<!-- Convert References -->
			<References>
				<xsl:for-each select="References/Reference[@assembly]">
					<Reference type   = "Assembly"
					           refto  = "{@assembly}"/>
				</xsl:for-each>
				
				<xsl:for-each select="References/Reference[@project]">
					<Reference type  = "Project"
					           refto = "{@project}"/>
				</xsl:for-each>
				
				<xsl:for-each select="References/Reference[@gac]">
					<Reference type  = "Gac"
					           refto = "{@gac}"/>
				</xsl:for-each>
			</References>
			
			<!-- Convert Configurations -->
			<xsl:apply-templates select="Configurations"/>
			
			<!-- Convert Deployment information by just copying them (nothing changed)-->
			<xsl:apply-templates select="DeploymentInformation"/>
		</Project>
	</xsl:template>
	
	<!-- A recursive copy template -->
	<xsl:template match="*|@*">
		<xsl:copy>
			<xsl:apply-templates select="*|@*"/>
		</xsl:copy>
	</xsl:template>
</xsl:transform>
