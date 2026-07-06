# Virtual Lab Unity Prototype - Current Version
Main scene:
Assets/Scenes/VirtualLab.unity

This project is a Unity 3D virtual laboratory for basic experiments with substances. It includes a laboratory bench, shelf with substances, glassware, a test tube, a round-bottom flask, a scale, and a Bunsen burner. The user can measure substances, prepare samples, pour them into glassware, mix them, heat the solution, and observe visual and sound reactions.

## How to run on PC

1. Open the project in Unity 6000.3.17f1.
2. Open Assets/Scenes/VirtualLab.unity.
3. Press Play.
4. Use the mouse to click jars and action buttons.
5. Press V if you want to preview the mobile 3D Box VR mode on PC.

## Main controls

Keyboard shortcuts:

- 1: select Kiselina / acid
- 2: select Baza / base
- 3: select Bakar sulfat / copper sulfate
- 4: select Karbonat / carbonate
- M: measure selected substance
- P: pour/add measured sample
- X: mix substances
- H: heat the solution
- U: activate device signal
- R: reset experiment
- V: toggle 3D Box VR preview mode

The same actions are also available from the in-scene control panel.

## Laboratory equipment and its role

- Vaga / scale: used by the Meri action. It measures the selected substance and gives visual feedback by changing color.
- Epruveta / test tube: receives a small sample after measuring. The sample appears inside the test tube.
- Casa / glassware: the main mixing vessel. Measured samples are poured from the test tube into the glassware.
- Kolba / round-bottom flask: used for heating. When heating starts, the prepared solution is shown in the flask.
- Plamenik / Bunsen burner: heats the round-bottom flask and shows a flame when active.
- Shelf jars: contain the available substances: Kiselina, Baza, Bakar sulfat, and Karbonat.

## Correct experiment workflow

Use this sequence for the simulation to make sense:

1. Select a substance from the shelf.
2. Press Meri. The scale measures it and a sample appears in the test tube.
3. Press Dodaj. The sample moves from the test tube into the glassware.
4. Repeat steps 1 to 3 for a second substance.
5. Press Mesaj. The reaction happens in the glassware.
6. Press Zagrej if you want to heat the prepared solution in the round-bottom flask.
7. Press Reset to start again.

## Example experiments

### Acid + Base

1. Select Kiselina.
2. Press Meri.
3. Press Dodaj.
4. Select Baza.
5. Press Meri.
6. Press Dodaj.
7. Press Mesaj.

Result: neutralization reaction. The solution in the glassware changes to a light green color and a sound effect is played.

### Acid + Carbonate

1. Select Kiselina.
2. Press Meri.
3. Press Dodaj.
4. Select Karbonat.
5. Press Meri.
6. Press Dodaj.
7. Press Mesaj.

Result: gas release reaction. The app shows smoke/bubbles and plays a reaction sound.

### Copper Sulfate + Base

1. Select Bakar sulfat.
2. Press Meri.
3. Press Dodaj.
4. Select Baza.
5. Press Meri.
6. Press Dodaj.
7. Press Mesaj.

Result: blue precipitate-style reaction. The liquid changes to a stronger blue color and a sound effect is played.

## Visual and sound feedback

The application gives feedback through:

- liquid appearing in the test tube after measurement
- liquid appearing in the glassware after adding
- color changes during reactions
- smoke and bubbles for gas reactions
- flame when the Bunsen burner is active
- liquid shown in the round-bottom flask during heating
- generated beep/reaction sounds
- a result message on the control panel

If sound is not heard, check that the Game view audio is not muted and that the computer volume is on.

## Mobile 3D Box VR mode

The project includes a simple 3D Box/Cardboard-style VR mode:

- split-screen stereo view with left-eye and right-eye cameras
- gyroscope head tracking on mobile devices that support gyro
- gaze-based selection using a small turquoise reticle
- look at an object or button for about 1 second to activate it

On Android/mobile, VR mode starts automatically. On PC, press V to preview it and use the mouse to simulate head movement.

## Assignment coverage

This project satisfies the required topic by including:

- a 3D laboratory environment
- interactive laboratory objects
- work with substances and simple solutions
- measuring, adding, mixing, heating, and device activation
- visual reactions such as color changes, smoke, bubbles, and flame
- sound reactions through generated audio effects
- observation/result messages
- a mobile 3D Box VR mode with stereo rendering, gyro head tracking, and gaze interaction

## Contributors

Marija Blazheska 221503
Darko Milenkoski 222088
Aleksandar Panovski 
