# Bean Pour Prototype

`FP_BeanPourPrototype.prefab` demonstrates tipping a bean container to emit
particles into a coffee maker's hopper. All component references and the particle
Trigger module are wired. The sample uses the package's existing URP dependency.

## Try it

1. Drag the prefab into a scene and enter Play Mode.
2. Select its **Bean Container - rotate X to 125 degrees** child. Change its local
   X rotation from `0` to `125` degrees. Leave the prototype root upright.
3. Brown bean particles fall from the container mouth into **Bean Fill Region**.
   Select **Coffee Maker Root** and watch **Beans Grams** increase on
   `FP_EquipmentCoffeeMaker`. Water starts at 1,000 ml and beans start empty.
4. Rotate the container back below `80` degrees. New particles stop; beans already
   falling remain alive and can still refill the hopper.
5. For a brew button, bind `FP_EquipmentCoffeeMaker.StartBrewing()` to your normal
   interaction. The default recipe needs 15 g and 250 ml. Require Cup is disabled
   in this refill-focused prototype; enable it and assign a cup region for cup gating.

The prototype includes a **Sample Tick System** for brewing. Remove that child
when using an existing scene tick system. No grabbing/input framework is bundled;
use Inspector rotation to try the sample, or attach your project's grab behavior
to the container. You can also create the hierarchy from
**GameObject > FuzzPhyte > Equipment > Bean Pour Prototype**.

## Components and configuration

- **FP_TipParticlePour** lives on the container. **Origin** is the transform whose
  rotation is measured. **Local Up Axis** describes the container's upright axis
  (default local Y). The angle is measured against world up, not its initial pose.
  Pouring begins at **Start Angle** (`100` degrees) and stops at **Stop Angle**
  (`80` degrees), providing hysteresis. Use OnPourStarted/OnPourStopped for audio.
- **FP_BeanParticleTrigger** must be on the **same GameObject as the ParticleSystem**.
  Its **Fill Regions** array supplies the trigger colliders. **Grams Per Particle**
  defaults to `0.2` g. Use **Refresh Trigger Regions** after changing that array in
  Play Mode. Enabling the component also refreshes the configuration.
- **FP_BeanFillRegion** lives on the hopper's BoxCollider GameObject and references
  your coffee maker. Its box is a trigger. The particle system detects entry,
  identifies the region, and calls `AddBeansGrams`. No Rigidbody is required for
  this particle-trigger path, and there are no `OnTriggerEnter` bean callbacks.

The prototype emits 30 particles/second, so a fully caught stream adds about
6 g/second. Only particles actually entering a receiver count. Trigger Enter is
Callback, collider query mode is All, and Inside/Exit/Outside are Ignore. A caught
particle is killed after one receiver handles it, including when the hopper is
full. It cannot add beans repeatedly while inside or fill two overlapping regions.
Disabled/unassigned receivers do not accept beans. The coffee maker clamps supply
to its configured capacity and broadcasts its existing OnSuppliesChanged event.

## Use with your own model

1. Put FP_BeanFillRegion and one BoxCollider around the real hopper opening;
   assign the existing coffee maker.
2. Move/copy **Bean Particles** to the real container mouth. Its local Z emission
   direction should point outward. Keep particle **Simulation Space = World**,
   **Play On Awake = off**, **Looping = on**, and a positive lifetime/gravity.
3. Add FP_TipParticlePour to the real container, assign its origin and particle
   system, and tune its local upright axis and angle thresholds.
4. Assign the real hopper region in FP_BeanParticleTrigger's Fill Regions array.
5. Tune particle size, rate, speed, lifetime, and grams per particle together.

This is a visual refill interaction with an unlimited source, not individual
rigidbody beans or conservation of a finite bag supply. Amounts depend on captured
particles. Fast particles can cross a thin region between simulation steps; keep
the receiver sufficiently thick and verify the actual scene's scale and frame rate.
The sample's supplied settings are tested in Play Mode. Scene-specific placement,
grabbing, sound, and device acceptance still need checking in the consuming project.

Unity's particle callback and collider-query behavior are documented in
[TriggerModule.colliderQueryMode](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/particlesystem/triggermodule/colliderquerymode)
and [ColliderData.GetCollider](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/particlesystem/colliderdata/getcollider).
