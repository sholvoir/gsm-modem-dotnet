using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Vultrue.Communication
{
    internal enum TaskResult { Unfinished = 0, Finished = 1, Repeat = 2 }

    internal interface ITask
    {
        string Instruction { get; }
        Exception ModemException { get; set; }
        List<Match> Matchs { get; }
        TaskResult DealLine(string line);
    }

    internal class ModemTask : ITask
    {
        private static Regex regexError1 = new Regex("^ERROR");
        private static Regex regexError2 = new Regex("^\\+CM[ES] ERROR:\\s*(?<errid>\\d+)");
        private static Regex regexOK = new Regex("^OK");

        private List<Match> matchs = new List<Match>();
        private string instruction, errorInfo, validateRegex;
        private Exception exception;
        private bool isNonOKCmd = false;
        private int nonOKDataLines = 1;

        public ModemTask(string instruction, string errorInfo, string validateRegex)
        {
            this.instruction = instruction;
            this.errorInfo = errorInfo;
            this.validateRegex = validateRegex;
        }

        public string Instruction { get { return instruction; } }
        public Exception ModemException { get { return exception; } set { exception = value; } }
        public List<Match> Matchs { get { return matchs; } }
        public bool IsNonOKCmd { set { isNonOKCmd = value; } }
        public int NonOKDataLines { set { nonOKDataLines = value; } }

        public TaskResult DealLine(string line)
        {
            Match match;
            if (regexError1.Match(line).Success)
            {
                exception = new ModemUnsupportedException(errorInfo);
                return TaskResult.Finished;
            }
            if ((match = regexError2.Match(line)).Success)
            {
                exception = new ModemUnsupportedException(int.Parse(match.Result("${errid}")), errorInfo);
                return TaskResult.Finished;
            }
            if (regexOK.Match(line).Success)
                return TaskResult.Finished;
            if (validateRegex.Length > 0)
                if ((match = new Regex(validateRegex).Match(line)).Success) matchs.Add(match);
                else
                {
                    exception = new ModemDataException(line);
                    return TaskResult.Finished;
                }
            if (!isNonOKCmd) return TaskResult.Unfinished;
            if (--nonOKDataLines <= 0) return TaskResult.Finished;
            else return TaskResult.Unfinished;
        }
    }

    internal class TaskGroup : ITask
    {
        private ModemTask[] taskgroup;
        private int index = 0;

        public TaskGroup(ModemTask[] taskgroup) { this.taskgroup = taskgroup; }

        public string Instruction { get { return taskgroup[index].Instruction; } }
        public Exception ModemException { get { return taskgroup[index].ModemException; } set { taskgroup[index].ModemException = value; } }
        public List<Match> Matchs { get { return taskgroup[index].Matchs; } }

        public TaskResult DealLine(string line)
        {
            TaskResult result = taskgroup[index].DealLine(line);
            if (result == TaskResult.Unfinished)
                return result;
            if (taskgroup[index].ModemException != null)
                return TaskResult.Finished;
            if (index >= taskgroup.Length - 1)
                return TaskResult.Finished;
            index++;
            return TaskResult.Repeat;
        }
    }
}
