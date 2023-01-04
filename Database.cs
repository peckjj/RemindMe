using Microsoft.Data.Sqlite;
using Task = RemindMe.Models.Task;
using RemindMe.Models;
using System.Reflection;

namespace RemindMe
{
    internal class Database
    {
        private static readonly string dbVersion = "0.1";

        private static string connString = "Data Source=" +
                                           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                           "/reminders.db";
        private static string allFieldsFromTasks = "desc, prio, date, due, status, project, id, isCompleted";
        private static string selectAllFieldsFromTasks = "SELECT " + allFieldsFromTasks + " FROM tasks ";
        internal static void CreateDb()
        {
            using (SqliteConnection connection = new SqliteConnection(connString)
                  )
            {
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
	                ""db_version""	TEXT NOT NULL UNIQUE,
	                ""createdOn""	TEXT NOT NULL,
	                ""updatedOn""	INTEGER NOT NULL
                )
                ;
                INSERT INTO meta VALUES(
                    $dbVersion,
                    $createdOn,
                    $updatedOn
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
        }
        public static Task? AddTask(Task task)
        {
            Task? insertedTask = null;
            long? insertedId = null;

            using (SqliteConnection conn = new SqliteConnection(connString))
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


                    } catch (Exception e)
                    {
                        if ( !( e is DBException ) )
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

            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
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
                        throw new Exception("GetTask(id) returned more than one row");
                    }

                    transaction.Commit();
                    task = tasks.FirstOrDefault();
                }
            }
            return task;
        }

        public static IEnumerable<Task> GetTaskByDesc(string desc)
        {
            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    var command = conn.CreateCommand();
                    command.CommandText = selectAllFieldsFromTasks +
                    @"
                        WHERE desc LIKE $desc;
                    ";

                    command.Parameters.AddWithValue("$desc", '%' + desc + '%');

                    var reader = command.ExecuteReader();

                    return ReaderToTaskList(reader);
                }
            }
        }

        public static IEnumerable<Task> GetTaskByPrio(string? desc, Priority? max, Priority? min = null) 
        {
            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
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

                    command.CommandText += ';';

                    var reader = command.ExecuteReader();

                    return ReaderToTaskList(reader);
                }
            }
        }

        public static Task? UpdateTask(Task task)
        {
            Task? result = null;

            if (task.Id == null)
            {
                throw new DBException("Task id must be provided to modify.");
            }

            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
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
            }
            return GetTask((long)task.Id);
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
