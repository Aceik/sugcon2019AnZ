namespace SUGCON2019Cortex.Website.Generators
{
    public interface IRandomGenerator
    {
        int GetInteger(int min, int max);
        decimal GetDecimal(int min, int max);
    }
}
