using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.DTOs;

public class ClientCommandDto
{
    public string Key { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public ClientCommandDto(string Key, string Command)
    {
        this.Key = Key;
        this.Command = Command;
    }
}
