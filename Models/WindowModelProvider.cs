using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using System.Windows.Input;
using Buddy.Overlay.Commands;
using Clio.Utilities;
using LeveGen.Utils;
using System.Windows.Forms;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Managers;
using ff14bot.NeoProfiles;

namespace LeveGen.Models
{
    public class WindowModelProvider : INotifyPropertyChanged
    {
        private LeveDatabase _database;
        public WindowModelProvider(LeveDatabase database)
        {
            _database = database;
            CurrentOrder = new ObservableCollection<Leve>();
            Leves = new ObservableCollection<Leve>(_database.Leves);

            FilteredLeves = CollectionViewSource.GetDefaultView(Leves);
            FilteredLeves.Filter = i => Filter((Leve)i);

            PropertyChanged += (obj, sender) =>
            {
                if (sender.PropertyName == "Search")
                {
                    FilteredLeves.Refresh();
                    OnPropertyChanged("FilteredLeves");
                }
            };
            ContinueOnLevel = true;
            GenerateLisbeth = false;
        }

        #region Commands

        /// <summary>
        /// starts the bot with our selected leves
        /// </summary>
        public ICommand Start
        {
            get
            {
                return new RelayCommand(async s =>
                {
                    if (TreeRoot.IsRunning)
                    {
                        await TreeRoot.StopGently("Switching to Order bot for LeveGen");
                    }
                    if (BotManager.Current.EnglishName != "Order Bot")
                    {
                        BotManager.SetCurrent(BotManager.Bots.First(i => i.EnglishName == "Order Bot"));
                    }
                    var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", "LeveGen");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    var file = Path.Combine(dir, "LastRun.xml");
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch (Exception)
                    {
                        Logger.Warn("we failed to delete the last profile. generating a new one at random");
                        file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", "LeveGen", $"{Path.GetRandomFileName()}.xml");
                    }

                    using (var stream = File.OpenWrite(file))
                    {
                        LeveGenerator.Generate(_database, CurrentOrder, ContinueOnLevel, TurninHqOnly, GenerateLisbeth, stream);
                    }

                    try
                    {
                        NeoProfileManager.Load(file);
                        Thread.Sleep(500);
                        TreeRoot.Start();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("Failed to load the profile " + file);
                        Logger.Warn("Error Starting Profile : " + ex.Message);
                    }
                });
            }
        }

        public ICommand Save
        {
            get
            {
                return new RelayCommand(s =>
                {
                    if (CurrentOrder == null || !CurrentOrder.Any())
                        return;

                    var sr = new SaveFileDialog();

                    sr.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";

                    if (sr.ShowDialog() == DialogResult.OK)
                    {
                        using (var savestrem = sr.OpenFile())
                        {
                            LeveGenerator.Generate(_database, CurrentOrder, ContinueOnLevel, TurninHqOnly, GenerateLisbeth, savestrem);
                        }
                    }


                });
            }
        }

        /// <summary>
        /// adds the currently selected profile to the list
        /// </summary>
        public ICommand Add
        {
            get
            {
                return new RelayCommand(s =>
                {
                    CurrentOrder.Add(SelectedRow);
                });
            }
        }

        /// <summary>
        /// remove the selected item from the list
        /// </summary>
        public ICommand Remove
        {
            get
            {
                return new RelayCommand(s =>
                {
                    CurrentOrder.RemoveAt(CurrentOrderIndex);
                });
            }
        }

        /// <summary>
        /// clear the selected leve queue
        /// </summary>
        public ICommand Clear
        {
            get
            {
                return new RelayCommand(s =>
                {
                    CurrentOrder.Clear();
                });
            }
        }

        #endregion

        #region Properties

        private int _CurrentOrderIndex;
        public int CurrentOrderIndex
        {
            get { return _CurrentOrderIndex; }
            set
            {
                _CurrentOrderIndex = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView FilteredLeves { get; set; }


        private bool FilterMatches(string term)
        {
            return term.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool Filter(Leve i)
        {
            if (i?.Name == null || i.ItemName == null)
                return true;
            if(Search == null)
                return validateToggle(i);
            return (FilterMatches(i.Name) || FilterMatches(i.ItemName)) && validateToggle(i);
        }
        private ObservableCollection<Leve> _CurrentOrder = new ObservableCollection<Leve>();
        public ObservableCollection<Leve> CurrentOrder
        {
            get { return _CurrentOrder; }
            set
            {
                _CurrentOrder = value;
                OnPropertyChanged();
                CurrentOrderIndex = 0;
            }
        }

        private ObservableCollection<Leve> _obser;
        public ObservableCollection<Leve> Leves
        {
            get { return _obser; }
            set
            {
                _obser = value;
                OnPropertyChanged();
            }
        }


        private Leve _selected;
        public Leve SelectedRow
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }

        private string _search;
        public string Search
        {
            get { return _search; }
            set
            {
                _search = value;
                OnPropertyChanged();
            }
        }

        private bool _TurnInHqOnly;

        public bool TurninHqOnly
        {
            get { return _TurnInHqOnly; }
            set
            {
                _TurnInHqOnly = value;
                OnPropertyChanged();
            }
        }

        private bool _ContinueOnLevel;

        public bool ContinueOnLevel
        {
            get { return _ContinueOnLevel; }
            set
            {
                _ContinueOnLevel = value;
                OnPropertyChanged();
            }
        }

        private bool _GenerateLisbeth;

        public bool GenerateLisbeth
        {
            get { return _GenerateLisbeth; }
            set
            {
                _GenerateLisbeth = value;
                OnPropertyChanged();
            }
        }

        #region button toggles

        /// <summary>
        /// checks a leave class against the toggles
        /// </summary>
        /// <param name="leve"></param>
        /// <returns></returns>
        private bool validateToggle(Leve leve)
        {
            if (!CRP && !BSM && !ARM && !GSM && !LTW && !WVR && !ALC && !CUL)
                return true;
            if (CRP && leve.Classes.Contains("Carpenter"))
                return true;
            if (BSM && leve.Classes.Contains("Blacksmith"))
                return true;
            if (ARM && leve.Classes.Contains("Armorer"))
                return true;
            if (GSM && leve.Classes.Contains("Goldsmith"))
                return true;
            if (LTW && leve.Classes.Contains("Leatherworker"))
                return true;
            if (WVR && leve.Classes.Contains("Weaver"))
                return true;
            if (ALC && leve.Classes.Contains("Alchemist"))
                return true;
            if (CUL && leve.Classes.Contains("Culinarian"))
                return true;
            return false;
        }

        private bool _crp;
        private bool _bsm;
        private bool _arm;
        private bool _gsm;
        private bool _ltw;
        private bool _wvr;
        private bool _alc;
        private bool _cul;

        public bool CRP { get { return _crp; } set { _crp = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool BSM { get { return _bsm; } set { _bsm = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool ARM { get { return _arm; } set { _arm = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool GSM { get { return _gsm; } set { _gsm = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool LTW { get { return _ltw; } set { _ltw = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool WVR { get { return _wvr; } set { _wvr = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool ALC { get { return _alc; } set { _alc = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }
        public bool CUL { get { return _cul; } set { _cul = value; OnPropertyChanged(); OnPropertyChanged("Search"); } }

        #endregion


        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}