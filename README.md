# RemindMe
Simple CLI tool for storing / reporting reminders and tasks

### Features:

- Terribly written and disorganized code ensures a fun challenge for anyone who wants to take up an issue
- Non-unique functionality that several other tools already provide, replacing the clutter of a useful GUI to focus on making installation difficult
- Needless enums, implemented through static class variables, that suggest a framework and code style that is rarely adhered to throughout the project

### Installation for Actual Use (Because You're Absolutely Nuts):

Right now, installation of the tool for use in your CLI of choice is manual, and you'll have to build the project yourself. **This is mostly a tool I've created for myself, 
so creating a mechanims for automated installation is not currently high in priority**. To build the solution, clone the repository and open it in Visual Studio (the purple one).
.NET should be cross-platform with version 6.0, so you shouldn't have trouble building for a non-Windows platform. You don't need the Visual Studio IDE for building the project,
but it does make it significantly easier, and you can customize it to your liking if you do! Don't forget, if you've customized it to do something really cool, share it on this
repo's page!

1. 

- Instlall Visual Studio 2022 (free Community Edition is perfectly fine)

**OR**

- Install the dotnet 6.0 SDK with CLI tools

2. Clone the repo and open in Visual Studio 2022 (free Community Edition is perfectly fine)
3. Build the project inside of Visual Studio 2022 (or open the solution in a shell and execute `dotnet build`)
4. Copy all the files/folders from the directory where the `reme.exe` executable is built and stored into your favorite directory to store 
executables you use in your shell. 
5. Add the directory where you copied the binary files to your `PATH` environment variable, if it isn't there already.The method for doing this will vary depending on your OS.

*note: If you are using windows with CMD/PowerShell, I placed my binaries inside of my `Program Files` folder. I then used the start menu to search for 
and select **"Edit environment variables"**. From there, you should modify your `PATH` env variable and add a new entry to where you copied your binaries.
