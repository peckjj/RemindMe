using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemindMe.Constants;

namespace RemindMe.Models
{
    public class Priority
    {
        private int value;

        public int Value
        { get { return value; } 
          set
            {
                this.value = Math.Clamp(value, PriorityConstants.HIGH, PriorityConstants.LOW);
            } 
        }

        public Priority(int p)
        {
            Value = p;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
