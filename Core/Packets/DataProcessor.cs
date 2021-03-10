using Dofus.Retro.Supertools.Core.Messages;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dofus.Retro.Supertools.Core.Packets
{
    public static class DataProcessor
    {

        #region Regex constants
        //Voir https://waytolearnx.com/2019/09/les-expressions-regulieres-en-csharp.html

        private const string GAME_TURN_START_MESSAGE_PATTERN = @"GTS(-?\d*\.{0,1}\d+)\|(\d+)\|(\d)";
        private const string CHAT_MESSAGE_PATTERN = @"cMK\|(\d+)\|(.+)\|\*Hey\*\|";
        private const string GROUP_INVITE_MESSAGE_PATTERN = @"PIK(.+)\|(.+)";
        private const string EXCHANGE_INVITE_MESSAGE_PATTERN = @"ERK(\d+)\|(\d+)";
        private const string CHARACTER_LIST_PATTERN = @"([0-9]+);([a-zA-Z0-9-_]+);[0-9];[0-9]+;[-0-9]+;[-0-9]+;[-0-9]+;,,,,;[0-9]+;([0-9]+);;;";


        #endregion

        /// <summary>
        /// Fonction qui permet de convertir un le string d'un paquet (data), en la classe voulue (paramètre générique "T")
        /// </summary>
        /// <typeparam name="T">Type de la classe attendue</typeparam>
        /// <param name="data">Donnée du paquet</param>
        /// <returns>L'objet ayant pour type la classe demandée</returns>
        public static T Deserialize<T>(string data, string ipSource = null, string ipDestination = null) where T : class
        {
            //On regarde si le type de "T" est un GameTurnStartMessage
            if (typeof(T) == typeof(GameTurnStartMessage))
            {
                var regMatch = Regex.Match(data, GAME_TURN_START_MESSAGE_PATTERN);

                if (regMatch.Success)
                {
                    return (T)Convert.ChangeType(new GameTurnStartMessage
                    {
                        ActorId = regMatch.Groups[1].Value,
                        TurnTime = regMatch.Groups[2].Value,
                        TurnRoundNumber = regMatch.Groups[3].Value
                    }, typeof(T));
                }
            }
            //On regarde si le type de "T" est un ChatMessage
            else if (typeof(T) == typeof(ChatMessage))
            {
                //Exemple de message envoyé dans le chat dans le but de s'enregistrer (ce qui ouvrira PersosManager.xaml)
                //cMK|1234567|MonPerso1|*Hey*|

                var regMatch = Regex.Match(data, CHAT_MESSAGE_PATTERN);

                if (regMatch.Success)
                {
                    return (T)Convert.ChangeType(new ChatMessage
                    {
                        ActorId = regMatch.Groups[1].Value,
                        ActorName = regMatch.Groups[2].Value,
                        Message = regMatch.Groups[3].Value,
                        SenderIp = ipSource
                    }, typeof(T));
                }
            }
            //...
            else if (typeof(T) == typeof(GroupInviteMessage))
            {
                //Exemple MonPerso1 qui invite MonPerso2
                //PIKMonPerso1|MonPerso2

                var regMatch = Regex.Match(data, GROUP_INVITE_MESSAGE_PATTERN);
                if (regMatch.Success)
                {
                    return (T)Convert.ChangeType(new GroupInviteMessage
                    {
                        InviteFrom = regMatch.Groups[1].Value.TrimEnd('\0'),
                        InviteTo = regMatch.Groups[2].Value.TrimEnd('\0')
                    }, typeof(T));
                }
            }
            else if (typeof(T) == typeof(ExchangeInviteMessage))
            {
                //Echange demandé par MonPerso1 (ID : 1234567) vers MonPerso2 (ID : 9876543)
                //ERK1234567|9876543|1
                //Nous n'utilisons pas le dernier "1", il ne nous sert à rien
                var regMatch = Regex.Match(data, EXCHANGE_INVITE_MESSAGE_PATTERN);
                if (regMatch.Success)
                {
                    return (T)Convert.ChangeType(new ExchangeInviteMessage
                    {
                        ExchangeInviteFrom = regMatch.Groups[1].Value,
                        ExchangeInviteTo = regMatch.Groups[2].Value
                    }, typeof(T));
                }

            }
            else if (typeof(T) == typeof(List<CharacterListMessage>))
            {
                var regMatches = Regex.Matches(data, CHARACTER_LIST_PATTERN);
                if (regMatches.Count > 0)
                {
                   
                    var charactersList = new List<CharacterListMessage>();
                    for (int i = 0; i < regMatches.Count; i++)
                    {
                        var characterListMsg = new CharacterListMessage
                        {
                            ActorId = regMatches[i].Groups[1].Value,
                            ActorName = regMatches[i].Groups[2].Value,
                            ServerId = regMatches[i].Groups[3].Value
                        };
                        charactersList.Add(characterListMsg);
                    }

                    return (T)Convert.ChangeType(charactersList, typeof(T));
                }
            }

            return null;
        }
    }
}
