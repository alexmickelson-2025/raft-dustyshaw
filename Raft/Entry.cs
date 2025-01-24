using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft;

public class Entry
{
    public string Command { get; set; } = "";
    public int TermReceived { get; set; }

    public Entry(string Command)
    {
        this.Command = Command;
    }

	public Entry(string Command, int TermReceived)
	{
		this.Command = Command;
        this.TermReceived = TermReceived;
	}
}
