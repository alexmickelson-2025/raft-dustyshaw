using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.DTOs;

// bool result, int termNumber
public record VoteFromFollowerRpc
{
    public bool result { get; set; }
    public int termNumber { get; set; }

    public VoteFromFollowerRpc(bool result, int termNumber)
    {
        this.result = result;
        this.termNumber = termNumber;
    }
}
