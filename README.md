# innoculus
A simple application that shuts down Oculus services when Home is not in use.  
My suggestion is to disable the default Windows service.

Feel free to improve upon it. I didn't plug in a real logger, commenting is at a minimum, error handling is mostly greedy and silent, and the code structure is heavily static.  
Also, no thread-safe code.

## Use
No arguments: attempts to hook in to existing Oculus Home instance, or starts a new one; starts the OVRServer before and stops it once Home is closed.

## Changelog
**v1.3**
* Support any installation path

**v1.2**
* Renamed to "innoculus" - suggested by /u/50bmg

**v1.1**  
* Remove "/start" option - all rolled in to one
* Will try to stop the Windows service before starting Home
* Now handles OVRService directly; no more need for the Windows service nor escalated privileges

## Build
Built in VS 2013 with .NET 4.5. Nothing special.

## Lifecycle
If it hooks to an existing (or started) Oculus Home instance, it will wait in idle until that instance closes. Then it will attempt to shut down the Oculus services before quitting.

## Design decisions
I could have had it always running and watching to manage the services, but what would that solve? You'd still have a service running that could be suspect.  
Also, I wouldn't be able to natively interrupt the start of Home in order to start the services, so there's no real gain.

## Permissions
**v1.0 only**  
The Oculus services must be stopped/started with Admin privileges, so you'll get a permission escalation request when it needs to do that.  
Since Oculus Home must be run on your profile, you can't use "noculus /start" with Admin privileges to reduce the requests.
