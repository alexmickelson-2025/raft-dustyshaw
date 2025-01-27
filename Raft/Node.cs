using System.Runtime.CompilerServices;
using System.Timers;
using System.Xml.Linq;
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
		public int nextIndex { get; set; }
		public Dictionary<Guid, int> NextIndexes = new();

		public Node(Node[] OtherNodes, int? IntervalScalar, int? NetworkDelayInMs)
		{
			LowerBoundElectionTime = IntervalScalar.HasValue ? LowerBoundElectionTime * IntervalScalar.Value : LowerBoundElectionTime;
			UpperBoundElectionTime = IntervalScalar.HasValue ? UpperBoundElectionTime * IntervalScalar.Value : UpperBoundElectionTime;
			HeartbeatTimeout = IntervalScalar.HasValue ? HeartbeatTimeout * IntervalScalar.Value : HeartbeatTimeout;
			NetworkRequestDelay = NetworkDelayInMs ?? 0;

			StartElectionTimer();

			this.OtherNodes = OtherNodes;
		}

		public void StartElectionTimer()
		{
			if (aTimer is not null)
			{
				aTimer.Stop();
			}
			int randomMs = new Random().Next(LowerBoundElectionTime, UpperBoundElectionTime);
			this.ElectionTimeout = randomMs;
			aTimer = new System.Timers.Timer(ElectionTimeout);
			aTimer.Elapsed += (s, e) => { TimeoutHasPassed(); };
			aTimer.AutoReset = false;
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;
		}

		public void BecomeLeader()
		{
			this.State = Node.NodeState.Leader;
			this.LeaderId = this.NodeId;

			CalculateNextIndecesList();

			StartLeaderTimer();

			SendAppendEntriesRPC();
		}

		public void CalculateNextIndecesList()
		{
			foreach (INode node in OtherNodes)
			{
				if (!NextIndexes.ContainsKey(node.NodeId))
				{
					NextIndexes.Add(node.NodeId, Entries.Count);
				}
			}
		}

		public void StartLeaderTimer()
		{
			aTimer.Stop();
			aTimer = new System.Timers.Timer(HeartbeatTimeout);
			aTimer.Elapsed += (s, e) => { TimeoutHasPassedForLeaders(); };
			aTimer.AutoReset = false;
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;
		}

		public void TimeoutHasPassed()
		{
			StartElection();

			//StartElectionTimer();
		}

		public void TimeoutHasPassedForLeaders()
		{
			SendAppendEntriesRPC();

			StartLeaderTimer();
		}

		public void SendAppendEntriesRPC()
		{
			// As the leader, I need to send an RPC to other nodes
			foreach (var node in OtherNodes)
			{
				List<Entry> entriesToSend = CalculateEntriesToSend(node);

				node.RecieveAppendEntriesRPC(this.TermNumber, this.NodeId, (this.Entries.Count - 1), entriesToSend, this.CommitIndex);
			}
		}

		public List<Entry> CalculateEntriesToSend(INode node)
		{
			List<Entry> entriesToSend = new();
			if (NextIndexes.Count > 0)
			{
				int nodesPrevLogIndex = NextIndexes[node.NodeId];
				var differenceInLogs = (this.Entries.Count - 1) - nodesPrevLogIndex;
				entriesToSend = new List<Entry>();
				entriesToSend = this.Entries.TakeLast(differenceInLogs + 1).ToList();
			}

			return entriesToSend;
		}

		public async Task RecieveAppendEntriesRPC(int LeadersTermNumber, Guid leaderId, int prevLogIndex, List<Entry> entries, int leaderCommit)
		{
			bool response = true;

			// Update state if a higher term number is received
			if (this.State == Node.NodeState.Candidate && LeadersTermNumber >= this.TermNumber)
			{
				this.State = Node.NodeState.Follower;
			}

			// Reject if leader's term is outdated
			if (LeadersTermNumber < this.TermNumber)
			{
				response = false;
			}

			// Update leader and reset election timeout
			this.LeaderId = leaderId;
			this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);
			this.WhenTimerStarted = DateTime.Now;
			this.CommitIndex = leaderCommit;

			// Log replication
			if (prevLogIndex <= this.Entries.Count) // Ensure leader logs aren't too far ahead
			{
				if (entries.Count > 0)
				{
					// Check and add new entries
					int matchIndex = this.Entries.FindLastIndex(e =>
						e.TermReceived == entries.First().TermReceived &&
						e.Command == entries.First().Command);

					if (matchIndex != -1)
					{
						this.Entries.AddRange(entries.Skip(matchIndex + 1));
					}
					else if (this.Entries.Count == 0)
					{
						this.Entries.AddRange(entries);
					}
					else
					{
						response = false;
					}
				}
			}
			else
			{
				response = false;
			}

			// Respond to leader
			foreach (var node in OtherNodes)
			{
				if (node.LeaderId == this.LeaderId)
				{
					node.RespondBackToLeader(response, this.TermNumber, this.CommitIndex);
				}
			}
		}

		//public async Task RecieveAppendEntriesRPC(int LeadersTermNumber, Guid leaderId, int prevLogIndex, List<Entry> entries, int leaderCommit)
		//{
		//	bool response = true;
		//	// As a follower, I have heard from the leader
		//	if (this.State == Node.NodeState.Candidate && LeadersTermNumber >= this.TermNumber)
		//	{
		//		this.State = Node.NodeState.Follower; // I'm a candidate, but I heard from someone with greater term #
		//	}

		//	if (LeadersTermNumber < this.TermNumber)
		//	{
		//		response = false;
		//	}

		//	// Looks good, I'll keep you as my leader
		//	this.LeaderId = leaderId;
		//	this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);
		//	WhenTimerStarted = DateTime.Now;

		//	this.CommitIndex = leaderCommit;


		//	// Replicating Logs
		//	if (prevLogIndex - (this.Entries.Count - 1) <= 1)  // make sure leaders logs aren't too far ahead in the future
		//	{
		//		if (entries.Count > 0)
		//		{
		//			foreach (var l in entries.AsEnumerable().Reverse())
		//			{

		//				foreach (var myLog in this.Entries.AsEnumerable().Reverse())
		//				{
		//					if (myLog != l && (l == entries.Last()))
		//					{
		//						response = false;
		//					}
		//					else if (myLog.TermReceived == l.TermReceived && myLog.Command == l.Command /* && (l == entries.First())*/)
		//					{
		//						foreach (var lToAdd in entries.Skip(1)) // skip the matching one
		//						{
		//							this.Entries.Add(lToAdd);
		//						}
		//						response = true;	// I have successfully replicated the logs
		//					}
		//				}
		//				if (this.Entries.Count == 0)
		//				{
		//					foreach (var lToAdd in entries.AsEnumerable().Reverse())
		//					{
		//						this.Entries.Add(lToAdd);
		//					}
		//					response = true;
		//				}
		//			}
		//		}
		//	}
		//	else
		//	{
		//		response = false;
		//	}


		//	// Respond back to leader with my response
		//	foreach (var n in OtherNodes)
		//	{
		//		if (n.LeaderId == this.LeaderId)
		//		{
		//			n.RespondBackToLeader(response, this.TermNumber, this.CommitIndex);
		//		}
		//	}
		//}

		public void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex)
		{
			// This is the leader
			//this.TermNumber = myTermNumber;
			//this.State = NodeState.Follower;
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

		public void CommitEntry()
		{
			this.CommitIndex++;
		}
	}
}
