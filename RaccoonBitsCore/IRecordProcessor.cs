namespace RaccoonBitsCore
{
    public interface IRecordProcessor<T>
    {
        T Process(T record);
    }
}