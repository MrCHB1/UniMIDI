using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Windows;
using System.IO;
using MIDIModificationFramework;
using MIDIModificationFramework.MIDIEvents;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using Pastel;

namespace UniMIDI {
    public class Program {
        static void Main(string[] args) {

            try {
            MidiFile file = new MidiFile(args[0]);
            var merge = Mergers.MergeSequences(file.IterateTracks()).ChangePPQ(file.PPQ, 1).CancelTempoEvents(250000);

            var time = new Stopwatch();
            time.Start();
            double midiTime = 0;
            double midiPlaybackTime = 0;

            try {
                KDMAPI.InitializeKDMAPIStream();
                Console.WriteLine("KDMAPI Initialized!");
            } catch {
                Console.WriteLine("An error occurred while KDMAPI tried to initialize the stream, continuing without audio");
            }
            
            int keyboardWidth = 128;

            if (args.Contains("-use256keys"))
                keyboardWidth = 256;
            else
                keyboardWidth = 128;
            List<string> notes = Enumerable.Repeat<string>(" ", keyboardWidth).ToList();
            int[] numOverlaps = Enumerable.Repeat<int>(0, keyboardWidth).ToArray();
            string[] noteColors = File.ReadAllLines("channelColorConfig.txt");
            bool midiEnded = false;
            bool useAudioFile = false;
            string audioPath = "";

            double playbackSpeed = 1;

            if (args.Contains("-playbackSpeed"))
                playbackSpeed = Convert.ToDouble(args[args.ToList().FindIndex(ps => ps.Contains("-playbackSpeed"))+1]);

            var midiInfo = new Thread(() => {

            });

            if (args.Contains("-useAudioFile")) {
                audioPath = args[args.ToList().FindIndex(af => af.Contains("-useAudioFile"))+1];
                useAudioFile = true;
            }

            var noteDisplay = new Thread(() => {
                while (!midiEnded) {
                    Console.WriteLine(string.Join("", notes.ToArray()));
                    Thread.Sleep(5*(int)(1/playbackSpeed));
                }
            });

            var playbackThread = new Thread(() => {
                if (useAudioFile) {
                    System.Media.SoundPlayer audio = new System.Media.SoundPlayer(audioPath);
                    audio.Play();
                } else {
                    try {
                        using (var mergeSequence = merge.GetEnumerator()) {
                            while (mergeSequence.MoveNext()) {
                                MIDIEvent e = mergeSequence.Current;
                                midiPlaybackTime += e.DeltaTime;
                                int delay = (int)((midiPlaybackTime - (time.Elapsed.TotalSeconds*playbackSpeed)) * 1000);
                                if (delay > 0)
                                    Thread.Sleep(delay);
                                if ((e is NoteOnEvent || e is NoteOffEvent || e is PolyphonicKeyPressureEvent || e is PitchWheelChangeEvent || e is ControlChangeEvent)) {
                                    var data = e.GetData();
                                    uint d = 0u;
                                    for (int i = data.Length - 1; i >= 0; i--) {
                                        uint dtmp = d << 8;
                                        d = dtmp | data[i];
                                    }
                                    KDMAPI.SendDirectData(d);
                                }
                            }
                        }
                    } catch {

                    }
                }
            });


            midiInfo.Start();
            noteDisplay.Start();
            playbackThread.Start();

            int ncl = noteColors.Length;
            
            using (var mergeSequence = merge.GetEnumerator()) {
                while (mergeSequence.MoveNext()) {
                    MIDIEvent e = mergeSequence.Current;
                    midiTime += e.DeltaTime;
                    int delay = (int)((midiTime - (time.Elapsed.TotalSeconds*playbackSpeed)) * 1000);
                    if (delay > 0)
                        Thread.Sleep(delay);
                    if (e is NoteOnEvent) {
                        var ev = e as NoteOnEvent;
                        int n = ev.Key % 12;
                        bool isBlackNote = n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
                        
                        if (args.Contains("-noColor") || args.Contains("-nocolor")) {
                            notes[ev.Key] = new List<string>() {".","~","!","+",":","I","0","P"}[(int)(ev.Channel&((1<<3)-1))];
                        } else {
                            List<int> rgbVals = new List<int> {Convert.ToInt32(noteColors[(int)ev.Channel%ncl].Split(" ")[0]), 
                                                               Convert.ToInt32(noteColors[(int)ev.Channel%ncl].Split(" ")[1]),
                                                               Convert.ToInt32(noteColors[(int)ev.Channel%ncl].Split(" ")[2])};
                            string encodedRGB = EncodeToRGB(rgbVals);
                            notes[ev.Key] = isBlackNote ? "#".Pastel("#"+encodedRGB) : "#".PastelBg("#"+encodedRGB);
                        }
                        numOverlaps[ev.Key]++;
                    } else if (e is NoteOffEvent) {
                        var ev = e as NoteOffEvent;
                        numOverlaps[ev.Key]--;
                        if (numOverlaps[ev.Key] == 0)
                            notes[ev.Key] = " ";
                    }
                }
            }
            midiEnded = true;
            midiInfo.Join();
            noteDisplay.Join();
            playbackThread.Join();
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
UniMIDI.exe [/path/to/midifile.mid] <nocolor>
"));
            }
        }

        public static string EncodeToRGB(List<int> rgbList) {
            int res1 = 0;
            for (int i = 0; i < rgbList.Count; i++) res1 = (res1 << 8) | rgbList[i];
            return res1.ToString("X");
        }
    }
}
