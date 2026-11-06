# Water Refill Prototype

`FP_WaterRefillPrototype.prefab` is a stationary particle stream wired to refill a
coffee maker. There is no tipping component or angle requirement. Import the
**Coffee Water Refill** sample through Package Manager, drag the prefab into a
scene, and enter Play Mode. Select **Coffee Maker Root** to watch Water Ml rise.
The reservoir starts empty, beans start at 60 g, and Require Cup is off for this
refill demonstration. The sample includes a tick system; remove that child if
your scene already has one.

You can also create the hierarchy from
**GameObject > FuzzPhyte > Equipment > Water Refill Prototype**.

## Wire an existing water source

1. Add `FP_WaterFillRegion` and one BoxCollider around the coffee maker's water
   inlet. Assign Coffee Maker. The collider must be enabled and marked Is Trigger;
   initialization sets it to a trigger.
2. Add `FP_WaterParticleTrigger` to the **same GameObject as your ParticleSystem**.
   Add the water inlet region to Fill Regions. Milliliters Per Particle defaults
   to 5 ml. Enabling the component configures Trigger Enter=Callback,
   Inside/Exit/Outside=Ignore, and collider query mode=All. Use its Refresh Trigger
   Regions context action if you change the array during Play Mode.
3. Aim the emitter's local Z direction into the inlet. Use World simulation space
   so existing droplets stay in the world when the source moves. Control the stream
   with the source's existing particle Play/Stop or emission controls. No Rigidbody
   or OnTriggerEnter refill script is needed for this particle-trigger path.

A captured droplet adds its configured milliliters once, then is killed. Only one
receiver accepts it even if regions overlap. A full tank consumes the droplet and
ignores excess volume. Disabled/unassigned receivers do not add water. The coffee
maker clamps water to Water Capacity Ml and fires its existing OnSuppliesChanged.
The sample emits 30 particles/second at 5 ml each: about 150 ml/second if all are
captured. Missed droplets do not refill the reservoir.

If a source needs tipping later, use the existing `FP_TipParticlePour` and assign
this particle system. There is no separate water tipping implementation.

## Water events

- `OnEnoughWater`: once when Water Ml reaches/exceeds Water Per Brew Ml.
- `OnWaterFull`: once when Water Ml reaches the positive Water Capacity Ml.

Each threshold re-arms independently after consumption or an explicit level
reduction. An initially prefilled machine reports satisfied thresholds once at
initialization. Repeated droplets while already ready/full do not repeat events.
When a refill crosses both thresholds, Enough fires before Full. `HasWater` and
`IsWaterTankFull` expose the current states; readiness here concerns water only.

Direct integrations can still call `AddWaterMl(float)` or `SetWaterMl(float)`.
Use `StartBrewing()` for brewing and the existing cup-region setup for cup gating.

This prototype is a volume-counting interaction with an unlimited source, not a
fluid solver. Keep the inlet thick enough for the particle speed and frame rate;
fast particles can cross a thin trigger between simulation steps. Verify actual
source placement, capture rate, and device behavior in the consuming scene.
