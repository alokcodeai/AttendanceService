using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceService.Models
{
    public class AttendanceResponse
    {
        public string Code { get; set; }
        public string Msg { get; set; }
        public ResponseData Data { get; set; }
    }

    public class ResponseData
    {
        public string NextPage { get; set; }
        public string PageNo { get; set; }
        public string PageSize { get; set; }
        public List<Record> Record { get; set; }
    }

    public class Record
    {
        public PersonInfo PersonInfo { get; set; }
        public string Date { get; set; }
        public string WeekDay { get; set; }
        public string AllDurationTime { get; set; }
        public PlanInfo PlanInfo { get; set; }
        public AttendanceBaseInfo AttendanceBaseInfo { get; set; }
        public AttendanceDetailInfo AttendanceDetailInfo { get; set; }
        public NormalInfo NormalInfo { get; set; }
        public LateInfo LateInfo { get; set; }
        public EarlyInfo EarlyInfo { get; set; }
        public AbsenceInfo AbsenceInfo { get; set; }
        public OvertimeInfo OvertimeInfo { get; set; }
        public LeaveInfo LeaveInfo { get; set; }
        public RestInfo RestInfo { get; set; }
    }

    public class PersonInfo
    {
        public string PersonID { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string FullName { get; set; }
        public string PersonCode { get; set; }
        public string OrgIndexCode { get; set; }
        public string OrgName { get; set; }
        public string PostID { get; set; }
        public string Post { get; set; }
    }

    public class PlanInfo
    {
        public string PeriodID { get; set; }
        public string PeriodName { get; set; }
        public string PlanBeginTime { get; set; }
        public string PlanEndTime { get; set; }
        public string PlanWorkDurationTime { get; set; }
    }

    public class AttendanceBaseInfo
    {
        public string BeginTime { get; set; }
        public string EndTime { get; set; }
        public string AttendanceStatus { get; set; }
    }

    public class AttendanceDetailInfo
    {
        public string BodyTemperature { get; set; }
        public string TemperatureType { get; set; }
        public List<RecordTime> RecordTime { get; set; } = null;
    }
    public class RecordTime
    {
        public string BeginTime { get; set; }
        public string EndTime { get; set; }
    }
    public class NormalInfo
    {
        public string DurationTime { get; set; }
    }

    public class LateInfo
    {
        public string DurationTime { get; set; }
    }

    public class EarlyInfo
    {
        public string DurationTime { get; set; }
    }

    public class AbsenceInfo
    {
        public string DurationTime { get; set; }
    }

    public class OvertimeInfo
    {
        public string AllOvertimeDurationTime { get; set; }
        public string WorkDayDurationTime { get; set; }
        public string WeekendDurationTime { get; set; }
        public string HolidayDurationTime { get; set; }
    }

    public class LeaveInfo
    {
        public string AllLeaveDurationTime { get; set; }
    }

    public class RestInfo
    {
        public string DurationTime { get; set; }
    }
}
