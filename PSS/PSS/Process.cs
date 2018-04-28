﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSS
{
    /// <summary>
    /// Process simulation
    /// </summary>
    public class Process
    {
        public enum State { RUNNING, BLOCKED, ENDED };

        /// <summary>
        /// Name of the process
        /// </summary>
        private string name;
        /// <summary>
        /// Probability of an I/O request
        /// </summary>
        private double ioProbability;
        /// <summary>
        /// Speed of the most common I/O operation. 
        /// </summary>
        private IO.Speed ioSwiftness;

        /// <summary>
        /// Length of the process (tick)
        /// </summary>
        private int length;

        /// <summary>
        /// Remaining time of the process (tick)
        /// </summary>
        private int remainingTick;

        /// <summary>
        /// Current I/O Operation (nullable)
        /// </summary>
        private IO currentIO;

        /// <summary>
        /// Process constructor
        /// </summary>
        /// <param name="id">ID of process</param>
        /// <param name="name">Name of the rpocess</param>
        /// <param name="ioProbability">I/O Probability of the process</param>
        /// <param name="length">Length of the process</param>
        public Process(string name, double ioProbability, IO.Speed ioSwiftness, int length)
        {
            if (name.Length < 2 || name.Length > 15)
            {
                throw new ArgumentException("Parameter must be between 2 and 15 character", "Process Name");
            }
            if (ioProbability < 0 || ioProbability > 1)
            {
                throw new ArgumentException("Parameter must be between 0 and 1", "I/O Probability");
            }
            if (length <= 0)
            {
                throw new ArgumentException("Parameter must be greater than 0", "Process Length");
            }
            this.name = name;
            this.ioSwiftness = ioSwiftness;
            this.ioProbability = ioProbability / 10;
            this.length = length;

            remainingTick = length;
        }

        /// <summary>
        /// This function equals as 'do something'
        /// We only simulate the process so there is isn't any real work, only a counter
        /// </summary>
        public void Do()
        {
            //If there is no I/O request running and not done
            if (!IsBlocked && remainingTick > 0)
            {
                AddIO();
                remainingTick--;
            }
        }

        /// <summary>
        /// Returns the blocked state of the process
        /// </summary>
        /// <returns>Process is blocked by an I/O operation</returns>
        public bool IsBlocked => (currentIO != null);

        /// <summary>
        /// Returns true if the process is done
        /// </summary>
        /// <returns>Process is done</returns>
        public bool IsDone => (remainingTick <= 0);

        /// <summary>
        /// Property of name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (value.Length < 2 || value.Length > 15)
                {
                    throw new ArgumentException("Parameter must be between 2 and 15 character", "Process Name");
                }
                name = value;
            }
        }

        /// <summary>
        /// Property of I/O probability
        /// </summary>
        public double IOProbability
        {
            // Capped to 10%?
            get { return ioProbability * 10; }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentException("Parameter must be between 0 and 1", "I/O Probability");
                }
                ioProbability = value / 10;
            }
        }

        /// <summary>
        /// Property of I/O probability scaled to 0-100 range
        /// </summary>
        public int IOProbabilityPercent
        {
            get { return (int)Math.Round(ioProbability * 1000); }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentException("Parameter must be between 0 and 100", "I/O Probability");
                }
                ioProbability = (double)value / 100 / 10;
            }
        }
        /// <summary>
        /// Property of I/O swiftness
        /// </summary>
        public IO.Speed IOSwiftness
        {
            get { return ioSwiftness; }
            set { ioSwiftness = value; }
        }

        /// <summary>
        /// Property of process length
        /// </summary>
        public int Length
        {
            get { return length; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Parameter must be greater than 0", "Process Length");
                }
                length = value;
            }
        }

        /// <summary>
        /// Returns the progress of the process
        /// In real life we don't know when the process will finish
        /// We do it because the interactivity
        /// </summary>
        public int Progress
        {
            get { return length - remainingTick; }
        }

        /// <summary>
        /// Returns the current I/O operation
        /// </summary>
        public IO CurrentIO
        {
            get { return currentIO; }
        }

        /// <summary>
        /// Waits for IO request
        /// Sets <c>currentIO</c> to null when it is done.
        /// </summary>
        public void WaitForIO()
        {
            if (currentIO != null)
            {
                if (!currentIO.Done)
                {
                    currentIO.GetData();
                }
                else
                {
                    currentIO = null;
                }
            }
        }

        /// <summary>
        /// This function starts an I/O operation.
        /// </summary>
        private void AddIO()
        {
            //There is no I/O operation
            if (!IsBlocked)
            {
                //Process has a chance to get I/O operation
                Random random = new Random();
                
                //TODO Change this to normal distribution
                if (random.NextDouble() <= ioProbability)
                {
                    //Our process wants I/O operation

                    //Determine speed of operation (80% chance to get ioSwiftness speed, 10-10% the other two)
                    int randomSwiftness = random.Next(0, 9);
                    IO.Speed speed = IO.Speed.FAST;

                    if (randomSwiftness < 8)
                    {
                        speed = ioSwiftness;
                    }
                    else
                    {
                        // This is overkill
                        // Cycles between enum values
                        // -- Adds randomSwiftness - 7 to currentSwiftness
                        // -- Wraps around enum values (e.g., If enum has 3 values, then 3 + 1 == 0)
                        speed = (IO.Speed)((randomSwiftness - 7 + (int)ioSwiftness) % Enum.GetValues(typeof(IO.Speed)).Length);
                    }

                    currentIO = new IO(speed);
                }
            }
        }

        /// <summary>
        /// Resets process state
        /// </summary>
        public void Reset()
        {
            remainingTick = length;
            currentIO = null;
        }
    }
}
