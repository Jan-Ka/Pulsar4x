﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Pulsar4X.ECSLib
{
    public delegate void DateChangedEventHandler(DateTime newDate);

    [JsonObject(MemberSerialization.OptIn)]
    public class TimeLoop : IEquatable<TimeLoop>
    {
        [JsonProperty]
        private SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>> EntityDictionary = new SortedDictionary<DateTime, Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>>();

        private Stopwatch _stopwatch = new Stopwatch();
        Stopwatch _subpulseStopwatch = new Stopwatch();
        private Timer _timer = new Timer();

        //changes how often the tick happens
        public float TimeMultiplier
        {
            get {return _timeMultiplier;}
            set
            {
                _timeMultiplier = value;
                _timer.Interval = _tickInterval.TotalMilliseconds * value;
            }
        } 
        private float _timeMultiplier = 1f;

        private TimeSpan _tickInterval = TimeSpan.FromMilliseconds(250);
        public TimeSpan TickFrequency { get { return _tickInterval; } set { _tickInterval = value;
            _timer.Interval = _tickInterval.TotalMilliseconds * _timeMultiplier;
        } }


        public TimeSpan Ticklength { get; set; } = TimeSpan.FromSeconds(3600);

        private bool _isProcessing = false;
        private bool _isOvertime = false;
        private Game _game;
        /// <summary>
        /// length of time it took to process the last DoProcess
        /// </summary>
        public TimeSpan LastProcessingTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan LastSubtickTime { get; private set; } = TimeSpan.Zero;
        /// <summary>
        /// This invokes the DateChangedEvent.
        /// </summary>
        /// <param name="state"></param>
        private void InvokeDateChange(object state)
        {
            Event logevent = new Event(GameGlobalDateTime, "Game Global Date Changed", null, null, null);
            logevent.EventType = EventType.GlobalDateChange;
            StaticRefLib.EventLog.AddEvent(logevent);

            GameGlobalDateChangedEvent?.Invoke(GameGlobalDateTime);
        }
        [JsonProperty]
        private DateTime _gameGlobalDateTime;
        public DateTime GameGlobalDateTime
        {
            get { return _gameGlobalDateTime; }
            internal set
            {
                _gameGlobalDateTime = value;
                if (StaticRefLib.SyncContext != null)
                    StaticRefLib.SyncContext.Post(InvokeDateChange, value); //marshal to the main (UI) thread, so the event is invoked on that thread.
                else
                    InvokeDateChange(value);//if context is null, we're probibly running tests or headless. in this case we're not going to marshal this.    
            }
        }
        /// <summary>
        /// Fired when the game date is incremented. 
        /// All systems are in sync at this event.
        /// </summary>
        public event DateChangedEventHandler GameGlobalDateChangedEvent;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game"></param>
        internal TimeLoop(Game game)
        {
            
            _game = game;
            _timer.Interval = _tickInterval.TotalMilliseconds;
            _timer.Enabled = false;
            _timer.Elapsed += Timer_Elapsed;
            
        }

        #region Public Time Methods. UI interacts with time here

        /// <summary>
        /// Pauses the timeloop
        /// </summary>
        public void PauseTime()
        {
            _timer.Stop();
        }
        /// <summary>
        /// Starts the timeloop
        /// </summary>
        public void StartTime()
        {
            _timer.Start();
        }


        /// <summary>
        /// Takes a single step in time
        /// </summary>
        public void TimeStep()
        {
            DoProcessing(GameGlobalDateTime + Ticklength);
            _timer.Stop();
        }

        /// <summary>
        /// Takes a single step in time
        /// </summary>
        public void TimeStep(DateTime toDate)
        {
            DoProcessing(toDate);
            _timer.Stop();
        }

        #endregion


        /// <summary>
        /// Adds an interupt where systems are interacting (ie an entity jumping between systems)
        /// this forces all systems to synch at this datetime.
        /// </summary>
        /// <param name="datetime"></param>
        /// <param name="action"></param>
        /// <param name="jumpPair"></param>
        internal void AddSystemInteractionInterupt(DateTime datetime, PulseActionEnum action, SystemEntityJumpPair jumpPair)
        {
            if (!EntityDictionary.ContainsKey(datetime))
                EntityDictionary.Add(datetime, new Dictionary<PulseActionEnum, List<SystemEntityJumpPair>>());
            if (!EntityDictionary[datetime].ContainsKey(action))
                EntityDictionary[datetime].Add(action, new List<SystemEntityJumpPair>());
            EntityDictionary[datetime][action].Add(jumpPair);
        }

        internal void AddHaltingInterupt(DateTime datetime)
        {
            throw new NotImplementedException();
        }


        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isProcessing)
            {   

                DoProcessing(GameGlobalDateTime + Ticklength); //run DoProcessing if we're not already processing
            }
            else
            {
                _isOvertime = true; //if we're processing, then processing it taking longer than the sim speed
            }
        }



        private void DoProcessing(DateTime targetDateTime)
        {
            _isProcessing = true;
            _timer.Stop();
            _timer.Start(); //reset timer
            _stopwatch.Start(); //start the processor loop stopwatch
            _isOvertime = false;

            //check for global interupts
            //_targetDateTime = GameGlobalDateTime + Ticklength;

         
            while (GameGlobalDateTime < targetDateTime)
            {
                _subpulseStopwatch.Start();
                DateTime nextInterupt = ProcessNextInterupt(targetDateTime);
                //do system processors
              
                if (_game.Settings.EnableMultiThreading == true) //threaded
                    Parallel.ForEach<StarSystem>(_game.Systems.Values, starSys => starSys.ManagerSubpulses.ProcessSystem(nextInterupt));
                //The above 'blocks' till all the tasks are done.
                else //non threaded
                {
                    foreach (StarSystem starSys in _game.Systems.Values)
                    {
                        starSys.ManagerSubpulses.ProcessSystem(nextInterupt);
                    }
                }
                LastSubtickTime = _subpulseStopwatch.Elapsed;
                GameGlobalDateTime = nextInterupt; //set the GlobalDateTime this will invoke the datechange event.
                _subpulseStopwatch.Reset();
            }

            LastProcessingTime = _stopwatch.Elapsed; //how long the processing took
            _stopwatch.Reset();

            _isProcessing = false;
        }

        private DateTime ProcessNextInterupt(DateTime maxDateTime)
        {
            DateTime processedTo;
            DateTime nextInteruptDateTime;
            if (EntityDictionary.Keys.Count != 0)
            {
                nextInteruptDateTime = EntityDictionary.Keys.Min();
                if (nextInteruptDateTime <= maxDateTime)
                {
                    foreach (var delegateListPair in EntityDictionary[nextInteruptDateTime])
                    {
                        foreach (var jumpPair in delegateListPair.Value) //foreach entity in the value list
                        {
                            //delegateListPair.Key.DynamicInvoke(_game, jumpPair);
                            PulseActionDictionary.DoAction(delegateListPair.Key, _game, jumpPair);
                        }

                    }
                    processedTo = nextInteruptDateTime;
                }
                else
                    processedTo = maxDateTime;
            }
            else
                processedTo = maxDateTime;

            return processedTo;
        }



        public bool Equals(TimeLoop other)
        {
            bool equality = false;
            if (GameGlobalDateTime.Equals(other.GameGlobalDateTime))
            {
                if (EntityDictionary.Count.Equals(other.EntityDictionary.Count))
                    equality = true;
            }
            return equality;
        }
    }


}
