using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Emailer
{
    public class EmailData
    {
        [JsonProperty("senderEmail")]
        public string SenderEmail { get; set; }

        [JsonProperty("senderName")]
        public string SenderName { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
