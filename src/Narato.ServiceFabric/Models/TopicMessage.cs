namespace Narato.ServiceFabric.Models
{
    public class TopicMessage
    {
        public string Action { get; set; }
        public dynamic Payload { get; set; }
    }
}