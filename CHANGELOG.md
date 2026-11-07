# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.6.4] - 2026-11-06

### [0.6.4] Added

- Coffee maker `OnCupRemoved` departure event for idle and brewing visual feedback,
  alongside the existing cup-arrival and missing-cup events.
- Coffee maker `OnEnoughWater` / `OnWaterFull` threshold events with initial-supply
  notification and independent re-arming, particle-based water inlet receivers,
  and a documented stationary Water Refill sample.
- Coffee maker `OnEnoughBeans` and `OnBeansFull` threshold events, initial-supply
  notification, independent re-arming after consumption/depletion, and regression tests.
- Coffee maker `OnCupPresent` arrival event, re-armed after removal, for both the
  configured cup region and existing manual detectors.
- `FP_EquipmentToaster` with a normalized toast setting, timed heat transfer to
  contained energy-reactive items, entry/exit, start/toasting/progress/finish/cancel
  events, empty-slot rejection, and break/repair commands.
- `FP_ToasterContainmentVolume` with trigger enter/exit tracking, child and compound
  colliders, overlapping slot volumes, and disabled/destroyed contact cleanup.
- Toaster setup documentation and Edit Mode/Play Mode coverage.

### [0.6.4] Changed

- Coffee maker supply shortages now publish existing `OnNoBeans` / `OnNoWater`
  events after brew completion/cancellation when the next recipe is unavailable,
  without another start attempt. Mid-brew refills clear pending notifications;
  idle depletion reports once when a threshold is crossed downward.
- Package version updated to `0.6.4`.
- `FP_EquipmentTickableBase.Tick` is virtual so timed equipment can transfer heat
  for the final partial tick before completing. Existing base behavior is preserved.

## [0.6.3] - 2026-11-06

### [0.6.3] Added

- Tip-controlled bean particle pouring, hopper particle-trigger receivers, and a
  documented `Samples/BeanPour/FP_BeanPourPrototype.prefab` with wired references.
- `FP_Cup` and `FP_CupRegion` for automatic coffee-maker cup detection, a referenced
  region used only when Require Cup is enabled, compound cup colliders, and
  stationary-cup/lifecycle handling with Edit Mode and Play Mode tests.
- `FP_EquipmentCoffeeMaker` with timed brewing, beans/water quantities and refills,
  optional cup gating, start/brewing/progress/finish/cancel/shortage/condition events,
  existing command support, and Edit Mode/Play Mode tests.
- `FP_EquipmentScale`, `FP_WeightedItem`, and `FP_ScaleContainmentVolume` for
  event-driven multi-item weight totals, compound colliders, overlapping volumes,
  and contact cleanup. Uses existing equipment commands without a tick dependency.
- `FP_ScaleDisplay` TextMeshPro binding, scale setup documentation, and Edit Mode tests.
- A scale creation menu with a wired world-space UI panel, plus a Play Mode
  physics/display integration test.

### [0.6.3] Changed

- Sink tests use direct lifecycle invocation to avoid Unity 6.6 Edit Mode
  `SendMessage` assertions.

## [0.6.2] - 2026-10-26

### [0.6.2] Added

- `FP_EquipmentSink` with independent hot/cold knobs, flow and condition events,
  existing equipment commands, explicit liquid/item fill state, and Edit Mode tests.

## [0.6.2] - 2026-06-04

### 0.6.2 Added

- [@JShull](https://github.com/jshull)
  - License Updates

## [0.6.1] - 2026-03-24

### 0.6.1 Changed

- [@JShull](https://github.com/jshull)
  - FP_PanMove.cs
    - Updated `DeactivateToolFromUI()` to override the base implementation for Unity 6.4 compatibility

## [0.6.0] - 2026-02-02

### 0.6.0 Added

- [@JShull](https://github.com/jshull)
  - Samples Updated to include SamplesEquip
  - FP_EnergyReactiveItem.cs
    - Allows a way to pass information from our equipment to something that might want to know based on trigger collision enter/exit

## [0.5.0] - 2026-01-27

### 0.5.0 Added

- [@JShull](https://github.com/jshull)
  - FP_EquipmentBase.cs
  - FP_EquipmentData.cs
  - FP_EquipmentMicrowave.cs
  - FP_EquipmentTickableBase.cs
    - Equipment Concept Added that relies on the FP_TickSystem as well as harnessing a binder architecture

### 0.5.0 Modified

- [@JShull](https://github.com/jshull)
  - FPToolUtility.cs
    - Interface/enums to support Equipment

## [0.4.0] - 2025-07-01

### 0.4.0 Added

- [@JShull](https://github.com/jshull)
  - FP Installer Support

## [0.1.0] - 2025-03-28

### 0.1.0 Added

- [@JShull](https://github.com/jshull)
  - Moved all test files to a Unity Package Distribution
  - Setup the ChangeLog.md

### 0.1.0 Changed

- None... yet

### 0.1.0 Fixed

- Setup the contents to align with Unity naming conventions

### 0.1.0 Removed

- None... yet
