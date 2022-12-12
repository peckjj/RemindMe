using RemindMe.Models;
using RemindMe.Constants;
using Microsoft.Data.Sqlite;
using Task = RemindMe.Models.Task;
using System.Reflection;

namespace RemindMe
{
    internal class RemindMe
    {
        static void Main(string[] args)
        {
            string? command = null;

            bool verbose = false;
            long? id = null;
            string[] data = new string[] { };

            ArgParseOption[] options = new ArgParseOption[]
            {
                new ArgParseOption (
                    new string[]{CommandConstants.ADD, CommandConstants.GET},
                    val => command = val,
                    null,
                    null,
                    passArgToLambda: true
                    ),
                new ArgParseOption (
                    new string[]{"-verbose", "--verbose", "-v"},
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
                        }
                    },
                    "Filter by a specific task <ID>. Returns only one result",
                    "<ID>"
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
                    Usage(options);
                    return;
                }
            }

            if (command == null)
            {
                Usage(options);
                return;
            }

            if (command == CommandConstants.ADD)
            // Add task to DB
            {
                if (data.Length < 1)
                {
                    Usage(options);
                    return;
                }

                if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/reminders.db"))
                {
                    Console.WriteLine("Database does not exist, creating reminders.db");
                    Database.CreateDb();
                }

                // Allows for shorthand, you can add a task without quotes
                Task? task = Database.AddTask(new Task(data.Aggregate("", (acc, cur) => acc + " " + cur).Trim()));

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
                } else
                {
                    tasks = Database.GetTaskByDesc(data[0]).ToArray();
                }

                DisplayTasks(tasks);
            }
            else
            {
                Usage(options);
            }

        }

        static void DisplayTasks(Task?[]? tasks)
        {
            if (tasks == null) { return; }
            foreach (Task? task in tasks)
            {
                if (task != null)
                {
                    Console.WriteLine(String.Format("{0}:\t{1}\t{2}", task.Id, task.Desc, task.Prio));
                }
            }
        }

        static void Usage(ArgParseOption[] opts = null)
        {
            Console.WriteLine("Usage: <Command> [opts]\nCommands:");
            Console.WriteLine("\nadd <task> [opts]:\t\tAdds a new task to the list");
            Console.WriteLine("get <task> [opts]:\t\tGets a list of tasks. [opts] can be used to apply filters. <task> will be used to search for similar tasks");
            if (opts == null)
            {
                return;
            }

            Console.WriteLine("\nOpts:");

            foreach (ArgParseOption option in opts)
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