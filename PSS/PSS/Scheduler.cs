﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSS
{
    /// <summary>
    /// This class contains the process scheduling logic
    /// </summary>
    public class Scheduler
    {
        /// <summary>
        /// PID counter
        /// </summary>
        public int counter;


        /// <summary>
        /// List of the process control vlocks
        /// </summary>
        private List<PCB> processList;

        /// <summary>
        /// Selected algorithm
        /// </summary>
        private SchedulingAlgorithm selectedAlgorithm;

        /// <summary>
        /// CPU which 'does the work'
        /// </summary>
        private CPU cpu;

        private int worktime;
        private int turn;
        private int elapsed;
        private int wasted;

        private int logCounter;

        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="list">List of the processes</param>
        /// <param name="alg">Instance of the chosen algorithm</param>
        public Scheduler(List<Process> list, SchedulingAlgorithm alg)
        {

            // Set PID default value
            counter = 0;

            processList = EncapsulateProcesses(list);
            selectedAlgorithm = alg;

            // Initialize algorithm with the list of processes
            selectedAlgorithm.Initialize(processList);

            cpu = new CPU();

            worktime = 0;
            turn = 1;
            elapsed = 1;
            logCounter = 0;
            wasted = 0;

            //Initialize EventLogger and set headers
            EventLogger.Initialize();
            EventLogger.AddEvent("Selected algorithm: " + selectedAlgorithm.GetType().Name);
            EventLogger.AddEvent("Process Count: " + processList.Count);
            EventLogger.AddEvent("Processes: ");
            processList.ForEach(x => EventLogger.AddEvent(x.Process.ToString()));
            LogCurrentState();
        }

        /// <summary>
        /// Resets the Scheduler to defaults
        /// </summary>
        public void Reset()
        {
            EventLogger.Clear();

            //Clears algorithm storage (queue, etc.)
            selectedAlgorithm.Reset();
            //Reset all process
            processList.ForEach(i => i.Reset());
            //Forward the processes (again...)
            selectedAlgorithm.Initialize(processList);

            // Unset process in CPU if there's any set
            cpu.UnsetProcess();

            worktime = 0;
            turn = 1;
            elapsed = 1;
            logCounter = 0;
            wasted = 0;

            //Initialize EventLogger and set headers
            EventLogger.Initialize();
            EventLogger.AddEvent("Selected algorithm: " + selectedAlgorithm.GetType().Name);
            EventLogger.AddEvent("Process Count: " + processList.Count);
            EventLogger.AddEvent("Processes: ");
            processList.ForEach(x => EventLogger.AddEvent(x.Process.ToString()));
            LogCurrentState();
        }

        /// <summary>
        /// Logs the current state (with EventLogger)
        /// </summary>
        public void LogCurrentState()
        {
            logCounter++;
            EventLogger.AddEventRaw("");
            EventLogger.AddEvent("########## Turn: #" + logCounter + ", Worktime: " + worktime + ", Elapsed: " + elapsed + " ##########");
            if (selectedAlgorithm.ShouldSwap)
            {
                EventLogger.AddEvent("Current process: " + selectedAlgorithm.RunningProcess.ToString());
            }
            else
            {
                EventLogger.AddEvent("Current process: " + "No process running");
            }
            processList.ForEach(x => EventLogger.AddEvent(x.ToString()));
        }

        /// <summary>
        /// Encapsulate the processes into PCB (Process Control Block)
        /// </summary>
        /// <param name="processes">List of the processes</param>
        /// <returns>List of the encapsulated processes (PCB)</returns>
        private List<PCB> EncapsulateProcesses(List<Process> processes)
        {
            List<PCB> list = new List<PCB>();
            processes.ForEach(x => list.Add(new PCB(counter++, x)));
            return list;
        }

        /// <summary>
        /// Encapsulate a single process into PCB (Process Control Block)
        /// </summary>
        /// <param name="process">The process</param>
        /// <returns>The encapsulated PCB</returns>
        public PCB AddAndEncapsulateProcess(Process process)
        {
            EventLogger.AddEvent("New process: " + process.ToString());
            PCB pcb = new PCB(counter++, process);
            ProcessList.Add(pcb);
            //Add process to the scheduling algorithm too
            selectedAlgorithm.AddNewProcess(pcb);
            return pcb;
        }

        /// <summary>
        /// Returns the amount of processes
        /// </summary>
        public int ProcessCount
        {
            get { return processList.Count; }
        }

        /// <summary>
        /// Returns the process by the index
        /// </summary>
        /// <param name="i">Index</param>
        /// <returns>Returned process</returns>
        public PCB ProcessAt(int i)
        {
            return processList[i];
        }

        /// <summary>
        /// Returns the list of the processes
        /// </summary>
        public List<PCB> ProcessList
        {
            get { return processList; }
        }

        /// <summary>
        /// Returns the current scheduling algorithm
        /// </summary>
        SchedulingAlgorithm Algorithm
        {
            get { return selectedAlgorithm; }
            set { selectedAlgorithm = value; }
        }

        /// <summary>
        /// Returns every process with READY or NEW states.
        /// </summary>
        public List<PCB> ReadyProcesses
        {
            get
            {
                return selectedAlgorithm.Pool
                    .Where(x => x.State == PCB.ProcessState.NEW || x.State == PCB.ProcessState.READY)
                    .ToList();
            }
        }

        /// <summary>
        /// Total CPU work time
        /// </summary>
        public int Worktime
        {
            get { return worktime; }
        }

        /// <summary>
        /// The number of total Process switching
        /// </summary>
        public int Turns
        {
            get { return turn; }
        }

        /// <summary>
        /// Elapsed time from the start of the simulation
        /// </summary>
        public int ElapsedTime
        {
            get { return elapsed; }
        }

        /// <summary>
        /// Wasted time (CPU is not running, waits for I/O)
        /// </summary>
        public int WastedTime
        {
            get { return wasted;  }
        }

        /// <summary>
        /// Useful CPU time (when the process is doing something)
        /// </summary>
        public double UsefulCPUTime
        {
            get
            {
                return ((double)worktime / (double)elapsed);
            }
        }

        /// <summary>
        /// Returns the Process instance by an ID
        /// </summary>
        /// <param name="PID">Process ID</param>
        /// <returns>Process Instance</returns>
        public PCB GetProcessByID(int PID)
        {
            return processList.Where(x => x.PID == PID).FirstOrDefault();
        }

        /// <summary>
        /// One step of the scheduler
        /// </summary>
        /// <returns>Returns true if all of the processes have been finished.</returns>
        public bool Step()
        {
            selectedAlgorithm.Work();
            
            if (selectedAlgorithm.ShouldSwap)
            {
                // Process changed
                turn++;
                elapsed++;
                LogCurrentState();
                cpu.UnsetProcess();
                cpu.SetProcess(selectedAlgorithm.RunningProcess);

                //Reset wait
                selectedAlgorithm.RunningProcess.Process.ResetWait();
            }

            //This must be before cpu.DoWork
            foreach (var pcb in selectedAlgorithm.Pool)
            {
                pcb.IOWork();

                if (pcb != selectedAlgorithm.RunningProcess)
                {
                    pcb.Process.Wait();
                }
            }

            // Do cpu work
            if (cpu.DoWork())
            {
                worktime++;
            }
            else
            {
                wasted++;
            }

            elapsed++;

            // Check if all processes are done
            return selectedAlgorithm.Done;
        }
    }
}
