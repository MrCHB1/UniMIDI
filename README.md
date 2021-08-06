# UniMIDI
A Console MIDI Player (soon to be a OpenGL MIDI Player)

### How to run
First of all, open your terminal and move your directory to the same location as "UniMIDI.csproj"
If you have a .NET Runtime installed (.NET 5 is recommended), go ahead and type this command in the console:

```dotnet run [path/to/midifile.mid] <arg>```

- `[]` is required
- `<>` is optional

Available Arguments:
- `-use256Keys`
- `-playbackSpeed [speed]`
- `cm [Color Method]`
    * `0: Colors notes by channel`
    * `1: Colors notes by track/channel (Recommended for MIDIs converted from images)`
    * `2: Colors notes by track`

If not, you can find .NET 5 here:
https://dotnet.microsoft.com/download/dotnet/5.0

UniMIDI will simply visualize the notes played as time progresses.
