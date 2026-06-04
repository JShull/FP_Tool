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

## FP_Tool Dependencies

Please see the [package.json](./package.json) file for more information.

## License Notes

See [LICENSE.md](LICENSE.md) for details

## Contact

* [John Shull](mailto:JShull@fuzzphyte.com)
