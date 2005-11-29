<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
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
				<title>Welcome Page</title>
				<!--<link rel="stylesheet" type="text/css" href="{ResourcePath}WelcomePage.css" />-->	
				<style type="text/css">
					@import "<xsl:value-of select="ResourcePath" />WelcomePage.css"; 

					body {
					background: #fff url('<xsl:value-of select="ResourcePath" />mono-bg.png') repeat-x top;
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
					height: 50px;
					}
				</style>
			</head>
			<body         class="ns-0"         id="page-MainPage">
				<div id="globalWrapper">
					<div id="bigWrapper">
						<div class="portlet" id="p-logo">
							<a href="http://monodevelop.com/Main_Page"
								title="MonoDevelop Home Page"></a>
						</div>
						<div id="caption">
							Free .Net Development Environment
						</div>
						<div id="column-content">
							<div>
								<div id="bodyContent">
									<h3 id="siteSub">From MonoDevelop</h3>
									<div id="contentSub"></div>
									<!-- start content -->
									<table id="welcome_content" >
										<tr>
											<td id="leftTopic">

												<h1>Common Actions</h1>
												<ul>
													<li><a href="monodevelop://NewProject">Start a New Project</a></li>
													<li><a href="monodevelop://OpenFile">Open a Project / File</a></li>
												</ul>
												<p />
												<h1>Recent Projects</h1>
												<p />
												<table class="topic1" id="recentprojects">
													<tr class="topicbar">
														<th>Project</th>
														<th class="proj_mod_col">Last Modified</th>
													</tr>

													<xsl:for-each select="RecentProjects/Project">
														<xsl:choose>
															<xsl:when test="position() mod 2 = 0">
																<tr class="evenrow">
																	<td><a href="project://{Uri}" title="{Uri}" alt="Open Project {Name}"><xsl:value-of select="Name" /></a></td>
																	<td class="proj_mod_col"><xsl:value-of select="DateModified" /></td>
																</tr>
															</xsl:when>
															<xsl:otherwise>
																<tr >
																	<td><a href="project://{Uri}" title="{Uri}" alt="Open Project {Name}"><xsl:value-of select="Name" /></a></td>
																	<td class="proj_mod_col"><xsl:value-of select="DateModified" /></td>
																</tr>
															</xsl:otherwise>
														</xsl:choose>
													</xsl:for-each>	

												</table>
											
											</td>
											<td id="rightTopic">
												<h1>Support Links</h1>
												<ul>
													<li><a href="http://www.monodevelop.com">MonoDevelop Home Page</a></li>
													<li><a href="http://www.mono-project.com/Main_Page">Mono Project Home Page</a></li>
												</ul>
												<p />
												<h1>Development Links</h1>
												<ul>
													<li><a href="http://www.cshrp.net/">CShrp.Net</a> - A C# Development community with news, tutorials, and references.</li>
													<li><a href="http://www.csharphelp.com/">C# Help</a> - A site with articles on common C# problems.</li>
													<li><a href="http://www.c-sharpcorner.com/">C# Corner</a> - Another C# community with articles and forums.</li>
													<li><a href="http://www.gotdotnet.com/">Got Dot Net</a> - A .NET framework community featuring user blogs, message boards, and code samples.</li>
												</ul>
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
				</div>
			</body>
		</html>

	</xsl:template>
</xsl:stylesheet>
