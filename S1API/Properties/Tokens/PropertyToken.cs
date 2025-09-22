using S1API.Properties.Interfaces;

namespace S1API.Properties.Tokens
{
    /// <summary>
    /// Base token that represents a product property without referencing runtime game types.
    /// </summary>
    public abstract class PropertyToken : PropertyBase
    {
        private readonly string _id;
        private readonly string _unityName;
        private readonly string _displayName;
        private readonly string _description;
        private readonly int _tier;
        private readonly float _addictiveness;

        protected PropertyToken(string id, string unityName = null, string displayName = null, string description = null, int tier = 1, float addictiveness = 0f)
        {
            _id = id ?? string.Empty;
            _unityName = unityName ?? id ?? string.Empty;
            _displayName = displayName ?? id ?? string.Empty;
            _description = description ?? string.Empty;
            _tier = tier;
            _addictiveness = addictiveness;
        }

        public override string ID => _id;
        public override string Name => _displayName;
        public override string name => _unityName;
        public override string Description => _description;
        public override int Tier => _tier;
        public override float Addictiveness => _addictiveness;
    }
}


