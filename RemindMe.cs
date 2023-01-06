using RemindMe.Constants;
using RemindMe.Models;
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
        static long? id = null;
        static string[] data = Array.Empty<string>();

        static Priority? priority = null;
        static Priority? minPrio = null;
        static Priority? maxPrio = null;

        static bool ignoreCompleted = false;
        static void Main(string[] args)
        {

            options = new ArgParseOption[]
            {
                new ArgParseOption (
                    CommandConstants.ADD.Concat(CommandConstants.GET).Concat(CommandConstants.MODIFY).ToArray(),
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
                            Console.WriteLine(String.Format("Discovered the command '{0}' in the description. Please use quotes for descriptions which include command aliases.", val));
                            System.Environment.Exit(1);
                        }
                    },
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
                            try
                            {
                                id = long.Parse(val);
                            } catch (FormatException)
                            {
                                Console.WriteLine("id must be an integer value, cannot accept \"" + val + "\"");
                                Usage();
                                System.Environment.Exit(1);
                            }
                        } else
                        {
                            Console.WriteLine("No id given.");
                            Usage();
                            System.Environment.Exit(1);
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
                            Usage();
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
                            Usage();
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
                    Usage();
                    System.Environment.Exit(1);
                }
            }

            if (command == null)
            {
                Usage();
                return;
            }

            if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/reminders.db"))
            {
                Console.WriteLine("Database does not exist, creating reminders.db");
                Database.CreateDb();
            }

            /**
             * ADD Command
             */

            if (CommandConstants.ADD.Contains(command))
            // Add task to DB
            {
                if (data.Length < 1)
                {
                    Usage();
                    return;
                }

                // Allows for shorthand, you can add a task without quotes
                Task? task = Database.AddTask(new Task(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur),
                                                       new Priority(priority != null ? priority.Value : PriorityConstants.MED), complete));

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
                else if (priority != null || minPrio != null || maxPrio != null)
                {
                    if (priority != null)
                    {
                        tasks = Database.GetTaskByPrio(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur), priority, priority).ToArray();
                    }
                    else
                    {
                        tasks = Database.GetTaskByPrio(data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur), maxPrio, minPrio).ToArray();
                    }
                }
                else
                {
                    string desc = "";

                    if (data.Any())
                    {
                        desc = data[0] + data.Skip(1).Aggregate("", (acc, cur) => acc + " " + cur);
                    }
                    tasks = Database.GetTaskByDesc(desc).ToArray();

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
                    Usage();
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
                //task.Project = ... No option for setting Project yet.
                //task.Status = ... No option for setting Status yet.
                task.IsCompleted = complete | task.IsCompleted;
                task = Database.UpdateTask(task);

                if (verbose)
                {
                    ignoreCompleted = true;
                    DisplayTasks(new Task?[] { task });
                }


            }
            else
            {
                Usage();
            }

        }

        static void DisplayTasks(Task?[]? tasks)
        {
            Task?[] filteredTasks = Array.Empty<Task?>();

            filteredTasks = tasks != null ? tasks.Where(t => t != null && (t.IsCompleted == complete || ignoreCompleted)).ToArray() : Array.Empty<Task?>();

            if (filteredTasks.Length < 1) { Console.WriteLine("No tasks found."); return; }

            if (verbose) { Console.WriteLine(String.Format("{0} tasks found:", filteredTasks.Length)); }
            foreach (Task? task in filteredTasks)
            {
                if (task != null)
                {
                    Console.WriteLine(string.Format("{0}:\t{1}\t{2}", task.Id, task.Desc, task.Prio));
                }
            }
        }

        static void Usage()
        {
            Console.WriteLine("Usage: reme <Command> [opts]\nCommands:");
            Console.WriteLine("\nadd <task> [opts]:\t\tAdds a new task to the list");
            Console.WriteLine("get <task> [opts]:\t\tGets a list of tasks. [opts] can be used to apply filters. <task> will be used to search for similar tasks");
            Console.WriteLine("mod|modify <task> [opts]:\tModifies a task with a given ID. The --id option must be provided. Overwrites Task description with <task> and other fields for provided [opts].");
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
    }
}