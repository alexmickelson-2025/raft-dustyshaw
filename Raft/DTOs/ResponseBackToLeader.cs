using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.DTOs;

public record ResponseBackToLeader
{
	//		public void RespondBackToLeader(bool response, int fTermNumber, int fPrevLogIndex, Guid fNodeId)

	public bool response { get; set; }
    public int fTermNumber { get; set; }
    public int fPrevLogIndex { get; set; }
    public Guid fNodeId { get; set; }

    public ResponseBackToLeader(bool response, int fTermNumber, int fPrevLogIndex, Guid fNodeId)
    {
        this.response = response;
        this.fTermNumber = fTermNumber;
        this.fPrevLogIndex = fPrevLogIndex;
        this.fNodeId = fNodeId;
    }

}
