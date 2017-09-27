using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using LeveGen.Models;
using System.Globalization;

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
        public static void Generate(LeveDatabase db, ObservableCollection<Leve> currentOrder, bool ContinueOnLevel, bool GenerateLisbeth, Stream savestrem)
        {
            using (var sw = new StreamWriter(savestrem))
            {
                sw.WriteLine(Header);

                if (GenerateLisbeth)
                {
                    sw.WriteLine(WriteLisbeth(db, currentOrder));
                }

                foreach (var x in currentOrder.OrderBy(i => i.Level))
                {
                    sw.WriteLine(WriteOrder(db, x, ContinueOnLevel));
                }

                sw.WriteLine(Footer);
            }
        }

        private static string WriteLisbeth(LeveDatabase db, ObservableCollection<Leve> currentOrder) {
            var lisbethOrder = $@"
        <Lisbeth Json=""[";

            foreach (var leve in currentOrder.OrderBy(i => i.Level))
            {
                var amount = (leve.Repeats > 0) ? leve.NumItems * (leve.Repeats + 1) : leve.NumItems;

                lisbethOrder += $@"
            {{'Item': {leve.ItemId},
               'Group': 0,
               'Amount': {amount},
               'Collectable': false,
               'QuickSynth': false,
               'SuborderQuickSynth': false,
               'Hq': false,
               'Food': 0,
               'Primary': true,
               'Type': '{leve.Classes}',
               'Enabled': true,
               'Manual': 0,
               'Medicine': 0}},";
            }

            lisbethOrder += $@"
            ]""
        />";

            return lisbethOrder;
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
            //force a us culture on the rest of the world because we don't need commas where we expect decimals...
            return val.ToString("G", CultureInfo.CreateSpecificCulture("en-US"));
        }
    }
}
