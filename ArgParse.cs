namespace RemindMe
{
    public class ArgParseOption
    {
        public Action<string?> lambda = (val) => { return; };
        public string[] aliases = Array.Empty<string>();
        public bool passArgToLambda = false;
        public string? desc;
        public string? optParam;

        public ArgParseOption(string[] aliases, Action<string?> lambda, string? desc = null, string? paramDesc = null, bool passArgToLambda = false)
        {
            this.lambda = lambda;
            this.aliases = aliases;
            this.desc = desc;
            this.optParam = paramDesc;
            this.passArgToLambda = passArgToLambda;
        }
    }
    internal class ArgParse
    {
        public static IEnumerable<string> ParseArgs(ArgParseOption[] opts, string[] args)
        {
            List<string> remaining = new();

            for (int i = 0; i < args.Length; i++)
            {
                string curArg = args[i];

                // find option in opts that corresponds to the current arg.
                // If more/less than one is found, throw error
                IEnumerable<ArgParseOption> matchedOps = opts.Where(opt => opt.aliases.Select(alias => alias.Replace("=", "")).Contains(curArg));
                if (matchedOps.Count() != 1)
                {
                    if (!matchedOps.Any())
                    {
                        // Todo: bug here, this will break if arguments have a '-' in the middle (not at the start)
                        if (args[i].Contains('-'))
                        {
                            throw new ArgParseNoMatchException(String.Format("No option corresponds to the argument {0}.", args[i]));
                        }
                        remaining.Add(args[i]);
                        // Continue because we do not need to store or execute arg values / lambdas
                        continue;
                    }
                    else
                    {
                        throw new ArgParseException(String.Format("More than 1 option corresponds to the argument {0}.", args[i]));
                    }
                }
                ArgParseOption curOpt = matchedOps.First();

                string? val = null;

                if (curOpt.aliases.Where(alias => alias.Contains('=')).Any())
                {
                    if (args.Length == i)
                    {
                        throw new ArgParseNoValueException(String.Format("No value could be parsed for {0}", args[i]));
                    }

                    // Increment i before being accessed to continue the for-loop
                    try
                    {
                        val = args[++i];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new ArgParseNoValueException("No value given for option: " + args[--i]);
                    }
                }

                if (curOpt.passArgToLambda)
                {
                    curOpt.lambda(args[i]);
                }
                else
                {
                    curOpt.lambda(val);
                }

            }

            return remaining;
        }
    }

    public class ArgParseException : Exception
    {
        public ArgParseException(string message) : base(message)
        { }

        public ArgParseException() : base()
        { }
    }

    public class ArgParseNoMatchException : ArgParseException
    {
        public ArgParseNoMatchException(string message) : base(message)
        { }

        public ArgParseNoMatchException() : base()
        { }
    }

    public class ArgParseNoValueException : ArgParseException
    {
        public ArgParseNoValueException(string message) : base(message)
        { }

        public ArgParseNoValueException() : base()
        { }
    }
}
