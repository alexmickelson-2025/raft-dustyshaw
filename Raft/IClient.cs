using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft;

public interface IClient
{
	void RecieveLogFromLeader(Entry e);
}

public class Client : IClient
{
	public void RecieveLogFromLeader(Entry e)
	{
		return;
	}
}
