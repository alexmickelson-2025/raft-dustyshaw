﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft;

public class AppendEntriesRPC
{
    public int term { get; set; } // leaders term
    public Guid leaderId { get; set; } // leaders GUID
    public int prevLogIndex { get; set; } // index of log entry immediately preceding new ones
    public List<Entry> entries { get; set; } = []; // log entries to store (empty for heartbeat; may send more than one for efficiency
    public int leaderCommit { get; set; }  // leader's commitIndex

    public AppendEntriesRPC(INode node)
    {
        term = node.TermNumber;
        leaderId = node.NodeId;
        prevLogIndex = node.Entries.Count - 1;
        entries = node.Entries;     // TODO: change this in the future to maybe calculate the actual entries to send
        leaderCommit = node.CommitIndex;
    }

    // For testing purposes
    public AppendEntriesRPC()
    {
        
    }
}
