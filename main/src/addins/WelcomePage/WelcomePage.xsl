<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:gtc="urn:MonoDevelop.Core.XslGettextCatalog">
	<xsl:output method="xml" 
		media-type="text/html" 
		doctype-public="-//W3C//DTD XHTML 1.0 Strict//EN"
		doctype-system="DTD/xhtml1-strict.dtd"
		cdata-section-elements="script style"
		indent="yes"
		encoding="ISO-8859-1"/>
	<xsl:template match="/WelcomePage">
		<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en" dir="ltr">
			<head>
				<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
				<link rel="copyright" href="http://www.gnu.org/copyleft/fdl.html" />
				<title><xsl:value-of select="gtc:GetString('Welcome Page')"/></title>
				<!--<link rel="stylesheet" type="text/css" href="{ResourcePath}WelcomePage.css" />-->	
				<style type="text/css">
					@import "<xsl:value-of select="ResourcePath" />WelcomePage.css"; 

					body {
					background: #fff url('<xsl:value-of select="ResourcePath" />mono-bg.png') repeat-x top left;
					}
					
					#p-logo {
					/* Wiki logo (replace with graphic via CSS) */
					position: absolute;
					top: 25px;
					left: 24px;
					background: transparent url('<xsl:value-of select="ResourcePath" />mono-logo.png') no-repeat center;
					}
					
					#p-logo a {
					/* Wiki logo, part 2 */
					display: block;
					width: 266px;
					height: 53px;
					}
					
					#bigWrapper {
					background: transparent url('<xsl:value-of select="ResourcePath" />mono-decoration.png') no-repeat top left;
					}
				</style>
			</head>
			<body         class="ns-0"         id="page-MainPage">
					<div id="bigWrapper">
						<div class="portlet" id="p-logo">
							<a href="http://monodevelop.com/Main_Page"
								title="{gtc:GetString(@_title)}"></a>
						</div>
						<div id="decoration"> </div>
						<div id="column-content">
							<div>
								<div id="bodyContent">
									<h3 id="siteSub"><xsl:value-of select="gtc:GetString(Projects/_siteSub)"/></h3>
									<!-- start content -->
									<table id="welcome_content" >
										<tr>
											<td id="leftTopic">

												<h1><xsl:value-of select="gtc:GetString(Actions/@_title)"/></h1>
												<ul>
													<xsl:for-each select="Actions/Action">
														<li><a href="{@href}"><xsl:value-of select="gtc:GetString(@_title)"/></a></li>
													</xsl:for-each>
												</ul>
												<p />
												<h1><xsl:value-of select="gtc:GetString(Projects/@_title)"/></h1>
												<p />
												<table class="topic1" id="recentprojects">
													<tr class="topicbar">
														<th><xsl:value-of select="gtc:GetString(Projects/@_col1)"/></th>
														<th class="proj_mod_col"><xsl:value-of select="gtc:GetString(Projects/@_col2)"/></th>
													</tr>
													<xsl:variable name="prjLinkTitle" select="Projects/@_linkTitle"/>

													<xsl:for-each select="RecentProjects/Project">
														<tr>
															<xsl:if test="position() mod 2 = 0"><xsl:attribute name="class">evenrow</xsl:attribute></xsl:if>
															<td><a href="project://{Uri}" title="{Uri}" alt="{gtc:GetString($prjLinkTitle)} {Name}"><xsl:value-of select="Name" /></a></td>
															<td class="proj_mod_col"><xsl:value-of select="DateModified" /></td>
														</tr>
													</xsl:for-each>	

												</table>
											
											</td>
											<td id="rightTopic">
												<xsl:for-each select="Links">
													<h1><xsl:value-of select="gtc:GetString(@_title)"/></h1>
													<ul>
														<xsl:for-each select="Link">
															<li>
																<a href="{@href}"><xsl:value-of select="gtc:GetString(@_title)"/></a>
																<xsl:if test="string-length(@_desc) != 0"> - <xsl:value-of select="gtc:GetString(@_desc)"/></xsl:if>
															</li>
														</xsl:for-each>
													<xsl:if test="normalize-space(.)">
														<xsl:if test="gtc:GetString(@_title)='News Links'">
															<li>No news can be found
															</li>
														</xsl:if>
													</xsl:if>
													</ul>
													<p />
												</xsl:for-each>
											</td>
										</tr>
									</table>
									<!-- end content -->
									<div class="visualClear"></div>
								</div>
							</div>
						</div>
						<div id="footer">
						</div>
					</div>
			</body>
		</html>

	</xsl:template>
</xsl:stylesheet>
