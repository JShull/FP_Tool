# FuzzPhyte Unity Tools

## Tool

FP_Tool is designed and built to be a simple set of base classes designed around interactive 'tool' use for 2D and 3D experiences. In a lot of Unity projects you find that you need some sort of 'weapon' and/or 'tool' that has the same sort of set of states: you have the tool (in hand), you are using the tool, you have stopped using the tool, and/or you have dropped the tool.

## Setup & Design

This package requires a few things on the sample side to get it up and running - specifically the 'MeasureExample'

### Sample Software Details

MeasureExample will require additional package imports those are listed below, before you bring in FP_Tools and after you've installed the additional packages, make sure you then bring in the FP_Game samples before the FP_Tools sample.

Additional Packages Needed for MeasureExample:

* [FP_Utility_Analytics](https://github.com/jshull/FP_Utility_Analytics.git)
* [FP_Game_Samples](https://github.com/jshull/FP_Game.git)

The Measure Example combines the FP_SystemBase functionality with the creation of an abstract class called 'FPGenericGameUtility'. The game utility is setup to force a few things but also easily expandable for whatever the needs are for a simple 'game'.

Before you can create your own GameManager from FPGenericGameUtility, you're going to want to first generate your FP_Data class and your FP_Event class.

If you use the MeasureExample as a guide these are the most important pieces of this setup:

* FPGameManagerExampleData --> extension of FP_Data and is our 'data' for how we want to setup/load the game
* FPGame_BootStrap_ToolExample.cs --> the extension of FPBootStrapper<FPGameManagerExampleData> and uses the data class
* FPGameManager_ToolExample.cs --> extension of the FPGenericGameUtility class and uses the FPGameManagerExampleData as well as works with the Bootloader to get setup. Interior to this you have your own ability to generate a customized event external of Unity by overriding the ProcessEvent function... take a look at the FPGenericGameUtility to see what is going on behind the scenes.
* FPUI_Clock.cs --> this can be extended/derived from and the important reference here is the parameter FP_Stat_GameClock
* FP_Stat_GameClock.cs --> a derived class of FP_Stat_event which is setup to get you a clock up and running fast.
* FP_Stat_Type.cs --> a scriptable object data block that represents a sort of 'stat' that we want to process that just happens to be a clock.
* FP_StatReporter_Float.cs--> the reporter that keeps tabs on our clock stat and is connected to our Stat Manager
* FP_StatManager.cs--> keeps tabs on all/any reporters in a scene

#### FP_Game

Is a separate package that just extends and utilizes the FP_SystemBase as well as offers some base level UI functionality and the concept of a generic game manager that manages states. This is to keep tabs on any/all FP_Systems in our experiences as it then later on allows us to isolate/order of operations/etc. them as needed. This also operates by using an interface that is directly connected to the entire overall general process of a game/experience.

**IFPGame<T,A> Interface**: has a set of required functions that we can hit from any sort of other system as needed.

* Setup
* Start
* Pause
* Resume
* Reset
* Stop
* Process Special Event

**FPGenericGameUtility**: When you derive from the FPGenericGameUtility class you are not only inheriting this interface, but a series of functions, internal parameters, delegates, audio, and UI related needs for just about any simple game. As this class extends the FPSystemBase, it can get a little confusing on who does what but once you understand that FPSystemBase is only responsible for setting us up as an instance, making sure we have the right data (initialized), as well as managing Awake/Start/OnDestroy and a faked after end of frame function -- AfterLateUpdate() (which isn't really needed); whereas FPGenericGameUtility is then stepping in and managing the entire state of the 'game' and also offers a way to override Start/Update/FixedUpdate/LateUpdate but doesn't touch Awake as well as manages a Clock if you have one. When you extend/derive from here, what you are saying is that you really are only focusing in on using this as an instance and then harnessing the event states tied to the delegates as you need them.

## Hot / Cold Sink

`FP_EquipmentSink` extends `FP_EquipmentTickableBase`. Bind the two knobs to
`SetHotWaterOn(bool)` / `SetColdWaterOn(bool)`, or their `ToggleHotWater()` /
`ToggleColdWater()` methods. Either open knob means `EquipmentPowerState.On`;
both closed means `Off`. Both knobs start closed.

Use `OnStarted` / `OnStopped` for shared running-water audio and particles, and
`OnHotWaterOn`, `OnHotWaterOff`, `OnColdWaterOn`, `OnColdWaterOff` for individual
knob feedback. These Inspector events pass the sink and fire only on transitions.
`OnStateChanged` also reports initialization and contents changes. The inherited
`OnEvent` and contained-item state/tick callbacks remain available.

Existing `SetPowerOn` commands open cold water; `SetPowerOff` closes both knobs;
`TogglePower` switches between those behaviors. `Break` sets `Broken` and closes
both knobs. `Repair` restores `OK` without reopening them. Broken sinks reject
opening requests. Disabling the component closes both knobs and stops effects.

Use inherited `AddItemToEquipment` / `RemoveItemFromEquipment` for items and
`SetContainsLiquid(bool)` for basin liquid. Fill reports `Empty`, `ContainsItem`,
`ContainsLiquid`, or `MixedContents`. Flow does not automatically fill the basin.
This initial sink does not simulate temperature, drainage, or energy transfer
(`EmitPowerUnit` is zero). No new equipment enums or runtime dependencies are added.
The inherited tick setup requires `FP_TickSystem` when automatic registration is
enabled; ticking is only needed here for contained-item callbacks.

## Scale

`FP_EquipmentScale` extends the same `FP_EquipmentBase` used by the microwave and
sink. It totals distinct `FP_WeightedItem` components in kilograms. Each item has
an authored weight independent of `Rigidbody.mass`; call `SetWeightKg(float)` to
change it while contained. Negative and non-finite inputs become zero. Totals use
double accumulation of the authored float values, with rounding only for display.
This is an interactive mass readout, not a simulation of force or load distribution.

### Setup

Use **GameObject > FuzzPhyte > Equipment > Scale** to create a platform, weighing
volume, and wired world-space TextMeshPro panel. The created hierarchy is editable
and supports Undo. Alternatively, assemble an existing scale model as follows.

1. Add `FP_EquipmentScale` to the scale root. It starts powered on and needs no
   `FP_TickSystem` or timer.
2. Add a child with one 3D trigger collider and `FP_ScaleContainmentVolume`. Assign
   its Scale reference and size the trigger above the platform. Use this volume
   instead of `FP_EquipmentContainmentVolume` for weighing. Multiple child volumes
   can reference the same scale; each volume must have exactly one trigger collider.
3. Add `FP_WeightedItem` to each item root and set Weight Kg and optional Item Id.
   Colliders may be on that object or children; the nearest weighted ancestor owns
   each collider. Multiple colliders and overlapping volumes count the item once.
   Give movable items a Rigidbody; Unity's physics layer collision matrix must
   allow the trigger pair. Trigger callbacks require a Rigidbody on at least one
   participant. The item can also carry `FP_EnergyReactiveItem` for microwave use;
   the scale-specific volume looks explicitly for the weight component.
4. Add `FP_ScaleDisplay` to a UI panel and assign the Scale and a TextMeshPro label.
   A world-space Canvas works for a display mounted on the scale. The binder
   initializes on enable and updates through `OnStateChanged`; no UI polling is
   required. Decimal Places controls formatting (default `0.000 kg`).

`TotalWeightKg`, `ItemCount`, `OnWeightChanged`, and `OnStateChanged` are available
for other UI or interactions. Inspector events pass the scale. `OnWeightChanged`
fires only when the numeric total changes; `OnStateChanged` also covers zero-mass
items and equipment state. Standard power, toggle, break, and repair commands work.
Power off or broken shows `-- kg`; contents continue to be tracked. Repair leaves
power off until explicitly switched on. No heat or energy is emitted.

Disabled/destroyed items are removed; disabled/destroyed colliders are pruned by
the volume. Disabling a volume releases its contacts, and disabling the scale
clears its contents and subscriptions. Known collider contacts recover after
re-enabling even with sleeping rigidbodies; `OnTriggerStay` recovers untracked
overlaps on the next physics callback. Contact entry/exit follows Unity
physics timing. Keep the assigned Scale reference stable while contacts are active.
Direct `AddItemToEquipment` / `RemoveItemFromEquipment` calls also support weighted
items for non-physics integrations; use either manual ownership or trigger ownership
for a given item, since a last-volume exit removes it.

The existing microwave/sink containment behavior is unchanged. Scale Edit Mode
tests cover aggregation, weight changes, collider ownership, overlapping volumes,
lifecycle cleanup, validation, events, and power/condition behavior. A Play Mode
integration test exercises actual 3D trigger contacts and display updates. UI
placement and device acceptance must also be checked in the consuming project.

## FP_Tool Dependencies

Please see the [package.json](./package.json) file for more information.

## License Notes

See [LICENSE.md](LICENSE.md) for details

## Contact

* [John Shull](mailto:JShull@fuzzphyte.com)
