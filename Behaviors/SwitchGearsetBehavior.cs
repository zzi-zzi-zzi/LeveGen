namespace LeveGen.Behaviors
{
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Buddy.Coroutines;
    using Clio.XmlEngine;
    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Managers;
    using ff14bot.NeoProfiles;
    using TreeSharp;

    [XmlElement("LgSwitchGearset")]
    // ReSharper disable once UnusedMember.Global
    public class SwitchGearsetBehavior : ProfileBehavior
    {
        #region Private Variables
        private bool _isDone;
        private GearSet _targetGearset;
        private int _tries;
        #endregion

        #region XML Attributes
        [DefaultValue(0)]
        [XmlAttribute("Gearset")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private int TargetGearset { get; set; }

        [DefaultValue(ClassJobType.Adventurer)]
        [XmlAttribute("Job")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private ClassJobType TargetJob { get; set; }
        #endregion

        #region Overrides
        public override bool IsDone => _isDone;

        protected override void OnStart()
        {
            ResetBehavior();
        }

        protected override void OnResetCachedDone()
        {
            ResetBehavior();
        }

        protected override void OnDone()
        {
            if (HasCorrectGearset)
                Log("Successfully switched to gearset {0}.", _targetGearset.Index);
            else
                LogSoftError("Behavior ended without being able to change gearset.");
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => SwitchGearset());
        }
        #endregion

        /// <summary>
        /// Parses the given XML attributes. Validates and/or determines the target gearset and performs the actual
        /// gearset switching via RB's GearsetManager.
        /// </summary>
        /// <returns>Always true</returns>
        private async Task<bool> SwitchGearset()
        {
            if (Core.Player.InCombat || Core.Player.IsDead || Core.Player.IsDying || CommonBehaviors.IsLoading || _isDone)
                return true;

            var targetGearsetIndex = GetTargetGearsetIndex();
            if (targetGearsetIndex == 0)
            {
                LogSoftError("Unable to switch gearset: Could not determine target gearset.");
                return _isDone = true;
            }

            _targetGearset = GearsetManager.GearSets.First(g => g.Index == targetGearsetIndex);

            while (!HasCorrectGearset && _tries++ < 5)
            {
                Log("Switching to gearset {0} ({1})... (Attempt {2} of {3})", _targetGearset.Index, _targetGearset.Class, _tries, 5);
                _targetGearset.Activate();
                await Coroutine.Wait(5000, () => HasCorrectGearset);
            }

            return _isDone = true;
        }

        /// <summary>
        /// Determines the target gearset index for the given XML input parameters.
        /// </summary>
        /// <returns>Index of the gearset to use. Returns 0 if no viable choice was available.</returns>
        private int GetTargetGearsetIndex()
        {
            int targetGearsetIndex = 0;

            if (TargetJob != ClassJobType.Adventurer)
            {
                var jobGearset = GearsetManager.GearSets.FirstOrDefault(g => g.Class == TargetJob);

                if (jobGearset.Index > 0)
                    targetGearsetIndex = jobGearset.Index;
                else
                    LogSoftError("Cannot find a registered gearset for {0}.", TargetJob);
            }

            // If both Job and Gearset were specified, Gearset takes priority.
            if (TargetGearset > 0)
            {
                if (GearsetManager.GearSets.Any(g => g.Index == TargetGearset))
                    targetGearsetIndex = TargetGearset;
                else
                    LogSoftError("Specified gearset {0} is not available.", TargetGearset);
            }

            return targetGearsetIndex;
        }

        /// <summary>
        /// Returns whether or not the active gearset machtes the desired target gearset.
        /// </summary>
        private bool HasCorrectGearset => Core.Player.CurrentJob == _targetGearset.Class
                                          && GearsetManager.ActiveGearsetIndex == _targetGearset.Index;

        /// <summary>
        /// Resets private variables back to their desired default state.
        /// </summary>
        private void ResetBehavior()
        {
            _isDone = false;
            _tries = 0;
        }
    }
}