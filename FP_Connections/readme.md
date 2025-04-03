# FP Connections ReadMe

This package is designed to help align ConnectableItem.cs that contain a list of possible ConnectionPointUnity.cs. The limits on the connections are associated with a scriptable object called 'ConnectionPointData.cs' which establishes the directional vectors relative our connectionpoint. We use the connection point forward and then the directional relative vectors to determine 3D alignment with other possible ConnectableItems.

## Current State

Connections are evaluated by a current minimal angle offset from forward vectors of each ConnectableItem via the Tolerance float on ConnectionPointUnity.cs. Assuming that we are close enough to alignment here, we then exhaustively search through each others ConnectionPointData.localRotationAngles and their cross product of this and their local forward, this gives us a 90 angle from each side facing in a 'similar' direction so when we do an angle check we are actually checking the angle of difference that matters between the two possible rotational local vectors. We are then looking for the smallest change here to **align our item to the target item** - big note there whoever is running the code as my and against 'other' is then trying to see if we can align to other based on the known rotational positions. At this point if everything holds true we adjust our pivot and do all of the rotation/alignment we need to do. We then attach a joint from this/my item to the target at which point we are basically making our target our 'parent' but via the physics system.

## Whats Left

The initial core algorithm works as intended. What needs work and is much more complicated is managing the relationships of multiple connections and their physics locked joints by VR interactions. Physics joints operate in a parent/child relationship and we have to manage how we swap between a sort of locked item by the joint vs a set of connected items that aren't locked but are using joints as well.

### Connection Condition

When we make a joint connection we sort of need to know if we are locking in to an existing static/fully locked series. This is the under pinning logic to the rest of these conditions.

### Condition One

Static mounted pipe coming out of a wall is connected to a grabbable pipe that we've joint locked. That joint locked pipe isn't moveable but might have a grabbable on it. We would need to 'detach' or 'dejoint' it first. Then it should be grabbable

### Condition Two

There are 2 or more pipes jointed together - we then connect this set to a static pipe coming out of a wall. At that moment all of the other pipes who had grabbable conditions would need to be deactivated. If we dejointed any single item we would then need to update/check via the joints if we are connected to a static item or not to then determine what items to turn on/off.

### Considerations

At the moment - by relying on the physics system - if we have multiple joined ConnectableItems, based on how the interaction system works with OVR and what we are going to be 'checking' we are only going to be checking the endpoints (ConnectionPointUnity) by the associated ConnectableItem. E.g. if we had a small section of two pipes that were connected, we grabbed the right item, and we tried to connect the left item of this two piece part to another floating pipe it would fail because we are only going to flag the items in our associated ConnectableItem... one way to possibly fix this would be on release/TryConnectOnRelease - when we normally make this call for just the item we have interacted with, we might suggest call all ConnectableItems that are part of our system. In order to do this we need a way to traverse the joints/rigidbodies

#### Joint Traversal

We are going to need this or some lookup tables that sync with it to get information back up and out...

// currently recursively getting all open connections across anyone that I'm already connected with
// I then need to package this list up - remove duplicates - and use this list to start my check against all of those items to see if any of them have 'hit/trigger' if they have 'hit' then run that compatibility checker on those items even if they aren't me specifically.
On release confirm/check what's up with possible target = the pipe wants to connect to itself if it has a connection already, goes from one end to another for example
