using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace AOSharp.Clientless
{
    [Serializable]
    public class CharacterSelect
    {
        public int AllowedCharacters;
        public ExpansionFlags Expansions;
        public List<Character> Characters;

        [Serializable]
        public class Character
        {
            public int Id;
            public string Name;

            public void Select()
            {
                Client.CharacterName = Name;
                Client.SelectCharacter(Id);
            }
        }
    }
}
