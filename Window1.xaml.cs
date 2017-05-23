using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Microsoft.Win32;


namespace MemoryPerformanceMonitoring
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        //instance name for which you want to monitor
        string sProcName = "regedit";        
        
        ObservableCollection<StatisticsData> _StatisticsCollection =
        new ObservableCollection<StatisticsData>();

        public ObservableCollection<StatisticsData> StatisticsCollection
        { get { return _StatisticsCollection; } }


        public Window1()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            txtFileName.Text = openFileDialog.FileName;
            if (openFileDialog.ShowDialog() == true)
                txtFileName.Text = openFileDialog.FileName;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            txtLog.Text = File.ReadAllText(txtFileName.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread MonitorThd = new Thread(this.StatisticsMonitoringThread);
            MonitorThd.Start();

          //  MemoryandProcessCheck();
                          
        }

        public void StatisticsMonitoringThread(object obj)
        {
                
            MonitorMemoryandProcess();
      
        }


        private Process CurrentlyRunning(string sProcessName)
        {
            //get a list of all running processes on current system
            Process[] Processes = Process.GetProcesses();

            //Iterate to every process to check if it is out required process
            foreach (Process SingleProcess in Processes)
            {

                if (SingleProcess.ProcessName.Contains(sProcessName))
                {
                    //process found                  
                    return SingleProcess;
                }
            }

            //Process not found
            return null;
        }


        private bool MonitorMemoryandProcess()
        {                        
            string ProcessStatus = null;
            string[] str = new string[10];

            Process ReqProcess;

            try
            { 
                    GC.GetTotalMemory(true); // how much GC total use 
                                         
                    ReqProcess = CurrentlyRunning(sProcName);
                    
                do
                {
                    if (ReqProcess != null)
                    {
                        
                        // Refresh the current process property values.
                        ReqProcess.Refresh();                        

                        if (ReqProcess.Responding)
                        {
                            ProcessStatus = "Running";
                        }
                        else
                        {
                            ProcessStatus = "Not Responding";
                        }


                        PerformanceCounter totalProcessorTimeCounter        = new PerformanceCounter("Process",
                                                                                                     "% Processor Time",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter UserProcessorTimeCounter         = new PerformanceCounter("Process",
                                                                                                     "% User Time",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter PrivilegedProcessorTimeCounter   = new PerformanceCounter("Process",
                                                                                                     "% Privileged Time",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter WorkingSetMemoryCounter          = new PerformanceCounter("Process",
                                                                                                     "Working Set",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter WorkingSetPeakMemoryCounter      = new PerformanceCounter("Process",
                                                                                                     "Working Set Peak",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter ThreadCountCounter               = new PerformanceCounter("Process",
                                                                                                     "Thread Count",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter WorkingSetPrivateMemoryCounter   = new PerformanceCounter("Process",
                                                                                                     "Working Set - Private",
                                                                                                     ReqProcess.ProcessName);

                        PerformanceCounter HandleCountCounter               = new PerformanceCounter("Process",
                                                                                                     "Handle Count",
                                                                                                     ReqProcess.ProcessName);


                        totalProcessorTimeCounter.NextValue();

                        UserProcessorTimeCounter.NextValue();
                        PrivilegedProcessorTimeCounter.NextValue();

                        System.Threading.Thread.Sleep(1000);// 1 second wait
                       // Dispatcher.Invoke(new ClearListViewFromOutside(ClearListView));

                        str[0] = ReqProcess.ProcessName;
                        str[1] = ProcessStatus;
                        str[2] = (WorkingSetMemoryCounter.NextValue() / 1024) + "K";
                        str[3] = (WorkingSetPrivateMemoryCounter.NextValue() / 1024) + "K";
                        str[4] = (WorkingSetPeakMemoryCounter.NextValue() / 1024) + "K";
                        str[5] = (ThreadCountCounter.NextValue()).ToString();
                        str[6] = (HandleCountCounter.NextValue()).ToString();

                        str[7] = (totalProcessorTimeCounter.NextValue()).ToString();
                        str[8] = (UserProcessorTimeCounter.NextValue()).ToString();
                        str[9] = (PrivilegedProcessorTimeCounter.NextValue()).ToString();                       

                    }
                    else
                    {
                        str[0] = sProcName;
                        str[1] = "Not Started";
                        str[2] = "";
                        str[3] = "";
                        str[4] = "";
                        str[5] = "";
                        str[6] = "";
                        str[7] = "";
                        str[8] = "";
                        str[9] = "";                      

                    }

                    Dispatcher.Invoke(new UpdateGUIOutsideFeedbackMessage(UpdateGUIOutsideFeedbackMsg), new object[] { str });

                }while (true);   //infinite loop

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Performance Monitoring Statistics Exception ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

       

        public delegate void UpdateGUIOutsideFeedbackMessage(string[] msg);


        public void UpdateGUIOutsideFeedbackMsg(string[] msg)
        {            
            Mutex firstMutex = new Mutex(false);

            firstMutex.WaitOne();

            //first clear the previous value and than add new one
            StatisticsCollection.Clear();            
            _StatisticsCollection.Add(new StatisticsData
            {
                ProcessName = msg[0],
                ProcessRunningStatus = msg[1],
                WorkingSetMemory = msg[2],
                WorkingSetPrivateMemory = msg[3],
                WorkingSetPeak = msg[4],
                ThreadCount = msg[5],
                HandleCount = msg[6],

                TotalProcessorTime = msg[7],
                UserProcessorTime = msg[8],
                PrivilegedProcessorTime = msg[9]
            });


            firstMutex.Close();
        }


        public delegate void ClearListViewFromOutside();


        public void ClearListView()
        {
            StatisticsCollection.Clear();

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //forcefully destroy the application
            Environment.Exit(0);
            
        }

        
        private bool MemoryandProcessCheck()
        {
           
           Process ReqProcess;

            try
            {
                GC.GetTotalMemory(true); // how much GC total use
             
                ReqProcess = CurrentlyRunning(sProcName);                

                if (ReqProcess != null)
                {
                    // calculate the CPU load
                    System.TimeSpan CPULoad = (DateTime.Now - ReqProcess.StartTime);                    
                    listView1.Items.Add("CPU load: " + (ReqProcess.TotalProcessorTime.TotalMilliseconds / CPULoad.TotalMilliseconds) * 100);

                    PerformanceCounter TotalProcessorTimeCounter            = new PerformanceCounter("Process",
                                                                                                     "% Processor Time",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter ProcessorUserTimeCounter             = new PerformanceCounter("Process",
                                                                                                     "% User Time",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter ProcessorPrivilegedTimeCounter       = new PerformanceCounter("Process",
                                                                                                     "% Privileged Time",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter ElapsedTimeCounter                   = new PerformanceCounter("Process",
                                                                                                     "Elapsed Time",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter VirtualBytesPeakMemoryCounter        = new PerformanceCounter("Process",
                                                                                                     "Virtual Bytes Peak",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter VirtualBytesMemoryCounter            = new PerformanceCounter("Process",
                                                                                                     "Virtual Bytes",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter WorkingSetMemoryCounter              = new PerformanceCounter("Process",
                                                                                                     "Working Set",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter WorkingSetPeakMemoryCounter          = new PerformanceCounter("Process",
                                                                                                     "Working Set Peak",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter PrivateBytesMemoryCounter            = new PerformanceCounter("Process",
                                                                                                     "Private Bytes",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter WorkingSetPrivateMemoryCounter       = new PerformanceCounter("Process",
                                                                                                     "Working Set - Private",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter PageFileBytesPeakMemoryCounter       = new PerformanceCounter("Process",
                                                                                                     "Page File Bytes Peak",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter ThreadCountCounter                   = new PerformanceCounter("Process",
                                                                                                     "Thread Count",
                                                                                                     ReqProcess.ProcessName);

                    PerformanceCounter HandleCountCounter                   = new PerformanceCounter("Process",
                                                                                                     "Handle Count",
                                                                                                     ReqProcess.ProcessName);


                    TotalProcessorTimeCounter.NextValue();

                    ProcessorUserTimeCounter.NextValue();
                    ProcessorPrivilegedTimeCounter.NextValue();

                    //if there is no wait for 1 second after the first call to NextValue() than processing time value will not be correct.
                    System.Threading.Thread.Sleep(1000);// 1 second wait
                    
                    if (!ReqProcess.HasExited)
                    {
                        // Refresh the process property values.
                        ReqProcess.Refresh();

                        // Display process statistics related to memory.
                        listView1.Items.Add(ReqProcess.ProcessName);
                        listView1.Items.Add("******************************");

                        listView1.Items.Add("Working Set: " + (WorkingSetMemoryCounter.NextValue() / 1024) + "K"); // more efficent. update quickly as compare to ReqProcess.WorkingSet64 if Process's Refresh() did not call
                        listView1.Items.Add("Physical memory usage(Working Set memory): " + ReqProcess.WorkingSet64 / 1024 + "K");

                        listView1.Items.Add("Working Set - Private: " + (WorkingSetPrivateMemoryCounter.NextValue() / 1024) + "K");

                        listView1.Items.Add("Private Memory Size: " + ReqProcess.PrivateMemorySize64 / 1024 + "K"); // usually same with PagedMemorySize64
                        listView1.Items.Add("Private Bytes: " + (PrivateBytesMemoryCounter.NextValue() / 1024) + "K");
                        listView1.Items.Add("Virtual memory paging file(Process using RAM): " + ReqProcess.PagedMemorySize64 / 1024 + "K");                              

                        listView1.Items.Add("Working Set Peak: " + (WorkingSetPeakMemoryCounter.NextValue() / 1024) + "K"); //same as peakWorkingSet
                        listView1.Items.Add("Peak physical memory usage: " + ReqProcess.PeakWorkingSet64 / 1024 + "K");


                        listView1.Items.Add("Thread Count: " + ThreadCountCounter.NextValue());    // how many threads are

                        listView1.Items.Add("Handle Count: " + HandleCountCounter.NextValue());   // how many handles


                        //The amount of system memory, in bytes, allocated for the associated process that can be written to the virtual memory paging file.
                        listView1.Items.Add("Page System Memory Size: " + ReqProcess.PagedSystemMemorySize64 / 1024 + "K");

                        //The amount of system memory, in bytes, allocated for the associated process that cannot be written to the virtual memory paging file.
                        listView1.Items.Add("Nonpage System Memory Size: " + ReqProcess.NonpagedSystemMemorySize64 / 1024 + "K");

                        listView1.Items.Add("Virtual Memory: " + ReqProcess.VirtualMemorySize64 / 1024 + "K");
                        listView1.Items.Add("Virtual Bytes: " + (VirtualBytesMemoryCounter.NextValue() / 1024) + "K");

                        listView1.Items.Add("Virtual Bytes Peak: " + (VirtualBytesPeakMemoryCounter.NextValue() / 1024) + "K");
                        listView1.Items.Add("Peak Virtual Memory usage: " + ReqProcess.PeakVirtualMemorySize64 / 1024 + "K");


                        listView1.Items.Add("Page File Bytes Peak: " + (PageFileBytesPeakMemoryCounter.NextValue() / 1024) + "K");
                        listView1.Items.Add("Peak virtual memory paging file usage: " + ReqProcess.PeakPagedMemorySize64 / 1024 + "K");


                        if (ReqProcess.Responding)
                        {
                            listView1.Items.Add("Status = Running");
                        }
                        else
                        {
                            listView1.Items.Add("Status = Not Responding");
                        }

                        //Display process statistics related to Processor
                        listView1.Items.Add("%Processor Time: " + TotalProcessorTimeCounter.NextValue());
                        listView1.Items.Add("%User Time: " + ProcessorUserTimeCounter.NextValue());
                        listView1.Items.Add("%Privileged Time: " + ProcessorPrivilegedTimeCounter.NextValue());
                        listView1.Items.Add("Elapsed Time: " + ElapsedTimeCounter.NextValue());
                    
                        listView1.Items.Add("Total processor time: " + ReqProcess.TotalProcessorTime);
                        listView1.Items.Add("User processor time: " + ReqProcess.UserProcessorTime);
                        listView1.Items.Add("Privileged processor time: " + ReqProcess.PrivilegedProcessorTime);
                    

                        //test code-Start
                        //code to see all the possible Counter values of a process.                         
                        PerformanceCounterCategory[] PCounterCtg = PerformanceCounterCategory.GetCategories();
                        foreach (PerformanceCounterCategory category in PCounterCtg)
                        {
                            if (category.CategoryName != "Process")
                                continue;

                            listView1.Items.Add("");
                            listView1.Items.Add("Category: " + category.CategoryName);

                            string[] instances = category.GetInstanceNames();
                            if (instances.Length == 0)
                            {
                                foreach (PerformanceCounter PCounter in category.GetCounters())
                                    listView1.Items.Add("  Counter: " + PCounter.CounterName);
                            }
                            else
                            {
                                foreach (string instance in instances)
                                {
                                    if (!(instance.Equals(sProcName)))
                                        continue;

                                    listView1.Items.Add("  Instance: " + instance);
                                    if (category.InstanceExists(instance))
                                        foreach (PerformanceCounter Pctr in category.GetCounters(instance))
                                            listView1.Items.Add("    Counter: " + Pctr.CounterName);

                                }
                            }
                        }                       
                        //test code-End

                    }
                    
                }
                else
                {
                    listView1.Items.Add("");
                    listView1.Items.Add("Process " + sProcName + " is not started. ");

                }


            }
            catch (Exception ex)
            {
                listView1.Items.Add("Process check exception: " + ex.Message);
                return false;
            }

            return true;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Process viewProcess = null;
            viewProcess = CurrentlyRunning(sProcName);

            if (viewProcess != null)
            {
                Window2 w2 = new Window2("Process", "Working Set - Private", sProcName, "Memory (Private Working Set)");
                w2.Show();
            }
            else
            {
                MessageBox.Show(sProcName + " instance is not running.", "Memory Graph Exception", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Process viewProcess = null;
            viewProcess = CurrentlyRunning(sProcName);
            
            if (viewProcess != null)
            {
                Window2 w2 = new Window2("Process", "% Processor Time", sProcName, "%Processor Time (Total)");
                w2.Show();
            }
            else
            {
                MessageBox.Show(sProcName + " instance is not running.", "Processor Graph Exception", MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }
        

    }


    public class StatisticsData
    {
        public string ProcessName { get; set; }
        public string ProcessRunningStatus { get; set; }
        public string WorkingSetMemory { get; set; }
        public string WorkingSetPrivateMemory { get; set; }
        public string WorkingSetPeak { get; set; }
        public string ThreadCount { get; set; }
        public string HandleCount { get; set; }
        public string TotalProcessorTime { get; set; }
        public string UserProcessorTime { get; set; }
        public string PrivilegedProcessorTime { get; set; }
    }
}
