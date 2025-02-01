using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.DTOs;

public record VoteRpc
{
	public Guid candidateId { get; set; }
    public bool result { get; set; }
    public VoteRpc(Guid c, bool r)
    {
        candidateId = c;
        result = r; 
    }
}
