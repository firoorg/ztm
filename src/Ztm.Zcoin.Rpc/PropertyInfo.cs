using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public class PropertyInfo
    {
        public PropertyId Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public PropertyType Type { get; set; }
    }
}
