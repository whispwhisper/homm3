﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace Heroes3Editor.Models
{
    public class Game
    {
        public byte[] Bytes { get; }

        public IList<Hero> Heroes { get; } = new List<Hero>();

        // CGM is supposed to be a GZip file, but GZipStream from .NET library throws a
        // "unsupported compression method" exception, which is why we use SharpZipLib.
        // Also CGM has incorrect CRC which every tool/library complains about.
        public Game(string file)
        {
            using var fileStream = (new FileInfo(file)).OpenRead();
            using var gzipStream = new GZipInputStream(fileStream);
            using var memoryStream = new MemoryStream();
            try
            {
                gzipStream.CopyTo(memoryStream);
            }
            catch (GZipException e) { /* Ignore the CRC error */ }

            Bytes = memoryStream.ToArray();

            AddCampaignHeroes();
        }

        public void Save(string file)
        {
            using var fileStream = (new FileInfo(file)).OpenWrite();
            using var gzipStream = new GZipOutputStream(fileStream);
            using var memoryStream = new MemoryStream(Bytes);
            memoryStream.CopyTo(gzipStream);
        }

        public void AddCampaignHeroes()
        {
            foreach (var hero in Constants.CampaignHeroes)
                AddHero(hero);
        }

        public bool AddHero(string name)
        {
            foreach (var hero in Heroes)
                if (hero.Name == name) return false;

            byte[] pattern = new byte[13];
            Encoding.ASCII.GetBytes(name).CopyTo(pattern, 0);
            for (int i = Bytes.Length - 13; i > 0; --i)
            {
                bool found = true;
                for (int j = 0; j < 13; ++j)
                    if (Bytes[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                if (found)
                {
                    Heroes.Add(new Hero(this, name, i));
                    return true;
                }
            }
            return false;
        }
    }

    public class Hero
    {
        private Game _game;

        public string Name { get; }

        public int BytePosition { get; }

        public int NumOfSkills { get; }
        public string[] Skills = new string[8];

        public ISet<string> Spells { get; } = new HashSet<string>();

        public Hero(Game game, string name, int bytePosition)
        {
            _game = game;
            Name = name;
            BytePosition = bytePosition;

            NumOfSkills = _game.Bytes[BytePosition + Constants.Offsets["NumOfSkils"]];
            for (int i = 0; i < 28; ++i)
            {
                var skillSlotIndex = _game.Bytes[BytePosition + Constants.Offsets["SkillSlots"] + i];
                if (skillSlotIndex != 0)
                    Skills[skillSlotIndex-1] = Constants.Skills[i];
            }

            for (int i = 0; i < 70; ++i)
            {
                if (_game.Bytes[BytePosition + Constants.Offsets["Spells"] + i] == 1)
                    Spells.Add(Constants.Spells[i]);
            }
        }
    }
}