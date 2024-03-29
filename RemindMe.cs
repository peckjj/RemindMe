﻿using DataTablePrettyPrinter;
using RemindMe.Constants;
using RemindMe.Models;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using Task = RemindMe.Models.Task;

namespace RemindMe
{
    internal class RemindMe
    {
        static ArgParseOption[] options = Array.Empty<ArgParseOption>();
        static string? command = null;

        static bool verbose = false;
        static bool complete = false;
        static bool getNotes = false;
        static bool getVersion = false;

        static long? id = null;
        static int taskWidth = -1;
        static string[] data = Array.Empty<string>();

        static Priority? priority = null;
        static Priority? minPrio = null;
        static Priority? maxPrio = null;

        static string? Project = null;

        static bool ignoreCompleted = false;
        static void Main(string[] args)
        {

            options = new ArgParseOption[]
            {
                new ArgParseOption (
                    CommandConstants.ADD.Concat(CommandConstants.GET).Concat(CommandConstants.MODIFY).Concat(CommandConstants.NOTE).ToArray(),
                    val =>
                    {
                        if (val == null)
                        {
                            return;
                        }

                        if (command == null)
                        {
                            command = val;
                        } else
                        {
                            Console.WriteLine(String.Format("Discovered the command '{0}' in the description. Only one command is allowed. Please use quotes for descriptions which include command aliases.", val));
                            System.Environment.Exit(1);
                        }
                    },
                    null,
                    null,
                    passArgToLambda: true
                    ),
                new ArgParseOption (
                    new string[]{"--verbose", "-V"},
                    val => verbose = true,
                    "Turns on verbose logging to console"
                    ),
                new ArgParseOption (
                    new string[] {"--id=", "-id="},
                    val =>
                    {
                        if (val != null)
                        {
                            try
                            {
                                id = long.Parse(val);
                            } catch (FormatException)
                            {
                                Console.WriteLine("id must be an integer value, cannot accept \"" + val + "\"");
                                UsageLite();
                                System.Environment.Exit(1);
                            }
                        } else
                        {
                            Console.WriteLine("No id given.");
                            UsageLite();
                            System.Environment.Exit(1);
                        }
                    },
                    "Filter by a specific task <ID>. Returns only one result",
                    "<ID>"
                    ),
                new ArgParseOption (
                    new string[] {"--project=", "--proj", "-p="},
                    val =>
                    {
                        Project = val;
                    }
                    ),
                new ArgParseOption (
                    new string[] {"--priority=", "--prio=", "-P="},
                    val =>
                    {
                        if (val != null)
                        {
                            priority = new Priority(int.Parse(val));
                        } else
                        {
                            Console.WriteLine("No priority given.");
                            UsageLite();
                            System.Environment.Exit(1);
                        }
                    },
                    "Filter tasks by a given priority",
                    "<priority>"
                    ),
                new ArgParseOption (
                    new string[] {"--minPrio=", "--min=", "-m="},
                    val =>
                    {
                        if (val != null)
                        {
                            minPrio = new Priority(int.Parse(val));

                            if (maxPrio != null && minPrio > maxPrio)
                            {
                                Console.WriteLine("Maximum priority cannot be less than Minimum priority.");
                                System.Environment.Exit(1);
                            }

                        } else
                        {
                            Console.WriteLine("No minimum priority given.");
                            UsageLite();
                            System.Environment.Exit(1);
                        }
                    },
                    "Filter tasks by a minimum priority, inclusively.",
                    "<priority>"
                    ),
                new ArgParseOption (
                    new string[] {"--maxPrio=", "--max=", "-M="},
                    val =>
                    {
                        if (val != null)
                        {
                            maxPrio = new Priority(int.Parse(val));

                            if (minPrio != null && maxPrio < minPrio)
                            {
                                Console.WriteLine("Maximum priority cannot be less than Minimum priority.");
                                System.Environment.Exit(1);
                            }
                        } else
                        {
                            Console.WriteLine("No maximum priority given.");
                            UsageLite();
                            System.Environment.Exit(1);
                        }
                    },
                    "Filter tasks by a given maximum priority, inclusively.",
                    "<priority>"
                    ),
                new ArgParseOption (
                    new string[] {"--help", "-help", "--?", "-?", "/?", "-h", "--h"},
                    val =>
                    {
                        Usage();
                        System.Environment.Exit(0);
                    }
                    ),
                new ArgParseOption (
                    new string[] {"--complete", "-c"},
                    val => complete = true,
                    "Marks a task as completed, or filters by complete tasks, which are normally hidden"
                    ),
                new ArgParseOption (
                    new string[] {"--taskWidth=", "-w="},
                    val => {
                    if (val != null)
                        {
                            try
                            {
                                taskWidth = int.Parse(val);
                            } catch (FormatException)
                            {
                                Console.WriteLine("Task Width must be given as integer character length.");
                                UsageLite();
                                System.Environment.Exit(1);
                            }

                            if (taskWidth < 1)
                            {
                                Console.WriteLine("Task Width must be a positive non-zero integer.");
                                UsageLite();
                                System.Environment.Exit(1);
                            }
                        }
                    },
                    "Overrides default maximum length for displaying task descriptions."
                    ),
                new ArgParseOption (
                    new string[] {"--note", "-n"},
                    val => getNotes = true,
                    "Get the notes for a Task with a given <id>"
                    ),
                new ArgParseOption (
                    new string[] {"--version", "-v"},
                    val => getVersion = true,
                    "Returns the current version of reme.exe"
                    )
            };
            try
            {
                data = ArgParse.ParseArgs(options, args).ToArray();
            }
            catch (ArgParseException e)
            {
                if (e is ArgParseNoMatchException || e is ArgParseNoValueException)
                {
                    Console.WriteLine(e.Message);
                    UsageLite();
                    System.Environment.Exit(1);
                }
            }

            if (command == null && !getVersion)
            {
                UsageLite();
                return;
            }

            if (getVersion)
            {
                Console.WriteLine("v" + getFileVersion());
                return;
            }

            Database.CheckDB();
            /**
             * ADD Command
             */

            if (CommandConstants.ADD.Contains(command))
            // Add task to DB
            {
                if (data.Length < 1)
                {
                    Console.WriteLine("A task must have a description body.");
                    UsageLite();
                    System.Environment.Exit(1);
                }

                // Allows for shorthand, you can add a task without quotes
                Task? task = Database.AddTask(
                    new Task(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur),
                             new Priority(priority != null ? priority.Value : PriorityConstants.MED),
                             DateTime.Now,
                             DateTime.MaxValue,
                             Project,
                             null,
                             "active",
                             complete));

                if (task != null)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Created new Task");
                    }
                    DisplayTasks(new Task[] { task });
                }
                else
                {
                    Console.WriteLine("Task insertion failed.");
                }

                return;

                /**
                * GET Command
                */

            }
            else if (CommandConstants.GET.Contains(command))
            // Get tasks from DB and display
            {
                Task?[] tasks = Array.Empty<Task?>();

                if (getNotes)
                {
                    GetAndDisplayNotes();
                    return;
                }

                string desc = "";
                if (data.Any())
                {
                    desc = data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur);
                }

                if (id != null)
                {
                    tasks = new Task?[] { Database.GetTask((long)id) };

                    if (tasks[0] == null)
                    {
                        Console.WriteLine(String.Format("No tasks with ID: {0} found", id));
                        return;
                    }

                    ignoreCompleted = true;
                }
                else
                {
                    if (priority != null)
                    {
                        tasks = Database.GetTaskByPrio(desc, priority, priority, Project).ToArray();
                    }
                    else
                    {
                        tasks = Database.GetTaskByPrio(desc, maxPrio, minPrio, Project).ToArray();
                    }
                }

                DisplayTasks(tasks);

                /**
                * MODIFY Command
                */

            }
            else if (CommandConstants.MODIFY.Contains(command))
            {
                if (id == null)
                {
                    Console.Write("ID of task must be given when using the 'modify' command");
                    UsageLite();
                    System.Environment.Exit(1);
                }

                string desc = "";

                if (data.Length > 0)
                {
                    desc = data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur);
                }


                Task? task = Database.GetTask((long)id);

                if (task == null)
                {
                    Console.Write(String.Format("No task with id {0} found", id));
                    System.Environment.Exit(1);
                }

                task.Desc = desc == "" ? task.Desc : desc;
                task.Prio = priority ?? task.Prio;
                //task.Date = ... No option for Date yet. TODO: Need to add a lastModified field?
                //task.Due = ... No option for setting Due date yet.
                task.Project = Project ?? task.Project;
                //task.Status = ... No option for setting Status yet.
                task.IsCompleted = complete | task.IsCompleted;
                task = Database.UpdateTask(task);

                if (verbose)
                {
                    ignoreCompleted = true;
                    DisplayTasks(new Task?[] { task });
                }


            }
            else if (CommandConstants.NOTE.Contains(command))
            {
                if (data.Length < 1)
                {
                    Console.WriteLine("A note must have a description body.");
                    UsageLite();
                    System.Environment.Exit(1);
                }

                string desc = data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur);

                if (id == null)
                {
                    Console.Write("Creating a note requires the id of a Task.");
                    UsageLite();
                    System.Environment.Exit(1);
                }
                // Allows for shorthand, you can add a note without quotes
                Note? note = Database.AddNote(new Note((long)id, desc, DateTime.Now));

                DisplayNotes(new Note?[] { note });

            }
            else
            {
                Console.WriteLine("Command not recognized.");
                UsageLite();
                System.Environment.Exit(1);
            }

        }

        static void GetAndDisplayNotes()
        {
            if (id == null)
            {
                Console.WriteLine("Fetching notes requires the id of a Task.");
                UsageLite();
                System.Environment.Exit(1);
            }

            Note[] notes = Database.GetNotes((long)id).ToArray();

            DisplayNotes(notes);

            return;
        }

        static void DisplayNotes(Note?[]? notes)
        {
            if (notes == null || id == null) return;

            DisplayTasks(new Task?[] { Database.GetTask((long)id) });

            if (notes.Length < 1)
            {
                Console.WriteLine($"No notes for Task ID: {id}");
            }

            Console.WriteLine();

            foreach (Note? note in notes)
            {
                if (note != null)
                {
                    Console.WriteLine($"{note.Desc}");
                }
            }
        }

        static void DisplayTasks(Task?[]? tasks)
        {
            Task?[] filteredTasks = Array.Empty<Task?>();

            filteredTasks = tasks != null ? tasks.Where(t => t != null && (t.IsCompleted == complete || ignoreCompleted)).ToArray() : Array.Empty<Task?>();

            if (filteredTasks.Length < 1) { Console.WriteLine("No tasks found."); return; }

            Console.WriteLine(
                (verbose ? VerboseTable(filteredTasks) : SimpleTable(filteredTasks)).ToPrettyPrintedString()
            );
        }

        static DataTable SimpleTable(Task?[] tasks)
        {
            DataTable table = new(string.Format("Found {0} tasks", tasks.Length));

            table.Columns.Add("ID", typeof(long));
            table.Columns.Add("Task", typeof(string));
            table.Columns.Add("Due", typeof(DateTime));
            table.Columns.Add("Prio", typeof(int));
            table.Columns.Add("Project", typeof(string));

            table.Columns[1].SetWidth(taskWidth > 0 ? taskWidth : DisplayConstants.TASK_DISPLAY_WIDTH);

            table.Columns[2].SetDataTextFormat((DataColumn c, DataRow r) =>
            {
                DateTime date = r.Field<DateTime>(c);

                return date.ToShortDateString();
            });

            foreach (Task? task in tasks)
            {
                if (task != null)
                {
                    table.Rows.Add(task.Id, task.Desc, task.Due, task.Prio.Value, task.Project);
                }
            }

            return table;
        }

        static DataTable VerboseTable(Task?[] tasks)
        {
            DataTable table = new(string.Format("Found {0} tasks", tasks.Length));

            table.Columns.Add("ID", typeof(long));
            table.Columns.Add("Task", typeof(string));
            table.Columns.Add("Due", typeof(DateTime));
            table.Columns.Add("Prio", typeof(int));
            table.Columns.Add("Project", typeof(string));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Completed", typeof(bool));

            if (taskWidth > 0)
            {
                table.Columns[1].SetWidth(taskWidth);
            }

            foreach (Task? task in tasks)
            {
                if (task != null)
                {
                    table.Rows.Add(task.Id, task.Desc, task.Due, task.Prio.Value, task.Project, task.Date, task.Status, task.IsCompleted);
                }
            }

            return table;
        }

        static void UsageLite()
        {
            Console.WriteLine("reme.exe v" + getFileVersion());

            Console.WriteLine("Usage: reme <Command> [opts]\nCommands:");
            Console.WriteLine("\nadd <task> [opts]:\t\tAdds a new task to the list");
            Console.WriteLine("get <task> [opts]:\t\tGets a list of tasks. [opts] can be used to apply filters. <task> will be used to search for similar tasks");
            Console.WriteLine("mod|modify <task> [opts]:\tModifies a task with a given ID. The --id option must be provided. Overwrites Task description with <task> and other fields for provided [opts].");

            Console.WriteLine("\nType 'reme -h' to view detailed options.\n");
        }

        static void Usage()
        {
            UsageLite();

            if (options == null)
            {
                return;
            }

            Console.WriteLine("\nOpts:");

            foreach (ArgParseOption option in options)
            {
                if (option.desc != null)
                {
                    Console.WriteLine(String.Format("\n{0}{1}:\t\t{2}", option.aliases[0].Replace("=", ""),
                                                                        option.optParam == null ? "" : " " + option.optParam + " ",
                                                                        option.desc));
                    foreach (string alias in option.aliases.Skip(1))
                    {
                        Console.WriteLine(String.Format("{0}", alias.Replace("=", "")));
                    }
                }
            }
        }

        static string getFileVersion()
        {
            string? FilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location + "./reme.exe");

            if (FilePath != null)
            {
                string? FileVersion = FileVersionInfo.GetVersionInfo(FilePath).FileVersion;

                if (FileVersion != null)
                {
                    return FileVersion;
                }
            }

            throw new Exception("Could not retrieve File Version.");
        }
    }
}