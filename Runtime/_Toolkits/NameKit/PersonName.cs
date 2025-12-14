namespace Twenty2.VomitLib.Tools
{
    // TODO 
    public static class PersonName
    {
        public enum Sex
        {
            Unknown,
            Male,
            Female
        }
        
        public enum Country
        {
            China,
            USA,
            Japan,
        }

        public static string GetName(Country country = Country.USA, string surname = null, Sex sex = Sex.Unknown)
        {
            return "Unknown Name";
        }
    }
}