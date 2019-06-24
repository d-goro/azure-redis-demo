using Newtonsoft.Json;

namespace RedisChat
{
    public class Message
    {
        [JsonProperty]
        public string Msg { get; set; }

        [JsonProperty]
        public string Sender { get; set; }
    }
}
