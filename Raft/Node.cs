﻿namespace Raft
{
    public class Node : INode
    {
        public enum NodeState
        {
            Follower,
            Candidate,
            Leader
        }
        public Guid NodeId { get; set; } = Guid.NewGuid();
        public Guid VoteForId { get; set; }

        public int ElectionTimeout { get; set; } // in ms
        public bool Vote { get; set; }
        public INode[] OtherNodes { get; set; }

        public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

        public Node(bool Vote, Node[] OtherNodes)
        {
            this.Vote = Vote;
            this.OtherNodes = OtherNodes;
            this.ElectionTimeout = Random.Shared.Next(150, 300);
        }

        public Node()
        {
        }

        public bool SendAppendEntriesRPC()
        {
            foreach (var node in OtherNodes)
            {
                node.RespondToAppendEntriesRPC();
            }
            return true;
        }

        public bool RespondToAppendEntriesRPC()
        {
            return true; // simplest case for now
        }

        public void StartElection()
        {
            this.State = NodeState.Candidate;
            this.VoteForId = this.NodeId;
        }

    }
}
