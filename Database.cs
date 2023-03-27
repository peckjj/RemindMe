using Microsoft.Data.Sqlite;
using RemindMe.Models;
using System.Reflection;
using Task = RemindMe.Models.Task;

namespace RemindMe
{
    internal class Database
    {
        private static readonly string dbVersion = "0.2";

        private static readonly string connString = "Data Source=" +
                                           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                           "/reminders.db";
        private static readonly string allFieldsFromTasks = "desc, prio, date, due, status, project, id, isCompleted";
        private static readonly string allFieldsFromNotes = "id, desc, date, taskId";
        private static readonly string selectAllFieldsFromTasks = "SELECT " + allFieldsFromTasks + " FROM tasks ";
        private static readonly string selectAllFieldsFromNotes = "SELECT " + allFieldsFromNotes + " FROM notes ";

        internal static void CheckDB()
        {
            if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/reminders.db"))
            {
                Console.WriteLine("Database does not exist, creating reminders.db");
                CreateDb();
            }
            else
            {
                string curVersion = GetDBVersion();

                if (curVersion != dbVersion)
                {
                    UpdateDB(curVersion);
                }
            }
        }
        internal static void CreateDb()
        {
            using SqliteConnection connection = new(connString);
            connection.Open();

            // Create tasks table
            string createTables = @"
                CREATE TABLE ""tasks"" (
                    ""id""	        INTEGER,
                    ""desc""	    TEXT NOT NULL,
                    ""date""	    TEXT NOT NULL,
                    ""due""	        TEXT,
                    ""prio""	    INTEGER NOT NULL,
                    ""project""	    TEXT,
                    ""status""	    TEXT NOT NULL,
                    ""isCompleted""	TEXT NOT NULL CHECK(""isCompleted"" = ""True"" OR ""isCompleted"" = ""False""),
                    PRIMARY KEY(""id"" AUTOINCREMENT)
                )
                ;
                CREATE TABLE ""meta"" (
                    ""dbVersion""	TEXT NOT NULL UNIQUE,
                    ""createdOn""	TEXT NOT NULL,
                    ""updatedOn""	INTEGER NOT NULL
                )
                ;
                INSERT INTO meta VALUES(
                    $dbVersion,
                    $createdOn,
                    $updatedOn
                )
                ;
                CREATE TABLE ""notes"" (
                    ""id""	INTEGER,
                    ""desc""	TEXT NOT NULL,
                    ""date""	TEXT NOT NULL,
                    ""taskId""	INTEGER NOT NULL,
                    FOREIGN KEY(""taskId"") REFERENCES ""tasks""(""id""),
                    PRIMARY KEY(""id"" AUTOINCREMENT)
                )
                ;";

            var command = connection.CreateCommand();
            command.CommandText = createTables;

            command.Parameters.AddWithValue("$dbVersion", dbVersion);
            command.Parameters.AddWithValue("createdOn", DateTime.Now.ToString());
            command.Parameters.AddWithValue("updatedOn", DateTime.Now.ToString());

            command.ExecuteNonQuery();

            return;
        }

        private static void UpdateDB(string curVersion)
        {
            Console.WriteLine(string.Format("Updating DB from {0} to {1}", curVersion, dbVersion));

            using SqliteConnection conn = new(connString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                switch (curVersion)
                {
                    case "0.1":
                        var command = conn.CreateCommand();

                        command.CommandText = @" 
                            CREATE TABLE """"notes"""" (
                                """"id""""	INTEGER,
                                """"desc""""	TEXT NOT NULL,
                                """"date""""	TEXT NOT NULL,
                                """"taskId""""	INTEGER NOT NULL,
                                FOREIGN KEY(""""taskId"""") REFERENCES """"tasks""""(""""id""""),
                                PRIMARY KEY(""""id"""" AUTOINCREMENT)
                            )
                            ;";
                        command.ExecuteNonQuery();
                        goto default;
                    default:
                        command = conn.CreateCommand();

                        command.CommandText = @"
                            UPDATE meta SET updatedOn = $updatedOn
                        ";

                        command.Parameters.AddWithValue("$updatedOn", DateTime.Now.ToString());

                        command.ExecuteNonQuery();
                        break;
                }

            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine(e.Message);
                System.Environment.Exit(1);
            }
            transaction.Commit();
        }

        private static string GetDBVersion()
        {
            using SqliteConnection conn = new(connString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                var command = conn.CreateCommand();

                command.CommandText = @"SELECT dbVersion FROM meta";

                object? result = command.ExecuteScalar();

                if (result == null || result.GetType() != typeof(string))
                {
                    throw new DBException("DB Version could not be retrieved from the Database.");
                }

                transaction.Commit();

                return (string)result;
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine(e.Message);
                System.Environment.Exit(1);
            }
            throw new DBException("Could not retrieve DB Version.");
        }

        public static Task? AddTask(Task task)
        {
            Task? insertedTask = null;
            long? insertedId = null;

            using (SqliteConnection conn = new(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {

                        var command = conn.CreateCommand();

                        command.CommandText =
                        @"
                        INSERT INTO tasks (
                            desc,
                            prio,
                            date, 
                            due, 
                            project,
                            status,
                            isCompleted
                        )
                        VALUES(
                            $Desc,
                            $Prio,
                            $Date,
                            $Due,
                            $Project,
                            $Status,
                            $IsCompleted
                        );
                        select last_insert_rowid();
                        ";

                        command.Parameters.AddWithValue("$Desc", task.Desc);
                        command.Parameters.AddWithValue("$Date", task.Date.ToString());
                        command.Parameters.AddWithValue("$Due", task.Due.ToString());
                        command.Parameters.AddWithValue("$Prio", task.Prio.Value);
                        command.Parameters.AddWithValue("$Project", task.Project);
                        command.Parameters.AddWithValue("$Status", task.Status.ToString());
                        command.Parameters.AddWithValue("$IsCompleted", task.IsCompleted.ToString());

                        insertedId = (long?)(command.ExecuteScalar());

                        if (insertedId == null)
                        {
                            throw new DBException("Insertion failed, no row id");
                        }

                        transaction.Commit();


                    }
                    catch (Exception e)
                    {
                        if (e is not DBException)
                        {
                            transaction.Rollback();
                            Console.WriteLine(e);
                            System.Environment.Exit(1);
                        }
                    }
                }
                if (insertedId != null) { insertedTask = GetTask((long)insertedId); }
            }

            return insertedTask;
        }

        public static Task? GetTask(long id)
        {
            Task? task = null;

            using (SqliteConnection conn = new(connString))
            {
                conn.Open();
                using var transaction = conn.BeginTransaction();
                var command = conn.CreateCommand();
                command.CommandText = selectAllFieldsFromTasks +
                @"
                        WHERE id = $id;
                    ";
                command.Parameters.AddWithValue("$id", id);

                var reader = command.ExecuteReader();

                IEnumerable<Task> tasks = ReaderToTaskList(reader);

                if (tasks.Count() > 1)
                {
                    transaction.Rollback();
                    throw new DBException("GetTask(id) returned more than one row");
                }

                transaction.Commit();
                task = tasks.FirstOrDefault();
            }
            return task;
        }

        public static IEnumerable<Task> GetTaskByDesc(string desc)
        {
            using SqliteConnection conn = new(connString);
            conn.Open();
            using SqliteTransaction transaction = conn.BeginTransaction();
            var command = conn.CreateCommand();
            command.CommandText = selectAllFieldsFromTasks +
            @"
                        WHERE desc LIKE $desc;
                    ";

            command.Parameters.AddWithValue("$desc", '%' + desc + '%');

            var reader = command.ExecuteReader();

            return ReaderToTaskList(reader);
        }

        public static IEnumerable<Task> GetTaskByPrio(string? desc, Priority? max, Priority? min, string? Project)
        {
            using SqliteConnection conn = new(connString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            bool firstCondition = true;

            var command = conn.CreateCommand();
            command.CommandText = selectAllFieldsFromTasks;

            if (desc != null)
            {
                firstCondition = false;
                command.CommandText += "WHERE desc LIKE $desc";
                command.Parameters.AddWithValue("$desc", '%' + desc + '%');
            }

            if (max != null)
            {
                firstCondition = false;
                command.CommandText += firstCondition ? "WHERE " : " AND " + "prio <= $max";
                command.Parameters.AddWithValue("$max", max.Value);
            }

            if (min != null)
            {
                firstCondition = false;
                command.CommandText += firstCondition ? "WHERE " : " AND " + "prio >= $min";
                command.Parameters.AddWithValue("$min", min.Value);
            }

            if (Project != null)
            {
                firstCondition = false;
                command.CommandText += firstCondition ? "WHERE " : " AND " + "project = $proj";
                command.Parameters.AddWithValue("$proj", Project);
            }

            command.CommandText += ';';

            var reader = command.ExecuteReader();

            return ReaderToTaskList(reader);
        }

        public static Task? UpdateTask(Task task)
        {
            if (task.Id == null)
            {
                throw new DBException("Task id must be provided to modify.");
            }

            using (SqliteConnection conn = new(connString))
            {
                conn.Open();
                using var transaction = conn.BeginTransaction();
                var command = conn.CreateCommand();

                command.CommandText = @"UPDATE tasks SET
                                                desc = $Desc,
                                                prio = $Prio,
                                                date = $Date, 
                                                due  = $Due, 
                                                project = $Project,
                                                status = $Status,
                                                isCompleted = $isCompleted
                                            WHERE id = $Id
                                           ";

                command.Parameters.AddWithValue("$Desc", task.Desc);
                command.Parameters.AddWithValue("$Prio", task.Prio.Value);
                command.Parameters.AddWithValue("$Date", task.Date.ToString());
                command.Parameters.AddWithValue("$Due", task.Due.ToString());
                command.Parameters.AddWithValue("$Project", task.Project);
                command.Parameters.AddWithValue("$Status", task.Status);
                command.Parameters.AddWithValue("$isCompleted", task.IsCompleted.ToString());
                command.Parameters.AddWithValue("$Id", task.Id);

                try
                {
                    if (command.ExecuteNonQuery() != 1)
                    {
                        throw new DBException("More than 1 task would be modified.");
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    transaction.Rollback();
                    Console.WriteLine("No modifications to DB were made.");
                    System.Environment.Exit(1);
                }
            }
            return GetTask((long)task.Id);
        }

        public static Note? AddNote(Note note)
        {
            Note? insertedNote = null;
            long? insertedId = null;

            using (SqliteConnection conn = new(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {

                        var command = conn.CreateCommand();

                        command.CommandText =
                        @"
                        INSERT INTO notes (
                            desc,
                            date, 
                            taskId
                        )
                        VALUES(
                            $Desc,
                            $Date,
                            $TaskId
                        );
                        select last_insert_rowid();
                        ";

                        command.Parameters.AddWithValue("$Desc", note.Desc);
                        command.Parameters.AddWithValue("$Date", note.Date.ToString());
                        command.Parameters.AddWithValue("$TaskId", note.TaskId);

                        insertedId = (long?)(command.ExecuteScalar());

                        if (insertedId == null)
                        {
                            throw new DBException("Insertion failed, no row id");
                        }

                        transaction.Commit();


                    }
                    catch (Exception e)
                    {
                        if (e is not DBException)
                        {
                            transaction.Rollback();
                            Console.WriteLine(e);
                            Console.WriteLine("\nThe database was not modified.");
                            System.Environment.Exit(1);
                        }
                    }
                }
                if (insertedId != null) { insertedNote = GetNote((long)insertedId); }
            }

            return insertedNote;
        }

        public static Note? GetNote(long id)
        {
            Note? note = null;

            using (SqliteConnection conn = new(connString))
            {
                conn.Open();
                using var transaction = conn.BeginTransaction();
                var command = conn.CreateCommand();
                command.CommandText = selectAllFieldsFromNotes +
                @"
                        WHERE id = $id;
                    ";
                command.Parameters.AddWithValue("$id", id);

                var reader = command.ExecuteReader();

                IEnumerable<Note> notes = ReaderToNoteList(reader);

                if (notes.Count() > 1)
                {
                    transaction.Rollback();
                    throw new DBException("GetNote(id) returned more than one row");
                }

                transaction.Commit();
                note = notes.FirstOrDefault();
            }
            return note;
        }

        public static IEnumerable<Note> GetNotes(long id)
        {
            using SqliteConnection conn = new(connString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var command = conn.CreateCommand();
            command.CommandText = selectAllFieldsFromNotes +
            @"
                        WHERE taskId = $id;
                    ";
            command.Parameters.AddWithValue("$id", id);

            var reader = command.ExecuteReader();


            transaction.Commit();

            return ReaderToNoteList(reader).OrderByDescending(note => note.Date);
        }

        private static IEnumerable<Note> ReaderToNoteList(SqliteDataReader reader)
        {
            List<Note> result = new();

            while (reader.Read())
            {
                result.Add(new Note(
                        reader.GetInt64(0), // Id,
                        reader.GetInt64(3), // TaskId
                        reader.GetString(1), // desc
                        reader.GetDateTime(2) // date
                        ));
            }

            return result;
        }

        private static IEnumerable<Task> ReaderToTaskList(SqliteDataReader reader)
        {
            List<Task> result = new();

            while (reader.Read())
            {
                result.Add(new Task(
                        reader.GetString(0), // desc
                        reader.GetInt32(1), // prio
                        reader.GetDateTime(2), // date
                        reader.GetDateTime(3), // due
                        reader.GetString(5), // project
                        reader.GetInt64(6), // Id,
                        reader.GetString(4), // Status
                        bool.Parse(reader.GetString(7)) // IsCompleted
                        ));
            }

            return result;
        }
    }

    class DBException : Exception
    {
        public DBException(string message) : base(message)
        {

        }

        public DBException() : base()
        {

        }
    }
}
