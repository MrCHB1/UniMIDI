using System;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Windows;
using System.IO;
using MIDIModificationFramework;
using MIDIModificationFramework.MIDIEvents;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace UniMIDI {
    public class Program {

        static void Main(string[] args) {
            try {
            MidiFile file = new MidiFile(args[0]);
            var merge = Mergers.MergeSequences(file.IterateTracks()).ChangePPQ(file.PPQ, 1).CancelTempoEvents(250000);

            var time = new Stopwatch();
            time.Start();
            double midiTime = 0;

            try {
                KDMAPI.InitializeKDMAPIStream();
                Console.WriteLine("KDMAPI Initialized!");
            } catch {
                Console.WriteLine("An error occurred while KDMAPI tried to initialize the stream, continuing without audio");
            }

            string[] notes = Enumerable.Repeat<string>(".", 128).ToArray();
            int[] numOverlaps = Enumerable.Repeat<int>(0, 128).ToArray();
            bool midiEnded = false;

            var noteDisplay = new Thread(() => {
                while (!midiEnded) {
                    Console.WriteLine(string.Join("", notes));
                    Thread.Sleep(10);
                }
            });
            
            noteDisplay.Start();
            foreach (MIDIEvent e in merge) {
                midiTime += e.DeltaTime;
                int delay = (int)((midiTime - time.Elapsed.TotalSeconds) * 1000);
                if (delay > 0)
                    Thread.Sleep(delay);
                if (e is NoteOnEvent) {
                    var ev = e as NoteOnEvent;
                    int n = (ev.Key % 12);
                    bool isBlackNote = n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
                    string blackChar = (isBlackNote ? 38 : 48).ToString();
                    
                    if (args.Length == 2 && args.Contains("noColor")) {
                        switch ((int)ev.Channel % 5) {
                            case 0:
                                notes[ev.Key] = "#";
                                break;
                            case 1:
                                notes[ev.Key] = "%";
                                break;
                            case 2:
                                notes[ev.Key] = "*";
                                break;
                            case 3:
                                notes[ev.Key] = "@";
                                break;
                            case 4:
                                notes[ev.Key] = "!";
                                break;
                            case 5:
                                notes[ev.Key] = "?";
                                break;
                            default:
                                notes[ev.Key] = "#";
                                break;
                        }
                    } else {
                    switch ((int)ev.Channel % 8) {
                        case 0:
                            notes[ev.Key] = "\u001b["+blackChar+";5;196m"+"#"+"\x1b[0m";
                            break;
                        case 1:
                            notes[ev.Key] = "\u001b["+blackChar+";5;208m"+"#"+"\x1b[0m";
                            break;
                        case 2:
                            notes[ev.Key] = "\u001b["+blackChar+";5;226m"+"#"+"\x1b[0m";
                            break;
                        case 3:
                            notes[ev.Key] = "\u001b["+blackChar+";5;34m"+"#"+"\x1b[0m";
                            break;
                        case 4:
                            notes[ev.Key] = "\u001b["+blackChar+";5;87m"+"#"+"\x1b[0m";
                            break;
                        case 5:
                            notes[ev.Key] = "\u001b["+blackChar+";5;21m"+"#"+"\x1b[0m";
                            break;
                        case 6:
                            notes[ev.Key] = "\u001b["+blackChar+";5;128m"+"#"+"\x1b[0m";
                            break;
                        case 7:
                            notes[ev.Key] = "\u001b["+blackChar+";5;200m"+"#"+"\x1b[0m";
                            break;
                        default:
                            notes[ev.Key] = "#";
                            break;
                    }
                    }
                    numOverlaps[ev.Key]++;
                } else if (e is NoteOffEvent) {
                    var ev = e as NoteOffEvent;
                    numOverlaps[ev.Key]--;
                    if (numOverlaps[ev.Key] == 0)
                        notes[ev.Key] = ".";
                }

                if (e is NoteOnEvent || e is NoteOffEvent || e is PolyphonicKeyPressureEvent || e is PitchWheelChangeEvent) {
                    try {
                        var data = e.GetData();
                        uint d = 0;
                        for (int i = data.Length - 1; i >= 0; i--)
                            d = (d << 8) | data[i];
                        KDMAPI.SendDirectData(d);
                    } catch {
                        // Ignore the error and do not send any message
                    }
                }
            }
            midiEnded = true;
            noteDisplay.Join();
            try {
                 KDMAPI.TerminateKDMAPIStream();
            } catch {
                 // Still ignore the error
            }

            } catch (FileNotFoundException) {
                Console.WriteLine("Invalid file directory, or file does not exist.");
            } catch (IndexOutOfRangeException) {
                Console.WriteLine((args.Length == 0 ? "No arguments specified" : @"Usage:
[]: Required
<>: Optional
dotnet run [/path/to/midifile.mid] <nocolor>
"));
            }
        }
    }
}
