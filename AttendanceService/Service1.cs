using AttendanceService.BussinessLogic;
using AttendanceService.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AttendanceService
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer timer = new System.Timers.Timer();
        GetAttendanceRecord record = new GetAttendanceRecord();
        public static string IsDebug = ConfigurationManager.AppSettings["IsDebug"];
        public static string Flag = ConfigurationManager.AppSettings["Flag"];
        Common common = new Common();
        public static string _connectionString = ConfigurationManager.ConnectionStrings["Hana"]?.ConnectionString;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            record.WriteToFile("Service Started - " + DateTime.Now);

            string TimeInterval = ConfigurationManager.AppSettings["IntervalMM"];
            int interval = Convert.ToInt32(TimeInterval) * 60000;
            if (Flag == "Y")
                record.WriteToFile("Flag - " + Flag);
            else
                record.WriteToFile("Flag - " + Flag);

            if (Flag == "Y")
                executefunction();
            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = interval;
            if (IsDebug == "Y")
                record.WriteToFile(" timer.Interval " + interval);
            timer.Enabled = true;
            timer.Start();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (IsDebug == "Y")
                    record.WriteToFile(" Timer_Elapsed ");
                DateTime now = DateTime.Now;
                int currentHour = now.Hour;   // Get current hour (0-23)
                int currentMinute = now.Minute; // Get current minute (0-59)

                if (currentHour == 23 && currentMinute == 00)
                {
                    record.WriteToFile($"Executing function at {now:HH:mm}" + " Date " + now.ToString("yyyy-MM-dd") + " currentHour " + currentHour + " currentMinute " + currentMinute);
                    executefunction();

                    if (IsDebug == "Y")
                        record.WriteToFile(" executefunction() complete");
                }
                else
                {
                    if (IsDebug == "Y")
                        record.WriteToFile($"Executing function at {now:HH:mm}" + " Date " + now.ToString("yyyy-MM-dd") + " currentHour " + currentHour + " currentMinute " + currentMinute);
                }

            }
            catch (Exception ex)
            {
                if (IsDebug == "Y")
                    record.WriteToFile("Exception - " + ex.Message);
            }
        }

        public async void executefunction()
        {
            if (IsDebug == "Y")
                record.WriteToFile("executefunction");

            Response<AttendanceResponse> response = new Response<AttendanceResponse>();

            try
            {
                int pageNo = Convert.ToInt32(ConfigurationManager.AppSettings["PageNo"]);
                int pageSize = Convert.ToInt32(ConfigurationManager.AppSettings["PageSize"]);
                int nextpage = 0;

                if (IsDebug == "Y")
                    record.WriteToFile("pageNo,pageSize" + pageNo + "," + pageSize);
                

                response = await record.GetAttendanceReportData(pageNo, pageSize);

                if (IsDebug == "Y")
                    record.WriteToFile("GetAttendanceReportData called,pageNo - " + pageNo);

                if (response.status_code == 200)
                {
                    nextpage = response.NextPageNo.Value;

                    if (IsDebug == "Y")
                        record.WriteToFile("first time nextpage " + nextpage);
                }

                while (nextpage > 0)
                {
                    pageNo = pageNo + 1;

                    if (IsDebug == "Y")
                        record.WriteToFile("pageNo - " + pageNo + ",nextpage - " + nextpage);

                    response = await record.GetAttendanceReportData(pageNo, pageSize);

                    if (response.status_code == 200)
                    {
                        nextpage = response.NextPageNo.Value;
                        if (IsDebug == "Y")
                            record.WriteToFile(" response = " + response.message + "," + response.status_code + ", nextpage " + nextpage);
                    }
                    else
                    {
                        nextpage = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsDebug == "Y")
                    record.WriteToFile("Exception - " + ex.Message);
            }
        }

        protected override void OnStop()
        {
            timer.Enabled = false;
            timer.Stop();
            record.WriteToFile("Service stoped - " + DateTime.Now);
        }
      
    }
}
