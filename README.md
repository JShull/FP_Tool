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

## Coffee Maker

Add `FP_EquipmentCoffeeMaker` to your coffee maker model. It follows the microwave
and sink's `FP_EquipmentTickableBase` pattern and uses `FP_TickSystem` to advance
brewing. Initialize the tick system before the maker's `Start`, or disable automatic
registration and call `Tick(dt)` from your own driver. No separate `FP_Timer` is
required. Disabling cancels brewing, powers off, and unregisters; re-enabling
registers again without restarting the cancelled drink.

Configure Brew Duration Seconds, Beans Per Brew Grams, and Water Per Brew Ml
(defaults: 30 seconds, 15 g, 250 ml). Set capacities and starting supplies in the
Inspector. Supplies start empty. Use `AddBeansGrams(float)` / `AddWaterMl(float)`
for refilling, or `SetBeansGrams(float)` / `SetWaterMl(float)` for absolute amounts.
Amounts clamp to capacity; invalid/non-finite absolute amounts become zero and
invalid/negative additions are ignored. `HasBeans` and `HasWater` mean enough
for the configured recipe, not merely nonempty.

Wire the model's brew button to `StartBrewing()` and its stop button to
`CancelBrewing()`. `StartBrewingForSeconds(float)` overrides only cycle duration.
Starting automatically powers on; powering on alone leaves the maker idle.
Repeated starts while brewing do nothing. The entire recipe is deducted once at
acceptance; cancellation does not refund it. Refilling during brewing changes the
reservoir for the next drink. This is a timed interaction, not fluid/temperature
simulation or automatic transfer into a cup.

All events below are Inspector UnityEvents passing the coffee maker:

| Event | Suggested model/audio binding |
| --- | --- |
| `OnStarted` | Accepted brew: button feedback or initial mechanism animation. |
| `OnBrewing` | Once per cycle after Started: start pump audio, coffee stream, steam, brewing light. |
| `OnBrewProgress` | Each advancing tick: read `BrewProgress01` or `Status.TimerRemainingSec` for a display/animation. |
| `OnFinished` | Successful end: stop effects and play ready feedback; `LastBrewVolumeMl` is available. |
| `OnCancelled` | Interrupted end: stop brewing effects. Also fires on power-off, break, disable, or required-cup removal during brewing. |
| `OnNoBeans` / `OnNoWater` | Start rejected because the recipe needs more supplies; also once when an available supply falls below one recipe. During brewing, depletion notifications wait until finish/cancel and are cleared by refilling. |
| `OnNoCup` | Required cup missing on start, or removed during brewing. |
| `OnCupPresent` | Once when cup presence changes from absent to present; re-arms on removal. A cup already in the assigned region is reported on the first required-cup check. |
| `OnCupRemoved` | Once when a previously present cup becomes absent, whether idle or brewing; use to update player visuals. Starting empty does not count as a departure. |
| `OnSuppliesChanged` | Refill or recipe consumption: update reservoir/hopper indicators. |
| `OnEnoughBeans` | Once when Beans Grams reaches/exceeds Beans Per Brew Grams; re-arms after dropping below the recipe amount. |
| `OnBeansFull` | Once when Beans Grams reaches the positive Bean Capacity Grams; re-arms after dropping below capacity. |
| `OnEnoughWater` | Once when Water Ml reaches/exceeds Water Per Brew Ml; re-arms after dropping below the recipe volume. |
| `OnWaterFull` | Once when Water Ml reaches the positive Water Capacity Ml; re-arms after dropping below capacity. |
| `OnBroken` / `OnRepaired` | Condition feedback; repair leaves the machine off. |
| `OnStateChanged` | General equipment display refresh and inherited state updates. |

Bean threshold events include initial supplies: a maker starting with enough beans
or a full hopper reports the corresponding event once during initialization.
Further refills, water changes, state updates, and disable/re-enable do not repeat
events while the threshold remains satisfied. Brew consumption and manual changes
re-arm each threshold independently. Particle refills use the same path. If one
refill crosses both thresholds, OnEnoughBeans fires before OnBeansFull. These
events describe bean supply only; water and cup checks still gate brewing.
`HasBeans` and `IsBeanHopperFull` expose current states. Inspector changes to bean
amount, recipe size, or capacity are also checked in LateUpdate while enabled.
Zero-capacity hoppers are not reported full.

When brewing consumes the last recipe's beans or water, the existing OnNoBeans /
OnNoWater events now report the resulting shortage at cycle completion (before
OnFinished), without requiring another start attempt. Cancellation also publishes
any remaining shortage. This does not interrupt an accepted drink: ingredients
were already allocated when it started. Refilling enough during brewing clears
the corresponding pending warning. Outside brewing, a downward crossing reports
immediately (Inspector edits are sampled in LateUpdate). State refreshes do not
repeat shortage notifications; refilling above the recipe amount re-arms them.
An explicit rejected start still reports its missing supplies on every attempt.

Water uses the same independent threshold behavior, including initial supplies,
brew consumption, Inspector monitoring, and non-repeating readiness/full events.
`HasWater` and `IsWaterTankFull` expose the current states. See
[Water Refill sample](Samples~/WaterRefill/README.md) for a stationary particle
stream, `FP_WaterFillRegion`, and `FP_WaterParticleTrigger`. Each captured particle
adds configurable milliliters through `AddWaterMl`; no tipping component is needed.

### Cup detection

1. Add `FP_Cup` to the cup root. Put its 3D colliders on that object or children;
   the nearest cup marker owns each collider. Multiple colliders count as one
   presence, and any remaining cup keeps the region occupied.
2. Create a child beneath the spout with `FP_CupRegion` and one `BoxCollider`.
   The component sets the box to a trigger when initialized. Adjust its Center
   and Size to cover the valid cup position; a cyan wire box appears when selected.
   Use a transform hierarchy without shear (nonuniformly scaled rotated parents).
3. Enable **Require Cup** on `FP_EquipmentCoffeeMaker` and assign **Cup Region**.
   Optionally restrict the region's **Cup Layers** to your cup collider layers.
   Layer filtering applies to the collider GameObjects, not only the marked root.

The coffee maker queries the region only when Require Cup is enabled, before
starting, before advancing a brew, and in LateUpdate. Unchecking Require Cup
ignores the region completely. This uses box-overlap queries in the region's
physics scene, so a stationary cup already inside is detected and no trigger-event
wiring or Rigidbody is required for detection. Position changes follow Unity's
physics transform synchronization timing. Ordinary unmarked objects are ignored.
Disabled/inactive/destroyed cups, disabled colliders, and disabled/destroyed regions
cannot retain a stale positive reading. Removing the last valid cup cancels brewing
and fires OnNoCup; disabling/re-enabling a region redetects stationary cups.
This checks overlap, not whether a cup is upright or correctly aligned with the spout.

Existing manual `SetCupPresent(true/false)` integrations remain supported when no
region has been assigned/used. Once a region owns detection, it is authoritative;
manual calls cannot override an empty or lost region. `IsCupPresent` exposes the
last sampled state. Without Require Cup, the maker can brew freely.

Existing commands map as follows: `InterfaceActions` starts the default recipe;
`StartTimerSeconds` uses `FValue` as the duration; `CancelTimer` cancels;
power/toggle/break/repair commands manage the normal equipment states. A broken or
disabled maker cannot start. No new command enums or package dependencies are added.
`EmitPowerUnit` stays zero, so contained energy-reactive items are not heated.
The existing contained-item callbacks remain available. Edit Mode tests cover
recipes, shortages, events, commands, cancellation and callback re-entry; a Play
Mode tests cover automatic ticking, disable/re-enable registration, and cup-region
detection in an isolated physics scene. Check the actual scene's region placement,
animations, particles, and audio separately.

## Toaster

Add `FP_EquipmentToaster` to the toaster root. Create a child for the slot region
with one 3D trigger collider and `FP_ToasterContainmentVolume`, then assign its
Toaster reference. One region may cover both slots, or multiple regions may
reference the same toaster. They share one cycle. Add `FP_EnergyReactiveItem` to
each bread/item root; colliders may be on that object or children. The nearest
energy-reactive ancestor owns each collider. The region uses OnTriggerEnter/Exit
like the microwave, with OnTriggerStay recovery and cleanup for contacts that
disappear without an exit. Multiple colliders and overlapping regions count each
item once. Unity's trigger setup needs a Rigidbody on at least one participant and
compatible physics layers. Use `Rigidbody.MovePosition` for physics-driven movement;
entry and exit follow Unity's physics callbacks. Disabling a region releases its
items. Re-enabled regions acquire overlaps on subsequent Enter/Stay callbacks.

Bind a 0–1 UI slider to `SetToastLevel(float)`, the lever/start control to
`StartToasting()`, and the stop/eject button to `StopToasting()`. Level 0 uses
Light Toast Seconds (default 5 s); level 1 uses Dark Toast Seconds (default 30 s).
Intermediate levels interpolate duration. Level changes during a cycle apply to
the next cycle; they do not reset the active timer. `ToastLevel` and
`Status.NormalizedLevel` expose the selected setting, `ToastProgress01` exposes
cycle progress, and `Status.TimerRemainingSec` exposes remaining time.

| Event | Use |
| --- | --- |
| `OnToastLevelChanged` | Update the knob/slider display. |
| `OnItemEntered` / `OnItemExited` | Entry/exit feedback; the existing item OnEnteredEquipment/OnExitedEquipment callbacks also run. |
| `OnContentsChanged` | Refresh slot occupancy using ItemCount or Status.ContainedItemIds. |
| `OnStarted` | Accepted start/lever-down feedback. |
| `OnToasting` | Once per cycle: start heating glow and looping audio. |
| `OnToastProgress` | Each advancing tick: update cycle display/animation. |
| `OnFinished` | Successful end: stop effects and animate pop-up. |
| `OnCancelled` | User stop, last item removed, disable, power-off, or break: stop effects/eject feedback. |
| `OnNoItem` | Start rejected because no valid item is inside. |
| `OnBroken` / `OnRepaired` | Condition feedback; repair does not restart. |
| `OnStateChanged` | General equipment status updates. |

Inspector events pass the toaster. Finishing/stopping does not physically move or
remove items; connect your existing pop/eject animation to those events. Removing
one of several items leaves the cycle running; removing the last cancels it.
Newly inserted items receive only the remaining cycle's heat. Disabled/destroyed
energy components are removed; disabled colliders are pruned by the region.

Use an initialized `FP_TickSystem`, like the microwave. Disabling cancels the cycle,
clears containment, and unregisters; re-enabling registers again. With automatic
registration disabled, call `Tick(dt)` from your driver. Heat Per Second (default
2) is passed through the existing `IFPContainedItem.OnEquipmentTick` callback.
The final tick is clamped to the remaining cycle time and applies heat before
completion. State notifications to contained items carry zero EmitPowerUnit to
avoid `FP_EnergyReactiveItem`'s legacy per-state-notification energy addition;
the toaster's own Status/OnEvent still exposes active emitted heat. ThresholdReachedEvent
and MaxThresholdReachedEvent on the energy component can drive bread appearance.
Energy remains accumulated between cycles; this is an interaction model, not a
temperature simulation or a guaranteed mapping from level to visible browning.

Commands: SetPowerOn/InterfaceActions start, SetPowerOff/CancelTimer stop,
TogglePower starts/stops, StartTimerSeconds uses FValue as an explicit duration,
and SetHeatLevel01 uses FValue as the normalized toast setting. Break/Repair use
the standard condition behavior. Suggested descriptive equipment data: Supports
Timer=true, Supports Heat=true, Supports Fill=false, Sub Unit Count=1 for the one
shared control unit. Physical slot count does not create independent timers.
The component's own settings control timing and heat; capability flags remain
descriptive. No new package dependencies are required.

## Jars and tool extraction

`FP_JarContent` represents a jar's fixed content identity and stored volume.
Use Content Id values such as `PeanutButter`, `Mayo`, `Yogurt`, or `TomatoSauce`.
Configure Capacity Ml, Amount Ml, and Take Amount Ml (default 5 ml). Milliliters
here are authored portion volumes, not weights or a viscosity/fluid simulation.
`Fill01` ranges from zero (empty) to one (full). The jar retains its content ID
when empty, so refilling it does not require selecting its food again.

### Reverse trigger: jar to tool

1. Add `FP_JarAccessRegion` to a child with one 3D trigger collider around the
   intended scoop area and assign its Jar reference.
2. Add `FP_JarContentTool` to a spoon/knife root, or implement
   `IFPJarContentReceiver` on your own MonoBehaviour. Its colliders can be on the
   root or children; the nearest receiver ancestor is used. A Rigidbody on at least
   one trigger participant and compatible physics layers are required, as with the
   other equipment trigger regions.
3. By default, one logical tool entry offers up to Take Amount Ml. Only the amount
   the receiver actually accepts is deducted. Compound colliders and overlapping
   regions for the same jar count as one entry, not multiple scoops. Staying inside
   does not repeatedly drain the jar. Exit/re-entry can take another portion.
4. For a button-controlled scoop, turn **Take On Tool Enter** off, and call
   `TakeWithTool(MonoBehaviour)` while that tool is inside. The direct
   `TryTakeContent(IFPJarContentReceiver)` method is also available for scripted
   integrations that manage their own access rules.

`FP_JarContentTool` stores AmountMl, ContentId, and LastSource after it leaves the
jar. Set its capacity for a spoon or knife; a nearly-full tool accepts only its
remaining capacity, and a full tool takes nothing. It holds one food at a time
and rejects a different content ID until emptied. Use its `RemoveContentMl(float)`
or `Empty()` when your spreading/eating/placing interaction consumes the portion.
Its OnContentReceived, OnContentChanged, OnFilled, and OnEmpty events can control
the tool's visible portion; OnEnteredJar / OnExitedJar provide interaction feedback.
Tool-empty clears its stored ID and LastSource. An ordinary unmarked object is
ignored. Disappearing/disabled colliders release their contacts, and disabled or
destroyed tools do not leave permanent jar occupancy.

The receiver interface contract is:

```csharp
void OnJarEntered(FP_JarContent jar);
void OnJarExited(FP_JarContent jar);
float ReceiveJarContent(FP_JarContent source, string contentId, float offeredMl);
void OnJarContentReceived(FP_JarContent source, string contentId, float acceptedMl);
```

ReceiveJarContent stores the accepted portion and returns an amount between zero
and the offer. It must not throw, mutate the source, or publish external events.
The source guards against reentrant quantity changes while acceptance runs. After the jar
debit commits, OnJarContentReceived is called to publish receiver feedback. This
ensures tool and jar quantities are updated before VFX/interaction callbacks run.
Return zero for rejection. Implementing a custom receiver lets you choose your
own capacity, filtering, and downstream item behavior without changing the jar.

### Optional pouring

Pouring is **disabled by default** on FP_JarContent. Peanut butter or mayo can stay
tool-only. For sauce/yogurt, enable **Pouring Enabled**, add `FP_JarPour` alongside
the existing `FP_TipParticlePour`, and assign the jar and spout particle system.
The existing angle thresholds/hysteresis are reused. Flow Ml Per Second drains
stored volume while tipped; empty, disabled, or pour-disabled jars stop emission.
`SetPouringEnabled(bool)` can switch this at runtime without disabling tool access.
For custom pouring updates, disable Auto Advance and call `AdvancePour(dt)` on an
enabled FP_JarPour. Match emission/particle transfer settings to the authored flow
rate if particles are also being used to refill another container. The jar's food
identity is passed to tools; particle outlets need their own explicit target/type
configuration. No automatic solid-food/temperature conversion is provided.

### Content mesh

Add `FP_JarFillVisual`, assign the jar and a separate content-mesh Transform, and
author that mesh at its **full** scale/position before enabling the binder. It
captures that full baseline once and scales only local Y by Fill01. Anchor Bottom
uses the MeshFilter mesh bounds to keep its bottom fixed as it shrinks; for meshes
without a MeshFilter, use a bottom pivot or disable anchoring. Hide When Empty
disables its Renderer at zero, then restores it on refill. Disabling/re-enabling
the binder does not recapture a partially-shrunken scale. Do not use the jar root
or the tool access region as the visual mesh transform.

| Jar event | Use |
| --- | --- |
| `OnToolEntered` / `OnToolExited` | Spoon/knife feedback; passes the jar and actual receiver MonoBehaviour. |
| `OnContentTaken` | Successful extraction after receiver acceptance; passes jar and receiver (null for plain C# receivers). |
| `OnContentChanged` | Initial quantity and actual amount changes; refresh Fill01 or a display. |
| `OnEmpty` / `OnFull` | Initial state and transitions to the corresponding limit. |
| `OnPourStarted` / `OnPourStopped` | Optional pouring effects/audio. |
| `OnStateChanged` | General state changes, including pour enablement. |

Amounts clamp to capacity; rejected or full tools do not deplete the jar. Disabling
the jar releases tracked tools and stops pouring while retaining its amount/ID.
Known contacts can re-enter after re-enabling; new or lost contacts follow Unity
Enter/Stay callbacks. No central tick system is needed. Source geometry and the
actual scoop region must be placed to match the consuming scene; volume extraction
does not simulate scraping, digging, or viscosity. The supplied receiver does not
automatically apply its portion to bread or return it to a jar—bind that interaction
through its events or your own interface implementation.

## Fillable cups: three inputs and Mixed

Add `FP_CupFill` to a cup root and configure Capacity Ml (default 250). It adds the
existing `FP_Cup` marker, so coffee-maker cup-presence regions still recognize it.
Starting Volume Ml and Liquid Type can be authored in the Inspector. Empty cups
always use **None**, including immediately before their empty events are invoked.

### Receive particles

1. Put the existing `FP_WaterFillRegion` with one BoxCollider around the cup's
   opening. Assign **Cup** and leave Coffee Maker / Kettle unassigned. Exactly one
   receiver target is allowed per region.
2. On each incoming ParticleSystem GameObject, add the existing
   `FP_WaterParticleTrigger`, assign the cup's region in Fill Regions, and choose
   its **Input Type**: Type1 (for example coffee), Type2 (tea), or Type3 (sink water).
   The source registers the trigger and configures particle callbacks automatically.
   Multiple sources can share this one cup inlet. Configure Milliliters Per Particle
   for each source; volume goes into one shared capacity, not three reservoirs.
3. Keep Input Type fixed while that source has live particles: classification is
   read when particles enter a receiver. Use a separate particle system for each
   source type, or stop/clear particles before changing its classification.

The first accepted input selects the cup's type. When another type is received,
the cup becomes **Mixed immediately**, even if only partly full. Further inputs
keep it mixed. Partial draining also keeps the type/mixed state. **Only completely
emptying resets it to None**, and the next fill starts a fresh type selection.
At capacity, volume stays capped but incoming liquid of another type still changes
a pure cup to Mixed. This fires OnMixed and OnMixedFilled once, plus
OnLiquidTypeChanged. It does not repeat generic OnFilled or OnFillChanged because
the volume has not changed. Further inputs to a full mixed cup do not repeat events.
A source explicitly marked Mixed can represent an already
mixed outlet. Coffee and kettle receiver targets ignore cup type classifications,
so existing water-fill setups keep their behavior. The one-argument
`TryReceiveWater(amount)` API defaults to Type3 when targeting a cup.

### Events and VFX

All events pass `FP_CupFill`:

| Event | When it fires |
| --- | --- |
| `OnFilled` | Overall volume reaches capacity. |
| `OnEmpty` | Overall volume reaches zero, or initial state is empty. |
| `OnType1Filled` / `OnType2Filled` / `OnType3Filled` | Capacity reached with that pure type. |
| `OnType1Empty` / `OnType2Empty` / `OnType3Empty` | The last volume of that pure type is drained. |
| `OnMixed` | A newly mixed state begins; use to switch to the fourth VFX immediately. |
| `OnMixedFilled` | Capacity reached while mixed, or a full pure cup becomes mixed. |
| `OnMixedEmpty` | A mixed cup is completely drained. |
| `OnLiquidTypeChanged` | Current classification changes, including reset to None. |
| `OnFillChanged` | Initial state or volume changes; read VolumeMl and Fill01. |
| `OnPourStarted` / `OnPourStopped` | Pour feedback from FP_CupPour. |
| `OnStateChanged` | General cup state refresh. |

For your four effects: use each Type Filled event to activate the appropriate pure
effect, each Type Empty event to disable it, and OnMixed to disable all three pure
effects and activate the fourth mixed effect. Use OnMixedEmpty to disable the
fourth. OnEmpty can additionally reset every effect to a default empty appearance.
For appearance while a pure cup is still filling, use OnLiquidTypeChanged rather
than waiting for capacity. `LiquidType` is already None during typed/generic empty
callbacks; the typed event identifies what was drained.

Events fire once per relevant transition. Further particles while full do not
repeat Filled. Draining below full and topping up re-arms Filled. A cup that starts
empty fires only generic OnEmpty, not all typed-empty events. A starting full cup
fires its matching typed and generic Filled events; a starting mixed cup also
fires OnMixed. Typed Filled/Empty events precede their generic counterpart.
Mixing does not fire a pure-type Empty event because the cup has not emptied.

### Pouring and scripting

Add `FP_CupPour` alongside the existing `FP_TipParticlePour`. Assign the cup in
FP_CupPour, and the origin/upright axis/spout ParticleSystem in FP_TipParticlePour.
This reuses the same angle thresholds and hysteresis as beans and the kettle.
Pouring requires liquid, drains Flow Ml Per Second (default 150), and stops at zero.
Disabling the binder stops emission and retains contents; re-enabling can resume
if the cup is still tipped and nonempty. Disabling the cup retains its contents.

Use `TryAddLiquid(FP_CupLiquidType, float)` for scripted fills,
`RemoveLiquidMl(float)` for draining, and `Empty()` to clear all contents and type.
For custom pouring updates, disable Auto Advance and call `AdvancePour(dt)` on an
enabled FP_CupPour. No tick system is required. Like kettle pouring, drained flow
and particle transfer are separate approximations: match emission rate times
Milliliters Per Particle to flow rate. If pouring into another receiver, configure
the outlet particle system's input classification explicitly. No temperature or
per-ingredient proportions are simulated; Mixed is a single state, not separate
ingredient volumes. Verify inlet placement and VFX bindings in the consuming scene.

## Electric water kettle

Add `FP_EquipmentKettle` to the kettle root. Configure Water Capacity Ml, starting
Water Ml, Heating Seconds, and Cooling Seconds. Water Hot can mark an initially
hot fill; empty kettles have no hot/cold water state. Initial volume, full/empty,
and hot/cold events are reported once during initialization for Inspector bindings.

### Filling and heating

Use the existing `FP_WaterFillRegion` with a BoxCollider at the inlet and assign
its **Kettle** reference. Leave **Coffee Maker** empty: a region with both targets
assigned rejects delivery. The existing `FP_WaterParticleTrigger` on the incoming
particle system registers that region and adds its configured milliliters per
captured droplet. Existing coffee-maker regions continue to work. Direct scripts
can call `AddWaterMl(float)`, `SetWaterMl(float)`, or `RemoveWaterMl(float)`.

Bind the power/start switch to `StartHeating()` and the stop switch to
`CancelHeating()`. Heating requires water, an enabled/unbroken kettle, and no active
pour. It finishes after Heating Seconds, switches power off, marks the retained
water hot, and starts Cooling Seconds. Once that timer expires, water flips to
cold in one step. `IsWaterHot`, `IsWaterCold`, `WaterMl`,
`CoolingRemainingSeconds`, and `HeatingProgress01` expose the current state.

This is a binary timed model, not Celsius/thermal physics. Partial draining retains
heat and remaining cooling time. Emptying clears stored heat. Adding any accepted
water marks the contents cold and cancels heating; adding excess to an already full
kettle changes nothing. Reheating pauses the old cooling countdown while heating,
then restarts it on completion. Cancelling heating retains any previously hot
water and resumes its remaining cooling timer. Cooling continues when the heater
is off or broken, while active ticks continue.

Like the coffee maker, the kettle uses `FP_TickSystem`. Disabling cancels heating,
stops pouring, and unregisters, but retains volume and heat/cooling state. Cooling
is paused while disabled or while the tick driver is paused; it resumes on enable.
For a custom driver, disable automatic tick registration and call `Tick(dt)`.
An oversized tick accounts for cooling time after heating finishes within that tick.

### Tipping and pouring

1. Add the existing `FP_TipParticlePour` to the kettle/pour origin. Assign its origin,
   upright axis, angle thresholds, and the particle system at the spout, as with beans.
2. Add `FP_KettlePour` to the **same GameObject as FP_TipParticlePour**, assign the
   kettle, and set Flow Ml Per Second (default 150). It gates emission by available
   water, active state, and condition. Empty/broken/disabled kettles cannot pour.
3. Pouring drains stored volume and stops emission immediately on empty. Starting
   a pour cancels heating; the current hot/cold state remains until a refill, empty,
   or cooling expiry. Returning upright stops new emission and retains falling droplets.
4. To refill another machine, put `FP_WaterParticleTrigger` on the spout particle
   GameObject and assign that machine's water region. Use World simulation space.
   Match particle emission rate × Milliliters Per Particle to Flow Ml Per Second.

The pourer advances using scaled Update time. For a custom flow driver, turn off
Auto Advance on `FP_KettlePour` and call `AdvancePour(dt)` while it stays enabled.
The default gate added to `FP_TipParticlePour` allows pouring unless a supply binder
sets `SetPouringAllowed(false)`, so existing bean pourers retain their behavior.
The pour binder owns the gate while enabled; assign its kettle before enabling it.
Volume drains by configured elapsed-time flow, independently of particle emission
and capture. Droplets are a visual refill interaction, not exact fluid conservation;
downstream receivers currently transfer volume only, not the source's heat state.

| Event | Use |
| --- | --- |
| `OnStarted` / `OnHeating` | Accepted heat start and one-time heater effects. |
| `OnHeatingProgress` | Advancing heating timer/display. |
| `OnFinished` | Heating completed; water is hot and retained. |
| `OnCancelled` | Active heating interrupted by stop, refill, empty, pouring, disable, or break. |
| `OnNoWater` | Heat request rejected because empty. |
| `OnWaterEmpty` / `OnWaterAvailable` | Initial state and subsequent empty/nonempty transitions. |
| `OnWaterFull` | Initial full state or a new crossing to capacity. |
| `OnWaterChanged` | Initial supply and actual volume changes. |
| `OnWaterHot` / `OnWaterCold` | Initial nonempty temperature state and subsequent transitions. Empty has neither state. |
| `OnPourStarted` / `OnPourStopped` | Pouring sound/visual feedback. |
| `OnBroken` / `OnRepaired` | Condition feedback; repair does not restart heating. |
| `OnStateChanged` | General status/display refresh. |

Events pass the kettle. Existing power/toggle/timer/break/repair commands are
supported; `StartTimerSeconds` uses FValue as a heating duration. Equipment data
may describe Supports Timer/Fill/Heat=true and Sub Unit Count=1; the component's
settings drive this simplified model. Stored hot water does not heat arbitrary
contained energy-reactive objects (`EmitPowerUnit` is zero).

## Particle bean refill sample

See [Samples/BeanPour](Samples/BeanPour/README.md) for a wired prefab with
angle-controlled particle pouring into a coffee maker's bean region. It includes
`FP_TipParticlePour`, `FP_BeanParticleTrigger`, and `FP_BeanFillRegion`, with setup,
prototype controls, per-particle mass, and integration instructions.

## FP_Tool Dependencies

Please see the [package.json](./package.json) file for more information.

## License Notes

See [LICENSE.md](LICENSE.md) for details

## Contact

* [John Shull](mailto:JShull@fuzzphyte.com)
