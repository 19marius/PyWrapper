###
## About the project
***PyWrapper*** is a simple program that takes a Python module file (`.py`) and wraps it into a single executable file. 

*Note*:
This is called a *wrapper* since it doesn't *actually* convert the module into a binary file, it just serves as a way to run it without hassle. Python still does all the work in the background.


## Utilities
***PyWrapper*** offers some additional customization options in regards to *how* the module is wrapped.

- <ins>*Building without a console*</ins>:
  You may build the executable to run without a console. This is useful if your module makes use of custom windows and you don't want the console to interfere.

	IMPORTANT:
	To avoid having to close the process manually in *Task Manager* or *cmd*, make sure your module properly handles the lack of a console window.
- <ins>*Building as  a release*</ins>
  Treats the executable as a 'release' by removing the `Press any key to continue . . .` message after program execution. *No effect on non-console builds*.
- <ins>*Setting an icon*</ins>
  You may select an icon for the generated executable to have. This is purely aesthetical, and only applies to the console window and the `.exe` file.
##
You can also add *PyWrapper* as a start-menu shortcut or make it accessible through the context menu. <sub>*(the menu that pops up after you right-click a file)*</sub>

*PyWrapper* can be accessed through the context menu in **2** ways:

- Using '*Build*':
 This will open the *PyWrapper* interface with the module path set to the selected file. You can use the directory of the selected file as the executable's output location by pressing the `^` button.

- Using '*Quick debug build*':
  This will instantly wrap the selected module into a non-release executable with no icon in the same directory as the module's file.

*Note*:
These options should only be used for `.py` files. Selecting either for a non `.py` file will have no effect.


## API
***PyWrapper*** exposes a simple API through the executable itself. Therefore, to use it, you must reference `pywrapper.exe` in your programs.

The API contains 3 methods contained within the static `Python.Wrapper.Wrapper` class:
```cs
public static string Build(BuildConfiguration config);
```
```cs
public static string QuickDebugBuild(string modulePath);
```
```cs
public static string QuickReleaseBuild(string modulePath);
```

Their names are pretty self-explanatory. They all return the absolute path of the outputted executable file.

`Python.Wrapper.BuildConfiguration` is a type that contains all the information (in order) on how the executable will be built:

- The location of the module file;
- The output directory;
- *(optional)[^1]* The path to the executable's icon;
- Whether to use a console or not;
- Whether it's a release build or not.
[^1]: If you don't want to use an icon, you can set the icon path to `null`. However, if the icon path is not `null`, then it <ins>must</ins> point to a valid icon file, otherwise a `FileNotFound` exception will be thrown.