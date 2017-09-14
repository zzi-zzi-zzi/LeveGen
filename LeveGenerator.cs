using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using LeveGen.Models;

namespace LeveGen
{
    public class LeveGenerator
    {
        private static string Header = @"<?xml version=""1.0"" encoding=""utf-8""?>"+ "\n" +
            "<Profile>\n"+
            "\t<Name>Levequests</Name>\n"+
            "\t<KillRadius>50</KillRadius>\n"+
            "\t<Order>";

        private static string Footer = "\t</Order>\n</Profile>";

        /// <summary>
        /// generate our XML file
        /// </summary>
        /// <param name="db"></param>
        /// <param name="currentOrder"></param>
        /// <param name="ContinueOnLevel"></param>
        /// <param name="savestrem"></param>
        public static void Generate(LeveDatabase db, ObservableCollection<Leve> currentOrder, bool ContinueOnLevel, Stream savestrem)
        {
            using (var sw = new StreamWriter(savestrem))
            {
                sw.WriteLine(Header);

                foreach (var x in currentOrder.OrderBy(i => i.Level))
                {
                    sw.WriteLine(WriteOrder(db, x, ContinueOnLevel));
                }

                sw.WriteLine(Footer);
            }
        }

        private static string WriteOrder(LeveDatabase db, Leve leve, bool continueOnLevel)
        {
            var pickup = db.Npcs.First(i => i.NpcId == leve.PickUpNpc);
            var pickuploc = $"{formatFloat(pickup.Pos.X)},{formatFloat(pickup.Pos.Y)},{formatFloat(pickup.Pos.Z)}";
            var turnin = db.Npcs.First(i => i.NpcId == leve.TurnInNpc);
            var turninloc = $"{formatFloat(turnin.Pos.X)},{formatFloat(turnin.Pos.Y)},{formatFloat(turnin.Pos.Z)}";
            var col = (continueOnLevel) ? " and Core.Player.ClassLevel &lt; " + (leve.Level >=50 ? leve.Level + 2 : leve.Level + 5) : "";
            return $@"
        <While condition=""ItemCount({leve.ItemId}) &gt; {leve.NumItems - 1}{col} and  Core.Player.ClassLevel &gt;= {leve.Level}"">
            <If Condition=""not IsOnMap({pickup.MapId})"">
                <GetTo ZoneId=""{pickup.MapId}"" XYZ=""{pickuploc}"" />
            </If>
            <ExPickupGuildLeve leveIds=""{leve.LeveId}"" leveType=""Tradecraft"" npcId=""{pickup.NpcId}"" npcLocation=""{pickuploc}"" />
            <If Condition=""not IsOnMap({turnin.MapId})"">
                <GetTo ZoneId=""{turnin.MapId}"" XYZ=""{turninloc}"" />
            </If>
            <ExTurnInGuildLeve npcId=""{turnin.NpcId}"" npcLocation=""{turninloc}"" />
        </While>"
                ;

        }
        
        private static string formatFloat(float val)
        {
            return val.ToString("G",  CultureInfo.CurrentCulture);
        }
    }
}
