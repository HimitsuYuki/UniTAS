namespace UniTAS.Patcher.Models.Utils;

public class Tuple<T1, T2>
{
    public T1 Item1 { get; }
    public T2 Item2 { get; }

    public Tuple(T1 item1, T2 item2)
    {
        Item1 = item1;
        Item2 = item2;
    }
}

public struct TupleValue<T1, T2>
{
    public T1 Item1 { get; }
    public T2 Item2 { get; }

    public TupleValue(T1 item1, T2 item2)
    {
        Item1 = item1;
        Item2 = item2;
    }
}