using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Xml.Linq;
using Raft.DTOs;
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
		public Dictionary<Guid, int> MatchIndexes = new();
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

			if (this.State != Node.NodeState.Leader)
			{
				return;
			}
			// As the leader, I need to send an RPC to other nodes
			foreach (var node in OtherNodes)
			{
				List<Entry> entriesToSend = CalculateEntriesToSend(node.NodeId);

				node.RecieveAppendEntriesRPC(new AppendEntriesRPC(this.TermNumber, this.NodeId, (this.Entries.Count - 1), entriesToSend, this.CommitIndex));
			}
		}

		public List<Entry> CalculateEntriesToSend(Guid nodeId)
		{
			//List<Entry> entriesWithIndexes = new();
			List<Entry> entriesToSend = new();

			// if I havent gotten the other indexes yet have a list of next indexes...
			if (NextIndexes.Count <= 0)
			{
				CalculateNextIndecesList();
			}
			
			int nodesPrevLogIndex = NextIndexes[nodeId];
			var differenceInLogs = (this.Entries.Count) - nodesPrevLogIndex; // was (this.Entries.Count - 1) - nodesPrevLogIndex;

			//var differenceInLogs = (this.Entries.Count - 1) - nodesPrevLogIndex; // was (this.Entries.Count - 1) - nodesPrevLogIndex;
			entriesToSend = this.Entries.TakeLast(differenceInLogs + 1).ToList();

			return entriesToSend;
		}

		public async Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc)
		{
			if (!IsRunning)
			{
				await Task.CompletedTask;
			}
			bool response = true;


			// Update state if a higher term number is received
			if (this.State == Node.NodeState.Candidate && rpc.term >= this.TermNumber)
			{
				this.State = Node.NodeState.Follower;
			}

			if (this.State == Node.NodeState.Leader && rpc.term > this.TermNumber)
			{
				this.State = Node.NodeState.Follower;
			}

			// Reply false if term < currentTerm
			if (rpc.term < this.TermNumber)
			{
				response = false;   // I have heard from a leader whos term is less than mine
				foreach (var n in this.OtherNodes)
				{
					if (n.NodeId == rpc.leaderId)
					{
						n.RespondBackToLeader(new ResponseBackToLeader(response, this.TermNumber, this.Entries.Count() - 1, this.NodeId));
					}
				}
				return; // return because we don't want to replicate the incoming logs?
			}

			this.LeaderId = rpc.leaderId;
			this.ElectionTimeout = Random.Shared.Next(LowerBoundElectionTime, UpperBoundElectionTime);
			this.WhenTimerStarted = DateTime.Now;


			// add my logs up until the committed index of the leader
			if (rpc.entries.Count == 0)  // but only do this if this is a heartbeat messsage...
			{
				this.StateMachine.Clear();
				this.StateMachine.AddRange(this.Entries.Take(rpc.leaderCommit + 1));
				this.CommitIndex = rpc.leaderCommit;
			}


			// Log Replication
			if (rpc.prevLogIndex <= this.Entries.Count) // Ensure leader logs aren't too far ahead...
			{
				if (rpc.entries is not null && rpc.entries.Count > 0)	// and if there are even logs to replicate...
				{
					bool matchFound = false;
					int matchIndex = -1;

					int leadersPrevLogIndex = rpc.prevLogIndex;
					int myIndexes = this.Entries.Count() - 1;

					foreach (var leaderLog in rpc.entries.AsEnumerable().Reverse())
					{
						foreach (var followerLog in this.Entries.AsEnumerable().Reverse())
						{
							if (myIndexes == leadersPrevLogIndex && leaderLog.TermReceived == followerLog.TermReceived)
							{
								matchFound = true;
								matchIndex = myIndexes;
								break;
							}
							myIndexes = myIndexes - 1;
						}
						myIndexes = this.Entries.Count() - 1;
						leadersPrevLogIndex = leadersPrevLogIndex - 1;
					}

					if (matchFound)
					{
						this.Entries.AddRange(rpc.entries.Skip(matchIndex + 1));
						response = true;    // I have replicated the logs up to the entries you have sent me
					}
					else if (this.Entries.Count == 0)
					{
						this.Entries.AddRange(rpc.entries);
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


			// TODO bad;;; ???
			if (this.CommitIndex < rpc.leaderCommit && rpc.entries.Count() > 0)
			{
				this.StateMachine.AddRange(rpc.entries.TakeLast(rpc.entries.Count() - this.StateMachine.Count()));
			}

			this.CommitIndex = rpc.leaderCommit;

			OtherNodes
				.Where(node => node.NodeId == this.LeaderId)
				.ToList()
				.ForEach(node => node.RespondBackToLeader(new ResponseBackToLeader( response, this.TermNumber, this.Entries.Count() - 1, this.NodeId)));
		}

		public void FindMatch(AppendEntriesRPC rpc)
		{
			bool matchFound = false;
			int matchIndex = -1;
			// Refactor this

			int leadersPrevLogIndex = rpc.prevLogIndex;
			int myIndexes = this.Entries.Count() - 1;

			foreach (var leaderLog in rpc.entries.AsEnumerable().Reverse())
			{
				foreach (var followerLog in this.Entries.AsEnumerable().Reverse())
				{
					if (myIndexes == leadersPrevLogIndex && leaderLog.TermReceived == followerLog.TermReceived)
					{
						matchFound = true;
						matchIndex = myIndexes;
						break;
					}
					myIndexes = myIndexes - 1;
				}
				myIndexes = this.Entries.Count() - 1;
				leadersPrevLogIndex = leadersPrevLogIndex - 1;
			}

			if (matchFound)
			{
				//this.Entries.AddRange()
				this.Entries.AddRange(rpc.entries.Skip(matchIndex + 1));
				//response = true;    // I have replicated the logs up to the entries you have sent me
			}
			else if (this.Entries.Count == 0)
			{
				this.Entries.AddRange(rpc.entries);
				//response = true;
			}
			else
			{
				//response = false;
			}
		}

		public async Task RespondBackToLeader(ResponseBackToLeader rpc)
		{
			// As the leader, I have heard from the response as a follower
			if (!IsRunning)
			{
				return;
			}

			if (this.State != Node.NodeState.Leader)
			{
				return; // this method is only for leaders
			}
			
			if (NextIndexes.ContainsKey(rpc.fNodeId))
			{
				if (!rpc.response)
				{
					NextIndexes[rpc.fNodeId]--; 
				}
				else
				{
					NextIndexes[rpc.fNodeId] = rpc.fPrevLogIndex;
				}
			}


			// as the leader, I have heard from my followers and I will commit my index
			bool hasMajority = HasMajorityLogConfirmations();

			if (hasMajority)
			{
				CommitEntry();
				// and finally, as the leader I need to respond to the client.
				Client.RecieveLogFromLeader(this.Entries.Last());
			}

			await Task.CompletedTask;
		}

		public bool HasMajorityLogConfirmations()
		{
			if (this.Entries.Count <= 0)
			{
				return false; // no logs at all
			}

			int potentialCommitIndex = this.CommitIndex + 1;
			List<bool> logConfirmations = new List<bool>();

			foreach(var ni in this.NextIndexes)
			{
				if (ni.Value == potentialCommitIndex)
				{
					logConfirmations.Add(true);
				}
				else
				{
					logConfirmations.Add(false);
				}
			}

			return HasMajority(logConfirmations);
		}

		public async Task SendVoteRequestRPCsToOtherNodes()
		{
			if (!IsRunning)
			{
				return;
			}
			// as the candidate, I am asking for votes from other nodes
			foreach (var node in OtherNodes)
			{
				VoteRequestFromCandidateRpc rpc = new VoteRequestFromCandidateRpc(this.NodeId, this.TermNumber);
				await node.RecieveAVoteRequestFromCandidate(rpc);
			}
		}

		public async Task RecieveVoteResults(VoteFromFollowerRpc vote)
		{
			// As a candidate, I am recieving votes from my followers
			if (!IsRunning)
			{
				return;
			}
			votesRecieved.Add(vote.result);

			bool won = HasMajority(votesRecieved);

			if (won)
			{
				BecomeLeader();
			}
			await Task.CompletedTask;
		}

		public bool HasMajority(List<bool> List)
		{
			int count = List.Count(x => x);
			if (List.Count(x => x) > OtherNodes.Count() / 2)
			{
				return true;
			}
			return false;
		}

		//public void HasLogMajority(List<bool> List)
		//{
		//	if (!IsRunning)
		//	{
		//		return;
		//	}
		//	if (List.Count(x => x) > OtherNodes.Count() / 2)
		//	{
		//		CommitEntry();
		//	}
		//}

		public async Task RecieveAVoteRequestFromCandidate(VoteRequestFromCandidateRpc rpc)
		{
			if (!IsRunning)
			{
				await Task.CompletedTask;
			}
			// as a server, I recieve a vote request from a candidate
			bool result = true;
			if (rpc.lastLogTerm < this.TermNumber || rpc.lastLogTerm == this.VotedForTermNumber)
			{
				result = false;
			}

			this.VoteForId = rpc.candidateId;
			this.VotedForTermNumber = rpc.lastLogTerm;
			await SendMyVoteToCandidate(new VoteRpc(rpc.candidateId, result));
		}

		public async Task SendMyVoteToCandidate(VoteRpc rpc)
		{
			if (!IsRunning)
			{
				await Task.CompletedTask;
			}
			// as a follower, I am sending a candidate my vote
			foreach (var node in OtherNodes)
			{
				if (node.NodeId == rpc.candidateId)
				{
					await node.RecieveVoteResults(new VoteFromFollowerRpc(rpc.result, this.TermNumber));
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

		public bool RecieveClientCommand(string key, string command)
		{
			if (!IsRunning)
			{
				return false;
			}

			if (this.State != NodeState.Leader)
			{
				return false; 
			}

			Entry l = new Entry(key, command);
			l.TermReceived = this.TermNumber;

			this.Entries.Add(l);

			SendAppendEntriesRPC();
			return true;
		}

		public void CommitEntry()
		{
			if (!IsRunning)
			{
				return;
			}
			if (this.CommitIndex + 1 >= this.Entries.Count)
			{
				return; // Prevent committing beyond available entries
			}

			this.CommitIndex++; // Move to the next commit index
			Entry entryToSend = this.Entries[this.CommitIndex]; // Get the new entry
			this.StateMachine.Add(entryToSend);
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
