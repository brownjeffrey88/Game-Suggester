using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GSMVC.Models
{
    public class GameModel
    {
        public string Image { get; set; }
        public string Description { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public float Rating { get; set; }
        public int SuggestedPlayers { get; set; }
        public int MinPlayTime { get; set; }
        public int MaxPlayTime { get; set; }
        public string Designer { get; set; }
        public string Artist { get; set; }
        public string Publisher { get; set; }
        public float weight { get; set; }
        public int BggRank { get; set; }
        public bool Solo { get; set; }
        public bool Competitive { get; set; }
        public bool Cooperative { get; set; }
        public IEnumerable<String> Mechanics { get; set; }
    }
}
