using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStatus = RemindMe.Models.TaskStatus;

namespace RemindMe.Constants
{
    public class TaskStatusConstants
    {
        public static readonly string NEW = "new";
        public static readonly string COMPLETE = "complete";
        public static readonly string FAILED = "failed";

        public static readonly string[] ALL_STATUSES = new string[] { NEW, COMPLETE, FAILED };
    }
}
