using System;
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
				List<Entry> entriesToSend = CalculateEntriesToSend(node.NodeId);

				node.RecieveAppendEntriesRPC(new AppendEntriesRPC(this.TermNumber, this.NodeId, (this.Entries.Count - 1), entriesToSend, this.CommitIndex));
			}
		}

		public List<Entry> CalculateEntriesToSend(Guid nodeId)
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

			// if I have a list of next indexes...
			if (NextIndexes.Count > 0)
			{
				int nodesPrevLogIndex = NextIndexes[nodeId];
				var differenceInLogs = (this.Entries.Count - 1) - nodesPrevLogIndex;
				entriesToSend = entriesWithIndexes.TakeLast(differenceInLogs + 1).ToList();
			}

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

			// Log replication
			List<Entry> followersEntriesWithIndexes = new();
			int index = 0;
			foreach (var entry in this.Entries)
			{
				entry.Index = index;
				followersEntriesWithIndexes.Add(entry);
				index++;
			}

			if (rpc.prevLogIndex <= this.Entries.Count) // Ensure leader logs aren't too far ahead
			{
				if (rpc.entries is not null && rpc.entries.Count > 0)
				{
					bool matchFound = false;
					foreach (var leaderLog in rpc.entries.AsEnumerable().Reverse())
					{
						foreach(var followerLog in followersEntriesWithIndexes.AsEnumerable().Reverse())
						{
							if (followerLog.Index == leaderLog.Index && leaderLog.TermReceived == followerLog.TermReceived)
							{
								matchFound = true;
							}
						}
					}

					if (matchFound)
					{
						int i = 1; // The index after which you want to truncate and append new items

						if (i < this.Entries.Count)
						{
							// Remove all elements after the specified index
							this.Entries.RemoveRange(i, this.Entries.Count - i);
						}

						// Append new elements from rpc.entries after the index
						this.Entries.AddRange(rpc.entries.Skip(1));

						//this.Entries.RemoveRange(rpc.entries.Skip(1));
						//this.Entries.AddRange(rpc.entries.Skip(1)); // WRONGGGGGGG
						response = true;	// I have replicated the logs up to the entries you have sent me
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

			OtherNodes
				.Where(node => node.LeaderId == this.LeaderId)
				.ToList()
				.ForEach(node => node.RespondBackToLeader(response, this.TermNumber, this.CommitIndex, this.NodeId));
		}

		public void RespondBackToLeader(bool response, int fTermNumber, int fCommitIndex, Guid fNodeId)
		{
			// This is the leader
			if (NextIndexes.ContainsKey(fNodeId))
			{
				if (!response)
				{
					NextIndexes[fNodeId]--; 
				}
				else
				{
					NextIndexes[fNodeId] = this.Entries.Count();	// not sure if this is correct, follower has replicated entries up to my prev log index?.
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
				AppendEntriesRPC rpc = new(this);
				rpc.entries = new List<Entry>();
				n.RecieveAppendEntriesRPC(new AppendEntriesRPC(this.TermNumber, this.NodeId, this.Entries.Count - 1, new List<Entry>(), this.CommitIndex));
			}

			// and finally, as the leader I need to respond to the client.

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
			int count = List.Count(x => x);
			if (List.Count(x => x) > OtherNodes.Count() / 2)
			{
				return true;
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
