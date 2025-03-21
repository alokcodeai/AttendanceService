using AttendanceService.Models;
using Newtonsoft.Json;
using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceService.BussinessLogic
{
    public class GetAttendanceRecord
    {
        public static string _URL = ConfigurationManager.AppSettings["URL"];
        public static string AppSecret = ConfigurationManager.AppSettings["AppSecret"];
        public static string _appKey = ConfigurationManager.AppSettings["AppKey"];
        public static string _Accept = ConfigurationManager.AppSettings["Accept"];
        public static string ContentType = ConfigurationManager.AppSettings["ContentType"];
        public static string IsDebug = ConfigurationManager.AppSettings["IsDebug"];
        public static string _connectionString = ConfigurationManager.ConnectionStrings["Hana"]?.ConnectionString;

        Response<AttendanceResponse> res = new Response<AttendanceResponse>();

        #region GetAttendanceReportData 
        public async Task<Response<AttendanceResponse>> GetAttendanceReportData(int pageNo, int PazeSize)
        {
            string beginTime = "";
            string endTime = "";
            int nextPage = 0;

            DateTime currentDate = DateTime.UtcNow.Date;
            //string beginTime = currentDate.ToString("yyyy-MM-ddT00:00:00 08:00");
            //string endTime = currentDate.ToString("yyyy-MM-ddT23:59:59 08:00");

            string flg = ConfigurationManager.AppSettings["Flag"];
            if (flg == "N")
            {
                beginTime = currentDate.ToString("yyyy-MM-ddT00:00:00 08:00");
                endTime = currentDate.ToString("yyyy-MM-ddT23:59:59 08:00");
                if (IsDebug == "Y")
                    WriteToFile("beginTime - " + beginTime + " endTime - " + endTime + " flg - " + flg);
            }
            else
            {
                beginTime = ConfigurationManager.AppSettings["beginTime"];
                endTime = ConfigurationManager.AppSettings["EndTime"];
                if (IsDebug == "Y")
                    WriteToFile("beginTime - " + beginTime + " endTime - " + endTime + " flg - " + flg);
            }

            if (IsDebug == "Y")
                WriteToFile("GetAttendanceReportData");

            try
            {
                string requestUrl = _URL;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                request.Accept = _Accept;
                request.Headers.Add("X-Ca-Key", _appKey);
                request.Headers.Add("X-Ca-Signature", GenerateSignature());

                if (IsDebug == "Y")
                    WriteToFile("GenerateSignature = " + GenerateSignature());

                if (IsDebug == "Y")
                    WriteToFile("_Accept =" + _Accept);

                if (IsDebug == "Y")
                    WriteToFile("_appKey =" + _appKey);

                var payload = new
                {
                    attendanceReportRequest = new
                    {
                        pageNo = pageNo,
                        pageSize = PazeSize,
                        queryInfo = new
                        {
                            personID = new int[] { },
                            beginTime = beginTime,
                            endTime = endTime,
                            sortInfo = new
                            {
                                sortField = 1,
                                sortType = 1
                            }
                        }
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);

                if (IsDebug == "Y")
                    WriteToFile("jsonPayload = " + jsonPayload);

                byte[] requestData = Encoding.UTF8.GetBytes(jsonPayload);
                request.ContentLength = requestData.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    requestStream.Write(requestData, 0, requestData.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string responseData = await reader.ReadToEndAsync();

                    var parsedJson = JsonConvert.DeserializeObject<AttendanceResponse>(responseData);

                    if (IsDebug == "Y")
                        WriteToFile("parsedJson Run" + parsedJson.Data.Record.FirstOrDefault());

                    nextPage = Convert.ToInt32(parsedJson.Data.NextPage);
                    pageNo = Convert.ToInt32(parsedJson.Data.PageNo);
                    PazeSize = Convert.ToInt32(parsedJson.Data.PageSize);

                    if (IsDebug == "Y")
                        WriteToFile("parsedJson.Data.Record.Count = " + parsedJson.Data.Record.Count);

                    if (parsedJson != null && parsedJson.Data != null && parsedJson.Data.Record.Count > 0)
                    {
                        res = await SaveDataToHana(parsedJson.Data.Record, _connectionString, pageNo, nextPage, PazeSize);
                        res.PageNo = pageNo;
                        res.NextPageNo = nextPage;

                        if (IsDebug == "Y")
                            WriteToFile("res.PageNo = " + res.PageNo + " res.message - " + res.message + " res.NextPageNo - " + res.NextPageNo);
                    }
                    else
                    {
                        res.status_code = 500; res.message = "No Data Found"; res.PageNo = pageNo; res.NextPageNo = nextPage;
                        if (IsDebug == "Y")
                            WriteToFile("res.PageNo = " + res.PageNo + " res.message - " + res.message + " res.status_code -" + res.status_code + " res.NextPageNo - " + res.NextPageNo);
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                res.status_code = 500; res.message = "Exception " + ex.Message; res.PageNo = pageNo; res.NextPageNo = nextPage;

                if (IsDebug == "Y")
                    WriteToFile("Exception = " + ex.Message + "," + ex.InnerException + " res :- " + res.message);
                return res;
            }
        }
        #endregion

        #region GenerateSignature
        public static string GenerateSignature()
        {
            string appSecret = "N1MWsvum6ZFj27DJD2KE";
            string textToSign = "POST\napplication/json\napplication/json;charset=UTF-8\n/artemis/api/attendance/v1/report";

            byte[] keyBytes = Encoding.UTF8.GetBytes(appSecret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(textToSign);

            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }
        #endregion

        #region SaveDataToHana
        public async Task<Response<AttendanceResponse>> SaveDataToHana(List<Record> records, string connectionString, int pageNo, int nextpage, int pazesize)
        {
            if (IsDebug == "Y")
                WriteToFile("SaveDataToHana");

            int result = 0;
            try
            {
                using (HanaConnection conn = new HanaConnection(connectionString))
                {
                    await conn.OpenAsync();

                    if (IsDebug == "Y")
                        WriteToFile("SaveDataToHana: " + conn.State);

                    foreach (var record in records)
                    {
                        string query = @"
                    INSERT INTO ""PPTL_LIVE"".""Attendance"" 
                    (
                    ""PersonID"", ""GivenName"", ""FamilyName"", ""FullName"", ""PersonCode"", 
                    ""OrgIndexCode"", ""OrgName"", ""PostID"", ""Post"", ""Date"", 
                    ""WeekDay"", ""AllDurationTime"", ""PeriodID"", ""PeriodName"", ""PlanBeginTime"", 
                    ""PlanEndTime"", ""PlanWorkDurationTime"", ""BeginTime"", ""EndTime"", ""AttendanceStatus"", 
                    ""BodyTemperature"", ""TemperatureType"",""RecordBeginTime"",""RecordEndTime"", ""NormalDurationTime"", ""LateDurationTime"", ""EarlyDurationTime"", 
                    ""AbsenceDurationTime"", ""AllOvertimeDurationTime"", ""WorkDayOvertimeDurationTime"", ""WeekendOvertimeDurationTime"", ""HolidayOvertimeDurationTime"", 
                    ""AllLeaveDurationTime"", ""RestDurationTime"" , ""pageNo"", ""nextPage"", ""pageSize""
                    )  
                    VALUES 
                    (
                    :PersonID, :GivenName, :FamilyName, :FullName, :PersonCode, 
                    :OrgIndexCode, :OrgName, :PostID, :Post, :Date, 
                    :WeekDay, :AllDurationTime, :PeriodID, :PeriodName, :PlanBeginTime, 
                    :PlanEndTime, :PlanWorkDurationTime, :BeginTime, :EndTime, :AttendanceStatus, 
                    :BodyTemperature, :TemperatureType,:RecordbeginTime,:RecordendTime, :NormalDurationTime, :LateDurationTime, :EarlyDurationTime, 
                    :AbsenceDurationTime, :AllOvertimeDurationTime, :WorkDayOvertimeDurationTime, :WeekendOvertimeDurationTime, :HolidayOvertimeDurationTime, 
                    :AllLeaveDurationTime, :RestDurationTime , :pageNo, :nextPage, :pageSize
                    )";

                        using (HanaCommand cmd = new HanaCommand(query, conn))
                        {
                            if (IsDebug == "Y")
                                WriteToFile("Inserting Record PersonInfo.FullName: " + record.PersonInfo.FullName);

                            // Person Information
                            cmd.Parameters.AddWithValue(":PersonID", record.PersonInfo.PersonID);
                            cmd.Parameters.AddWithValue(":GivenName", record.PersonInfo.GivenName);
                            cmd.Parameters.AddWithValue(":FamilyName", record.PersonInfo.FamilyName);
                            cmd.Parameters.AddWithValue(":FullName", record.PersonInfo.FullName);
                            cmd.Parameters.AddWithValue(":PersonCode", record.PersonInfo.PersonCode);
                            cmd.Parameters.AddWithValue(":OrgIndexCode", record.PersonInfo.OrgIndexCode);
                            cmd.Parameters.AddWithValue(":OrgName", record.PersonInfo.OrgName);
                            cmd.Parameters.AddWithValue(":PostID", record.PersonInfo.PostID);
                            cmd.Parameters.AddWithValue(":Post", record.PersonInfo.Post);

                            // Attendance Information
                            cmd.Parameters.AddWithValue(":Date", record.Date);
                            cmd.Parameters.AddWithValue(":WeekDay", record.WeekDay);
                            cmd.Parameters.AddWithValue(":AllDurationTime", record.AllDurationTime);

                            // Plan Information
                            cmd.Parameters.AddWithValue(":PeriodID", record.PlanInfo.PeriodID);
                            cmd.Parameters.AddWithValue(":PeriodName", record.PlanInfo.PeriodName);
                            cmd.Parameters.AddWithValue(":PlanBeginTime", record.PlanInfo.PlanBeginTime);
                            cmd.Parameters.AddWithValue(":PlanEndTime", record.PlanInfo.PlanEndTime);
                            cmd.Parameters.AddWithValue(":PlanWorkDurationTime", record.PlanInfo.PlanWorkDurationTime);

                            // Attendance Base Information
                            cmd.Parameters.AddWithValue(":BeginTime", record.AttendanceBaseInfo.BeginTime);
                            cmd.Parameters.AddWithValue(":EndTime", record.AttendanceBaseInfo.EndTime);
                            cmd.Parameters.AddWithValue(":AttendanceStatus", record.AttendanceBaseInfo.AttendanceStatus);

                            // Attendance Detail Information
                            cmd.Parameters.AddWithValue(":BodyTemperature", record.AttendanceDetailInfo.BodyTemperature);
                            cmd.Parameters.AddWithValue(":TemperatureType", record.AttendanceDetailInfo.TemperatureType);
                            if (record.AttendanceDetailInfo.RecordTime != null && record.AttendanceDetailInfo.RecordTime.Count > 0)
                            {
                                var firstRecord = record.AttendanceDetailInfo.RecordTime[0];

                                cmd.Parameters.AddWithValue(":RecordbeginTime", firstRecord.BeginTime);
                                cmd.Parameters.AddWithValue(":RecordendTime", firstRecord.EndTime);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue(":RecordbeginTime", " ");
                                cmd.Parameters.AddWithValue(":RecordendTime", " ");
                            }

                            // Normal, Late, Early, and Absence Duration
                            cmd.Parameters.AddWithValue(":NormalDurationTime", record.NormalInfo?.DurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":LateDurationTime", record.LateInfo?.DurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":EarlyDurationTime", record.EarlyInfo?.DurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":AbsenceDurationTime", record.AbsenceInfo?.DurationTime ?? (object)DBNull.Value);

                            // Overtime Information
                            cmd.Parameters.AddWithValue(":AllOvertimeDurationTime", record.OvertimeInfo?.AllOvertimeDurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":WorkDayOvertimeDurationTime", record.OvertimeInfo?.WorkDayDurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":WeekendOvertimeDurationTime", record.OvertimeInfo?.WeekendDurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":HolidayOvertimeDurationTime", record.OvertimeInfo?.HolidayDurationTime ?? (object)DBNull.Value);

                            // Leave and Rest Information
                            cmd.Parameters.AddWithValue(":AllLeaveDurationTime", record.LeaveInfo?.AllLeaveDurationTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue(":RestDurationTime", record.RestInfo?.DurationTime ?? (object)DBNull.Value);

                            // Response Data
                            cmd.Parameters.AddWithValue(":pageNo", pageNo.ToString());
                            cmd.Parameters.AddWithValue(":nextPage", nextpage.ToString());
                            cmd.Parameters.AddWithValue(":pageSize", pazesize.ToString());
                           
                            result = await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    if (result > 0)
                    {
                        using (HanaCommand cmd = new HanaCommand("CALL PPTL_LIVE.SaveAttendanceData()", conn))
                        {
                            if (IsDebug == "Y")
                                WriteToFile("Calling stored procedure: CALL PPTL_LIVE.SaveAttendanceData()");

                            cmd.CommandType = CommandType.StoredProcedure;

                            int procedureResult = await cmd.ExecuteNonQueryAsync();

                            if (procedureResult > 0)
                            {
                                res.status_code = 200;
                                res.message = "Success: Data Inserted & Procedure Executed";
                            }
                            else
                            {
                                res.status_code = 500;
                                res.message = "Stored Procedure Execution Failed";
                            }
                        }
                    }
                    else
                    {
                        res.status_code = 500; res.message = "Data Insertion Failed";
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                res.status_code = 500; res.message = "Data Insertion Exception " + ex.Message;
                return res;
            }
        }
        #endregion

        #region WriteToFile
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            try
            {
                if (!File.Exists(filepath))
                {
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                else
                {
                    using (var fileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (var writer = new StreamWriter(fileStream))
                        {
                            writer.WriteLine($"{DateTime.Now}: {Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine("WriteToFile - - " + Message);
                }
            }
        }
        #endregion
    }
}
