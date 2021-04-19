# UniMIDI
A Console MIDI Player (soon to be a OpenGL MIDI Player)

### How to run
First of all, open your terminal and move your directory to the same location as "UniMIDI.csproj"
If you have .NET Runtime installed (Core 3.1 is recommended, but you can also give .NET 5 a go), go ahead and type this command in the console:

```dotnet run [path/to/midifile.mid] <noColor>```

- `[]` is required
- `<>` is optional

If not, you can find .NET Core 3.1 here:
https://dotnet.microsoft.com/download/dotnet/3.1

UniMIDI will simply visualize the notes played as time progresses. (Audio is not yet implemented (on Linux))

# Notes
__For Windows 10 users__
If you're planning to run UniMIDI with colors, I'd recommend that you use **Windows Powershell** for this, otherwise the notes would visualize wrong and wouldn't work properly.
- If the notes do not visualize correctly, stop the process, then re-run again with the argument "noColor".
