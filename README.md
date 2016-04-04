# noculus
A simple application that shuts down Oculus services when Home is not in use

## Use
No arguments: attempts to hook in to existing Oculus Home instance, or exits  
With "/start": starts Oculus Home if not already running, starting service first

## Build
Built in VS 2013 with .NET 4.5. Nothing special.

## Lifecycle
If it hooks to an existing (or started) Oculus Home instance, it will wait in idle until that instance closes. Then it will attempt to shut down the Oculus services before quitting.

## Design decisions
I could have had it always running and watching to manage the services, but what would that solve? You'd still have a service running that could be suspect.  
Also, I wouldn't be able to natively interrupt the start of Home in order to start the services, so there's no real gain.

## Permissions
The Oculus services must be stopped/started with Admin privileges, so you'll get a permission escalation request when it needs to do that.  
Since Oculus Home must be run on your profile, you can't use "noculus /start" with Admin privileges to reduce the requests.
