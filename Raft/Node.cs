using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
		public int HeartbeatTimeout { get; set; } = 50; // in ms

		public int TermNumber { get; set; } = 0;
		public INode[] OtherNodes { get; set; }
		public List<bool> votesRecieved { get; set; } = new();
		public int NetworkRequestDelay { get; set; } = 0;

		public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

		public int LowerBoundElectionTime { get; set; } = 150;
		public int UpperBoundElectionTime { get; set; } = 300;


		// Log Replications
		public List<Entry> Entries { get; set; } = new();
		public List<Entry> StateMachine { get; set; } = new();
		public int CommitIndex { get; set; } = -1;
		public int nextIndex { get; set; }
		public Dictionary<Guid, int> NextIndexes = new();
		public List<bool> LogConfirmationsRecieved { get; set; } = new();
		public IClient Client { get; set; }

		// Simulation Stuff
		public bool IsRunning { get; set; } = true;

		public static int IntervalScalar { get; set; } = 1;

		public Node(INode[] OtherNodes, int? NetworkDelayInMs)
		{
			HeartbeatTimeout = 50 * Node.IntervalScalar;
			NetworkRequestDelay = NetworkDelayInMs ?? 0;

			Client = new Client();
			Entries = new();

			StartElectionTimer();

			this.OtherNodes = OtherNodes;
		}

		public void StartElectionTimer()
		{
			if (!IsRunning)
			{
				return;
			}
			if (aTimer is not null)
			{
				aTimer.Stop();
			}

			LowerBoundElectionTime = 150 * Node.IntervalScalar;
			UpperBoundElectionTime = 300 * Node.IntervalScalar;
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
			if (!IsRunning)
			{
				return;
			}

			this.State = Node.NodeState.Leader;
			this.LeaderId = this.NodeId;

			votesRecieved.Clear();

			CalculateNextIndecesList();

			StartLeaderTimer();

			SendAppendEntriesRPC();
		}

		public void CalculateNextIndecesList()
		{
			if (!IsRunning)
			{
				return;
			}
			foreach (var node in OtherNodes)
			{
				if (!NextIndexes.ContainsKey(node.NodeId))
				{
					NextIndexes.Add(node.NodeId, Entries.Count);
				}
			}
		}

		public void StartLeaderTimer()
		{
			if (!IsRunning)
			{
				return;
			}

			aTimer.Stop();

			HeartbeatTimeout = 50 * Node.IntervalScalar;
			ElectionTimeout = HeartbeatTimeout;
			aTimer = new System.Timers.Timer(HeartbeatTimeout);
			aTimer.Elapsed += (s, e) => { TimeoutHasPassedForLeaders(); };
			aTimer.AutoReset = false;
			aTimer.Start();
			WhenTimerStarted = DateTime.Now;
		}

		public void TimeoutHasPassed()
		{
			if (!IsRunning)
			{
				return;
			}
			StartElection();

			//StartElectionTimer();
		}

		public void TimeoutHasPassedForLeaders()
		{
			if (!IsRunning)
			{
				return;
			}
			SendAppendEntriesRPC();

			StartLeaderTimer();
		}

		public void SendAppendEntriesRPC()
		{
			if (!IsRunning)
			{
				return;
			}
			// As the leader, I need to send an RPC to other nodes
			foreach (var node in OtherNodes)
			{
				List<Entry> entriesToSend = CalculateEntriesToSend(node);

				node.RecieveAppendEntriesRPC(this.TermNumber, this.NodeId, (this.Entries.Count - 1), entriesToSend, this.CommitIndex);
			}
		}

		public List<Entry> CalculateEntriesToSend(INode node)
		{
			List<Entry> entriesWithIndexes = new();
			List<Entry> entriesToSend = new();

			// assign their indexes
			int index = 0;
			foreach (var entry in this.Entries)
			{
				entry.Index = index;
				entriesWithIndexes.Add(entry);
				index++;
			}

			if (NextIndexes.Count > 0)
			{
				int nodesPrevLogIndex = NextIndexes[node.NodeId];
				var differenceInLogs = (this.Entries.Count - 1) - nodesPrevLogIndex;
				entriesToSend = entriesWithIndexes.TakeLast(differenceInLogs + 1).ToList();
			}

			return entriesToSend;
		}

		public async Task RecieveAppendEntriesRPC(int LeadersTermNumber, Guid leaderId, int prevLogIndex, List<Entry> entries, int leaderCommit)
		{

			if (!IsRunning)
			{
				await Task.CompletedTask;
			}
			bool response = true;

			// Update state if a higher term number is received
			if (this.State == Node.NodeState.Candidate && LeadersTermNumber >= this.TermNumber)
			{
				this.State = Node.NodeState.Follower;
			}

			if (this.State == Node.NodeState.Leader && LeadersTermNumber > this.TermNumber)
			{
				this.State = Node.NodeState.Follower;
			}

			// Reply false if term < currentTerm
			if (LeadersTermNumber < this.TermNumber)
			{
				response = false;   // I have heard from a leader whos term is less than mine
			}

			this.LeaderId = leaderId;
			this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);
			this.WhenTimerStarted = DateTime.Now;


			// add my logs up until the committed index of the leader
			if (entries.Count == 0)  // but only do this if this is a heartbeat messsage...
			{
				this.StateMachine.Clear();
				this.StateMachine.AddRange(this.Entries.Take(leaderCommit + 1));
				this.CommitIndex = leaderCommit;
			}

			// Log replication
			if (prevLogIndex <= this.Entries.Count) // Ensure leader logs aren't too far ahead
			{
				if (entries is not null && entries.Count > 0)
				{
					// Finding the last matching log entry (-1 for no match found)
					int matchIndex = this.Entries.FindLastIndex(e =>
						e.TermReceived == entries.First().TermReceived &&
						e.Command == entries.First().Command);

					if (matchIndex != -1)
					{
						this.Entries.AddRange(entries.Skip(matchIndex + 1));
						response = true;
					}
					else if (this.Entries.Count == 0)
					{
						this.Entries.AddRange(entries);
						response = true;
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

			// Respond to leader with my response
			//foreach (var node in OtherNodes)
			//{
			//	if (node.LeaderId == this.LeaderId)
			//	{
			//		node.RespondBackToLeader(response, this.TermNumber, this.CommitIndex);
			//	}
			//}
			OtherNodes
				.Where(node => node.LeaderId == this.LeaderId)
				.ToList()
				.ForEach(node => node.RespondBackToLeader(response, this.TermNumber, this.CommitIndex, this.NodeId));
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


		public void RespondBackToLeader(bool response, int fTermNumber, int fCommitIndex, Guid fNodeId)
		{
			// This is the leader

			if (!response)
			{
				// wuh oh. Let me decriment your prevLogIndex little guy
				if (NextIndexes.ContainsKey(fNodeId))
				{
					NextIndexes[fNodeId]--; // Decrement the value by 1
				}
			}

			// as the leader, I have heard from my followers and I will commit my index
			LogConfirmationsRecieved.Add(response);
			bool hasMajority = HasMajority(LogConfirmationsRecieved);

			if (hasMajority)
			{
				CommitEntry();
			}

			// send a confirmation heartbeat to other nodes saying I have committed an entry
			foreach (var n in this.OtherNodes)
			{
				n.RecieveAppendEntriesRPC(this.TermNumber, this.NodeId, this.Entries.Count - 1, new List<Entry>(), this.CommitIndex);
			}
		}

		public void SendVoteRequestRPCsToOtherNodes()
		{
			if (!IsRunning)
			{
				return;
			}
			// as the candidate, I am asking for votes from other nodes
			foreach (var node in OtherNodes)
			{
				node.RecieveAVoteRequestFromCandidate(this.NodeId, this.TermNumber);
			}
		}

		public void RecieveVoteResults(bool result, int termNumber)
		{
			if (!IsRunning)
			{
				return;
			}
			// As a candidate, I am recieving votes from my followers
			votesRecieved.Add(result);

			bool won = HasMajority(votesRecieved);

			if (won)
			{
				BecomeLeader();
			}

		}

		public bool HasMajority(List<bool> List)
		{
			if (List.Count(x => x) > OtherNodes.Count() / 2)
			{
				return true;
				//BecomeLeader();
			}
			return false;
		}

		public void HasLogMajority(List<bool> List)
		{
			if (!IsRunning)
			{
				return;
			}
			if (List.Count(x => x) > OtherNodes.Count() / 2)
			{
				CommitEntry();
			}
		}

		public async Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
		{
			if (!IsRunning)
			{
				await Task.CompletedTask;
			}
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
			if (!IsRunning)
			{
				await Task.CompletedTask;
			}
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
			if (!IsRunning)
			{
				return;
			}

			votesRecieved.Clear();

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

			//StartElectionTimer();

			// Send vote requests
			SendVoteRequestRPCsToOtherNodes();
		}

		public void RecieveClientCommand(string key, string command)
		{
			if (!IsRunning)
			{
				return;
			}
			Entry l = new Entry(key, command);
			l.TermReceived = this.TermNumber;

			this.Entries.Add(l);

			SendAppendEntriesRPC();
		}

		public void CommitEntry()
		{
			if (!IsRunning)
			{
				return;
			}
			this.CommitIndex++;
			this.StateMachine.Clear();
			this.StateMachine = this.Entries.Take(this.CommitIndex + 1).ToList();
			Entry entryToSend = this.StateMachine.Last();
			this.Client.RecieveLogFromLeader(entryToSend);
		}

		public void PauseNode()
		{
			IsRunning = false;
			aTimer.Stop();
			aTimer.Dispose();
		}

		public void UnpauseNode()
		{
			IsRunning = true;
			if (this.State == NodeState.Leader)
			{
				StartLeaderTimer();
				SendAppendEntriesRPC();
			}
			else
			{
				StartElectionTimer();
			}
		}
	}
}
