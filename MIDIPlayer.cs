using System;
using System.IO;
using System.Collections.Generic;
using MIDIModificationFramework;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using MIDIModificationFramework.MIDIEvents;
using System.Drawing;
using Pastel;

namespace UniMIDI {
    public class MIDIPlayer {
        public int FirstKey { get; set; }
        public int LastKey { get; set; }

        public MidiFile _File { get; private set; }

        public bool Initialized { get; private set; } = false;

        public List<string> KeyboardString;
        private string[] noteColors;
        int ncl;

        private string[,] overlappingNoteColors;

        private IEnumerable<MIDIEvent> merged;

        List<int> numOverlaps;

        public MIDIPlayer(string filePath, int fk = 0, int lk = 127) {
            _File = new MidiFile(File.OpenRead(filePath));
            FirstKey = fk;
            LastKey = lk;

            KeyboardString = Enumerable.Repeat<string>(" ", GetKeyboardWidth()+1).ToList();
            numOverlaps = Enumerable.Repeat<int>(0, GetKeyboardWidth()+1).ToList();

            merged = Mergers.MergeSequences(_File.IterateTracks()).ChangePPQ(_File.PPQ, 1).CancelTempoEvents(250000);

            noteColors = File.ReadAllLines("./channelColorConfig.txt");
            ncl = noteColors.Length;

            Initialized = true;
        }

        public int GetKeyboardWidth() => LastKey - FirstKey;

        public void PlayAudio(double playbackSpeed = 1) {
            if (Initialized) {
                double playbackTime = 0;

                var time = new Stopwatch();
                time.Start();

                KDMAPI.InitializeKDMAPIStream();
                var thread = new Thread(() => {
                    foreach (var e in merged) {
                        playbackTime += e.DeltaTime;
                        int delay = (int)((playbackTime - (time.Elapsed.TotalSeconds*playbackSpeed)) * 1000);
                        if (delay > 0) Thread.Sleep(delay);
                        if ((e is NoteOnEvent || e is NoteOffEvent || e is PolyphonicKeyPressureEvent || e is PitchWheelChangeEvent || e is ControlChangeEvent)) {
                            KDMAPI.SendDirectData(ConvertDataToKDMAPI(e.GetData()));
                        }
                    }
                });
                thread.Start();
                thread.Join();
                KDMAPI.TerminateKDMAPIStream();
            } else
                return;
        }

        public void PlayVisuals(int colorMethod = 0, bool noColor = false, double playbackSpeed = 1) {
            if (Initialized) {
                double midiTime = 0;

                var time = new Stopwatch();
                time.Start();

                bool midiEnded = false;

                var thread1 = new Thread(() => {
                    if (colorMethod == 0) {
                        foreach (var e in merged) {
                            midiTime += e.DeltaTime;
                            int delay = (int)((midiTime - (time.Elapsed.TotalSeconds*playbackSpeed)) * 1000);
                            if (delay > 0) Thread.Sleep(delay);

                            string oldRgb = "";
                            
                            if (e is NoteOnEvent) {
                                var ev = e as NoteOnEvent;
                                if (ev.Key >= FirstKey && ev.Key <= LastKey) {
                                    int n = ev.Key % 12;
                                    bool isBlackNote = n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
                                    if (noColor) KeyboardString[ev.Key-FirstKey] = new List<string>() {".","~","!","+",":","I","0","P"}[(int)(ev.Channel&((1<<3)-1))];
                                    else {
                                        List<int> rgbVals = new List<int> {Convert.ToInt32(noteColors[(int)ev.Channel%ncl].Split(" ")[0]), 
                                                                        Convert.ToInt32(noteColors[(int)ev.Channel%ncl].Split(" ")[1]),
                                                                        Convert.ToInt32(noteColors[(int)ev.Channel%ncl].Split(" ")[2])};
                                        string encodedRGB = EncodeToRGB(rgbVals);
                                        KeyboardString[ev.Key-FirstKey] = isBlackNote ? "#".Pastel("#"+encodedRGB) : "#".PastelBg("#"+encodedRGB);
                                    }
                                    numOverlaps[ev.Key-FirstKey]++;
                                }
                            } else if (e is NoteOffEvent) {
                                var ev = e as NoteOffEvent;
                                numOverlaps[ev.Key-FirstKey]--;
                                if (ev.Key >= FirstKey && ev.Key <= LastKey) {
                                    if (numOverlaps[ev.Key-FirstKey] == 0)
                                        KeyboardString[ev.Key-FirstKey] = " ";
                                }
                            }
                        }
                    } else if (colorMethod == 1) {
                        string[] colorID = Enumerable.Repeat("0 0 0", _File.TrackCount*16).ToArray();
                        foreach (var e in merged) {
                            midiTime += e.DeltaTime;
                            int delay = (int)((midiTime - (time.Elapsed.TotalSeconds*playbackSpeed)) * 1000);
                            if (delay > 0) Thread.Sleep(delay);

                            string oldRgb = "";

                            if (e is ColorEvent) {
                                var cEv = e as ColorEvent;
                                noteColors[(cEv.Track*16+(int)cEv.Channel)%ncl] = $"{Convert.ToString(cEv.GetData()[7])} {Convert.ToString(cEv.GetData()[8])} {Convert.ToString(cEv.GetData()[9])}";
                            } if (e is NoteOnEvent) {
                                var ev = e as NoteOnEvent;
                                if (ev.Key >= FirstKey && ev.Key <= LastKey) {
                                    int n = ev.Key % 12;
                                    bool isBlackNote = n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
                                    if (noColor) KeyboardString[ev.Key-FirstKey] = new List<string>() {".","~","!","+",":","I","0","P"}[(int)(ev.Channel&((1<<3)-1))];
                                    else {
                                        List<int> rgbVals = new List<int> {Convert.ToInt32(noteColors[(int)(ev.Track*16+ev.Channel)%ncl].Split(" ")[0]), 
                                                                        Convert.ToInt32(noteColors[(int)(ev.Track*16+ev.Channel)%ncl].Split(" ")[1]),
                                                                        Convert.ToInt32(noteColors[(int)(ev.Track*16+ev.Channel)%ncl].Split(" ")[2])};
                                        string encodedRGB = EncodeToRGB(rgbVals);
                                        KeyboardString[ev.Key-FirstKey] = isBlackNote ? " ".PastelBg(" "+encodedRGB) : " ".PastelBg(" "+encodedRGB);
                                    }
                                    numOverlaps[ev.Key-FirstKey]++;
                                }
                            } else if (e is NoteOffEvent) {
                                var ev = e as NoteOffEvent;
                                numOverlaps[ev.Key-FirstKey]--;
                                if (ev.Key >= FirstKey && ev.Key <= LastKey) {
                                    if (numOverlaps[ev.Key-FirstKey] == 0)
                                        KeyboardString[ev.Key-FirstKey] = " ";
                                }
                            }
                        }
                    } else if (colorMethod == 2) {
                        foreach (var e in merged) {
                            midiTime += e.DeltaTime;
                            int delay = (int)((midiTime - (time.Elapsed.TotalSeconds*playbackSpeed)) * 1000);
                            if (delay > 0) Thread.Sleep(delay);

                            string oldRgb = "";

                            if (e is ColorEvent) {
                                var cEv = e as ColorEvent;
                                noteColors[(cEv.Track*16+(int)cEv.Channel)%ncl] = $"{Convert.ToString(cEv.GetData()[7])} {Convert.ToString(cEv.GetData()[8])} {Convert.ToString(cEv.GetData()[9])}";
                            } if (e is NoteOnEvent) {
                                var ev = e as NoteOnEvent;
                                if (ev.Key >= FirstKey && ev.Key <= LastKey) {
                                    int n = ev.Key % 12;
                                    bool isBlackNote = n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
                                    if (noColor) KeyboardString[ev.Key-FirstKey] = new List<string>() {".","~","!","+",":","I","0","P"}[(int)(ev.Channel&((1<<3)-1))];
                                    else {
                                        List<int> rgbVals = new List<int> {Convert.ToInt32(noteColors[(int)(ev.Track)%ncl].Split(" ")[0]), 
                                                                        Convert.ToInt32(noteColors[(int)(ev.Track)%ncl].Split(" ")[1]),
                                                                        Convert.ToInt32(noteColors[(int)(ev.Track)%ncl].Split(" ")[2])};
                                        string encodedRGB = EncodeToRGB(rgbVals);
                                        KeyboardString[ev.Key-FirstKey] = isBlackNote ? "#".Pastel("#"+encodedRGB) : "#".PastelBg("#"+encodedRGB);
                                    }
                                    numOverlaps[ev.Key-FirstKey]++;
                                }
                            } else if (e is NoteOffEvent) {
                                var ev = e as NoteOffEvent;
                                numOverlaps[ev.Key-FirstKey]--;
                                if (ev.Key >= FirstKey && ev.Key <= LastKey) {
                                    if (numOverlaps[ev.Key-FirstKey] == 0)
                                        KeyboardString[ev.Key-FirstKey] = " ";
                                }
                            }
                        }
                    }
                    midiEnded = true;
                });

                var thread2 = new Thread(() => {
                    Console.Clear();
                    while (!midiEnded) {
                        Console.WriteLine(string.Join("", KeyboardString.ToArray()));
                        Thread.Sleep((int)(5.5*(1/playbackSpeed)));
                    }
                });

                thread1.Start();
                thread2.Start();
                thread1.Join();
                thread2.Join();
            }
        }

        uint ConvertDataToKDMAPI(byte[] data) {
            uint d = 0u;
            for (int i = data.Length - 1; i >= 0; i--) {
                uint dtmp = d << 8;
                d = dtmp | data[i];
            }
            return d;
        }

        string EncodeToRGB(List<int> rgbList) {
            int res1 = 0;
            for (int i = 0; i < rgbList.Count; i++) res1 = (res1 << 8) | rgbList[i];
            return res1.ToString("X");
        }

        string EncodeToRGBFromImage(Bitmap inp, int x, int y) {
            Color pixelcolor = inp.GetPixel(x, y%inp.Height);
            return EncodeToRGB(new List<int>{pixelcolor.R, pixelcolor.G, pixelcolor.B});
        }
    }
}