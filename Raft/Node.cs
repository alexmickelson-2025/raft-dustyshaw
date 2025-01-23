﻿using System.Runtime.CompilerServices;
using System.Timers;
using Xunit.Sdk;

namespace Raft
{
	public partial class Node : INode
	{
		public Guid NodeId { get; set; } = Guid.NewGuid();
		public Guid VoteForId { get; set; }
		public int VotedForTermNumber { get; set; }
		public Guid LeaderId { get; set; }

		public int ElectionTimeout { get; set; } // in ms
		public System.Timers.Timer aTimer { get; set; }
		public DateTime WhenTimerStarted { get; set; }
		public int HeartbeatTimeout { get; } = 50; // in ms

		public int TermNumber { get; set; } = 0;
		public INode[] OtherNodes { get; set; }
		public List<bool> votesRecieved { get; set; } = new();
		public int NetworkRequestDelay { get; set; } = 0;

		public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

		public int LowerBoundElectionTime { get; set; } = 150;
		public int UpperBoundElectionTime { get; set; } = 300;


		// Log Replications
		public List<Entry> Entries { get; set; } = new();
		public int CommitIndex { get; set; } = 0;

		public Node(Node[] OtherNodes, int? IntervalScalar, int? NetworkDelayInMs)
		{
			LowerBoundElectionTime = IntervalScalar.HasValue ? LowerBoundElectionTime * IntervalScalar.Value : LowerBoundElectionTime;
			UpperBoundElectionTime = IntervalScalar.HasValue ? UpperBoundElectionTime * IntervalScalar.Value : UpperBoundElectionTime;
			HeartbeatTimeout = IntervalScalar.HasValue ? HeartbeatTimeout * IntervalScalar.Value : HeartbeatTimeout;
			NetworkRequestDelay = NetworkDelayInMs ?? 0;	

			this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);
			aTimer = new System.Timers.Timer(ElectionTimeout);
			aTimer.Elapsed += (s, e) => { TimeoutHasPassed(); };
			aTimer.AutoReset = false;
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;

			this.OtherNodes = OtherNodes;
		}

		public void BecomeLeader()
		{
			aTimer.Stop();
			this.State = Node.NodeState.Leader;
			aTimer = new System.Timers.Timer(HeartbeatTimeout);
			aTimer.Elapsed += (s, e) => { TimeoutHasPassedForLeaders(); };
			aTimer.AutoReset = false;
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;
			this.LeaderId = this.NodeId;

			SendAppendEntriesRPC();
		}

		public void TimeoutHasPassed()
		{
			StartElection();
		}

		public void TimeoutHasPassedForLeaders()
		{
			SendAppendEntriesRPC();
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;
		}

		public void SendAppendEntriesRPC()
		{
			// As the leader, I need to send an RPC to other nodes
			foreach (var node in OtherNodes)
			{
				node.RecieveAppendEntriesRPC(this.NodeId, this.TermNumber, this.CommitIndex);
			}
		}

		public async Task RecieveAppendEntriesRPC(Guid leaderId, int TermNumber, int CommitIndex)
		{
			// As a follower, I have heard from the leader
			if (this.State == Node.NodeState.Candidate && TermNumber >= this.TermNumber)
			{
				this.State = Node.NodeState.Follower; // I heard from someone with greater term #
			}

			if (TermNumber < this.TermNumber)
			{
				foreach (var n in OtherNodes)
				{
					// As a follower, I'm like what the heck? your term number sucks
					n.RespondBackToLeader(false, this.TermNumber);
				}
			}

			// Looks good, I'll keep you as my leader
			this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);
			this.LeaderId = leaderId;
			WhenTimerStarted = DateTime.Now;
		}

		public void RespondBackToLeader(bool response, int myTermNumber)
		{
			this.TermNumber = myTermNumber;
			this.State = NodeState.Follower;
		}

		public void SendVoteRequestRPCsToOtherNodes()
		{
			// as the candidate, I am asking for votes from other nodes
			foreach (var node in OtherNodes)
			{
				node.RecieveAVoteRequestFromCandidate(this.NodeId, this.TermNumber);
			}
		}

		public void RecieveVoteResults(bool result, int termNumber)
		{
			// As a candidate, I am recieving votes from my followers
			votesRecieved.Add(result);

			DetermineElectionResults();
		}

		public void DetermineElectionResults()
		{
			if (votesRecieved.Count(x => x) > OtherNodes.Count() / 2)
			{
				BecomeLeader();
			}
		}

		public async Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
		{
			// as a server, I recieve a vote request from a candidate
			bool result = true;
			if (lastLogTerm < this.TermNumber || lastLogTerm == this.VotedForTermNumber)
			{
				result = false;
			}

			this.VoteForId = candidateId;
			this.VotedForTermNumber = lastLogTerm;
			await SendMyVoteToCandidate(candidateId, result);
		}

		public async Task SendMyVoteToCandidate(Guid candidateId, bool result)
		{
			// as a follower, I am sending a candidate my vote
			foreach (var node in OtherNodes)
			{
				if (node.NodeId == candidateId)
				{
					node.RecieveVoteResults(result, this.TermNumber);
				}
			}
		}

		public void StartElection()
		{
			this.State = NodeState.Candidate;
			this.VoteForId = this.NodeId;
			this.TermNumber = this.TermNumber + 1;
			this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);

			//aTimer.Stop();
			//aTimer.Dispose();
			aTimer = new System.Timers.Timer(ElectionTimeout);
			//aTimer.Elapsed += (s, e) => { StartElection(); };
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;

			// Send vote requests
			SendVoteRequestRPCsToOtherNodes();
		}

		public void RecieveClientCommand(string command)
		{
			Entry l = new Entry(command);
			l.TermReceived = this.TermNumber;

			this.Entries.Add(l);

			SendAppendEntriesRPC();
		}
	}
}
