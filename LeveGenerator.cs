using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeveGen.Models;
using System.Globalization;
using ff14bot;
using ff14bot.Enums;

namespace LeveGen
{
    public class LeveGenerator
    {
        private const string Header =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<Profile>\n"+
            "\t<Name>Levequests</Name>\n"+
            "\t<KillRadius>50</KillRadius>\n"+
            "\t<Order>";

        private const string Footer =
            "\t</Order>\n" +
             "</Profile>";

        /// <summary>
        /// Generate our XML file
        /// </summary>
        /// <param name="db"></param>
        /// <param name="currentOrder"></param>
        /// <param name="ContinueOnLevel"></param>
        /// <param name="HqOnly"></param>
        /// <param name="GenerateLisbeth"></param>
        /// <param name="savestrem"></param>
        public static void Generate(LeveDatabase db, ObservableCollection<Leve> currentOrder, bool ContinueOnLevel, bool HqOnly, bool GenerateLisbeth, Stream savestrem)
        {
            using (var sw = new StreamWriter(savestrem))
            {
                sw.WriteLine(Header);

                foreach (var x in currentOrder.OrderBy(i => i.Level))
                {
                    sw.WriteLine(WriteOrder(db, x, ContinueOnLevel, HqOnly, GenerateLisbeth));
                }

                sw.WriteLine(Footer);
            }
        }

        private static string WriteLisbethSubOrder(Leve leve, int numLeves)
        {
            var amount = (leve.Repeats > 0) ? leve.NumItems * (leve.Repeats + 1) : leve.NumItems;
            if (numLeves > 1)
            {
                amount *= numLeves;
            }

            return $@"
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

        private static string WriteLisbeth(LeveDatabase db, Leve leve, int numLeves)
        {
            return $@"
        <Lisbeth Json=""[{WriteLisbethSubOrder(leve, numLeves)}]"" />";
        }

        private static string WriteOrder(LeveDatabase db, Leve leve, bool continueOnLevel, bool hqOnly, bool generateLisbeth)
        {
            var pickup = db.Npcs.First(i => i.NpcId == leve.PickUpNpc);
            var pickuploc = $"{formatFloat(pickup.Pos.X)},{formatFloat(pickup.Pos.Y)},{formatFloat(pickup.Pos.Z)}";
            var turnin = db.Npcs.First(i => i.NpcId == leve.TurnInNpc);
            var turninloc = $"{formatFloat(turnin.Pos.X)},{formatFloat(turnin.Pos.Y)},{formatFloat(turnin.Pos.Z)}";
            var itemcount = hqOnly ? "HqItemCount" : "ItemCount";
            var hqonlyattrib = hqOnly ? @"HqOnly=""true""" : string.Empty;

            ClassJobType leveClass;
            ClassJobType.TryParse(leve.Classes, out leveClass);
            var col = (continueOnLevel) ? $" and Core.Me.Levels[ClassJobType.{leveClass}] &lt; " + (leve.Level >=50 ? leve.Level + 2 : leve.Level + 5) : "";
            // Default to 5 leves (5 items for single turnins, 15 items for triple turnins)
            int numLeves = 5;
            // ExpReward * 2.0 is assuming all of the items are HQ'd and .5 for the exp crafting suborders.
            var rewardModifier = 2.5;

            var output = "";
#if RB_CN
            var LeveTag = "YesText=\"继续交货\""; //
#else
            var LeveTag = @"";
#endif

            var outputTurnin = $@"
        <LgSwitchGearset Job=""{leve.Classes}"" />
        <While Condition=""ExBuddy.Windows.GuildLeve.Allowances &gt; 0 and {itemcount}({leve.ItemId}) &gt; {leve.NumItems - 1}{col} and Core.Me.Levels[ClassJobType.{leveClass}] &gt;= {leve.Level}"">
            <If Condition=""not IsOnMap({pickup.MapId})"">
                <GetTo ZoneId=""{pickup.MapId}"" XYZ=""{pickuploc}"" />
            </If>
            <ExPickupGuildLeve LeveIds=""{leve.LeveId}"" LeveType=""{Localization.Localization.Tradecraft}"" NpcId=""{pickup.NpcId}"" NpcLocation=""{pickuploc}"" Timeout=""5"" />
            <If Condition=""not IsOnMap({turnin.MapId})"">
                <GetTo ZoneId=""{turnin.MapId}"" XYZ=""{turninloc}"" />
            </If>
            <ExTurnInGuildLeve NpcId=""{turnin.NpcId}"" NpcLocation=""{turninloc}"" {hqonlyattrib} {LeveTag} />
        </While>";

            if (generateLisbeth)
            {
                // Optimize EXP is checked, figure out optimal orders to craft.
                if (continueOnLevel)
                {
                    var currentLevel = Core.Me.Levels[leveClass];
                    var nextLeveJump = leve.Level >= 50 ? 2 : 5;
                    var nextLeveLevel = leve.Level + nextLeveJump;
                    var requiredExp = 0;

                    for (var i=0; i < (nextLeveLevel - currentLevel); i++)
                    {
                        requiredExp += ExpRequired[currentLevel+i];
                    }

                    numLeves = (int)(requiredExp / (leve.ExpReward * rewardModifier));

                    output += $@"
        <While Condition=""Core.Me.Levels[ClassJobType.{leveClass}] &gt;= {leve.Level} and Core.Me.Levels[ClassJobType.{leveClass}] &lt; {nextLeveLevel}"">";
                }
                output += $@"
        <If Condition=""ItemCount({leve.ItemId}) &lt; {leve.NumItems}"">
            {WriteLisbeth(db, leve, numLeves)}
        </If>
        {outputTurnin}
        ";
                if (continueOnLevel)
                {
                    output += $@"
        </While>";
                }
            }
            else
            {
                output += outputTurnin;
            }

            return output;

        }

        private static string formatFloat(float val)
        {
            //force a us culture on the rest of the world because we don't need commas where we expect decimals...
            return val.ToString("G", CultureInfo.CreateSpecificCulture("en-US"));
        }

        private static Dictionary<int, int> ExpRequired = new Dictionary<int, int>()
        {
            {1,300},
            {2,600},
            {3,1100},
            {4,1700},
            {5,2300},
            {6,4200},
            {7,6000},
            {8,7350},
            {9,9930},
            {10,11800},
            {11,15600},
            {12,19600},
            {13,23700},
            {14,26400},
            {15,30500},
            {16,35400},
            {17,40500},
            {18,45700},
            {19,51000},
            {20,56600},
            {21,63900},
            {22,71400},
            {23,79100},
            {24,87100},
            {25,95200},
            {26,109800},
            {27,124800},
            {28,140200},
            {29,155900},
            {30,162500},
            {31,175900},
            {32,189600},
            {33,203500},
            {34,217900},
            {35,232320},
            {36,249900},
            {37,267800},
            {38,286200},
            {39,304900},
            {40,324000},
            {41,340200},
            {42,356800},
            {43,373700},
            {44,390800},
            {45,408200},
            {46,437600},
            {47,467500},
            {48,498000},
            {49,529000},
            {50,864000},
            {51,1058400},
            {52,1267200},
            {53,1555200},
            {54,1872000},
            {55,2217600},
            {56,2592000},
            {57,2995200},
            {58,3427200},
            {59,3888000},
            {60,4470000},
            {61,4873000},
            {62,5316000},
            {63,5809000},
            {64,6364000},
            {65,6995000},
            {66,7722000},
            {67,8575000},
            {68,9593000},
            {69,10826000}
        };
    }
}
