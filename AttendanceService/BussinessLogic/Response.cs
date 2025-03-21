using AttendanceService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceService.BussinessLogic
{
    public class Response<T>
    {
        public int? status_code { get; set; }
        public string message { get; set; }
        public int? NextPageNo { get; set; }
        public int? PageNo { get; set; }
    }
}
