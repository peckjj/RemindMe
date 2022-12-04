using RemindMe.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemindMe.Models
{
    internal class Task
    {
        public long? Id { get; set; }
        public string Desc { get; set; }
        public DateTime Date { get; set; }
        public DateTime Due { get; set; }
        public Priority Prio { get; set; }
        public string Project { get; set; }
        public TaskStatus Status { get; set; }
        public Task(string v) : this(v, null, null, null, null, null, new TaskStatus(TaskStatusConstants.NEW))
        {   
            
        }

        public Task(string v, Priority prio) : this(v, prio, null, null, null, null, new TaskStatus(TaskStatusConstants.NEW))
        {

        }

        public Task(string desc, int prio, DateTime date, DateTime due, string project, long id, TaskStatus status) : this(desc, new Priority(prio), date, due, project, id, status)
        {

        }

        public Task(string desc, Priority? prio, DateTime? date, DateTime? due, string? project, long? id, TaskStatus status)
        {
            Id = id;
            Desc = desc;
            Prio = prio ?? new Priority(PriorityConstants.MED);
            Date = date ?? DateTime.Now;
            Due = due ?? DateTime.MaxValue;
            Project = project ?? "";
            Status = status;
        }
    }
}
