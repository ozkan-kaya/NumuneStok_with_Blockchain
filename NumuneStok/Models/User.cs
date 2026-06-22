using System;
using System.Data;

namespace NumuneStok.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public string? BlockchainRole { get; set; }
        public string? WalletAddress { get; set; }
    }
}
