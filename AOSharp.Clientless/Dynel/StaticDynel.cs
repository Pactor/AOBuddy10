using AOSharp.Common.GameData;

namespace AOSharp.Clientless
{
    public class StaticDynel : Dynel
    {
        public int TemplateId;

        public DummyItem DummyItem;

        public StaticDynel(int templateId, Identity identity, Vector3 position) : base(identity, position, Quaternion.Identity)
        {
            if (ItemData.Find(templateId, out DummyItem item))
            {
                Name = item.Name;
                DummyItem = item;
            }
            else
            {
                Name = "";
            }

            SetStat(Stat.StaticInstance, templateId);
            SetStat(Stat.Type, (int)identity.Type);
            TemplateId = templateId;
        }
    }
}
