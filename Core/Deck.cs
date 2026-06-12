using System;
using System.Collections.Generic;
using System.Linq;

namespace LoteriaMexicanaApp.Core
{
    public class Deck
    {
        // Predefined list of the 54 traditional cards in official Clemente Jacques order
        public static readonly List<Card> BaseCards = new List<Card>
        {
            new Card(1, "El Gallo", " Rooster 🐓", "El que le cantó a San Pedro no le volverá a cantar.", "#FFEBEE"),
            new Card(2, "El Diablito", "Devil 😈", "Pórtate bien cuatito, si no te lleva el coloradito.", "#FFEBEE"),
            new Card(3, "La Dama", "Lady 👩", "La dama pulida y bella, no se me escape con ella.", "#E3F2FD"),
            new Card(4, "El Catrín", "Dandy 🎩", "Don Ferruco en la alameda, su bastón tiró en la arena.", "#E3F2FD"),
            new Card(5, "El Paraguas", "Umbrella ☂️", "Para el sol y para el agua, ya viene el paraguas.", "#EDE7F6"),
            new Card(6, "La Sirena", "Mermaid 🧜‍♀️", "Medio cuerpo de mujer, medio cuerpo de pez.", "#E0F2F1"),
            new Card(7, "La Escalera", "Ladder 🪜", "Súbete paso a pasito, no te caigas de espaldas.", "#F1F8E9"),
            new Card(8, "La Botella", "Bottle 🍾", "La botella del jerez, tómate una de una vez.", "#F1F8E9"),
            new Card(9, "El Barril", "Barrel 🛢️", "El barril de la cerveza, ya me duele la cabeza.", "#FFFDE7"),
            new Card(10, "El Árbol", "Tree 🌳", "El árbol de la llanura, que nos da su fresca sombra.", "#E8F5E9"),
            new Card(11, "El Melón", "Melon 🍈", "El melón con su olor, a todos nos da sabor.", "#FFF3E0"),
            new Card(12, "El Valiente", "Brave 🗡️", "Por qué le corres cobarde, trayendo tan buen machete.", "#FFEBEE"),
            new Card(13, "El Gorrito", "Bonnet 👒", "Ponle el gorrito al nene, no se vaya a resfriar.", "#EDE7F6"),
            new Card(14, "La Muerte", "Death 💀", "La muerte calaca y flaca, a todos nos va a llevar.", "#ECEFF1"),
            new Card(15, "La Pera", "Pear 🍐", "Me espera la pera verde, que a todos nos gusta comer.", "#E8F5E9"),
            new Card(16, "La Bandera", "Flag 🇲🇽", "La bandera mexicana, verde, blanca y roja es.", "#E8F5E9"),
            new Card(17, "El Bandolón", "Mandolin 🪕", "El bandolón del mariachi, toca y toca sin parar.", "#FDF5E6"),
            new Card(18, "El Violoncello", "Cello 🎻", "El violoncello elegante, que toca música suave.", "#FDF5E6"),
            new Card(19, "La Garza", "Heron 🦩", "La garza de la laguna, elegante y oportuna.", "#FFF0F5"),
            new Card(20, "El Pájaro", "Bird 🐦", "El pájaro cantador, vuela alegre con amor.", "#E3F2FD"),
            new Card(21, "La Mano", "Hand ✋", "La mano del escribano, escribe con gran decoro.", "#FFF5EE"),
            new Card(22, "La Bota", "Boot 👢", "La bota del militar, lista para marchar.", "#F5F5DC"),
            new Card(23, "La Luna", "Moon 🌙", "El farol de los enamorados.", "#EDE7F6"),
            new Card(24, "El Cotorro", "Parrot 🦜", "Habla y habla el cotorrito, pero no entiende nadita.", "#E8F5E9"),
            new Card(25, "El Borracho", "Drunkard 🥴", "¡Ay que borracho tan necio, ya no puede ni hablar!", "#FFFDE7"),
            new Card(26, "El Negrito", "Negrito 🧑🏿", "El negrito bailarín, con su traje de catrín.", "#EAEAEA"),
            new Card(27, "El Corazón", "Heart ❤️", "El corazón de la amada, palpita por su querer.", "#FFEBEE"),
            new Card(28, "La Sandía", "Watermelon 🍉", "La sandía jugosa y fresca, cura el calor del verano.", "#FFEBEE"),
            new Card(29, "El Tambor", "Drum 🥁", "El tambor de la banda, redobla y redobla ya.", "#FFF9C4"),
            new Card(30, "El Camarón", "Shrimp 🦐", "Camarón que se duerme, se lo lleva la corriente.", "#FFE0B2"),
            new Card(31, "Las Jaras", "Arrows 🏹", "Las jaras del cazador, apuntan al corazón.", "#FFF5EE"),
            new Card(32, "El Músico", "Musician 🎺", "El músico toca la trompeta, deleitando a la orquesta.", "#E8EAF6"),
            new Card(33, "La Araña", "Spider 🕷️", "La araña teje su red, con paciencia en la pared.", "#ECEFF1"),
            new Card(34, "El Soldado", "Soldier 🪖", "Uno, dos, tres, el soldado marcha al revés.", "#EAEAEA"),
            new Card(35, "La Estrella", "Star ⭐", "La estrella del firmamento, guía al navegante hambriento.", "#FFFDE7"),
            new Card(36, "El Cazo", "Saucepan 🍲", "El cazo de cobre viejo, donde se hace el chicharrón.", "#FFE0B2"),
            new Card(37, "El Mundo", "World 🌍", "El mundo redondo gira, sin cansarse de rodar.", "#E0F2F1"),
            new Card(38, "El Apache", "Apache 🏹", "El apache con sus flechas, defiende su territorio.", "#FFF5EE"),
            new Card(39, "El Nopal", "Cactus 🌵", "El nopal con sus espinas, nos da deliciosas tunas.", "#E8F5E9"),
            new Card(40, "El Alacrán", "Scorpion 🦂", "El alacrán venenoso, cuida de no pisarlo.", "#FFFDE7"),
            new Card(41, "La Rosa", "Rose 🌹", "La rosa fresca y fragante, adorna el jardín amante.", "#FFF0F5"),
            new Card(42, "La Calavera", "Skull 💀", "La calavera sonriente, se ríe de la gente.", "#F5F5F5"),
            new Card(43, "La Campana", "Bell 🔔", "La campana de la iglesia, dobla al amanecer.", "#FFFDE7"),
            new Card(44, "El Cantarito", "Pitcher 🏺", "El cantarito de barro, guarda el agua bien fresca.", "#FDF5E6"),
            new Card(45, "El Venado", "Deer 🦌", "El venado saltarín, corre por el bosque sin fin.", "#F5F5DC"),
            new Card(46, "El Sol", "Sun ☀️", "El sol brillante y cálido, nos da luz y calor.", "#FFFDE7"),
            new Card(47, "La Corona", "Crown 👑", "La corona del rey, símbolo de poder y ley.", "#FFF9C4"),
            new Card(48, "La Chalupa", "Canoe 🛶", "La chalupa va flotando, en el canal navegando.", "#E0F2F1"),
            new Card(49, "El Pino", "Pine 🌲", "El pino alto y verde, nunca sus hojas pierde.", "#E8F5E9"),
            new Card(50, "El Pescado", "Fish 🐟", "El pescado en el agua limpia, nada alegre y sin prisa.", "#E0F2F1"),
            new Card(51, "La Palma", "Palm 🌴", "La palma del desierto, da sombra al caminante inquieto.", "#E8F5E9"),
            new Card(52, "La Maceta", "Flowerpot 🪴", "La maceta del balcón, florece en primavera.", "#FFF0F5"),
            new Card(53, "El Arpa", "Harp 🪕", "El arpa con sus cuerdas, toca melodías bellas.", "#FDF5E6"),
            new Card(54, "La Rana", "Frog 🐸", "La rana verde y saltarina, canta en la orilla vecina.", "#E8F5E9")
        };

        private List<Card> _cards = new List<Card>();
        private readonly Random _random = new Random();

        public List<Card> Cards => _cards;

        public Deck()
        {
            _cards = new List<Card>(BaseCards);
        }

        public void GenerateCustomDeck(int duplicateCount)
        {
            _cards.Clear();
            _cards.AddRange(BaseCards);

            if (duplicateCount > 0)
            {
                duplicateCount = Math.Min(duplicateCount, BaseCards.Count);
                var toDuplicate = BaseCards.OrderBy(x => _random.Next()).Take(duplicateCount).ToList();
                foreach (var card in toDuplicate)
                {
                    _cards.Add(new Card(card.Id, card.Name, card.Emoji, card.Riddle, card.BackgroundColorCode));
                }
            }
        }

        public void Shuffle()
        {
            int n = _cards.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                Card value = _cards[k];
                _cards[k] = _cards[n];
                _cards[n] = value;
            }
        }

        public Card? Draw()
        {
            if (_cards.Count == 0) return null;
            Card card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }

        public int Count => _cards.Count;
    }
}
