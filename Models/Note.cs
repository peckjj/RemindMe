namespace RemindMe.Models
{
    public class Note
    {
        long? Id { get; set; }
        public string Desc { get; set; }
        public long TaskId { get; set; }
        public DateTime Date { get; set; }


        public Note(long taskId, string desc, DateTime? date = null) : this(null, taskId, desc, date ?? DateTime.Now)
        {
        }

        public Note(long? id, long taskId, string desc, DateTime date)
        {
            this.Id = id;
            this.TaskId = taskId;
            this.Desc = desc;
            this.Date = date;
        }
    }
}
