using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Email_Client
{
    public class EmailData
    {
        public uint Uid { get; set; }
        public string Sender { get; set; }
        public string Topic { get; set; }
        public DateTime Date { get; set; }

        public EmailData(uint uid, string sender, string topic, DateTime date)
        {
            Uid = uid;
            Sender = sender;
            Topic = topic;
            Date = date;
        }
    }
}
