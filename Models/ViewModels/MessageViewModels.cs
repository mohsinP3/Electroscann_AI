using System;
using System.Collections.Generic;
using ElectroScanAI.Models.Entities;

namespace Electroscann_ai.Models.ViewModels
{
    public class ConversationItem
    {
        public User Partner { get; set; } = null!;
        public string PartnerRole { get; set; } = string.Empty;
        public Message LastMessage { get; set; } = null!;
        public int UnreadCount { get; set; }
        public bool IsSelected { get; set; }
    }

    public class InboxViewModel
    {
        public List<ConversationItem> Conversations { get; set; } = new();
        public User? SelectedPartner { get; set; }
        public List<Message> Messages { get; set; } = new();
        public List<User> EligibleContacts { get; set; } = new();
    }
}
