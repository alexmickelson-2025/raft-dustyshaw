using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft;

public class Entry
{
    public string Key { get; set; } 
    public string Command { get; set; }
    public int TermReceived { get; set; }

    public Entry(string Key, string Command)
    {
        this.Key = Key;
        this.Command = Command;
    }

	public Entry(string Key, string Command, int TermReceived)
	{
		this.Key = Key;
		this.Command = Command;
        this.TermReceived = TermReceived;
	}
}
