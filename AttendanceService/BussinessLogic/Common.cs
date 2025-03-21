using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceService.BussinessLogic
{
    public class Common
    {
        public object ExecuteScalar(string Query, string connectionstring)
        {
            object result = null;
            using (HanaConnection con = new HanaConnection(connectionstring))
            {
                con.Open();
                using (HanaCommand cmd = new HanaCommand(Query, con))
                {
                    result = cmd.ExecuteScalar();
                }
            }
            return result;
        }
    }
}
