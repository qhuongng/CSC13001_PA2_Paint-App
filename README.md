# WPF Paint Clone

This is a group project assignment from the course **CSC13001 – Windows Programming (21KTPM2)** at VNU-HCM, University of Science.

## Project contributors
1. Nguyễn Quỳnh Hương ([qhuongng](https://github.com/qhuongng))

2. Đặng Nhật Hòa ([hoadang0305](https://github.com/hoadang0305))

## General information
This is a simple clone of **Microsoft Paint**, written with **.NET 8.0**. Features include:

- Drawing various shapes with different fill and stroke colors, stroke widths, and stroke dashes.
- Selecting drawn shapes to resize, move, rotate, flip, or modify their properties via a selection pane.
- Adding text to shapes and modify the text's font, font size, foreground and background color, formatting, and alignment.
- Edit-menu tasks: cut, copy, paste, undo, redo.
- Saving and loading files to resume working.
- "Snipping Tool"-esque function—select an area on the canvas to copy it to the clipboard.
- Layers.

Design patterns applied to this project include Factory Method for dynamically loading class libraries (for handling shape-drawing logic), and Memento for undoing/redoing. 

Features to consider for further development include canvas panning/zooming, the ability to select shapes directly on the canvas, more flexible editing adorners, customizable color palette, and free-hand tools (brushes, pens, eraser, etc.).

## Demo
The demo video for this application is available on [YouTube](https://youtu.be/7OsoypPsgrc) (in Vietnamese).

## Build & run the application locally
[**Microsoft Visual Studio**](https://visualstudio.microsoft.com/vs/community/), with C# and .NET development packages installed, is required to build and run the project source code.

1. Clone this repository to your local machine.

2. Open the solution file in Visual Studio.

3. Build and run the project.

**Note:** If you move the contents of the **PaintApp/bin/Debug/net8.0-windows** folder elsewhere, you can still run the app without building it in Visual Studio by running **PaintApp.exe**, as long as all the content in the aforementioned folder is in the same directory at the time of execution, and [**.NET Desktop Runtime 8.0 or above**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed.
