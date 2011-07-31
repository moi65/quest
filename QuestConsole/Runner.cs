﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AxeSoftware.Quest;

namespace QuestConsole
{
    public class Runner
    {
        private string m_filename;
        private ConsolePlayer m_player;
        private PlayerHelper m_helper;

        public Runner(string filename)
        {
            m_filename = filename;
        }

        public void Start()
        {
            IASL game = GameLauncher.GetGame(m_filename, null);

            IASLTimer gameTimer = game as IASLTimer;
            if (gameTimer != null)
            {
                gameTimer.RequestNextTimerTick += game_RequestNextTimerTick;
            }

            m_player = new ConsolePlayer();
            m_helper = new PlayerHelper(game, m_player);

            m_player.Output += player_Output;
            List<string> errors = new List<string>();
            if (!m_helper.Initialise(m_player, out errors))
            {
                Console.WriteLine("Failed to load game");
                foreach (string error in errors)
                {
                    Console.WriteLine(error);
                }
                Console.ReadKey();
            }
            else
            {
                game.Begin();
                PlayGame();
            }
        }

        private void game_RequestNextTimerTick(int obj)
        {
            //Console.WriteLine("<timers not implemented>");
        }

        private void PlayGame()
        {
            m_player.OutputText(m_helper.ClearBuffer());
            string input = Console.ReadLine();
        }

        private void player_Output(string text)
        {
            Console.Write(text);
        }
    }
}