using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft;

public class Log
{
    public string Command { get; set; } = "";
    public int TermReceived { get; set; }
}
