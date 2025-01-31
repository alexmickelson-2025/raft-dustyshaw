using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.DTOs;

public record VoteRequestFromCandidateRpc
{
    public Guid candidateId { get; set; }
    public int lastLogTerm { get; set; }

    public VoteRequestFromCandidateRpc(Guid candidateId, int lastLogTerm)
    {
        this.candidateId = candidateId;
        this.lastLogTerm = lastLogTerm;
    }
}
