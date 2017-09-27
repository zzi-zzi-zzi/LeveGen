using System;
using System.Collections.Generic;
using Clio.Utilities;

namespace LeveGen.Models
{
    public class LeveDatabase
    {
        public List<Leve> Leves;
        public List<LeveNpc> Npcs;
        public Dictionary<string, int> ExperienceRequired;
    }

    public class Leve
    {
        public int LeveId { get; set; }
        public string Name { get; set; }
        public string Classes { get; set; }
        public int Level { get; set; } 
        public int PickUpNpc { get; set; }
        public int TurnInNpc { get; set; }
        public int NumItems { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Repeats { get; set; }
        public int ExpReward { get; set; }
    }

    public class LeveNpc
    {
        public int NpcId { get; set; }
        public Vector3 Pos { get; set; }
        public int MapId { get; set; }
        public string LocationName { get; set; }
    }
}