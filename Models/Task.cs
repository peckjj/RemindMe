using RemindMe.Constants;

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
        public string Status { get; set; }
        public bool IsCompleted { get; set; }
        public Task(string v) : this(v, null, null, null, null, null, "active", false)
        {

        }

        public Task(string v, Priority prio) : this(v, prio, false)
        {

        }

        public Task(string v, Priority prio, bool isCompleted) : this(v, prio, null, null, null, null, "active", isCompleted)
        {

        }

        public Task(string desc, int prio, DateTime date, DateTime due, string project, long id, string status, bool isCompleted) : this(desc, new Priority(prio), date, due, project, id, status, isCompleted)
        {

        }

        public Task(string desc, Priority? prio, DateTime? date, DateTime? due, string? project, long? id, string status, bool isCompleted)
        {
            Id = id;
            Desc = desc;
            Prio = prio ?? new Priority(PriorityConstants.MED);
            Date = date ?? DateTime.Now;
            Due = due ?? DateTime.MaxValue;
            Project = project ?? "";
            Status = status;
            IsCompleted = isCompleted;
        }
    }
}
