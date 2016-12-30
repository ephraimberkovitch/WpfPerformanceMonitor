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
using System.Diagnostics;
using System.Collections.Specialized;

using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Collections.ObjectModel;
using Microsoft.Research.DynamicDataDisplay.Charts;

namespace MemoryPerformanceMonitoring
{
	/// <summary>
	/// Interaction logic for Window2.xaml
	/// </summary>
	public partial class Window2 : Window
	{
        string strProcName = null;
        string strCategoryName = null;
        string strCounterName = null;
        string strDisplayName = null;

        public Window2(string sCategoryName, string sCounterName, string sProcName, string sDisplayName)
		{
			InitializeComponent();
            
            strCategoryName = sCategoryName;
            strCounterName = sCounterName;
            strProcName = sProcName;
            strDisplayName = sDisplayName;

			Loaded += new RoutedEventHandler(Window2_Loaded);
		}

		private void Window2_Loaded(object sender, RoutedEventArgs e)
		{

            CreatePerformanceGraph(strCategoryName, strCounterName, strProcName);
            
		}

		private LineGraph CreatePerformanceGraph(string categoryName, string counterName, string instanceName)
		{
			PerformanceData data = new PerformanceData(new PerformanceCounter(categoryName, counterName, instanceName));

			var filteredData = new FilteringDataSource<PerformanceInfo>(data, new MaxSizeFilter());

			var ds = new EnumerableDataSource<PerformanceInfo>(filteredData);
			ds.SetXMapping(pi => pi.Time.TimeOfDay.TotalSeconds);

            Color cr = new Color();
            LineGraph chart = plotter.AddLineGraph(ds, 3.0, String.Format("{0} - {1}", instanceName, strDisplayName));
                   
            if(strDisplayName.Contains("Memory"))
            {
                ds.SetYMapping(pi => pi.Value/1024);
                plotter.AddLineGraph(ds, cr, 3.0, String.Format("{0} - {1}", "X axis = Total# of seconds from today’s start", "Y axis = K Bytes"));
            }
            else
            {
                ds.SetYMapping(pi => pi.Value);
                plotter.AddLineGraph(ds, cr, 3.0, String.Format("{0} - {1}", "X axis = Total# of seconds from today’s start", "Y axis = CPU Usage"));
            }
         
             return chart;
		}

        private void chart_DataChanged(object sender, EventArgs e)
        {
            LineGraph graph = (LineGraph)sender;

            double mbytes = graph.DataSource.GetPoints().LastOrDefault().Y;

            graph.Description = new PenDescription(String.Format("Memory - available {0} MBytes", mbytes));
        }
	}
}
