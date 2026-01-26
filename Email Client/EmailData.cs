using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Email_Client
{
    public class EmailData
    {
        public int Id { get; set; }
        public string Sender { get; set; }
        public string Topic { get; set; }
        public DateTime Date { get; set; }

        public EmailData(int id, string sender, string topic, DateTime date)
        {
            Id = id;
            Sender = sender;
            Topic = topic;
            Date = date;
        }
    }
}
