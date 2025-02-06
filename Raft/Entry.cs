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
    public int Index { get; set; }

    public Entry() { }

    public Entry(string _Key, string _Command)
    {
        this.Key = _Key;
        this.Command = _Command;
    }

	public Entry(string _Key, string _Command, int _TermReceived)
	{
		this.Key = _Key;
		this.Command = _Command;
        this.TermReceived = _TermReceived;
	}

	public Entry(string _Key, string _Command, int _TermReceived, int _Index)
	{
		this.Key = _Key;
		this.Command = _Command;
		this.TermReceived = _TermReceived;
		this.Index = _Index;
	}
}
