namespace Dofus.Retro.Supertools.Core.Messages
{
    public class ChatMessage
    {
        public string ActorId { get; set; } //Id unique du personnage qui parle
        public string ActorName { get; set; } //Nom
        public string Message { get; set; } //Message
        public string SenderIp { get; set; } //Ip du serveur ayant envoyé le message
    }
}
