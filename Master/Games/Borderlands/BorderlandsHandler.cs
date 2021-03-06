﻿using Nucleus.Gaming;
using Nucleus.Gaming.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowScrape.Types;

namespace Games
{
    public class Borderlands_PlayerData
    {
        public Point Position;
        public HwndObject HWND;
        public Size Size;
        public bool Set;
    }
    public class BorderlandsHandler : IGameHandler
    {
        protected string executablePlace;
        protected List<PlayerInfo> playas;
        protected Dictionary<string, GameOption> options;
        protected string saveFile;
        protected int delayTime;
        protected int titleHeight;
        private bool end;
        private int delay;
        public int TimerInterval
        {
            get { return 33; }
        }

        public bool Initialize(string gameFilename, List<PlayerInfo> players, Dictionary<string, GameOption> options, List<System.Windows.Forms.Control> addSteps, int titleHeight)
        {
            this.executablePlace = gameFilename;
            this.playas = players;
            this.options = options;
            this.titleHeight = titleHeight - 5;

            delayTime = (int)options["delay"].Value * 1000;

            // Let's search for the save file
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string myGames = Path.Combine(documents, @"My Games\Borderlands\WillowGame\Config");
            string willowEngine = Path.Combine(myGames, "WillowEngine.ini");

            if (File.Exists(willowEngine))
            {
                saveFile = willowEngine;
            }
            else
            {
                MessageBox.Show("Could not find WillowEngine.ini file!");

                using (OpenFileDialog open = new OpenFileDialog())
                {
                    open.Filter = "WillowEngine.ini file|WillowEngine.ini";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        saveFile = open.FileName;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public string Play()
        {
            if (!SteamUtil.IsSteamRunning())
            {
                MessageBox.Show("If you own the Steam Version, please open Steam, then click OK");
            }

            IniFile file = new IniFile(saveFile);
            file.IniWriteValue("WillowGame.WillowGameEngine", "bPauseLostFocusWindowed", "FALSE");
            file.IniWriteValue("WillowGame.WillowGameEngine", "bMuteAudioWhenNotInFocus", "False");
            file.IniWriteValue("Engine.Engine", "bPauseOnLossOfFocus", "False");

            Screen[] all = Screen.AllScreens;
            Screen first = all[0];
            Rectangle fbounds = first.Bounds;

            // minimize everything
            User32.MinimizeEverything();

            object playerKey = options["keyboardPlayer"].Value;
            bool playerKeyboard = (bool)playerKey;

            for (int i = 0; i < playas.Count; i++)
            {
                PlayerInfo player = playas[i];
                // Set Borderlands 2 Resolution and stuff to run
                Screen screen = all[player.ScreenIndex];
                int width = 0;
                int height = 0;
                Rectangle bounds = screen.Bounds;

                Point location = new Point();

                ViewportUtil.GetPlayerViewport(player, titleHeight, out width, out height, out location);

                file.IniWriteValue("SystemSettings", "ResX", width.ToString(CultureInfo.InvariantCulture));
                file.IniWriteValue("SystemSettings", "ResY", height.ToString(CultureInfo.InvariantCulture));

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = executablePlace;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // NEW
                object option = options["saveid" + i].Value;
                int id = (int)option;


                if (playerKeyboard)
                {
                    startInfo.Arguments = "-windowed -AlwaysFocus -NoController -ControllerOffset=" + (i - 1).ToString(CultureInfo.InvariantCulture) + " -SaveDataId=" + id.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    startInfo.Arguments = "-windowed -AlwaysFocus -ControllerOffset=" + i.ToString(CultureInfo.InvariantCulture) + " -SaveDataId=" + id.ToString(CultureInfo.InvariantCulture);
                }


                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Path.GetDirectoryName(executablePlace);

                Process proc = Process.Start(startInfo);
                HwndObject hwnd = new HwndObject(proc.Handle);

                ScreenData data = new ScreenData();
                data.Position = location;
                data.HWND = hwnd;
                data.Size = new Size(width, height);
                player.Process = proc;
                player.Tag = data;

                Thread.Sleep(delayTime);
            }

            return string.Empty;
        }

        public void Update(int delayMS)
        {
            delay += delayMS;
            if (delay > this.delayTime)
            {
                int exited = 0;
                for (int i = 0; i < playas.Count; i++)
                {
                    PlayerInfo p = playas[i];
                    ScreenData data = (ScreenData)p.Tag;
                    if (!data.Set)
                    {
                        data.HWND = new HwndObject(p.Process.MainWindowHandle);
                        data.HWND.Location = data.Position;

                        User32.HideBorder(p.Process.MainWindowHandle);
                        data.Set = true;
                    }

                    if (p.Process.HasExited)
                    {
                        exited++;
                    }
                }

                if (exited == playas.Count)
                {
                    end = true;
                }
            }
        }

        public void End()
        {
        }

        public bool HideTaskBar
        {
            get { return true; }
        }
        public bool Ended
        {
            get { return false; }
        }
    }
}
