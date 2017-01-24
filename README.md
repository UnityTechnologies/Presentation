# Presentation Project (Unity)
No need to switch to Power Point anymore.

## Description
This is an Editor extension for making and presenting slide decks in Unity. It allows you to easily mix static slides with interactive slides and in-editor demonstrations. Each slide is a Unity Scene which can work in or outside of Play Mode.

**Supported Unity versions**: 5.5+ (though, should work in 5.3+)

## What it is for
The original idea most likely was by [Andy Touch](https://twitter.com/andytouch), Unity Evangelist. He likes showing Unity features in the actual Editor, not on boring Power Point slides. But when showing a presentation about Unity you usually have to have a few static slides in Power Point or Keynote, even if you switch to a live demo later. This sometimes results in video capture errors and Unity crashing (which is a great way to entertain the audience!).

This project allows you to build a full presentation in Unity and never leave it. 

**You can:**

1. Show a few static slides and move to live demo without switching applications.
1. Show in-editor features.
1. Use Unity UI to design slides.
1. Make interactive slides (there are examples in the sample presentation).
1. Even load whole games as slides!
1. ... and more!

## Getting started
1. Download the project.
1. Open Presentation Window from Window > Presentation menu.
1. Locate the sample Slide Deck in Presentation > Unity folder. It is called **Unity Sample Presentation**.
1. Select it and click **Load This Slide Deck** button in the Inspector.
1. Press **[> B]** button in the top right corner of the Presentation Window.
1. Use Left and Right arrows on your keyboard or **[<<]** and **[>>]** buttons in the top right corner of the Presentation Window to switch slides.

## How it works
A Slide Deck is an asset in the Project. It consists of slides with the following properties:

1. Scene to load,
1. Visibility (will be skipped when presenting if not visible),
1. If this slide should switch to Play Mode.

You can create one with the **[New]** button in the Presentation Window and save it on disk with  **[Save]** button.

Slides in the editor interface have the following control elements:

1. Handler to drag it up and down,
1. Button with "eye" icon switches visibility,
1. Button with "play" icon controls if this slide should go to Play Mode,
1. Object field for the scene to load,
1. Button with "play" icon which starts presentation from this slide or jumps to the slide if already presenting.

## Controls
**Right Arrow** — next slide,
**Left Arrow** — previous slide,
**Shift + Space** — maximize/minimize Game View (works when Game View is selected).

## Slide scene structure
If you check the demo Slide Deck you will see that slide scenes all have the same naming convention. This will later be used for transition effects and compatibility.

## TODO
1. Proper fullscreen.
1. Some way to export slides to more common formats (PNGs, PDF, PPT) when one is unable to run Unity during a presentation.
1. Better slide list interface.

## Known issues
1. When fast switching between slides sometimes two slides can be loaded at once.

## Authors
The original project was made by Valentin Simonov, Andy Touch, Rus Scammel and Adam Buckner.
