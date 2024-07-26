namespace Installer.Helpers;

internal class AutoDisposeList<T> : List<T>, IDisposable where T : IDisposable
{
    public AutoDisposeList(int i)
        : base(i)
    {
    }

    public void Dispose()
    {
        foreach (var obj in this)
        {
            obj.Dispose();
        }
    }
}
