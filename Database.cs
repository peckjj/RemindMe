﻿using Microsoft.Data.Sqlite;
using Task = RemindMe.Models.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemindMe.Models;
using TaskStatus = RemindMe.Models.TaskStatus;
using System.Reflection;

namespace RemindMe
{
    internal class Database
    {
        private static string connString = "Data Source=" +
                                           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                           "/reminders.db";
        internal static void CreateDb()
        {
            using (SqliteConnection connection = new SqliteConnection(connString)
                  )
            {
                connection.Open();

                // Create tasks table
                string taskTable = @"
                CREATE TABLE ""tasks"" (
                    ""id""      INTEGER,
	                ""desc""    TEXT NOT NULL,
	                ""date""    TEXT NOT NULL,
	                ""due""     TEXT,
	                ""prio""    INTEGER NOT NULL,
                    ""project"" TEXT,
                    ""status""  TEXT NOT NULL,
	                PRIMARY KEY(""id"" AUTOINCREMENT)
                ); ";

                var command = connection.CreateCommand();
                command.CommandText = taskTable;

                command.ExecuteNonQuery();

                return;
            }
        }
        public static Task? AddTask(Task task)
        {
            Task? insertedTask;
            long insertedId;

            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
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
                        status
                    )
                    VALUES(
                        $Desc,
                        $Prio,
                        $Date,
                        $Due,
                        $Project,
                        $Status
                    );
                    select last_insert_rowid();
                ";

                    command.Parameters.AddWithValue("$Desc", task.Desc);
                    command.Parameters.AddWithValue("$Date", task.Date.ToString());
                    command.Parameters.AddWithValue("$Due", task.Due.ToString());
                    command.Parameters.AddWithValue("$Prio", task.Prio.Value);
                    command.Parameters.AddWithValue("$Project", task.Project);
                    command.Parameters.AddWithValue("$Status", task.Status.ToString());

                    var id = command.ExecuteScalar();

                    if (id == null)
                    {
                        transaction.Rollback();
                        throw new Exception("Insertion failed, no row id");
                    }

                    transaction.Commit();
                    
                    insertedId = (long)id;
                    insertedTask = GetTask(insertedId);
                }
                return insertedTask;
            }
        }

        public static Task? GetTask(long id)
        {
            Task? task;

            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    var command = conn.CreateCommand();
                    command.CommandText =
                    @"
                        SELECT desc, prio, date, due, status, project, id FROM tasks
                        WHERE id = $id;
                    ";
                    command.Parameters.AddWithValue("$id", id);

                    var reader = command.ExecuteReader();

                    if (!reader.Read())
                    {
                        transaction.Rollback();
                        return null;
                    }

                    task = new Task(
                        reader.GetString(0), // desc
                        reader.GetInt32(1), // prio
                        reader.GetDateTime(2), // date
                        reader.GetDateTime(3), // due
                        reader.GetString(5), // project
                        reader.GetInt64(6), // Id,
                        new TaskStatus(reader.GetString(4)) // Status
                    );

                    if (reader.Read())
                    {
                        transaction.Rollback();
                        throw new Exception("GetTask(id) returned multiple rows");
                    }

                    transaction.Commit();
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
                    command.CommandText =
                    @"
                        SELECT desc, prio, date, due, status, project, id FROM tasks
                        WHERE desc LIKE $desc;
                    ";

                    command.Parameters.AddWithValue("$desc", '%' + desc + '%');

                    var reader = command.ExecuteReader();

                    return ReaderToTaskList(reader).ToList();
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
                    var command = conn.CreateCommand();
                    command.CommandText =
                    @"
                        SELECT desc, prio, date, due, status, project, id FROM tasks
                    ";

                    if (desc != null)
                    {
                        command.CommandText += "WHERE desc LIKE $desc";
                        command.Parameters.AddWithValue("$desc", '%' + desc + '%');
                    }

                    if (max != null)
                    {
                        command.CommandText += " AND prio <= $max";
                        command.Parameters.AddWithValue("$max", max.Value);
                    }

                    if (min != null)
                    {
                        command.CommandText += " AND prio >= $min";
                        command.Parameters.AddWithValue("$min", min.Value);
                    }

                    command.CommandText += ';';

                    var reader = command.ExecuteReader();

                    return ReaderToTaskList(reader);
                }
            }
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
                        new TaskStatus(reader.GetString(4)) // Status
                        ));
            }

            return result;
        }
    }
}
