using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MIDIModificationFramework;
using MIDIModificationFramework.MIDIEvents;
using System.Runtime.InteropServices;
using Pastel;

using DiscordRPC;

namespace UniMIDI {
    public class Program {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        static void Main(string[] args) {
            DiscordRpcClient client = new DiscordRpcClient("869625346155249664");
            client.OnReady += (sender, e) => {
                //Console.WriteLine("Recieved Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) => {
                //Console.WriteLine("Received Update! {0}", e.Presence);
            };

            client.Initialize();

            var timer = new Stopwatch();
            timer.Start();

            client.SetPresence(new RichPresence() {
                Details = "Playing a MIDI file",
                State = Path.GetFileName(args[0]),
                Timestamps = new Timestamps(DateTime.UtcNow)
            });
            
            if (!File.Exists("./channelColorConfig.txt") {
                using (FileStream fs = File.Create("channelColorConfig.txt") {
                    Byte[] defaultColors = new UTF8Encoding(true).GetBytes(@"255 0 0
    255 128 0
    255 255 0
    0 255 0
    0 255 255
    0 0 255
    128 0 255
    255 0 255
    ");
                    fs.Write(defaultColors, 0, defaultColors.length);
                }
            }

            try {
                double playbackSpeed = 1;
                MIDIPlayer player = new MIDIPlayer(args[0], 0, 128);
                int colorMethod = 0;

                if (args.Contains("-playbackSpeed"))
                    playbackSpeed = Convert.ToDouble(args[args.ToList().FindIndex(ps => ps.Contains("-playbackSpeed"))+1]);

                if (args.Contains("-use256Keys"))
                    player.LastKey = 256;
                else
                    player.LastKey = 128;

                if (args.Contains("-cm"))
                    colorMethod = Convert.ToInt32(args[args.ToList().FindIndex(ps => ps.Contains("-cm"))+1]);

                
                new Thread(() => player.PlayAudio(playbackSpeed)).Start();
                new Thread(() => player.PlayVisuals(colorMethod, noColor:(args.Contains("-noColor") || args.Contains("-nocolor")), playbackSpeed:playbackSpeed)).Start();
            } catch (IndexOutOfRangeException) {
                Console.WriteLine((args.Length == 0 ? "No arguments specified" : @"Usage:
[]: Required
<>: Optional
UniMIDI.exe [/path/to/midifile.mid] <arg>

Available Arguments:
-use256Keys
-playbackSpeed [Speed]
-cm [Color Method]
    0: Colors notes by channel
    1: Colors notes by track/channel (Recommended for MIDIs converted from images, chance of lag if used on Black MIDIs)
    2: Colors notes by track
"));
            } catch (FileNotFoundException) {
                Console.WriteLine("Invalid file directory, or file does not exist.");
            }
        }
    }
}
