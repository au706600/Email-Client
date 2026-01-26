using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Email_Client
{
    internal class EmailData
    {
        public int Id { get; set; }
        public string Topic { get; set; }
        public string Sender { get; set; }
        public DateTime Date { get; set; }

        public EmailData(int id, string topic, string sender, DateTime date)
        {
            Id = id;
            Topic = topic;
            Sender = sender;
            Date = date;
        }
    }
}
