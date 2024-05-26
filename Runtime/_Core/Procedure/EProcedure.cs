using QFramework;

public abstract class EProcedure
{
    public struct Changed<T>
    {
        public T Prev;

        public T Curr;
    }
}