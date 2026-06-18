using System;
using System.Collections.Generic;
using LoteriaMexicanaApp.Core;

namespace LoteriaMexicanaApp.UI
{
    public static class TranslationManager
    {
        public static string CurrentLanguage { get; set; } = "ES"; // "ES" or "EN"

        private static readonly Dictionary<string, Dictionary<string, string>> UiTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["ES"] = new Dictionary<string, string>
            {
                ["Text_Title"] = "Lotería Mexicana - Juego Premium",
                ["Config_DoublesMode"] = "Modo Dobles",
                ["Config_WinPattern"] = "Patrón de Victoria",
                ["Pattern_Linea5"] = "Línea de 5",
                ["Pattern_Full"] = "Tabla Llena",
                ["Pattern_Cruz"] = "Cruz (X)",
                ["Pattern_LetraL"] = "Letra L",
                ["Pattern_Esquinas"] = "Cuatro Esquinas",
                ["Profile_Player"] = "PERFIL JUGADOR",
                ["Config"] = "CONFIGURACIÓN",
                ["Btn_Design"] = "🃏 Diseñar",
                ["Btn_Stats"] = "📊 Historial",
                ["Speed_Interval"] = "Intervalo (Segundos)",
                ["Chips_Type"] = "Tipo de Ficha",
                ["Btn_ConnectLan"] = "📡 Conectar LAN / Multijugador",
                ["Btn_DisconnectLan"] = "🔌 Desconectar LAN",
                ["Mode_Solo"] = "Modo: Solitario",
                ["Mode_LanHost"] = "Modo: LAN (Anfitrión)",
                ["Mode_LanClient"] = "Modo: LAN (Cliente)",
                ["Status_Disconnected"] = "Desconectado",
                ["Status_Connecting"] = "Conectando...",
                ["Status_Connected"] = "Conectado al Host LAN",
                ["Players_Connected"] = "JUGADORES CONECTADOS",
                ["Chat_Network"] = "CHAT DE RED",
                ["Current_Card"] = "CARTA ACTUAL",
                ["Remaining_Cards"] = "Mazo: {0} cartas restantes",
                ["History_Cards"] = "HISTORIAL DE CARTAS CANTADAS",
                ["Btn_Start"] = "▶ Iniciar",
                ["Btn_Pause"] = "⏸ Pausar",
                ["Btn_Resume"] = "▶ Reanudar",
                ["Btn_Stop"] = "⏹ Reiniciar",
                ["Btn_Loteria"] = "🎉 ¡LOTERÍA! 🎉",
                ["Boards_Count"] = "Cantidad de Tablas",
                ["Msg_ActiveGameMark"] = "El juego debe estar activo para marcar cartas.",
                ["Msg_AccidentalClaimFalse"] = "¡Lotería falsa! No tienes una línea completa válida de 5 cartas.",
                ["Msg_UncalledClaimFalse"] = "¡Lotería falsa! Tienes celdas marcadas en tu línea de victoria ({0}) que aún no han sido cantadas en la {2}.\n\nLas cartas que faltan por cantar son: {1}.",
                ["Msg_SavedSuccess"] = "¡Tabla '{0}' guardada correctamente!",
                ["Msg_DeckExhausted"] = "Se han agotado todas las cartas del mazo.",
                ["Msg_EmptyCells"] = "Hay celdas vacías en la tabla. ¿Deseas guardarla de todas formas?",
                ["Msg_EnterBoardName"] = "Por favor ingresa un nombre para la tabla.",
                ["Msg_DesignActiveGame"] = "No puedes editar tu tabla mientras una partida está activa.",
                ["Msg_NoDuplicateLimit"] = "La tabla tiene {0} cartas repetidas (dobles). Solo se permite un máximo de 2 cartas dobles (repetidas) en toda la tabla.",
                ["Msg_NoTripleLimit"] = "Una carta no se puede repetir más de 2 veces en la tabla (no se permiten cartas triples o superiores).",
                ["Lobby_DefaultUsername"] = "Jugador",
                ["Host_StartChat"] = "🏁 ¡El anfitrión ha iniciado el juego!",
                ["Host_PauseChat"] = "⏸ Juego pausado por el anfitrión.",
                ["Host_ResumeChat"] = "▶ Juego reanudado por el anfitrión.",
                ["Host_StopChat"] = "⏹ El juego ha sido detenido.",
                ["Host_ExhaustedChat"] = "🃏 Se terminó el mazo de cartas.",
                ["Win_Message"] = "🏆 ¡LOTERÍA! 🏆\nGanaste con la línea: {0}",
                ["Win_Message_Multi"] = "🏆 ¡LOTERÍA! 🏆\nGanaste en la {0} con la línea: {1}",
                ["Win_Message_Announce"] = "🏆 ¡LOTERÍA! 🏆\nEl jugador '{0}' ganó la partida completando la línea: {1}.",
                ["Lan_Warning_SingleBoard"] = "Para jugar por LAN, se cambiará automáticamente a 1 tabla.",
                ["Lang_Select"] = "Idioma / Language",
                ["Chat_Send"] = "Enviar",
                ["Title_Catalog"] = "Catálogo de Cartas (54)",
                ["Btn_Save_Editor"] = "Guardar Tabla",
                ["Btn_Autofill_Editor"] = "Auto-Llenar",
                ["Btn_Clear_Editor"] = "Limpiar",
                ["Btn_Cancel_Editor"] = "Cancelar",
                ["Editor_Title"] = "Diseñador de Tablas - Lotería Mexicana",
                ["Editor_Label_Name"] = "Nombre de la Tabla:",
                ["Editor_Instruction"] = "Haz clic en una celda y luego selecciona una carta del catálogo derecho.",
                ["Editor_SelectCell"] = "Celda [{0}, {1}] seleccionada. Elige una carta a la derecha.",
                ["Editor_ConfirmRepeat"] = "La carta '{0}' ya está asignada en esta tabla. ¿Deseas repetirla?",
                ["Editor_ConfirmTitle"] = "Validación",
                ["Editor_ConfirmSaveEmpty"] = "Hay celdas vacías en la tabla. ¿Deseas guardarla de todas formas?",
                ["Editor_ConfirmSaveEmptyTitle"] = "Celdas Vacías"
            },
            ["EN"] = new Dictionary<string, string>
            {
                ["Text_Title"] = "Mexican Lottery - Premium Game",
                ["Config_DoublesMode"] = "Doubles Mode",
                ["Config_WinPattern"] = "Winning Pattern",
                ["Pattern_Linea5"] = "5 in a Line",
                ["Pattern_Full"] = "Full Board",
                ["Pattern_Cruz"] = "Cross (X)",
                ["Pattern_LetraL"] = "Letter L",
                ["Pattern_Esquinas"] = "Four Corners",
                ["Profile_Player"] = "PLAYER PROFILE",
                ["Config"] = "CONFIGURATION",
                ["Btn_Design"] = "🃏 Design",
                ["Btn_Stats"] = "📊 Match History",
                ["Speed_Interval"] = "Interval (Seconds)",
                ["Chips_Type"] = "Chip Type",
                ["Btn_ConnectLan"] = "📡 Connect LAN / Multiplayer",
                ["Btn_DisconnectLan"] = "🔌 Disconnect LAN",
                ["Mode_Solo"] = "Mode: Solitary",
                ["Mode_LanHost"] = "Mode: LAN (Host)",
                ["Mode_LanClient"] = "Mode: LAN (Client)",
                ["Status_Disconnected"] = "Disconnected",
                ["Status_Connecting"] = "Connecting...",
                ["Status_Connected"] = "Connected to LAN Host",
                ["Players_Connected"] = "CONNECTED PLAYERS",
                ["Chat_Network"] = "NETWORK CHAT",
                ["Current_Card"] = "CURRENT CARD",
                ["Remaining_Cards"] = "Deck: {0} cards remaining",
                ["History_Cards"] = "CALLED CARDS HISTORY",
                ["Btn_Start"] = "▶ Start",
                ["Btn_Pause"] = "⏸ Pause",
                ["Btn_Resume"] = "▶ Resume",
                ["Btn_Stop"] = "⏹ Reset",
                ["Btn_Loteria"] = "🎉 ¡LOTERÍA! 🎉",
                ["Boards_Count"] = "Number of Boards",
                ["Msg_ActiveGameMark"] = "The game must be active to mark cards.",
                ["Msg_AccidentalClaimFalse"] = "False Lottery! You do not have a valid completed line of 5 cards.",
                ["Msg_UncalledClaimFalse"] = "False Lottery! You have marked cells in your winning line ({0}) that have not been called yet in {2}.\n\nMissing cards: {1}.",
                ["Msg_SavedSuccess"] = "Board '{0}' saved successfully!",
                ["Msg_DeckExhausted"] = "All cards in the deck have been exhausted.",
                ["Msg_EmptyCells"] = "There are empty cells on the board. Do you want to save it anyway?",
                ["Msg_EnterBoardName"] = "Please enter a name for the board.",
                ["Msg_DesignActiveGame"] = "You cannot edit your board while a game is active.",
                ["Msg_NoDuplicateLimit"] = "The board has {0} duplicate cards. Only a maximum of 2 duplicate cards are allowed on the board.",
                ["Msg_NoTripleLimit"] = "A card cannot be repeated more than twice on the board (triples or higher are not allowed).",
                ["Lobby_DefaultUsername"] = "Player",
                ["Host_StartChat"] = "🏁 The host has started the game!",
                ["Host_PauseChat"] = "⏸ Game paused by the host.",
                ["Host_ResumeChat"] = "▶ Game resumed by the host.",
                ["Host_StopChat"] = "⏹ The game has been stopped.",
                ["Host_ExhaustedChat"] = "🃏 The card deck is empty.",
                ["Win_Message"] = "🏆 ¡LOTERÍA! 🏆\nYou won with the line: {0}",
                ["Win_Message_Multi"] = "🏆 ¡LOTERÍA! 🏆\nYou won on {0} with the line: {1}",
                ["Win_Message_Announce"] = "🏆 ¡LOTERÍA! 🏆\nPlayer '{0}' won the game by completing the line: {1}.",
                ["Lan_Warning_SingleBoard"] = "To play via LAN, it will automatically switch to 1 board.",
                ["Lang_Select"] = "Idioma / Language",
                ["Chat_Send"] = "Send",
                ["Title_Catalog"] = "Card Catalog (54)",
                ["Btn_Save_Editor"] = "Save Board",
                ["Btn_Autofill_Editor"] = "Autofill",
                ["Btn_Clear_Editor"] = "Clear",
                ["Btn_Cancel_Editor"] = "Cancel",
                ["Editor_Title"] = "Board Designer - Mexican Lottery",
                ["Editor_Label_Name"] = "Board Name:",
                ["Editor_Instruction"] = "Click on a cell, then choose a card from the catalog on the right.",
                ["Editor_SelectCell"] = "Cell [{0}, {1}] selected. Choose a card on the right.",
                ["Editor_ConfirmRepeat"] = "Card '{0}' is already assigned to this board. Do you want to repeat it?",
                ["Editor_ConfirmTitle"] = "Validation",
                ["Editor_ConfirmSaveEmpty"] = "There are empty cells on the board. Do you want to save it anyway?",
                ["Editor_ConfirmSaveEmptyTitle"] = "Empty Cells"
            }
        };

        private static readonly Dictionary<int, (string NameEn, string RiddleEn)> CardTranslations = new Dictionary<int, (string, string)>
        {
            [1] = ("The Rooster", "The one that sang to Saint Peter won't sing to him again."),
            [2] = ("The Little Devil", "Behave yourself, buddy, or the red one will take you away."),
            [3] = ("The Lady", "The polished and beautiful lady, don't let her escape with you."),
            [4] = ("The Dandy", "Don Ferruco in the avenue, his cane he dropped in the sand."),
            [5] = ("The Umbrella", "For the sun and for the rain, here comes the umbrella."),
            [6] = ("The Mermaid", "Half body of a woman, half body of a fish."),
            [7] = ("The Ladder", "Climb up step by step, don't fall on your back."),
            [8] = ("The Bottle", "The bottle of sherry, drink one at once."),
            [9] = ("The Barrel", "The barrel of beer, my head already hurts."),
            [10] = ("The Tree", "The tree of the plain, that gives us its fresh shade."),
            [11] = ("The Melon", "The melon with its scent, gives flavor to us all."),
            [12] = ("The Brave Man", "Why do you run like a coward, carrying such a good machete."),
            [13] = ("The Little Cap", "Put the cap on the baby, so he doesn't catch a cold."),
            [14] = ("The Death", "Death, bony and skinny, is going to take us all."),
            [15] = ("The Pear", "The green pear awaits me, which we all like to eat."),
            [16] = ("The Flag", "The Mexican flag, green, white, and red it is."),
            [17] = ("The Mandolin", "The mariachi's mandolin, plays and plays without stopping."),
            [18] = ("The Cello", "The elegant cello, playing soft music."),
            [19] = ("The Heron", "The heron of the lagoon, elegant and timely."),
            [20] = ("The Bird", "The singing bird flies happily with love."),
            [21] = ("The Hand", "The scribe's hand writes with great decorum."),
            [22] = ("The Boot", "The soldier's boot, ready to march."),
            [23] = ("The Moon", "The lantern of lovers."),
            [24] = ("The Parrot", "The parrot talks and talks, but understands nothing."),
            [25] = ("The Drunkard", "Oh, what a stubborn drunk, he can't even speak!"),
            [26] = ("The Little Black Man", "The dancing little black man, with his dandy suit."),
            [27] = ("The Heart", "The heart of the beloved beats for her love."),
            [28] = ("The Watermelon", "The juicy and fresh watermelon cures the summer heat."),
            [29] = ("The Drum", "The drum of the band, rolls and rolls now."),
            [30] = ("The Shrimp", "The sleeping shrimp gets carried away by the current."),
            [31] = ("Las Jaras", "The hunter's arrows point to the heart."),
            [32] = ("The Musician", "The musician plays the trumpet, delighting the orchestra."),
            [33] = ("The Spider", "The spider weaves its web patiently on the wall."),
            [34] = ("The Soldier", "One, two, three, the soldier marches backward."),
            [35] = ("The Star", "The star of the firmament guides the hungry sailor."),
            [36] = ("The Saucepan", "The old copper pot, where the pork rind is made."),
            [37] = ("The World", "The round world spins without getting tired of rolling."),
            [38] = ("The Apache", "The Apache with his arrows defends his territory."),
            [39] = ("The Cactus", "The cactus with its thorns gives us delicious prickly pears."),
            [40] = ("The Scorpion", "The venomous scorpion, take care not to step on it."),
            [41] = ("The Rose", "The fresh and fragrant rose adorns the loving garden."),
            [42] = ("The Skull", "The smiling skull laughs at the people."),
            [43] = ("The Bell", "The church bell tolls at dawn."),
            [44] = ("The Pitcher", "The clay pitcher keeps the water very fresh."),
            [45] = ("The Deer", "The jumping deer runs through the endless forest."),
            [46] = ("The Sun", "The bright and warm sun gives us light and warmth."),
            [47] = ("The Crown", "The king's crown, symbol of power and law."),
            [48] = ("The Canoe", "The canoe floats, navigating in the canal."),
            [49] = ("The Pine Tree", "The tall green pine never loses its leaves."),
            [50] = ("The Fish", "The fish in the clean water swims happily and slowly."),
            [51] = ("The Palm Tree", "The desert palm tree gives shade to the restless traveler."),
            [52] = ("The Flowerpot", "The balcony flowerpot blooms in spring."),
            [53] = ("The Harp", "The harp with its strings plays beautiful melodies."),
            [54] = ("The Frog", "The green and jumping frog sings on the neighboring shore.")
        };

        public static string Get(string key)
        {
            if (UiTranslations.TryGetValue(CurrentLanguage, out var langDict) && langDict.TryGetValue(key, out var val))
            {
                return val;
            }
            return key;
        }

        public static string GetCardName(Card card)
        {
            if (CurrentLanguage == "EN" && CardTranslations.TryGetValue(card.Id, out var trans))
            {
                return trans.NameEn;
            }
            return card.Name;
        }

        public static string GetCardRiddle(Card card)
        {
            if (CurrentLanguage == "EN" && CardTranslations.TryGetValue(card.Id, out var trans))
            {
                return trans.RiddleEn;
            }
            return card.Riddle;
        }
    }
}
