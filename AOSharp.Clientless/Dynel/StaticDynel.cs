using AOSharp.Common.GameData;

namespace AOSharp.Clientless
{
    public class StaticDynel : Dynel
    {
        public int TemplateId;

        public DummyItem DummyItem;

        /// <param name="rotation">The placement's rotation from the statel record (v2 bin); facing identity
        /// in the old v1 bin, which stored none.</param>
        public StaticDynel(int templateId, Identity identity, Vector3 position, Quaternion rotation) : base(identity, position, rotation)
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
