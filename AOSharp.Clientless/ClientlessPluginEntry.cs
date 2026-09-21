namespace AOSharp.Clientless
{
    public interface IClientlessPluginEntry
    {
        void Init(string pluginDir);
        void Teardown();
    }

    public abstract class ClientlessPluginEntry : IClientlessPluginEntry
    {
        public abstract void Init(string pluginDir);
        public virtual void Teardown()
        {
        }
    }
}
