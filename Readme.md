### EventStore.Rebuilder

Re-publishes events for a given type to a specified endpoint. Currently only works with ravendb and masstransit, but this is easily fixed - just change the Wireup.Init-statement and dispatch through another message bus.

At the moment you need to have the message assembly available to load - this is because MassTransit works on actual types when choosing how to dispatch messages (polymorphic routing etc), so just place your message assembly (the one with the event types) next to the binary `EventStore.Rebuilder.exe`.

E-mail comments: henrik@haf.se