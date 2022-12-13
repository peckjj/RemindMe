using RemindMe.Models;
using RemindMe.Constants;
using Microsoft.Data.Sqlite;
using Task = RemindMe.Models.Task;
using System.Reflection;

namespace RemindMe
{
    internal class RemindMe
    {
        static ArgParseOption[] options = { };
        static string? command = null;

        static bool verbose = false;
        static long? id = null;
        static string[] data = new string[] { };

        static Priority? priority = null;
        static Priority? minPrio = null;
        static Priority? maxPrio = null;
        static void Main(string[] args)
        {

            options = new ArgParseOption[]
            {
                new ArgParseOption (
                    new string[]{CommandConstants.ADD, CommandConstants.GET},
                    val => command = val,
                    null,
                    null,
                    passArgToLambda: true
                    ),
                new ArgParseOption (
                    new string[]{"--verbose", "-v"},
                    val => verbose = true,
                    "Turns on verbose logging to console"
                    ),
                new ArgParseOption (
                    new string[] {"--id=", "-id="},
                    val =>
                    {
                        if (val != null)
                        {
                            id = long.Parse(val);
                        } else
                        {
                            Console.WriteLine("No id given.");
                            Usage();
                            System.Environment.Exit(0);
                        }
                    },
                    "Filter by a specific task <ID>. Returns only one result",
                    "<ID>"
                    ),
                new ArgParseOption (
                    new string[] {"--priority=", "--prio=", "-p="},
                    val =>
                    {
                        if (val != null)
                        {
                            priority = new Priority(int.Parse(val));
                        } else
                        {
                            Console.WriteLine("No priority given.");
                            Usage();
                            System.Environment.Exit(0);
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
                                System.Environment.Exit(0);
                            }

                        } else
                        {
                            Console.WriteLine("No minimum priority given.");
                            Usage();
                            System.Environment.Exit(0);
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
                                System.Environment.Exit(0);
                            }
                        } else
                        {
                            Console.WriteLine("No maximum priority given.");
                            Usage();
                            System.Environment.Exit(0);
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
                    )
            };
            try
            {
                data = ArgParse.ParseArgs(options, args).ToArray();
            } catch (ArgParseException e)
            {
                if (e is ArgParseNoMatchException || e is ArgParseNoValueException)
                {
                    Console.WriteLine(e.Message);
                    Usage();
                    return;
                }
            }

            if (command == null)
            {
                Usage();
                return;
            }

            if (command == CommandConstants.ADD)
            // Add task to DB
            {
                if (data.Length < 1)
                {
                    Usage();
                    return;
                }

                if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/reminders.db"))
                {
                    Console.WriteLine("Database does not exist, creating reminders.db");
                    Database.CreateDb();
                }

                // Allows for shorthand, you can add a task without quotes
                Task? task = Database.AddTask(new Task(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur),
                                                       new Priority(priority != null ? priority.Value : PriorityConstants.MED)));

                if (task != null)
                {
                    if (verbose)
                    {
                    Console.WriteLine("Created new Task");
                    }
                    DisplayTasks(new Task[] { task });
                } else
                {
                    Console.WriteLine("Task insertion failed.");
                }

                return;
            } else if (command == CommandConstants.GET)
            // Get tasks from DB and display
            {
                Task?[] tasks;

                if (id != null)
                {
                    tasks = new Task?[] { Database.GetTask((long)id) };
                } else if (priority != null || minPrio != null || maxPrio != null)
                {
                    if (priority != null)
                    {
                        tasks = Database.GetTaskByPrio(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur), priority, priority).ToArray();
                    } else
                    {
                        tasks = Database.GetTaskByPrio(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur), maxPrio, minPrio).ToArray();
                    }
                }
                else
                {
                    tasks = Database.GetTaskByDesc(data[0]).ToArray();
                }

                DisplayTasks(tasks);
            }
            else
            {
                Usage();
            }

        }

        static void DisplayTasks(Task?[]? tasks)
        {
            if (tasks == null || tasks.Length < 1) { Console.WriteLine("No tasks found."); return; }
            if (verbose) { Console.WriteLine(String.Format("{0} tasks found:", tasks.Length)); }
            foreach (Task? task in tasks)
            {
                if (task != null)
                {
                    Console.WriteLine(String.Format("{0}:\t{1}\t{2}", task.Id, task.Desc, task.Prio));
                }
            }
        }

        static void Usage()
        {
            Console.WriteLine("Usage: <Command> [opts]\nCommands:");
            Console.WriteLine("\nadd <task> [opts]:\t\tAdds a new task to the list");
            Console.WriteLine("get <task> [opts]:\t\tGets a list of tasks. [opts] can be used to apply filters. <task> will be used to search for similar tasks");
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
                                                                        option.paramDesc == null ? "" : " " + option.paramDesc + " ", 
                                                                        option.desc));
                    foreach (string alias in option.aliases.Skip(1))
                    {
                        Console.WriteLine(String.Format("{0}", alias.Replace("=", "")));
                    }
                }
            }
        }
    }
}