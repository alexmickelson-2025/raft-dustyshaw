using Raft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftTests
{
	public class LogTests
	{
		// Testing #1
		[Fact]
		public void TestCase01_NodesHoldLogs()
		{
			// Arrange
			Node n = new Node([], null, null);
			Log l = new Log();
			l.Command = "a";
			n.Entries = [l];

			// Act
			Thread.Sleep(600);

			// Assert
			Assert.True(n.Entries.Count() > 0);
		}
	}
}
