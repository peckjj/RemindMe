using RemindMe.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemindMe.Models
{
    public class TaskStatus
    {
        public static readonly TaskStatus NEW = new TaskStatus("new");
        public static readonly TaskStatus COMPLETE = new TaskStatus("complete");
        public static readonly TaskStatus FAILED = new TaskStatus("failed");

        private string status;

        public TaskStatus(string status)
        {
            if (!TaskStatusConstants.ALL_STATUSES.Contains(status))
            {
                throw new ArgumentException(status + " is not a valid TaskStatus");
            }
            this.status = status;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!(obj is TaskStatus))
            {
                return obj.Equals(status);
            }

            return status == ((TaskStatus)(obj)).status;
        }

        public override string ToString()
        {
            return status;
        }
    }
}
