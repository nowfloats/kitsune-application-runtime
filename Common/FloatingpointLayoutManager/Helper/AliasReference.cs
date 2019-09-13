namespace KitsuneLayoutManager.Helper
{
    public enum AliasType { referenceAlias, absoluteAlias };
    public class AliasReference
    {
        public dynamic referenceObject;
        public int iteration;
        public int maxIteration;
        public AliasType aliasType = AliasType.referenceAlias;
        public int kobjectIndex;

        public AliasReference Clone()
        {
            AliasReference newObj = new AliasReference();
            newObj.referenceObject = this.referenceObject;
            newObj.iteration = this.iteration;
            newObj.maxIteration = this.maxIteration;
            newObj.aliasType = this.aliasType;
            newObj.kobjectIndex = this.kobjectIndex;
            return newObj;
        }
    }
}
