Intial architecture at the moment

 - create an extension that attaches to all projects, this will maintain state for the connected services node for that project
 - it will gather the service provider instances (IConnectedService) for that project
 - each IConnectedService service instance can then maintain state about whether it has been added to the project etc without having to re-query the
 disk. Is this an issue if the user manually deletes things from the project?
  
 - the CS node can then query the extension on the project to get state about the services that are connected.



 - by default all dependencies are assumed to be added to the project when the service is added to the project. Nuget dependencies have an additional property so that 
 we can tell if the package was removed and offer a fix it button
